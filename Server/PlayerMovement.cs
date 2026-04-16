using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// [PlayerData] 네트워크 동기화를 위한 플레이어 스탯 구조체
public struct PlayerData: INetworkSerializable
{
    public float Hp;
    public float MaxHp;
    public float ATK;
    public int Level;

    // NGO 이 데이터를 어떻게 읽고 써야 하는지 알려주는 함수
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Hp);
        serializer.SerializeValue(ref MaxHp);
        serializer.SerializeValue(ref ATK);
        serializer.SerializeValue(ref Level);
    }
}

public class PlayerMovement : NetworkBehaviour, IDamageable
{
    [SerializeField]    private float moveSpeed = 8;
    [SerializeField]    private float rotationSpeed = 500;
    [SerializeField]    private float positionRange = 5;

    private Animator animator;

    [SerializeField] private NetworkSkillPool skillPool;    // 앞서 만든 스킬 풀 참조

    // [NetworkVariable] 서버만 수정 가능하고 모두가 읽을 수 있는 스탯 변수
    public NetworkVariable<PlayerData> PlayerStats = new NetworkVariable<PlayerData>(
        new PlayerData { Hp = 100f, MaxHp = 100f, ATK = 10, Level = 1 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // 서버만 수정 가능 (보안)
    );

    // [FixedString32Bytes] 가변적 string 대신 네트워크 최적화된 고정 문자열 사용
    public NetworkVariable<FixedString32Bytes> PlayerId = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Start()
    {
        skillPool = FindFirstObjectByType<NetworkSkillPool>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        // 1. 스폰 시 서버 권한으로 위치 및 ID 초기화
        float x = Random.Range(positionRange, -positionRange);
        float z = Random.Range(positionRange, -positionRange);

        transform.position = new Vector3(x, 0, z);

        Vector3 dir = Camera.main.transform.position- transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        // 플레이어 id 부여
        if (IsServer)
        {
            // 1. 서버에서 아이디 부여 (예: OwnerClientId 기반 또는 랜덤 ID)
            string assignedId = $"Player_{OwnerClientId}";
            PlayerId.Value = assignedId;

            Debug.Log($"[Server] 부여된 아이디: {assignedId}");
        }

        // 2. 아이디가 변경될 때 실행될 콜백 등록 (UI 업데이트 등에 활용)
        PlayerId.OnValueChanged += OnPlayerIdChanged;

        // 스폰 시점에 이미 값이 있을 수 있으므로 초기 업데이트 호출
        UpdatePlayerNameTag(PlayerId.Value.ToString());

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        Move();

        // 스페이스바 입력 시 서버에 스킬 발사 요청
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestFireServerRpc();
        }
    }

    private void Move()
    {
        float h_input = Input.GetAxis("Horizontal");
        float v_input = Input.GetAxis("Vertical");

        Vector3 moveDir = new Vector3(h_input, 0, v_input);
        moveDir.Normalize();

        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        if (moveDir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }

        animator.SetFloat("run", moveDir.magnitude);
    }

    // 데미지 처리
    public void OnDamage(float damage)
    {
        if (!IsServer)
        {
            return;
        }

        // 중요: 구조체는 내부 값만 바꿀 수 없고, 전체를 새로 갈아끼워야 동기화가 감지됩니다.
        var currentData = PlayerStats.Value;
        currentData.Hp -= damage;
        PlayerStats.Value = currentData; // 덮어쓰기 (이때 패킷이 전송됨)
    }


    [ServerRpc] // 클라이언트가 서버에 실행을 요청하는 함수
    void RequestFireServerRpc()
    {
        // 스킬 풀에서 인스턴스 가져오기 (Instantiate -> Pool에서 Pop)
        NetworkObject netObj = skillPool.Instantiate(OwnerClientId, transform.position + transform.rotation * new Vector3(0, 1, 2), transform.rotation);
        netObj.GetComponent<ServerSkill>().Setup_Player(this);

        // 네트워크 스폰 (이미 풀링된 객체이므로 중복 스폰 방지 체크)
        if (!netObj.IsSpawned)
        {
            netObj.Spawn();
        }

        // 모든 클라이언트에게 스킬 발사 이펙트 등 연출 요청
        netObj.GetComponent<ServerSkill>().FireClientRpc();
    }

    private void OnPlayerIdChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdatePlayerNameTag(newValue.ToString());
    }

    private void UpdatePlayerNameTag(string id)
    {
        // 실제 구현 시 상단 텍스트 UI 컴포넌트 업데이트 로직 위치
        if (string.IsNullOrEmpty(id))
        {
            return;
        }
        Debug.Log($"[Client] 내 화면에 표시될 플레이어 아이디: {id}");
    }

}
