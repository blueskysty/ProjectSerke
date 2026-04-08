using UnityEngine;
using System;

//player 스테이터스 클래스
public class PlayerStatus
{
    public event Action OnStatusChanged;

    // ---- hp ----//
    public float hpMax{ get; private set; }
    public float hpCurrent{ get; private set; }

    // ---- sp ----//
    public float spMax{ get; private set; }
    public float spCurrent{ get; private set;}
         
    public float hpConsumeSpeed{ get; private set;}     //hp 자동 감소 속도
    public float spRecoverySpeed{ get; private set; }   //sp 자동 회복 속도

    //초기화
    public PlayerStatus(float _hpMax, float _spMax, float _hpConsumeSpeed, float _spRecoverySpeed)
    {
        hpMax = _hpMax;
        hpCurrent = hpMax;
        spMax = _spMax;
        spCurrent = spMax;
        hpConsumeSpeed = _hpConsumeSpeed;
        spRecoverySpeed = _spRecoverySpeed;
    }


    //hp 회복
    public void HPRecovery(float amount)
    {
        hpCurrent = Mathf.Max(0, hpCurrent + amount);
        OnStatusChanged?.Invoke();
    }

    //hp 자동 감소
    public void HPAutoConsume()
    {
        if (hpCurrent > 0)
        {
            hpCurrent -= hpConsumeSpeed * Time.deltaTime;
            hpCurrent = Mathf.Min(hpCurrent, hpMax);
            OnStatusChanged?.Invoke();
        }
    }


    //sp 자동 회복
    public void SPAutoRecovery()
    {
        if (spCurrent < spMax)
        {            
            spCurrent += spRecoverySpeed * Time.deltaTime;
            spCurrent = Mathf.Min(spCurrent, spMax);
            OnStatusChanged?.Invoke();
        }
    }

    //sp 소모
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
    private float hpConsumeSpeed;   //hp 자동 소모 속도
    [SerializeField]
    private float spRecoverySpeed;  //sp 자동 회복 속도


    private void Start()
    {
        playerStatus = new PlayerStatus(100, 10, hpConsumeSpeed , spRecoverySpeed);
        ui_Status.SetStatus(playerStatus);
    }

    void Update()
    {
        StatusUpdate();
        SkillInput();
    }

    private void StatusUpdate()
    {
        playerStatus.HPAutoConsume();   // hp 자동 소모
        playerStatus.SPAutoRecovery();  // sp 자동 회복
    }

    public void SPConsume(float amount)
    {
        playerStatus.SPConsume(amount); //sp 소모
    }

    public void HPRecorvery(float amount)
    {
        playerStatus.HPRecovery(amount);    //hp 회복
    }

    // 1~4번 누를시 대응하는 스킬을 사용
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
