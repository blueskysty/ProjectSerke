using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot1 : SlotBase<int>, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Text text_Id;          // 슬롯에 표시할 텍스트
    [SerializeField] private Inventory inventory;   // 속해있는 인벤토리

    bool clickDown;    // 클릭 상태
    float dragdis;
    Vector2 startPos;

    // 초기화 (슬롯 초기화 시 호출)
    public override void Init()
    {
        clickDown = false;
        dragdis = 3;
    }

    void Update()
    {
        if (clickDown && Input.GetMouseButton(0))
        {
            if (Vector2.Distance(startPos, Input.mousePosition) > dragdis)
            {
                clickDown = false;
                inventory.DragStart(SlotIndex);
            }
        }
    }

    // 슬롯 데이터 설정
    public override void SetDataSlot(int _index)
    {
        text_Id.text = _index.ToString();   // 슬롯에 텍스트 표시
        SlotIndex = _index;
    }

    public override void SlotSelect()
    {        
        text_Id.color = Color.red;  // 슬롯이 선택되었을 때 붉은색으로
    }

    public override void SlotNoSelect()
    {        
        text_Id.color = Color.black;  // 슬롯이 선택되지 않았을 때 검은색으로
    }

    // 버튼 클릭 시 호출
    public void ButtonClick()
    {
        inventory.SlotSelect(SlotIndex);
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
        clickDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        clickDown = false;
    }
}
