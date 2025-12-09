using UnityEngine;
using System;

public class PlayerStatus
{
    public event Action OnStatusChanged;

    // ---- hp ----
    public float hpMax{ get; private set; }
    public float hpCurrent{ get; private set; }

    // ---- sp ----
    public float spMax{ get; private set; }
    public float spCurrent{ get; private set;}

    // hp 소비 속도, sp 회복 속도
    public float hpConsumeSpeed{ get; private set;}
    public float spRecoverySpeed{ get; private set; }

    public PlayerStatus(float _hpMax, float _spMax, float _hpConsumeSpeed, float _spRecoverySpeed)
    {
        hpMax = _hpMax;
        hpCurrent = hpMax;
        spMax = _spMax;
        spCurrent = spMax;
        hpConsumeSpeed = _hpConsumeSpeed;
        spRecoverySpeed = _spRecoverySpeed;
    }


    // ---- hp ----
    public void HPRecovery(float amount)
    {
        hpCurrent = Mathf.Max(0, hpCurrent + amount);
        OnStatusChanged?.Invoke();
    }

    public void HPConsume()
    {
        if (hpCurrent > 0)
        {
            hpCurrent -= hpConsumeSpeed * Time.deltaTime;
            hpCurrent = Mathf.Min(hpCurrent, hpMax);
            OnStatusChanged?.Invoke();
        }
    }


    // ---- sp ----
    public void SPRecovery()
    {
        if (spCurrent < spMax)
        {            
            spCurrent += spRecoverySpeed * Time.deltaTime;
            spCurrent = Mathf.Min(spCurrent, spMax);
            OnStatusChanged?.Invoke();
        }
    }

    public void SPConsume(float amount)
    {
        spCurrent = Mathf.Max(0, spCurrent - amount);
        OnStatusChanged?.Invoke();
    }
}

public class Player_Skill : MonoBehaviour
{
    [SerializeField]
    private SkillManager skillManager;
    [SerializeField]
    private UI_Status ui_Status;
    public PlayerStatus playerStatus;

    [SerializeField]
    private float hpConsumeSpeed;
    [SerializeField]
    private float spRecoverySpeed;


    private void Start()
    {
        playerStatus = new PlayerStatus(100, 100, hpConsumeSpeed , spRecoverySpeed);
        ui_Status.SetStatus(playerStatus);
    }

    void Update()
    {
        StatusUpdate();
        SkillInput();
    }

    private void StatusUpdate()
    {
        playerStatus.HPConsume();   // hp 자동 소모
        playerStatus.SPRecovery();  // sp 자동 회복
    }

    public void SPConsume(float amount)
    {
        playerStatus.SPConsume(amount);
    }

    public void HPRecorvery(float amount)
    {
        playerStatus.HPRecovery(amount);
    }

    private void SkillInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            skillManager.UseSkill(0, this);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            skillManager.UseSkill(1, this);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            skillManager.UseSkill(2, this);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            skillManager.UseSkill(3, this);
        }
    }
}
