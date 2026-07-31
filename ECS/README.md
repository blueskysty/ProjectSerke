# 🏗 Project Architecture: ECS-Driven Game Engine

본 프로젝트는 Unity ECS(DOTS)의 성능을 극대화하면서도 안정적인 프로젝트 운영을 위해 **Main Scene(Management Layer)**과 **Subscene(Simulation Layer)**을 분리한 아키텍처를 채택했습니다.

## 💡 Architectural Decisions

많은 개발자가 ECS 도입 시 겪는 '하이브리드 워크플로우의 혼란'을 해결하기 위해, 다음과 같은 설계 원칙을 준수했습니다.

### 1. Data Ownership (데이터 주도권 분리)
* **Main Scene:** `MonoBehaviour` 기반의 시스템(UI, 게임 매니저)을 담당합니다. 유니티 엔진의 기존 시스템과의 호환성을 보장합니다.
* **Subscene:** 모든 물리 연산 객체를 ECS 전용으로 전환하여 데이터 소유권을 100% ECS로 이양합니다.
* **이유:** 시스템 간의 데이터 주도권 충돌을 원천 차단하여, 컴포넌트 동기화 시 발생하는 '데이터 오버라이드' 버그와 경고를 제거하였습니다.

### 2. Physics World Isolation (물리 월드 격리)
* **문제:** 메인 씬의 GameObject와 ECS 엔티티를 혼용할 경우, 물리 엔진이 초기 스캔 시 객체를 누락하거나 충돌 판정을 무시하는 현상 발생.
* **해결:** 모든 충돌 개체를 Subscene 내부에 배치하여, 물리 엔진이 동일한 시점에 동일한 ECS World 데이터로 시뮬레이션을 시작하도록 보장.
* **결과:** 안정적이고 정밀한 물리 충돌 판정 구현.

### 3. Separation of Concerns (역할의 분리)
* **Main Scene (Management Layer):** UI 시스템(UGUI), 카메라, 전체 씬 흐름 제어.
* **Subscene (Simulation Layer):** 플레이어, 몬스터, 사물 등 고성능 연산이 필요한 개체.
* **기대 효과:** * 시스템 간 결합도를 낮추어 유지보수성 향상.
    * 특정 씬 스트리밍(Scene Streaming)을 통한 효율적인 메모리 관리.
    * ECS 멀티스레드 연산과 UI 메인 스레드 연산 간의 병목 현상 완화.

### 4. ECS와 UI 간의 통신 전략
ECS 월드(Subscene)와 MonoBehaviour 월드(Main Scene UI) 사이의 데이터 전달 병목을 최소화하기 위해 다음 원칙을 적용하였습니다.

- **Reactive Data Binding:** 직접 참조 대신, 메인 씬의 UI 매니저가 ECS 시스템의 특정 컴포넌트(HP, Stamina 등)를 쿼리(Query)하여 UI를 갱신하는 방식을 채택.
- **Decoupling:** 싱글턴 참조의 의존성을 줄이기 위해 시스템 간의 데이터 결합을 최소화하고, 필요시 EntityCommandBuffer를 통한 비동기 데이터 전달 구조를 설계함.


### 5. 대규모 개체(Entity) UI 최적화 전략
수백 개 이상의 엔티티가 동시에 존재하는 환경에서, UI가 메인 스레드의 병목을 유발하지 않도록 다음 최적화 전략을 도입했습니다.

- **갱신 주기 제어 (Throttling):** 매 프레임 UI를 갱신하는 대신 0.1초 단위의 업데이트 주기를 적용하여 불필요한 연산을 90% 이상 절감하고 프레임 안정성을 확보했습니다.

- **데이터 일괄 처리 (Batch Query):** EntityQuery.ToComponentDataArray<T>()를 사용하여 메모리 블록 단위로 데이터를 일괄 복사함으로써 쿼리 호출 비용을 최소화하였습니다.

- **메모리 효율화 (UI Object Pooling):** 몬스터 생성/소멸 시 빈번한 Instantiate/Destroy 대신 오브젝트 풀링을 적용하여 가비지 컬렉션(GC) 발생을 방지하고 메모리 단편화를 해결했습니다.

### 6. 대규모 AI 네비게이션 최적화
* **NavMeshQuery 시스템:** 기존 `NavMeshAgent`의 오버헤드와 단일 스레드 한계를 극복하기 위해 `NavMeshQuery` API 기반의 멀티스레드 경로 탐색 시스템을 구축했습니다.
* **메인 스레드 Direct Access 최적화:** 스폰 직후 발생할 수 있는 1프레임의 프레임 동기화 지연(Stale Data)을 차단하기 위해, 길찾기 및 타겟 지정 데이터(`NavAgentComponent`)는 ECB(EntityCommandBuffer) 지연 적용 대신 메인 스레드 Direct Access 패턴을 적용하여 즉시 데이터 주입을 보장했습니다.
* **NavMeshQuery 대용량 버퍼 및 비동기 연산 완결 최적화:** 먼 거리 길찾기 시 100회 단위의 단일 연산 조기 종료로 인해 AI가 멈추던 `PathQueryStatus.InProgress` 동결 버그를 규명했습니다. 경로 노드 버퍼(`maxNodes`) 확장 및 누적 연산(`UpdateFindPath`) 완결 Loop를 구축하여, 대규모 맵 반대편 추적 시에도 동결 현상 없는 완벽한 경로 탐색을 보장했습니다.