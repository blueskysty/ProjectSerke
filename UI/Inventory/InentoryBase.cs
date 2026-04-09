using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class InentoryBase<T>: MonoBehaviour
{
    [Header("Rect")]
    [SerializeField] protected RectTransform contentRect;
    [SerializeField] protected ScrollRect scrollRect;
    [SerializeField] protected SlotBase<T> itemSlot;

    [Space]
    [Header("Option")]
    [SerializeField] protected int bufferCount = 5; //추가적으로 미리 로드할 슬롯의 개수
    [SerializeField] protected float spacing;       //아이템 간의 간격

    [Space]
    [Header("ScrollView Option")]
    [SerializeField] protected int itemsPerRow = 1;     //한 줄에 보여줄 아이템 수
    [SerializeField] protected float topOffset;         //스크롤 뷰의 위쪽 여백
    [SerializeField] protected float botOffset;         //스크롤 뷰의 아래쪽 여백
    [SerializeField] protected float horizonOffset;     //가로 여백

    protected List<SlotBase<T>> list_Slot = new List<SlotBase<T>>();    // 슬롯 리스트
    protected List<T> list_itemdata = new List<T>();                    // 데이터 리스트
    protected float slotH;                                              // 슬롯의 높이
    protected float slotW;                                              // 슬롯의 너비
    protected int poolSize;                                             // 슬롯 풀의 크기
    protected int tmpfirstVisibleIndex;                                 // 임시 첫 번째로 보이는 슬롯의 인덱스
    protected int contentVisibleSlotCount;                              // 화면에 보이는 슬롯 개수

    //키 입력 관련
    protected bool keyPress = true;     // 키 입력이 가능한 상태인지 여부
    protected float presstime = 0;      // 키 입력 후 경과 시간
    protected int slotMaxCount = 0;     // 슬롯 최대 개수
    protected int selectedIndex = 0;    // 선택된 슬롯의 인덱스

    //키 입력 방향 반환
    protected int ArrowDirection()
    {        
        if (Input.GetKey(KeyCode.A))
        {
            return -1;
        }

        else if (Input.GetKey(KeyCode.D))
        {
            return 1;
        }

        else if (Input.GetKey(KeyCode.S))
        {
            return itemsPerRow;
        }

        else if (Input.GetKey(KeyCode.W))
        {
            return -itemsPerRow;
        }

        return 0;
    }

    //키 입력 체크
    protected int KeyInputCheck()
    {
        //키 입력이 가능한 상태인지 여부
        if (keyPress)
        {  
            //키 입력 방향 반환
            int arr = ArrowDirection();
            if (arr != 0)
            {
                keyPress = false;
                presstime = 0;
                return arr;
            }           
        }

        //키 입력 후 경과 시간 체크
        else
        {            
            presstime += Time.deltaTime;

            if (presstime > 0.15f)
            {
                keyPress = true;
                return 0;
            }
        }

        return 0;
    }

    //슬롯 선택
    public abstract void SlotSelect(T slotdata);

    //초기화
    public virtual void Init(List<T> list_data)
    {
        selectedIndex = 0;
        list_itemdata = list_data;

        // 슬롯 크기
        slotH = itemSlot.Height;
        slotW = itemSlot.Width;

        // 전체 행 수 계산
        int totalRows = Mathf.CeilToInt((float)list_itemdata.Count / itemsPerRow);
        float contentHeight = slotH * totalRows + ( ( totalRows - 1 ) * spacing ) + topOffset + botOffset;

        //Anchor 설정(위쪽 고정)
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.anchorMin = new Vector2(0f, 1f);

        //contentRect 크기 계산
        contentVisibleSlotCount = (int)( scrollRect.GetComponent<RectTransform>().rect.height / slotH ) * itemsPerRow;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

        poolSize = contentVisibleSlotCount + ( bufferCount * 2 * itemsPerRow );
        int index = -bufferCount * itemsPerRow;
        for (int i = 0; i < poolSize; i++)
        {
            SlotBase<T> item = Instantiate(itemSlot, contentRect);
            list_Slot.Add(item);
            item.Init();
            UpdateSlot(item, index++);
        }

        slotMaxCount = list_itemdata.Count - 1;

        SelectCheck();

        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    //인벤토리 로드
    public virtual void InventoryLoad(List<T> list_data)
    {
        list_itemdata = list_data;

        poolSize = contentVisibleSlotCount + ( bufferCount * 2 * itemsPerRow );
        int index = -bufferCount * itemsPerRow;
        for (int i = 0; i < poolSize; i++)
        {
            SlotBase<T> item = list_Slot[i];
            UpdateSlot(item, index++);
        }

        SelectCheck();
    }

    protected void OnScroll(Vector2 scrollPosition)
    {
        float contentY = contentRect.anchoredPosition.y;

        //첫 번째 보이는 행의 인덱스 계산
        int firstVisibleRowIndex = Mathf.Max(0, Mathf.FloorToInt(contentY / ( slotH + spacing )));
        int firstVisibleIndex = firstVisibleRowIndex * itemsPerRow;

        // 이전 첫 번째 보이는 인덱스와 현재 첫 번째 보이는 인덱스가 다르면 슬롯 업데이트
        if (tmpfirstVisibleIndex != firstVisibleIndex)
        {
            int index = ( tmpfirstVisibleIndex - firstVisibleIndex ) / itemsPerRow;

            // 슬롯 업데이트(현재 인덱스가 작음)
            if (index < 0)
            {
                int lastVisibleIndex = tmpfirstVisibleIndex + contentVisibleSlotCount;
                for (int i = 0, cnt = Mathf.Abs(index) * itemsPerRow; i < cnt; i++)
                {
                    SlotBase<T> item = list_Slot[0];
                    list_Slot.RemoveAt(0);
                    list_Slot.Add(item);

                    int newIndex = lastVisibleIndex + ( bufferCount * itemsPerRow ) + i;
                    UpdateSlot(item, newIndex);
                }
            }

            // 슬롯 업데이트(현재 인덱스가 큼)
            else if (index > 0)
            {
                for (int i = 0, cnt = Mathf.Abs(index) * itemsPerRow; i < cnt; i++)
                {
                    SlotBase<T> item = list_Slot[list_Slot.Count - 1];
                    list_Slot.RemoveAt(list_Slot.Count - 1);
                    list_Slot.Insert(0, item);

                    int newIndex = tmpfirstVisibleIndex - ( bufferCount * itemsPerRow ) - i;
                    UpdateSlot(item, newIndex);
                }
            }

            tmpfirstVisibleIndex = firstVisibleIndex;
        }

        SelectCheck();
    }
    
    // 스크롤 y값에 따라 화면에 보이는 슬롯 위치를 조정
    protected void ScrollView_ValueCheck(float _y)
    {
        float content_y = contentRect.sizeDelta.y;                                  // 컨텐츠 전체 높이
        float scrollrect_y = scrollRect.GetComponent<RectTransform>().sizeDelta.y;  // 스크롤뷰 높이
        float scrollrect_y_half = scrollrect_y * 0.5f;                              // 스크롤뷰 절반 높이
        float objectsize_h = ( slotH + spacing ) * 0.5f;                            // 슬롯 + 간격 절반 높이
        float nomoverange = scrollrect_y_half - objectsize_h;                       // 움직이지 않는 범위

        float value = 1 - ( -_y / ( content_y - scrollrect_y ) );                                                                         // y값 계산
        float view_y = -( ( content_y - scrollrect_y - botOffset ) * ( 1 - scrollRect.verticalScrollbar.value ) ) - scrollrect_y_half;    // 화면의 위치 계산

        // 현재 화면보다 슬롯이 위에 있음
        if (_y >= view_y + nomoverange)
        {
            float rivision = ( objectsize_h / ( content_y - scrollrect_y ) );
            scrollRect.verticalNormalizedPosition = value + rivision;
        }

        // 현재 화면보다 슬롯이 아래에 있음
        else if (_y < view_y - nomoverange)
        {
            float rivision = ( ( scrollrect_y - objectsize_h ) / ( content_y - scrollrect_y ) );
            scrollRect.verticalNormalizedPosition = value + rivision;
        }
    }

    protected void UpdateSlot(SlotBase<T> item, int index)
    {
        // 인덱스에 따른 행과 열 계산
        int row = 0 <= index ? index / itemsPerRow : ( index - 1 ) / itemsPerRow;
        int column = Mathf.Abs(index) % itemsPerRow;

        // X와 Y 위치 계산 (슬롯의 피벗을 고려하여 위치 조정)
        Vector2 pivot = item.RectTransform.pivot;
        float totalWidth = ( itemsPerRow * ( slotW + spacing ) ) - spacing;
        float contentWidth = contentRect.rect.width;
        float offsetX = ( contentWidth - totalWidth ) / 2f;
        float adjustedY = -( row * ( slotH + spacing ) ) - slotH * ( 1 - pivot.y );
        float adjustedX = column * ( slotW + spacing ) + slotW * pivot.x;
        adjustedX += offsetX + horizonOffset;
        adjustedY -= topOffset;
        item.RectTransform.localPosition = new Vector3(adjustedX, adjustedY, 0);

        // 인덱스가 데이터 리스트의 범위를 벗어나면 슬롯을 비활성화
        if (index < 0 || index >= list_itemdata.Count)
        {
            item.gameObject.SetActive(false);
            return;
        }
        else
        {
            item.SetDataSlot(list_itemdata[index]);
            item.gameObject.SetActive(true);
        }
    }

    protected void SelectCheck()
    {
        foreach(SlotBase<T> slot in list_Slot)
        {
            // 선택된 슬롯 확인
            if (slot.SlotIndex == selectedIndex && slot.gameObject.activeSelf)
            {
                slot.SlotSelect();
            }

            // 선택되지 않은 슬롯 확인
            else
            {
                slot.SlotNoSelect();
            }
        }
    }
}
