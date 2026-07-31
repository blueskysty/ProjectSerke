using Unity.Entities;
using Unity.Mathematics;

public struct NavAgentComponent : IComponentData
{
	public Entity targetEntity;
	public bool pathCalculated;
	public int currentWaypoint;
	public float moveSpeed;
	public float nextPathCalculateTime;
}

public struct WaypointBuffer : IBufferElementData
{
	public float3 wayPoint;
}

public struct NavMeshTracker : IComponentData
{
    public float3 LastValidPosition; // 가장 최근 안전했던 NavMesh 위치
    public float TimeOutOfNavmesh;   // NavMesh 이탈 누적 시간
    public float CheckTimer;          // 주기적 검사용 내부 타이머
    public bool HasValidPosition;     // 저장된 위치 존재 여부
}