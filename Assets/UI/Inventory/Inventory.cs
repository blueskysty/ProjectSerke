using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : InentoryBase<int>
{
    [Space]
    [Header("Inventory")]
    [SerializeField] private int slotCount; // 생성할 슬롯 수

    void Start()
    {
        List<int> dataList = new List<int>();

        // 슬롯 수에 맞춰 데이터 리스트를 초기화
        for (int i = 0; i < slotCount; i++)
        {
            dataList.Add(i); // 0부터 _slotCount까지의 숫자를 추가
        }

        // 인벤토리 초기화
        Init(dataList);
    }

    private void Update()
    {
        KeyInput();
    }

    public override void SlotSelect(int slotdata)
    {
        if (list_Slot.Count == 0)
        {
            return;
        }

        //선택된 인덱스 입력
        selectedIndex = slotdata;

        // 스크롤 위치 조정
        float targetPos = ( slotdata / itemsPerRow * ( slotH + spacing ) ) + topOffset + ( slotH / 2 );
        ScrollView_ValueCheck(-targetPos);

        //선택된 슬롯 표시
        SelectCheck();
    }
}
