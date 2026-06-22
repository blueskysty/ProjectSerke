using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Diagnostics;

/// <summary>
/// Burst Compiler와 Job System의 성능 차이를 비교하기 위한 벤치마크 예제.
///
/// 비교 대상:
/// 1. 일반 C# (Single Thread)
/// 2. Burst Only (Single Thread + Burst Optimization)
/// 3. Job Only (Multi Thread)
/// 4. Burst + Job (Multi Thread + Burst Optimization)
///
/// 100만 개의 float 데이터에 대해 sqrt, sin, cos 연산을 수행한다.
/// </summary>
public class BurstJobBenchmark: MonoBehaviour
{
    /// <summary>
    /// 테스트할 데이터 개수
    /// </summary>
    private const int Count = 1_000_000;

    /// <summary>
    /// Job System에서 사용 가능한 NativeArray
    /// </summary>
    private NativeArray<float> values;

    private void Start()
    {
        // NativeArray 생성
        values = new NativeArray<float>(Count, Allocator.Persistent);

        // 테스트 데이터 초기화
        for (int i = 0; i < Count; i++)
        {
            values[i] = i * 0.001f;
        }

        UnityEngine.Debug.Log("===== Benchmark Start =====");

        TestNormal();
        TestBurstOnly();
        TestJobOnly();
        TestBurstJob();

        UnityEngine.Debug.Log("===== Benchmark End =====");
    }

    private void OnDestroy()
    {
        // NativeArray 메모리 해제
        if (values.IsCreated)
        {
            values.Dispose();
        }
    }

    #region Benchmark Methods

    /// <summary>
    /// 일반 C# 단일 스레드 처리
    /// </summary>
    private void TestNormal()
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < values.Length; i++)
        {
            float value = values[i];

            value = math.sqrt(value);
            value = math.sin(value);
            value = math.cos(value);
            value = math.log(math.abs(value) + 0.0001f);
            value = math.exp(math.clamp(value, -5f, 5f));

            values[i] = value;
        }

        stopwatch.Stop();

        UnityEngine.Debug.Log(
            $"[Normal] Single Thread : {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Burst Compiler만 적용
    /// Job은 사용하지 않으므로 단일 스레드로 동작
    /// </summary>
    private void TestBurstOnly()
    {
        var stopwatch = Stopwatch.StartNew();

        var job = new BurstSingleThreadJob
        {
            values = values
        };

        // 현재 스레드에서 즉시 실행
        job.Run();

        stopwatch.Stop();

        UnityEngine.Debug.Log(
            $"[Burst Only] Single Thread + Burst : {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Job System만 적용
    /// Burst 최적화는 사용하지 않음
    /// </summary>
    private void TestJobOnly()
    {
        var stopwatch = Stopwatch.StartNew();

        var job = new JobOnlyParallel
        {
            values = values
        };

        // 멀티스레드 스케줄링
        JobHandle handle = job.Schedule(values.Length, 64);

        handle.Complete();

        stopwatch.Stop();

        UnityEngine.Debug.Log(
            $"[Job Only] Multi Thread : {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Burst + Job System 적용
    /// 가장 높은 성능을 기대할 수 있는 방식
    /// </summary>
    private void TestBurstJob()
    {
        var stopwatch = Stopwatch.StartNew();

        var job = new BurstParallelJob
        {
            values = values
        };

        JobHandle handle = job.Schedule(values.Length, 64);

        handle.Complete();

        stopwatch.Stop();

        UnityEngine.Debug.Log(
            $"[Burst + Job] Multi Thread + Burst : {stopwatch.ElapsedMilliseconds} ms");
    }

    #endregion
}

#region Burst Only

/// <summary>
/// Burst Compiler만 적용된 Job
///
/// IJob:
/// - 단일 Execute 호출
/// - 단일 스레드 처리
///
/// Burst:
/// - SIMD(Vectorization)
/// - CPU 명령어 최적화
/// </summary>
[BurstCompile]
public struct BurstSingleThreadJob: IJob
{
    public NativeArray<float> values;

    public void Execute()
    {
        for (int i = 0; i < values.Length; i++)
        {
            float value = values[i];

            value = math.sqrt(value);
            value = math.sin(value);
            value = math.cos(value);
            value = math.log(math.abs(value) + 0.0001f);
            value = math.exp(math.clamp(value, -5f, 5f));

            values[i] = value;
        }
    }
}

#endregion

#region Job Only

/// <summary>
/// Job System만 사용
///
/// IJobParallelFor:
/// - 여러 Worker Thread에 분산 실행
///
/// Burst 미사용:
/// - 일반 네이티브 Job 코드로 실행
/// </summary>
public struct JobOnlyParallel: IJobParallelFor
{
    public NativeArray<float> values;

    public void Execute(int index)
    {
        float value = values[index];

        value = math.sqrt(value);
        value = math.sin(value);
        value = math.cos(value);
        value = math.log(math.abs(value) + 0.0001f);
        value = math.exp(math.clamp(value, -5f, 5f));

        values[index] = value;
    }
}

#endregion

#region Burst + Job

/// <summary>
/// Burst + Job System 조합
///
/// Job System:
/// - 멀티코어 활용
///
/// Burst:
/// - SIMD 최적화
/// - CPU 명령어 최적화
///
/// 일반적으로 가장 높은 성능을 제공
/// </summary>
[BurstCompile]
public struct BurstParallelJob: IJobParallelFor
{
    public NativeArray<float> values;

    public void Execute(int index)
    {
        float value = values[index];

        value = math.sqrt(value);
        value = math.sin(value);
        value = math.cos(value);
        value = math.log(math.abs(value) + 0.0001f);
        value = math.exp(math.clamp(value, -5f, 5f));

        values[index] = value;
    }
}

#endregion