using UnityEngine;

public abstract class SlotBase<T>: MonoBehaviour
{
    [SerializeField]
    protected RectTransform _rectTransform;
    protected int slotdata;                       //데이터 정보

    public RectTransform RectTransform => _rectTransform;
    public float Height => _rectTransform.rect.height;
    public float Width => _rectTransform.rect.width;
    public int SlotData => slotdata;

    public abstract void Init();

    public abstract void UpdateSlot(T data);

    public abstract void SlotSelect();

    public abstract void SlotNoSelect();
}