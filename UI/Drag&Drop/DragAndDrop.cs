using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DragState
{
    None,               // 드래그 중이 아님
    InventoryStart,     // 인벤토리에서 드래그 시작
    DropSlotStart       // 드롭 슬롯에서 드래그 시작
}


public class DragAndDrop : MonoBehaviour
{
    [SerializeField]
    private List<DropSlot> list_dropSlot;     // 드롭 슬롯 리스트
    [SerializeField]
    private Inventory inventory;              // 인벤토리
    [SerializeField]
    private RectTransform dragIcon;           // 드래그 중인 슬롯 아이콘

    private DragState dragState;              // 드래그 상태
    private int dropSlotNumber_dragStart;     // 드래그 시작 슬롯 번호
    private int dropSlotNumber_dragEnd;       // 드래그 종료 슬롯 번호
    private int inventorySlotData;            // 드래그 중인 슬롯 데이터(예: int)

    Vector2 screenSize;                       // canvas 크기에 따른 화면 비율

    private void Awake()
    {
        ScreenSizeCheck();

        int count = 0;
        foreach(DropSlot slot in list_dropSlot)
        {
            slot.Init(count);
            count++;
        }
    }

    void Update()
    {
        // 드래그 중일 때
        if (dragState != DragState.None)
        {
            // 드래그 중인 아이콘 위치 갱신
            dragIcon.anchoredPosition3D = new Vector3(Input.mousePosition.x * screenSize.x, Input.mousePosition.y * screenSize.y, 0);
            
            // 드래그 종료
            if (Input.GetMouseButtonUp(0))
            {
                DragEnd();
            }
        }            
    }

    // 드래그 시작
    public void DragStart(DragState _draggingState, int _startnumber, int _slotdata)
    {
        dragState = _draggingState;
        dropSlotNumber_dragStart = _startnumber;
        dropSlotNumber_dragEnd = -1;
        inventorySlotData = _slotdata;

        dragIcon.gameObject.SetActive(true);
        dragIcon.GetComponent<DragIcon>().SetData(inventorySlotData);
        dragIcon.anchoredPosition3D = new Vector3(Input.mousePosition.x * screenSize.x, Input.mousePosition.y * screenSize.y, 0);
    }

    // 드래그 종료
    public void DragEnd()
    {
        //인벤토리에서 시작했을때
        if (dragState == DragState.InventoryStart)
        {
            if (dropSlotNumber_dragEnd >= 0)
            {
                // 슬롯에 데이터가 있을 경우 비우기
                foreach (DropSlot slot in list_dropSlot)
                {
                    if (slot.DataCheck_Duplication(inventorySlotData))
                    {
                        break;
                    }
                }
                list_dropSlot[dropSlotNumber_dragEnd].SetData(inventorySlotData);
            }            
        }

        // 드롭 슬롯에서 시작했을 때
        else if (dragState == DragState.DropSlotStart)
        {
            list_dropSlot[dropSlotNumber_dragStart].InitData();

            // 드롭 슬롯에 드롭 했을때만 정보 이동
            if (dropSlotNumber_dragEnd >= 0)
            {
                list_dropSlot[dropSlotNumber_dragEnd].SetData(inventorySlotData);
            }
        }

        // 드래그 상태 초기화
        dragState = DragState.None;
        dropSlotNumber_dragStart = -1;
        dropSlotNumber_dragEnd = -1;
        inventorySlotData = -1;

        dragIcon.gameObject.SetActive(false);

        inventory.DragEnd();
    }

    public void DropSlotEnterExit(bool _enter, int _endnumber)
    {
        // 드롭 슬롯에 마우스가 들어오거나 나갈 때 호출 (드래그 중일 때만 처리)
        if (dragState != DragState.None)
        {
            if (_enter)
            {
                dropSlotNumber_dragEnd = _endnumber;
            }

            else
            {
                dropSlotNumber_dragEnd = -1;
            }
        }
    }

    void ScreenSizeCheck()
    {
        Vector2 canvas = GameObject.Find("Canvas").GetComponent<RectTransform>().sizeDelta;
        screenSize = new Vector2(canvas.x / Screen.width, canvas.y / Screen.height);
    }
}
