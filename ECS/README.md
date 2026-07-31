
# 🏗️ Project Architecture: ECS-Driven Game Engine

> **5,000+ Active Entities | ~200 FPS Runtime Stability | Zero-GC (0 B) Optimization**

---

### 📹 System Performance Demonstration

https://github.com/user-attachments/assets/466b1482-06b3-46ce-bd29-3d49a8fd3260

*(▲ 5,000개 엔티티 실시간 스폰, NavMeshQuery 기반 동적 길찾기 및 타겟 추적 시 평균 200 FPS 유지 시연)*

* **Max Active Monsters:** `5,000`
* **Average Frame Rate:** `190 ~ 220 FPS`
* **Runtime GC Alloc:** `0 B` (Zero GC)

---

## 💡 Architectural Decisions

본 프로젝트는 Unity ECS(DOTS)의 성능을 극대화하면서도 안정적인 프로젝트 운영을 위해 **Main Scene (Management Layer)**과 **Subscene (Simulation Layer)**을 완벽히 분리한 구조를 채택했습니다. 하이브리드 워크플로우에서 흔히 발생하는 혼란과 성능 병목을 해결하기 위한 6가지 핵심 설계 원칙은 다음과 같습니다.

### 1. Data Ownership (데이터 주도권 분리)
* **Main Scene:** MonoBehaviour 기반의 시스템(UI, 전체 씬 흐름)을 담당하여 유니티 기존 엔진과의 호환성을 보장합니다.
* **Subscene:** 모든 물리 및 게임 플레이 연산 객체를 ECS 전용으로 전환하여 데이터 소유권을 100% ECS로 이양했습니다.
* **도입 효과:** 시스템 간 데이터 주도권 충돌을 원천 차단하여, 컴포넌트 동기화 시 발생하는 '데이터 오버라이드' 버그와 경고를 완전히 제거했습니다.

### 2. Physics World Isolation (물리 월드 격리)
* **문제 상황:** 메인 씬의 GameObject와 ECS 엔티티를 혼용할 경우, 물리 엔진 초기 스캔 시 객체가 누락되거나 충돌 판정이 무시되는 현상이 발생했습니다.
* **해결 방법:** 모든 충돌 개체를 Subscene 내부에만 배치하여, 물리 엔진이 동일한 시점에 동일한 ECS World 데이터로 시뮬레이션을 시작하도록 보장했습니다.
* **결과:** 대규모 개체 환경에서도 오차 없는 정밀하고 안정적인 물리 충돌 판정을 구현했습니다.

### 3. Separation of Concerns (역할의 분리)
* **Main Scene (Management Layer):** UI 시스템(UGUI), 카메라, 게임 루프 제어.
* **Subscene (Simulation Layer):** 플레이어, 몬스터, 사물 등 고성능 연산이 필요한 대규모 개체.
* **기대 효과:**
  * 시스템 간 결합도를 낮추어 코드의 확장성 및 유지보수성 향상.
  * Subscene 스트리밍(Scene Streaming)을 통한 효율적인 메모리 관리.
  * ECS 멀티스레드 연산과 UI 메인 스레드 연산 간의 병목 현상 최소화.

### 4. ECS ↔ UI 통신 및 데이터 바인딩 전략
ECS World(Subscene)와 MonoBehaviour World(Main Scene UI) 사이의 데이터 전달 병목을 최소화하기 위해 다음 구조를 설계했습니다.
* **Reactive Data Binding:** 직접 참조 방식 대신, 메인 씬의 UI 매니저가 ECS 시스템의 특정 컴포넌트(HP, 몬스터 수 등)를 주기적으로 쿼리(Query)하여 UI를 안전하게 갱신합니다.
* **Decoupling (의존성 제거):** 싱글턴 참조 의존성을 축소하고, 필요시 `EntityCommandBuffer`를 통한 비동기 데이터 전달 구조를 적용하여 결합도를 낮췄습니다.

### 5. 대규모 개체(Entity) UI 최적화 전략
수천 개 이상의 엔티티가 동시 존재하는 환경에서 UI 연산이 메인 스레드 병목을 유발하지 않도록 최적화하였습니다.
* **갱신 주기 제어 (Throttling):** 매 프레임 UI를 갱신하는 대신 0.1초 단위의 업데이트 주기를 적용, 불필요한 연산을 90% 이상 절감하여 프레임 안정성을 확보했습니다.
* **데이터 일괄 처리 (Batch Query):** `EntityQuery.ToComponentDataArray()`를 통해 메모리 블록 단위로 데이터를 일괄 복사하여 쿼리 호출 오버헤드를 최소화했습니다.
* **UI Object Pooling:** 몬스터 생성/소멸 시 빈번한 `Instantiate` / `Destroy` 대신 오브젝트 풀링을 적용하여 runtime 가비지 컬렉션(GC) 발생을 차단했습니다.

### 6. 대규모 AI 네비게이션 & Pathfinding 최적화
* **NavMeshQuery 시스템:** 기존 `NavMeshAgent`의 메인 스레드 단일 연산 오버헤드를 극복하고자, `NavMeshQuery` API 기반의 C# Job System 병렬 경로 탐색 시스템을 구축했습니다.
* **메인 스레드 Direct Access 최적화:** 스폰 직후 발생하던 1프레임의 동기화 지연(Stale Data)을 막기 위해, 길찾기 및 타겟 지정 데이터(`NavAgentComponent`)는 ECB 지연 적용 대신 **메인 스레드 Direct Access 패턴**을 적용해 스폰 프레임에 즉시 주입되도록 보장했습니다.
* **NavMeshQuery 누적 연산 Loop 및 대용량 버퍼 구축:** 장거리 길찾기 시 단일 프레임 연산 제한(100회)으로 인해 AI가 멈추는 `PathQueryStatus.InProgress` 동결 버그를 규명했습니다. 경로 노드 버퍼(`maxNodes`) 확장 및 누적 연산(`UpdateFindPath`) 완결 Loop를 구축하여, **5,000마리의 몬스터가 맵 반대편까지 동결 없이 완벽하게 추적**하도록 최적화했습니다.
* **예외 상황 대비 Safe-Fail System (NavMesh 이탈 자동 복구):** 대규모 엔티티 밀집 시 발생할 수 있는 Physics Clipping(벽 뚫림/맵 이탈) 현상에 대비하여, 5초 이상 NavMesh 영역을 벗어난 객체를 감지하고 가장 최근 안전 좌표로 자동 복구(Teleport)하는 예외 처리 루틴을 구축했습니다.
