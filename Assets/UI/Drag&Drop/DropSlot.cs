using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DropSlot:MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    protected Text text_Id;             // 슬롯 표시

    [SerializeField]
    protected DragAndDrop dragAndDrop;  

    protected int slotId;       // 슬롯 id
    private int slotIndex;

    // 드래그 감지
    bool clickDown;
    float dragdis;
    Vector2 startPos;

    // 슬롯 번호 입력과 초기화
    public void Init(int _index)
    {
        slotIndex = _index;
        InitData();
    }

    void Update()
    {
        if (clickDown && Input.GetMouseButton(0))
        {
            if (Vector2.Distance(startPos, Input.mousePosition) > dragdis)
            {
                clickDown = false;
                dragAndDrop.DragStart(DragState.DropSlotStart, slotIndex, slotId);
            }
        }
    }


    // 슬롯 데이터 초기화 
    public void InitData()
    {
        slotId = -1;
        text_Id.text = "Empty";
    }

    // 슬롯 데이터 입력
    public void SetData(int _data)
    {
        slotId = _data;
        text_Id.text = slotId.ToString();
    }

    public bool DataCheck_Duplication(int _data)
    {
        if(slotId == _data)
        {
            InitData();
            return true;
        }

        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        dragAndDrop.DropSlotEnterExit(true, slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        dragAndDrop.DropSlotEnterExit(false, slotIndex);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 데이터가 없으면 동작 안하도록
        if(slotId != -1)
        {
            startPos = eventData.position;
            clickDown = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        clickDown = false;
    }
}
