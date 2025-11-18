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
    [SerializeField] protected float bottomOffset;      //스크롤 뷰의 아래쪽 여백
    [SerializeField] protected float horizonOffset;     //가로 여백

    protected List<SlotBase<T>> list_Slot = new List<SlotBase<T>>();    // 슬롯 리스트
    protected List<T> list_itemdata = new List<T>();                    // 데이터 리스트
    protected float slotH;                                              // 슬롯의 높이
    protected float slotW;                                              // 슬롯의 너비
    protected int poolSize;                                             // 재사용할 슬롯의 수
    protected int tmpfirstVisibleIndex;                                 // 현재 첫 번째로 보이는 아이템의 인덱스
    protected int contentVisibleSlotCount;                              // 현재 화면에 보이는 슬롯 개수

    //네비게이션
    protected bool keyPress = false;
    protected float presstime = 0;

    protected int slotMaxCount = 0;   // 실제 아이템 개수
    protected int selectedIndex = 0;    // 선택된 아이템 리스트 순서



    //방향키 눌렀을때 이동해야할 인덱스 반환
    protected int ArrowDirection()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            return -1;
        }

        else if (Input.GetKeyDown(KeyCode.D))
        {
            return 1;
        }

        else if (Input.GetKeyDown(KeyCode.S))
        {
            return itemsPerRow;
        }

        else if (Input.GetKeyDown(KeyCode.W))
        {
            return -itemsPerRow;
        }

        return 0;
    }

    public abstract void SlotSelect(T slotdata);

    //초기화
    public virtual void Init(List<T> list_data)
    {
        selectedIndex = 0;
        list_itemdata = list_data;

        // 슬롯 크기
        slotH = itemSlot.Height;
        slotW = itemSlot.Width;

        // 전체 높이 계산
        int totalRows = Mathf.CeilToInt((float)list_itemdata.Count / itemsPerRow);
        float contentHeight = slotH * totalRows + ( ( totalRows - 1 ) * spacing ) + topOffset + bottomOffset;

        //Anchor값 고정(계산 오류 방지)
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.anchorMin = new Vector2(0f, 1f);

        //contentRect의 높이 계산
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

    //다시 불러올때
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

        //현재 인덱스 위치 계산 
        int firstVisibleRowIndex = Mathf.Max(0, Mathf.FloorToInt(contentY / ( slotH + spacing )));
        int firstVisibleIndex = firstVisibleRowIndex * itemsPerRow;

        // 만약 이전 위치와 현재 위치가 달라졌다면 슬롯 재배치
        if (tmpfirstVisibleIndex != firstVisibleIndex)
        {
            int index = ( tmpfirstVisibleIndex - firstVisibleIndex ) / itemsPerRow;

            // 현재 인덱스가 큼
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

            // 현재 인덱스가 작음
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

    // 슬롯의 y값을 이용해 화면에 슬롯이 완전히 표시되지 않을때 슬롯 위치로 이동
    protected void ScrollView_ValueCheck(float _y)
    {
        float content_y = contentRect.sizeDelta.y;                                  // 인벤토리 전체사이즈
        float scrollrect_y = scrollRect.GetComponent<RectTransform>().sizeDelta.y;  // 인벤토리 사이즈
        float scrollrect_y_half = scrollrect_y * 0.5f;                              // 인벤토리 절반 사이즈
        float objectsize_h = ( slotH + spacing ) * 0.5f;                            // 슬롯 + 슬롯 간격 절반 높이
        float nomoverange = scrollrect_y_half - objectsize_h;                       // 안움직여도 되는 범위

        float value = 1 - ( -_y / ( content_y - scrollrect_y ) );                                                                         // y의 스크롤바 값
        float view_y = -( ( content_y - scrollrect_y - bottomOffset ) * ( 1 - scrollRect.verticalScrollbar.value ) ) - scrollrect_y_half; // 현재 화면의 중심 좌표

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
        //현재 Index의 행과 열을 계산
        int row = 0 <= index ? index / itemsPerRow : ( index - 1 ) / itemsPerRow;
        int column = Mathf.Abs(index) % itemsPerRow;

        // X축 및 Y축 위치 계산 (가로를 기준으로 중앙 정렬 및 피벗 보정)
        Vector2 pivot = item.RectTransform.pivot;
        float totalWidth = ( itemsPerRow * ( slotW + spacing ) ) - spacing;
        float contentWidth = contentRect.rect.width;
        float offsetX = ( contentWidth - totalWidth ) / 2f;
        float adjustedY = -( row * ( slotH + spacing ) ) - slotH * ( 1 - pivot.y );
        float adjustedX = column * ( slotW + spacing ) + slotW * pivot.x;
        adjustedX += offsetX + horizonOffset;
        adjustedY -= topOffset;
        item.RectTransform.localPosition = new Vector3(adjustedX, adjustedY, 0);

        //Index가 입력된 DataList의 크기를 넘어가거나 0미만이면 슬롯을 끄고 Update를 진행하지 않는다.
        if (index < 0 || index >= list_itemdata.Count)
        {
            item.gameObject.SetActive(false);
            return;
        }
        else
        {
            item.UpdateSlot(list_itemdata[index]);
            item.gameObject.SetActive(true);
        }
    }

    protected void SelectCheck()
    {
        foreach(SlotBase<T> slot in list_Slot)
        {
            if(slot.gameObject.activeInHierarchy && slot.SlotData == selectedIndex)
            {
                slot.SlotSelect();
            }

            else
            {
                slot.SlotNoSelect();
            }
        }
    }
}
