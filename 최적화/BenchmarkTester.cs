using UnityEngine;
using System.Threading.Tasks;
using System.Diagnostics; // 시간 측정용
using System.Collections.Generic;

public class BenchmarkTester: MonoBehaviour
{
    // 테스트용 가상 데이터 (데이터가 클수록 격차가 뚜렷해집니다)
    private const int DATA_COUNT = 10000000;

    void Start()
    {
        RunBenchmark();
    }

    void Update()
    {
        // 메인 스레드가 멈추면 이 회전이 뚝뚝 끊깁니다.
        transform.Rotate(0, 100 * Time.deltaTime, 0);
    }

    async void RunBenchmark()
    {
        //  3초 대기후 벤치마크 테스트 시작
        await Task.Delay(3000);

        UnityEngine.Debug.Log("--- 벤치마크 테스트 시작 ---");

        // 1. 동기 방식 테스트 (메인 스레드 점유)
        Stopwatch sw = new Stopwatch();
        sw.Start();

        List<int> syncResult = HeavyParsingTask(); // 파싱 작업 수행

        sw.Stop();
        UnityEngine.Debug.Log($"[동기 방식] 소요 시간: {sw.ElapsedMilliseconds}ms (메인 스레드 블로킹됨)");

        sw.Reset();

        //  3초 대기(메인 스레드 멈추지 않음)
        // await Task.Delay는 메인 스레드를 블로킹하지 않아서 큐브가 계속 돕니다.
        await Task.Delay(3000);

        // 2. 비동기 방식 테스트 (백그라운드 스레드 분리)
        sw.Start();

        // Task.Run을 통해 다른 코어에서 작업 수행
        List<int> asyncResult = await Task.Run(() => HeavyParsingTask());

        sw.Stop();
        UnityEngine.Debug.Log($"[비동기 방식] 소요 시간: {sw.ElapsedMilliseconds}ms (메인 스레드 영향 없음)");

        UnityEngine.Debug.Log("--- 벤치마크 테스트 종료 ---");
    }

    // 의도적으로 무거운 연산을 수행하는 함수
    List<int> HeavyParsingTask()
    {
        List<int> results = new List<int>();
        for (int i = 0; i < DATA_COUNT; i++)
        {
            // 삼각함수나 제곱근 등을 사용하여 CPU 부하를 의도적으로 높임
            double val = System.Math.Sqrt(i) * System.Math.Sin(i);
            results.Add((int)val % 10);
        }
        return results;
    }
}