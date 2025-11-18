using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot1 : SlotBase<int>
{
    [SerializeField] private Text text; // 슬롯에 표시할 텍스트 UI
    [SerializeField] private Inventory inventory;

    // 슬롯 초기화 (필요 시 추가 설정 가능)
    public override void Init()
    {
    }

    // 슬롯 업데이트 메서드 (데이터 변경 시 호출)
    public override void UpdateSlot(int data)
    {
        // 슬롯에 정수형 데이터를 텍스트로 변환하여 표시
        text.text = data.ToString();
        SlotId = data;
    }

    public override void SlotSelect()
    {
        text.color = Color.red;
    }

    public override void SlotNoSelect()
    {
        text.color = Color.black;
    }

    public void ButtonClick()
    {
        inventory.SlotSelect(SlotData);
    }
}
