using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Presentation group에서 동작하며, Player 오브젝트와 Animator 연결 역할 수행
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PlayerObjectCreateSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // PrefabManager의 초기화 및 로딩 완료 여부 확인 후 처리
        if (PrefabManager.Instance == null || !PrefabManager.Instance.IsLoaded) 
            return;

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // PlayerAnimatorReference가 없는 ECSPlayerData 엔티티를 대상으로 실행
        foreach (var (playergameobj, entity) in SystemAPI.Query<ECSPlayerData>().WithNone<PlayerAnimatorReference>().WithEntityAccess())
        {
            // Player 프리팹 가져오기
            GameObject prefab = PrefabManager.Instance.GetPrefab("Player");

            if (prefab != null)
            {
                // Player 오브젝트 인스턴스 생성
                GameObject spawnedObj = Object.Instantiate(prefab);

                // 1. PlayerGameObjectPrefab 컴포넌트 추가 (씬 오브젝트 연동)
                ecb.AddComponent(entity, new PlayerGameObjectPrefab
                {
                    Object = spawnedObj
                });

                // 2. PlayerAnimatorReference 컴포넌트 추가 (Animator 연결)
                ecb.AddComponent(entity, new PlayerAnimatorReference
                {
                    Animator = spawnedObj.GetComponent<Animator>()
                });
            }
        }

        // 커맨드 버퍼 반영 및 해제
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

/// <summary>
/// InitializationSystemGroup에서 동작하며, WASD 입력을 받아 MoveInput에 갱신
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerInputSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float3 move = float3.zero;

        // 키 입력에 따라 방향 벡터 업데이트
        if (Input.GetKey(KeyCode.W))
            move.z += 1;
        if (Input.GetKey(KeyCode.S))
            move.z -= 1;
        if (Input.GetKey(KeyCode.A))
            move.x -= 1;
        if (Input.GetKey(KeyCode.D))
            move.x += 1;

        // 모든 MoveInput 컴포넌트에 move 값 적용
        foreach (var input in SystemAPI.Query<RefRW<MoveInput>>())
        {
            input.ValueRW.Direction = move;
        }
    }
}

/// <summary>
/// PlayerInputSystem 수행 후, TransformSystemGroup 이전에 이동 로직 처리
/// </summary>
[UpdateAfter(typeof(PlayerInputSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerMovementSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 입력 값에 따라 위치 이동 및 상태(State) 변경
        foreach (var (playerData, transform, input) in SystemAPI.Query<RefRW<ECSPlayerData>, RefRW<LocalTransform>, RefRO<MoveInput>>())
        {
            float3 move = input.ValueRO.Direction;

            if (math.lengthsq(move) > 0)
            {
                // 방향 벡터를 정규화하여 이동
                transform.ValueRW.Position += math.normalize(move) * playerData.ValueRO.Speed * deltaTime;
                playerData.ValueRW.State = PlayerState.Moving;
            }
            else
            {
                // 이동 입력 없으면 Idle 상태
                playerData.ValueRW.State = PlayerState.Idle;
            }
        }
    }
}

/// <summary>
/// TransformSystemGroup 내에서 Player와 관련된 오브젝트 Transform을 동기화
/// </summary>
[UpdateInGroup(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial struct PlayerTransformSyncSystem: ISystem
{
    private EntityQuery playerQuery;

    public void OnCreate(ref SystemState state)
    {
        // PlayerGameObjectPrefab 컴포넌트가 있는 엔티티 쿼리 생성
        playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerGameObjectPrefab>());

        // 쿼리에 결과가 없으면 OnUpdate 자체를 스킵
        state.RequireForUpdate(playerQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        // 단일 플레이어 엔티티 추출
        Entity playerEntity = playerQuery.GetSingletonEntity();

        // 씬 오브젝트 참조 가져오기
        var gameObjectPrefab = state.EntityManager.GetComponentObject<PlayerGameObjectPrefab>(playerEntity);

        if (gameObjectPrefab != null && gameObjectPrefab.Object != null)
        {        
            // 오브젝트의 Transform을 ECS LocalTransform과 동기화
            Transform targetTransform = gameObjectPrefab.Object.transform;            
            RefRW<LocalTransform> playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);

            targetTransform.position = playerTransform.ValueRW.Position;
            targetTransform.rotation = playerTransform.ValueRW.Rotation;
        }
    }
}

/// <summary>
/// 플레이어 상태(State)를 ECS에서 Animator로 동기화
/// </summary>
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial struct PlayerAnimationSyncSystem: ISystem
{
    private static readonly int StateHash = Animator.StringToHash("State");

    public void OnUpdate(ref SystemState state)
    {
        // PlayerData와 Animator를 가진 모든 엔티티에서 처리
        foreach (var (playerData, animRef) in SystemAPI.Query<RefRO<ECSPlayerData>, PlayerAnimatorReference>())
        {
            if (animRef.Animator == null)
                continue;

            int currentState = animRef.Animator.GetInteger(StateHash);

            // 상태가 다를 때만 변경
            if (currentState != (int)playerData.ValueRO.State)
            {
                animRef.Animator.SetInteger(StateHash, (int)playerData.ValueRO.State);
            }

            // 반드시 최신 값 할당 (중복 가능, 안전성 확보)
            animRef.Animator.SetInteger(StateHash, (int)playerData.ValueRO.State);
        }
    }
}

/// <summary>
/// 일정 간격으로 NavMesh 위에 플레이어가 존재하는지 확인하고,
/// 이탈 시 최근 유효 위치로 복구하는 시스템
/// </summary>
public partial struct NavMeshTrackerSystem : ISystem
{
    private const float checkInterval = 0.5f;     // NavMesh 검사 주기 (초)
    private const float sampleRadius = 1.0f;      // NavMesh 샘플 반경 (단위)
    private const float outLimitTime = 5.0f;      // 복귀 트리거 누적 시간 (초)

    public void OnCreate(ref SystemState state)
    {
        // NavMeshTracker 컴포넌트를 가진 엔티티가 있을 때만 동작
        state.RequireForUpdate<NavMeshTracker>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // NavMeshTracker와 LocalTransform을 가진 엔티티 순회
        foreach (var (transform, tracker) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<NavMeshTracker>>())
        {
            ref var trackerVal = ref tracker.ValueRW;
            ref var transformVal = ref transform.ValueRW;

            // 1. 검사 쿨타임 타이머 누적
            trackerVal.CheckTimer += deltaTime;

            // 설정된 검사 주기에만 NavMesh 상태 체크
            if (trackerVal.CheckTimer >= checkInterval)
            {   
                trackerVal.CheckTimer -= checkInterval;

                float3 currentPos = transformVal.Position;

                // 2. 현재 위치가 NavMesh 상에 있는지 검사 (NavMesh.AllAreas = -1)
                if (NavMesh.SamplePosition(currentPos, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                {
                    // 정상 위치: 유효 위치 갱신, 이탈 시간 초기화
                    trackerVal.LastValidPosition = new float3(hit.position.x, 0, hit.position.z);
                    trackerVal.HasValidPosition = true;
                    trackerVal.TimeOutOfNavmesh = 0f;
                }
                else
                {
                    // 이탈 감지: 이탈 시간 누적
                    trackerVal.TimeOutOfNavmesh += checkInterval;

                    // 3. 이탈 시간 누적이 임계치 초과 시 복구
                    if (trackerVal.TimeOutOfNavmesh >= outLimitTime)
                    {
                        if (trackerVal.HasValidPosition)
                        {
                            // 최근 유효 위치로 플레이어 이동(텔레포트)
                            transformVal.Position = trackerVal.LastValidPosition;
                            trackerVal.TimeOutOfNavmesh = 0f;
                        }
                    }
                }
            }
        }
    }
}