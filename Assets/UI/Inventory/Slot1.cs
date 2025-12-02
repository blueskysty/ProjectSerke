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

    // 드래그 감지
    bool clickDown;
    float dragdis;
    Vector2 startPos;

    // 슬롯 초기화 (필요 시 추가 설정 가능)
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

    // 슬롯 정보 입력
    public override void SetDataSlot(int _index)
    {
        //아이디 저장 및 표시
        text_Id.text = _index.ToString();
        SlotIndex = _index;
    }

    public override void SlotSelect()
    {
        //선택되면 붉은색으로
        text_Id.color = Color.red;
    }

    public override void SlotNoSelect()
    {
        //선택되지 않으면 검은색으로
        text_Id.color = Color.black;
    }

    //버튼 클릭했을때 인벤토리에 신고
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
