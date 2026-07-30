# 🧠 유니티 성능 최적화 & CPU 아키텍처 메모리

Unity의 **Burst Compiler**, **Job System**, 그리고 **CPU 하드웨어 구조(L1i/L1d, Register, Core)** 간의 상호작용과 최적화 원리를 정리한 메모입니다.

---

## 1. ⚡ Burst Compiler와 코드 영역 (Instruction Section)

### ❓ 버스트 컴파일러를 써야만 코드 영역에 들어가는가?
* **NO.** 일반 C# (Mono/IL2CPP)이든 버스트 컴파일러든, **모든 C# 코드는 기계어로 번역되어 메모리의 코드 영역(Instruction / Text Section)으로 들어가 실행**됩니다.
* 차이는 **"코드 영역으로 들어가느냐 마느냐"**가 아니라 **"코드 영역에 들어가는 기계어의 품질과 크기"**입니다.

### 🔍 일반 C# 번역 vs Burst 번역 비교
* **일반 C# (Mono/JIT):**
  * Null 체크, 배열 범위 검사(`IndexOutOfRangeException`), GC 추적 등 **안전장치 기계어 코드**가 대량으로 삽입됩니다.
  * 명령어 길이가 길어 **L1i (Instruction Cache)** 공간을 많이 차지하고 실행 속도가 보통입니다.
* **버스트 컴파일러 (Burst Compiler):**
  * 예외 처리와 안전 검사 코드를 과감히 제거합니다 (`Struct` 사용 강제, 배열 검사 생략 등).
  * SIMD(AVX 등) CPU 특수 명령어를 활용해 **극도로 압축되고 정교한 기계어**를 만듭니다.
  * 명령어 길이가 짧아 **L1i 캐시 적중률(Hit Rate)이 대폭 상승**하며 폭발적인 연산 속도를 제공합니다.

---

## 2. 🛡️ Job System의 `[ReadOnly]` / Read-Write와 Burst의 관계

### 1) [ReadOnly] / Read-Write의 주체 = Job System
* **안전장치 (Safety Check):** `[ReadOnly]`와 Read-Write(RW)를 감시하고 스레드 충돌(`InvalidOperationException`)을 막는 것은 **유니티 잡 시스템의 역할**입니다.
* 잡 시스템은 RW 데이터에 대해 다른 스레드가 동시 접근하지 못하도록 순서를 제어합니다.

### 2) Burst Compiler가 `[ReadOnly]`를 다루는 방식 (성능 최적화)
* 버스트 컴파일러는 잡 시스템이 제공한 `[ReadOnly]` 힌트를 바탕으로 **기계어 레벨에서 극상 최적화**를 수행합니다.
* **RW (Read-Write)일 때:** 중간에 값이 바뀔 수 있으므로, 매번 메모리(RAM/L1d 캐시) 주소를 찾아가서 값을 읽어오는 기계어를 생성합니다.
* **`[ReadOnly]` (RO)일 때:** 값이 절대 바뀌지 않음을 확신하고, 메모리 재방문을 생략한 채 **CPU 최정예 저장소인 레지스터(Register)에 값을 고정(Register Optimization / Alias Restriction)해 두고 즉시 연산**합니다.

---

## 3. 🏛️ CPU 메모리 계층 및 구조 (Memory Hierarchy)

연산 장치(ALU)와의 물리적 거리에 따른 용량 및 속도 관계입니다.

```text
[ 연산 장치 (ALU/FPU) ]
        ▲
        │ (0~1 클럭: 0초 만에 접근)
[ Register ]      ──▶ 코어 바로 내부, ALU 손끝에 붙어 있는 최고속 저장소 (수B ~ 수KB)
        ▲
        │ (약 4~5 클럭)
[ L1 Cache ]     ──▶ L1i(코드 명령어) / L1d(데이터) 분리형 캐시 (수십 KB)
        ▲
        │ (약 10~15 클럭)
[ L2 Cache ]     ──▶ 코어 전용 중간 크기 캐시 (수 MB)
        ▲
        │ (약 40~60 클럭)
[ L3 Cache ]     ──▶ 모든 코어 공유 대용량 캐시 (수십 MB)
        ▲
        │ (약 200+ 클럭: 매우 느림)
[ RAM (주기억장치) ] ──▶ CPU 외부 메모리
> **규칙:** 연산 장치와 물리적으로 가까울수록 용량은 작아지고, 속도는 압도적으로 빨라집니다.

---

### 🎖️ CPU 핵심 장치 군대 조직도 비유

| 조직 역할 | CPU 구성 요소 | 핵심 역할 및 특징 |
| :--- | :--- | :--- |
| **대대장 / 지휘관** | **Control Unit (CU)** | 메모리에서 명령어를 가져와 해독(Decode)하고 각 부품에 제어 신호를 보냄 |
| **참모 / 정보장교** | **Branch Predictor** | `if`문 조건 분기를 과거 데이터 기반으로 사전 예측해 **미리 실행(Predictive Execution)** |
| **일반병사** | **ALU (정수 연산 장치)** | 기본적인 정수 계산 및 참/거짓 논리 판단을 전담 수행 |
| **특수부대** | **FPU (실수 연산 장치)** | 복잡한 소수점 연산 및 3D/물리 수식 등 고난도 정밀 계산 전담 |
| **병사들의 무기** | **Register** | ALU/FPU가 연산에 즉시 사용하도록 손에 쥐고 있는 가장 빠른 저장소 |
| **보급 창고** | **L1 / L2 / L3 Cache** | **L1(부대 창고)** $\rightarrow$ **L2(여단 창고)** $\rightarrow$ **L3(사단 창고)** 순으로 배치해 RAM 병목 방지 |
| **군수물자 수송대** | **Memory Controller & Bus** | 캐시 미스 발생 시 멀리 있는 외부 RAM(중앙 본부)에서 데이터를 가져와 보급함 |

---

### 💡 최종 결론 & 최적화의 의미

* **DOD (Data-Oriented Design):** 데이터를 메모리에 연속적으로 정렬하여 **보급 창고(L1d/L2/L3 Cache)**의 적중률을 극대화하는 기법.
* **Burst Compiler (`[ReadOnly]`):** 병사(ALU)가 매번 창고(L1d)에 가지 않고 **무기(Register)**를 손에 쥐고 연산하게 만들어 기계어 성능을 쥐어짜 내는 기법.
* **Burst Compiler (Instruction):** 코드 크기를 줄여 **보급 창고(L1i Cache)**에 명령어가 꽉 채워지게 만드는 기법.