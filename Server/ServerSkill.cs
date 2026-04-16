using UnityEngine;
using System.Collections;
using Unity.Netcode;

// 네트워크 상의 스킬 투사체 로직을 관리하는 클래스입니다.
// 서버 권한으로 이동 및 충돌 판정을 수행하며, 클라이언트에 연출을 지시합니다.
public class ServerSkill : NetworkBehaviour
{
    private NetworkSkillPool networkSkillPool;

    [SerializeField] private GameObject skillObject;        // 스킬 본체 모델링
    [SerializeField] private ServerSkillEffect skillEffect; // 충돌 시 폭발 이펙트

    [SerializeField] private float skillSpeed;
    private PlayerMovement attackPlayer;        // 이 스킬을 쏜 플레이어 (자폭 방지 및 스탯 참조용)

    private Rigidbody rb;
    private bool canforward;    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // GetInstanceID는 디버깅 시 풀링된 오브젝트들이 각각 고유한지 확인하기 유용합니다.
        Debug.Log($"[Start] {gameObject.name} (ID: {GetInstanceID()})");
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // 물리 이동은 서버에서만 계산하여 위치를 동기화합니다.
        if (IsServer && canforward)
        {
            rb.linearVelocity = rb.transform.forward * skillSpeed;
        }
    }

    public void Setup_Network(NetworkSkillPool network)
    {
        networkSkillPool = network;
    }

    public void Setup_Player(PlayerMovement attacker)
    {
        attackPlayer = attacker;
    }

    [ClientRpc] // 모든 클라이언트에서 스킬 활성화 및 이동 시작을 알립니다.
    public void FireClientRpc()
    {
        canforward = true;
        skillObject.SetActive(true);
        skillEffect.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 데미지 판정은 서버에서 수행
        if (!IsServer)
        {
            return;
        }

        bool isPlayer = other.CompareTag("Player");
        bool isController = other.CompareTag("GameController");

        if (isPlayer || isController)
        {
            if (isPlayer)
            {
                // 본인(공격자)에게는 데미지를 입히지 않음
                if (other.gameObject == attackPlayer)
                {
                    return;
                }

                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    if(attackPlayer != null)
                    {
                        // 공격자의 현재 네트워크 변수(PlayerData)에서 공격력을 가져옴
                        float finalAtk = attackPlayer.PlayerStats.Value.ATK;

                        damageable.OnDamage(finalAtk);
                        Debug.Log($"[Damage] {attackPlayer.NetworkObjectId} {other.GetComponent<NetworkBehaviour>().NetworkObjectId} {finalAtk}");
                    }
                }
            }

            // 충돌 시 이동 중지 및 연출 실행
            StopProjectile();
            ApplyHitEffectClientRpc();
            StartCoroutine(EndTimer());
        }
    }

    private void StopProjectile()
    {
        canforward = false;
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [ClientRpc] // 충돌 연출(폭발 등)을 모든 클라이언트에게 재생하도록 명령합니다.
    void ApplyHitEffectClientRpc()
    {
        skillObject.SetActive(false);
        skillEffect.gameObject.SetActive(true);

        // 클라이언트 사이드에서도 즉시 정지
        if (!IsServer)
        {
            StopProjectile();
        }
    }

    // 연출 재생 후 풀로 반환하기 위한 타이머
    IEnumerator EndTimer()
    {
        yield return new WaitForSeconds(1f);

        if (IsServer)
        {
            ForceDisableClientRpc();

            var networkObj = GetComponent<NetworkObject>();
            if ( networkObj != null && networkObj.IsSpawned)
            {
                // [중요] 풀링 시스템으로 반환하여 재사용 준비
                networkSkillPool.ReturnToPool(networkObj);
                networkObj.Despawn(false); // false: 오브젝트를 Destroy하지 않고 연결만 끊음
            }
        }
    }

    [ClientRpc]
    void ForceDisableClientRpc()
    {
        gameObject.SetActive(false);
    }
}
