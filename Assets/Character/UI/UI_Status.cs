using UnityEngine;
using TMPro;

public class UI_Status : MonoBehaviour
{
    PlayerStatus status;

    public TextMeshProUGUI uiHP;
    public TextMeshProUGUI uiSP;

    int lasthp = 0;
    int lastsp = 0;

    public void SetStatus(PlayerStatus _status)
    {
        status = _status;

        status.OnStatusChanged += UpdateUI;

        UpdateUI();
    }

    void UpdateUI()
    {
        int hp = Mathf.CeilToInt(status.hpCurrent);
        int sp = Mathf.CeilToInt(status.spCurrent);

        if (lasthp != hp)
        {
            uiHP.text = $"{hp} / {status.hpMax: 0}";
        }

        if (lastsp != sp)
        {
            uiSP.text = $"{sp} / {status.spMax: 0}";
        }
    }
}
