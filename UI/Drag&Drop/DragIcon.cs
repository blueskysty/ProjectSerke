using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragIcon : MonoBehaviour
{
    [SerializeField]
    private Text textID;

    public void SetData(int _data)
    {
        textID.text = _data.ToString();
    }
}
