# 📝 [Unity/C#] 동기 vs 비동기 & 코루틴 vs async/await 완벽 정리

프로젝트를 진행하며 대용량 데이터 로딩 시 발생하는 프레임 드랍(프리징) 문제를 해결하기 위해, 유니티의 비동기 처리 메커니즘과 멀티 스레드 활용법을 깊이 있게 공부하고 정리한 내용입니다.

---

## 1. 동기(Sync) vs 비동기(Async)의 본질
* **동기 (Synchronous):** 하나의 작업이 완전히 끝날 때까지 다음 작업을 블로킹(Blocking)합니다. 대용량 파일 처리 시 메인 스레드가 멈춰 **화면 프리징(0 FPS 렉)**을 유발합니다.
* **비동기 (Asynchronous):** 대기 시간이 필요한 작업을 백그라운드에 위임하고 곧바로 다음 로직을 실행합니다. 메인 프레임을 방어하는 **최적화의 핵심**입니다.
* **Fact Check:** 동기와 비동기는 일을 처리하는 '과정(스레드 분리 및 제어권 양보)'의 차이일 뿐, 하드웨어가 소모해야 하는 **절대적인 연산량의 총합은 동일**합니다.

---

## 2. 유니티 비동기 구현 방식 비교: 코루틴 vs async/await

### ❌ 코루틴 (`IEnumerator` + `yield return`)
* **구동 원리:** 멀티 스레드가 아닙니다. 오직 **메인 스레드(코어 1개) 안에서** 프레임을 아주 잘게 쪼개어 번갈아 실행하는 **'싱글 스레드 시분할 처리'**입니다.
* **한계점:** 코루틴 내부에서 수만 줄짜리 XML/JSON 데이터를 분석(Parsing)하는 무거운 반복문을 돌리면, 결국 메인 스레드가 과부하되어 **화면이 똑같이 끊깁니다.**
* **적합한 상황:** 유니티 생명주기 및 게임 시간(`Time.timeScale`)과 연동되므로 **몬스터 AI 패턴, 인게임 타이머, UI 페이드인/아웃 연출** 등에 적합합니다.

### ⭕ async / await (`async` + `await`)
* **구동 원리:** C# 표준 비동기 문법입니다. `Task.Run`을 활용하면 메인 스레드가 아닌 **'진짜 다른 CPU 코어(백그라운드 스레드Pool)'**로 연산 자체를 넘길 수 있습니다.
* **장점:** 백그라운드 코어에서 무거운 연산을 처리하는 동안에도 메인 스레드는 방해받지 않으므로, 유저 화면은 **단 1프레임도 끊기지 않고 부드럽게 구동**됩니다.
* **적합한 상황:** 대용량 데이터 로딩/파싱, 웹 서버 통신(Network), Addressables 에셋 로딩 등 **CPU 소모가 극심한 자원 관리**에 적합합니다.
* **⚠️ 주의사항 (Thread Safety):** 백그라운드 스레드에서는 `Transform`, `GameObject` 등 유니티 API를 직접 조작할 수 없습니다. 반드시 `await`를 통해 메인 스레드로 복귀한 후 오브젝트를 생성해야 합니다.

---

## 3. 실전 최적화 구조 및 코드 매커니즘(예시)

```csharp
// 함수 앞에 async를 붙여 비동기 함수임을 명시
public async void LoadAndParseMapData()
{
    // 1. 디스크에서 파일을 읽어오는 동안 메인 스레드는 화면을 계속 그리며 대기 (Non-blocking)
    ResourceRequest request = Resources.LoadAsync<TextAsset>("BigMapData");
    await request; 
    
    string rawXmlText = ((TextAsset)request.asset).text;

    // 2. [핵심 최적화] 무거운 파싱(Text 해독) 연산 자체를 백그라운드 서브 코어로 강제 이주
    //    이 반복문이 도는 동안에도 인게임 로딩 애니메이션은 60 FPS를 유지함
    var parsedData = await Task.Run(() => {
        return ParseXmlInternal(rawXmlText); // 순수 C# 연산 전담
    });

    // 3. await 완료 후 자동으로 메인 스레드 복귀. 안전하게 유니티 오브젝트 생성
    GenerateMap(parsedData); 
}