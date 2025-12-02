using UnityEngine;

public abstract class SlotBase<T>: MonoBehaviour
{
    [SerializeField]
    protected RectTransform _rectTransform;
    protected int slotIndex;                       //슬롯 인덱스

    public int SlotIndex
    {
        get
        {
            return slotIndex;
        }
        set
        {
            slotIndex = value;
        }
    }

    public RectTransform RectTransform => _rectTransform;
    public float Height => _rectTransform.rect.height;
    public float Width => _rectTransform.rect.width;

    public abstract void Init();                //초기화

    public abstract void SetDataSlot(T data);   //정보 입력 받고 이미지나 텍스트로 표기

    public abstract void SlotSelect();          //슬롯이 선택되었을 때 

    public abstract void SlotNoSelect();        //슬롯이 선택되지 않았을 때
}