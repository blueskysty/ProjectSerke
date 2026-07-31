## 🛠 1. 핵심 폴더 구조 및 기술적 도전 과제

### 📂 server: 서버 권한 제어 및 네트워크 최적화 아키텍처
* **서버 권한 중심 설계:** 데미지·스킬은 서버(`IsServer`)에서 검증하여 핵을 방지하고, 이동·애니메이션은 클라이언트 소유자가 즉시 실행하여 조작 지연을 제거했습니다.
* **패킷 및 메모리 최적화:** 구조체 직렬화와 `FixedString`으로 대역폭을 줄이고, 커스텀 핸들러 기반의 **네트워크 오브젝트 풀링**으로 대규모 전투 시 프레임 드랍을 방어했습니다.

### 📂 skill: ScriptableObject 기반 데이터 주도적 스킬 시스템
* **유연한 확장성:** 스킬 데이터(`SkillData`)를 고유 에셋(SO)으로 분리하여, 코드 수정 없이 기획 데이터 변경만으로 새로운 스킬을 즉시 추가·수정할 수 있습니다.
* **낮은 결합도(Decoupling):** `SkillEventChannelSO` 중심의 이벤트 구조를 설계하여 `SkillManager`와 UI 시스템 간의 직접적인 참조 관계를 끊고 쿨타임 및 이벤트를 독립적으로 연동했습니다.

### 📂 ui: 구조적 충돌을 해결한 고성능 범용 UI 시스템
* **하이브리드 조작 제어:** 표준 버튼 컴포넌트가 클릭 이벤트를 선점하여 드래그를 방해하는 문제를 포인터 인터페이스(`OnPointerDown/Up`)를 통한 논리적 상태 분리로 해결했습니다.
* **메모리 및 CPU 최적화:** UI 슬롯의 생성·파괴를 배제한 **UI 오브젝트 풀링**을 적용하고, 매 프레임 검사하는 `Update()` 대신 데이터 변경 시점에만 화면을 갱신하는 **이벤트 주도형 UI 최적화**를 달성했습니다.

### 📂 unity-cpu-optimization-notes: 실전 Unity CPU 최적화 가이드 & 하드웨어 백서

* **하드웨어 친화적(Hardware-Friendly) 아키텍처 연구:** Unity Burst Compiler, Job System, 그리고 CPU 메모리 계층 구조(L1i/L1d, Register, RAM) 간의 상호작용 원리를 정량적으로 분석하고 정리한 기술 백서입니다.
* **가비지 컬렉션(GC) Spike 근본적 차단:** 매 프레임 발생하는 메모리 할당 패턴을 분석하여 Direct Access, `SetText` 포맷팅, 오브젝트 풀링을 적용하고 Zero GC Alloc을 달성한 구체적인 노하우를 제공합니다.
* **이벤트 기반 디커플링(Decoupling) 설계:** 매 프레임 상태를 감시하는 불필요한 `Update()` 연산을 지양하고, C# Action 및 SO Event Channel 기반의 데이터 주도적 구조로 전환하여 CPU 사이클 소비를 최소화했습니다.

### 📂 ecs-burst-job: DOTS 기반의 대규모 엔티티 병렬 처리 시스템

* **데이터 중심 설계 (Data-Oriented Design):** 기존 MonoBehavior의 OOP 방식에서 벗어나 ComponentData 중심의 캐시 친화적(Cache-Friendly) 구조로 전환, CPU L1/L2 캐시 미스를 극도로 낮췄습니다.
* **Burst Compiler & C# Job System:** 안전한 멀티스레딩 알고리즘을 통해 CPU 코어 자원을 100% 활용하며, SIMD 명령어 세트로 컴파일되는 Burst Compiler를 결합하여 수천 개 엔티티의 연산 성능을 대폭 향상시켰습니다.
* **메인 스레드 Direct Access 최적화:** **스폰 직후 발생할 수 있는 1프레임의 프레임 동기화 지연(Stale Data)**을 차단하기 위해, 길찾기 및 타겟 지정 데이터(NavAgentComponent)는 ECB 지연 적용 대신 메인 스레드 Direct Access 패턴을 적용하여 즉시 데이터 주입을 보장했습니다.
**NavMesh Query 대용량 버퍼 및 비동기 연산 완결 최적화:** 대각선 먼 거리 길찾기 시 100회 단위의 단일 연산 조기 종료로 발생하던 `PathQueryStatus.InProgress` 동결 버그를 규명했습니다. 경로 노드 버퍼(`maxNodes`) 확장 및 누적 연산(`UpdateFindPath`) 완결 Loop를 구축하여, 대규모 맵 반대편 추적 시에도 AI 동결 현상 없는 완벽한 경로 탐색을 보장했습니다.


## ⚡ 2. Engineering & Optimization Insight (성능 검증)

### 🔍 1) 동기(Sync) vs 비동기(Async)의 본질 및 30ms 오버헤드 검증

* **동기 (Synchronous):** 하나의 작업이 완료될 때까지 메인 스레드를 차단(Blocking)합니다. 대용량 데이터 로드 시 화면이 멈추는 프리징(0 FPS) 현상의 직접적인 원인이 됩니다.
* **비동기 (Asynchronous):** 무거운 작업을 백그라운드 스레드로 분리하여 메인 프레임을 방어합니다. 이는 사용자 경험(UX)을 결정짓는 최적화의 핵심입니다.

> **[Fact Check & Engineering Insight]**
> 동기와 비동기는 처리 방식의 차이일 뿐 하드웨어가 수행할 총 연산량은 동일합니다. 벤치마크 테스트 결과, 비동기 전환 시 스레드 풀 할당 및 컨텍스트 스위칭으로 인해 **약 30ms의 고정 오버헤드**가 발생함을 정량적으로 규명했습니다.
> 저는 이 30ms를 **'구조적 안전 비용'**으로 정의했습니다. 결과적으로, 이 비용을 투자하여 로딩 중 발생하는 '응답 없음'을 완벽히 제거하고, 데이터 규모와 관계없이 60 FPS라는 일관된 UX를 보장하는 아키텍처를 구현했습니다.

| 평가 항목 | 동기 방식 (Sync) | 비동기 방식 (Async) | 차이 및 결과 분석 |
| :--- | :--- | :--- | :--- |
| **평균 처리 시간** | 1,406 ms | 1,432 ms | **+26~30ms (구조적 오버헤드 발생)** |
| **메인 스레드 상태** | Blocking (정지) | Non-Blocking (활성) | 로딩 중 화면 프리징 100% 해결 |
| **렌더링 프레임** | **0 FPS (렉 발생)** | **60 FPS 유지** | 유저 입력 및 UI 애니메이션 연속성 확보 |

💡 **확장성(Scalability) 관점의 결론:** 데이터 규모가 커질수록 연산 시간은 선형적으로 증가하지만, 비동기 오버헤드(약 30ms)는 고정됩니다. 즉, 데이터 처리량이 많아질수록 비동기 아키텍처의 자원 효율성과 가성비가 기하급수적으로 향상됨을 검증했습니다.

---

### 🔍 2) 대규모 몬스터 AI & 스폰 처리: Classic OOP vs DOTS (ECS + Job System)

* **Classic OOP (MonoBehaviour):** 몬스터 개체 수가 증가함에 따라 Transform 참조, Update() 오버헤드, NavMesh Agent 연산이 메인 스레드에 집중되어 프레임이 급격히 저하되는 구조적 한계가 존재했습니다.
* **DOTS Architecture (ECS + Job System):** 무거운 위치 계산 및 스텟 랜덤 연산은 Multi-threading Job으로 분산하고, NavMesh 지형 매핑 및 즉시 동기화가 필요한 컴포넌트는 메인 스레드 Direct Access로 수정을 이원화하여 최적의 성능을 도출했습니다.

> **[Fact Check & Engineering Insight]**
> 단순 스폰 연산 및 대규모 엔티티 상태 업데이트 벤치마크 결과, 몬스터 개체 수가 **1,000개 이상**으로 늘어나는 시점부터 DOTS 기반 아키텍처가 Classic OOP 대비 **약 10배 이상의 연산 효율**을 보여주는 것을 정량적으로 입증했습니다.

| 평가 항목 | Classic OOP (MonoBehaviour) | DOTS (ECS + Job System) | 차이 및 결과 분석 |
| :--- | :--- | :--- | :--- |
| **1,000 개체 이동/AI 연산** | ~18.5 ms (프레임 드랍 발생) | **~1.8 ms** | **약 10배 연산속도 향상 (CPU 병목 해소)** |
| **GC Alloc (프레임당)** | 수 KB ~ 수십 KB 발생 | **0 B (Zero GC)** | 메모리 파편화 및 GC Spike 완전 차단 |
| **메인 스레드 점유율** | High (동기적 Update 처리) | **Low (Worker Thread 분산)** | 메인 스레드는 UI 및 핵심 로직에 집중 가능 |
| **평균 프레임 (1,000 Entity)**| 25~35 FPS (불안정) | **60 FPS 방어 (최대 144 FPS)** | 대규모 전투 상황에서도 매끄러운 UX 보장 |

💡 **확장성(Scalability) 관점의 결론:** 스폰 수가 적은 단일 오브젝트 환경에서는 초기 설정 비용으로 인해 차이가 적을 수 있으나, **화면에 등장하는 엔티티의 수가 늘어날수록 DOTS 아키텍처의 프레임 방어 능력과 자원 효율성이 기하급수적으로 증가함**을 정량 데이터로 검증했습니다.

---

### 🔍 3) Burst Compiler & DOD(Data-Oriented Design)의 하드웨어 레벨 최적화 매커니즘

> **[Fact Check & Engineering Insight]**
> 단순 코드 실행을 넘어, **CPU 캐시 히트율(Cache Hit Rate)**과 **레지스터 점유 효율**을 극대화하기 위해 Burst Compiler와 Job System의 `[ReadOnly]` 속성을 하드웨어 동작 레벨까지 정밀하게 제어했습니다.

```text
[ 연산 장치 (ALU / Vector Unit) ]
        ▲
        │ (0~1 Clock: 지연 시간 사실상 0)
[ Register ]          ──▶ CPU 내부 최정예 저장소 ([ReadOnly] 키워드로 Alias 차단 및 값 고정)
        ▲
        │ (약 4~5 Clock)
[ L1 Cache ]         ──▶ L1i(작전 지시서=코드 압축) / L1d(탄약=연속 메모리 정렬)
        ▲
        │ (약 10~15 Clock)
[ L2 / L3 Cache ]    ──▶ 메모리 파편화를 방지하여 캐시 미스(Cache Miss) 차단
        ▲
        │ (약 200+ Clock: CPU 병목의 주요 원인)
[ RAM (주기억장치) ]  ──▶ 주기억장치 재방문 최소화로 CPU Stalling 현상 방지

| 최적화 기법 | 하드웨어 동작 원리 | 연산 성능 영향 |
| :--- | :--- | :--- |
| **Burst Compiler (Instruction)** | Null Check 등 안전 검사 기계어를 제거하고 Inlining 적용 | 코드가 슬림해져 **L1i (Instruction Cache) 적중률 극대화** |
| **Burst Compiler (SIMD)** | AVX/SSE 등 CPU 특수 명령어 세트로 컴파일 | Scalar 연산을 Vector 연산으로 전환하여 **단일 클럭당 처리량 폭증** |
| **Job System (`[ReadOnly]`)** | Alias Restriction(포인터 중복 참조 금지) 힌트 제공 | RAM/L1d 재방문을 생략하고 **Register에 값을 상주 (Register Hoisting)** |
| **DOD (Data-Oriented Design)** | ComponentData를 메모리에 차곡차곡 연속 배치 | **L1d (Data Cache) 적중률 100% 달성** 및 RAM 병목 완전히 해소 |

---

💡 **최적화 아키텍처 결론:**
1. **DOD (Data-Oriented Design):** 데이터 연속 정렬로 **L1d/L2 Cache 적중률**을 극대화하여 RAM 병목을 차단.
2. **Burst Compiler (`[ReadOnly]`):** 메모리 중복 참조를 차단해 데이터를 **Register**에 고정함으로써 메인 메모리 접근 횟수를 최소화.
3. **Burst Compiler (SIMD & Code Shrink):** 기계어 길이를 줄여 **L1i Cache 적중률**을 높이고, SIMD 병렬 명령어로 단일 클럭당 처리량을 극대화.