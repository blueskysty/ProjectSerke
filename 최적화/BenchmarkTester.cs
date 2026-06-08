using UnityEngine;
using System.Threading.Tasks;
using System.Diagnostics; // �ð� ������
using System.Collections.Generic;

public class BenchmarkTester: MonoBehaviour
{
    // �׽�Ʈ�� ���� ������ (�����Ͱ� Ŭ���� ������ �ѷ������ϴ�)
    private const int DATA_COUNT = 10000000;

    void Start()
    {
        RunBenchmark();
    }

    void Update()
    {
        // ���� �����尡 ���߸� �� ȸ���� �Ҷ� ����ϴ�.
        transform.Rotate(0, 100 * Time.deltaTime, 0);
    }

    async void RunBenchmark()
    {
        //  3�� ����� ��ġ��ũ �׽�Ʈ ����
        await Task.Delay(3000);

        UnityEngine.Debug.Log("--- ��ġ��ũ �׽�Ʈ ���� ---");

        // 1. ���� ��� �׽�Ʈ (���� ������ ����)
        Stopwatch sw = new Stopwatch();
        sw.Start();

        List<int> syncResult = HeavyParsingTask(); // �Ľ� �۾� ����

        sw.Stop();
        UnityEngine.Debug.Log($"[���� ���] �ҿ� �ð�: {sw.ElapsedMilliseconds}ms (���� ������ ����ŷ��)");

        sw.Reset();

        //  3�� ���(���� ������ ������ ����)
        // await Task.Delay�� ���� �����带 ����ŷ���� �ʾƼ� ť�갡 ��� ���ϴ�.
        await Task.Delay(3000);

        // 2. �񵿱� ��� �׽�Ʈ (��׶��� ������ �и�)
        sw.Start();

        // Task.Run�� ���� �ٸ� �ھ�� �۾� ����
        List<int> asyncResult = await Task.Run(() => HeavyParsingTask());

        sw.Stop();
        UnityEngine.Debug.Log($"[�񵿱� ���] �ҿ� �ð�: {sw.ElapsedMilliseconds}ms (���� ������ ���� ����)");

        UnityEngine.Debug.Log("--- ��ġ��ũ �׽�Ʈ ���� ---");
    }

    // �ǵ������� ���ſ� ������ �����ϴ� �Լ�
    List<int> HeavyParsingTask()
    {
        List<int> results = new List<int>();
        for (int i = 0; i < DATA_COUNT; i++)
        {
            // �ﰢ�Լ��� ������ ���� ����Ͽ� CPU ���ϸ� �ǵ������� ����
            double val = System.Math.Sqrt(i) * System.Math.Sin(i);
            results.Add((int)val % 10);
        }
        return results;
    }
}