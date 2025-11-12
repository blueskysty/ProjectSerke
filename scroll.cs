using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class scroll: RecyclableVerticalScrollView<int>
{
    [SerializeField] private int _slotCount; // 생성할 슬롯 수

    void Start()
    {
        List<int> dataList = new List<int>();

        // 슬롯 수에 맞춰 데이터 리스트를 초기화
        for (int i = 0; i < _slotCount; i++)
        {
            dataList.Add(i); // 0부터 _slotCount까지의 숫자를 추가
        }

        // 스크롤 뷰 초기화
        Init(dataList);
    }
}
