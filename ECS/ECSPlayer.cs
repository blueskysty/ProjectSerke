using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public enum PlayerState
{
    Idle,
    Moving,
    Attacking
}

public struct ECSPlayerData : IComponentData
{
    public float Speed;
    public PlayerState State;

    public static ECSPlayerData Random(uint seed)
    {
        return new ECSPlayerData
        {
            Speed = seed,
            State= PlayerState.Idle
        };
    }
}

public struct MoveInput : IComponentData
{
    public float3 Direction; // WASD 입력을 여기 저장
    public bool IsJumpPressed;
}

// 관리형 컴포넌트
public class  PlayerGameObjectPrefab : IComponentData
{
    public GameObject Object;
}

// 이 컴포넌트 덕분에 ECS 시스템 안에서 Animator를 직접 만질 수 있게 됩니다.
public class PlayerAnimatorReference: IComponentData
{
    public Animator Animator;
}

// 오서링 (MonoBehaviour)
public class ECSPlayer: MonoBehaviour
{
    public GameObject Object;
    public float speed = 10f;
    private MoveInput moveInput;

    public class PlayerGameObjectPrefabBaker : Baker<ECSPlayer>
    {
        public override void Bake(ECSPlayer authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ECSPlayerData
            {
                Speed = authoring.speed
            });

            AddComponent(entity, new MoveInput
            {
                Direction = float3.zero,
                IsJumpPressed = false
            });

            AddComponentObject(entity, new PlayerGameObjectPrefab
            {
                Object = authoring.Object
            });
        }
    }
}



[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PlayerAnimationSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 애니메이션 동기화 로직
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (playergameobj, entity) in SystemAPI.Query<PlayerGameObjectPrefab>().WithNone<PlayerAnimatorReference>().WithEntityAccess())
        {
            var newcompaniongameobj = Object.Instantiate(playergameobj.Object, playergameobj.Object.transform.position, playergameobj.Object.transform.rotation);
            var newAnimator = new PlayerAnimatorReference
            {
                Animator = newcompaniongameobj.GetComponent<Animator>()
            };
            ecb.AddComponent(entity, newAnimator);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct PlayerInputSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 입력 방향 계산
        float3 move = float3.zero;
        if (Input.GetKey(KeyCode.W))
            move.z += 1;
        if (Input.GetKey(KeyCode.S))
            move.z -= 1;
        if (Input.GetKey(KeyCode.A))
            move.x -= 1;
        if (Input.GetKey(KeyCode.D))
            move.x += 1;

        // 모든 플레이어 엔티티의 MoveInput 업데이트
        foreach (var input in SystemAPI.Query<RefRW<MoveInput>>())
        {
            input.ValueRW.Direction = move;
        }
    }
}

[UpdateAfter(typeof(PlayerInputSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerMovementSystem: ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (playerData, transform, input) in SystemAPI.Query<RefRW<ECSPlayerData>, RefRW<LocalTransform>, RefRO<MoveInput>>())
        {
            float3 move = input.ValueRO.Direction;

            if (math.lengthsq(move) > 0)
            {
                transform.ValueRW.Position += math.normalize(move) * playerData.ValueRO.Speed * deltaTime;
                playerData.ValueRW.State = PlayerState.Moving;
            }
            else
            {
                playerData.ValueRW.State = PlayerState.Idle;
            }
        }
    }
}

[UpdateInGroup(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial struct PlayerTransformSyncSystem: ISystem
{
    private EntityQuery playerQuery;

    public void OnCreate(ref SystemState state)
    {
        // PlayerGameObjectPrefab을 포함하는 엔티티를 쿼리
        playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerGameObjectPrefab>());
    }

    public void OnUpdate(ref SystemState state)
    {
        // 쿼리에 해당하는 엔티티가 비어있다면 리턴
        if (playerQuery.IsEmpty)
            return;

        Entity playerEntity = playerQuery.GetSingletonEntity();

        var gameObjectPrefab = state.EntityManager.GetComponentObject<PlayerGameObjectPrefab>(playerEntity);

        if (gameObjectPrefab != null && gameObjectPrefab.Object != null)
        {
            // Transform 좌표 가져오기
            Transform targetTransform = gameObjectPrefab.Object.transform;

            // LocalTransform 읽기/쓰기 참조(RefRW) 가져오기
            RefRW<LocalTransform> playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);

            // 좌표 및 회전 동기화
            targetTransform.position = playerTransform.ValueRW.Position ;
            targetTransform.rotation = playerTransform.ValueRW.Rotation ;
        }
    }
}

// 애니메이션 동기화 시스템 (뷰 표현 연산)
// 이동 시스템 뒤에 실행되며, ECS 데이터를 기존 Animator 파라미터로 주입합니다.
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial struct PlayerAnimationSyncSystem: ISystem
{
    private static readonly int StateHash = Animator.StringToHash("State");

    public void OnUpdate(ref SystemState state)
    {
        // 관리형 컴포넌트(PlayerAnimatorReference)를 쿼리하므로 이 시스템은 메인 스레드에서 돕니다.
        foreach (var (playerData, animRef) in SystemAPI.Query<RefRO<ECSPlayerData>, PlayerAnimatorReference>())
        {
            if (animRef.Animator == null)
                continue;

            int currentState = animRef.Animator.GetInteger(StateHash);
            if (currentState != (int)playerData.ValueRO.State)
            {
                Debug.Log($"애니메이터 값 변경: {currentState} -> {(int)playerData.ValueRO.State}");
                animRef.Animator.SetInteger(StateHash, (int)playerData.ValueRO.State);
            }

            // ECS 상태 데이터를 기반으로 전통 Animator 컨트롤러 제어
            animRef.Animator.SetInteger(StateHash, (int)playerData.ValueRO.State);
        }
    }
}