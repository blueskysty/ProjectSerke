# ProjectSerke

UI 

이 프로젝트는 **오브젝트 풀링(Object Pooling)**과 **유니티 이벤트 인터페이스(Event Interfaces)**를 활용하여 성능과 UX를 동시에 잡은 범용 UI 시스템입니다. 인벤토리, 장착창, 단축바 등 다양한 UI 환경에서 즉시 재사용이 가능하도록 설계되었습니다.

1. 효율적인 데이터 이동 전략 (Move & Clear)
Data Migration: 아이템을 다른 슬롯으로 드래그할 때, 데이터 복사가 아닌 이전(Move) 방식을 채택했습니다. 목적지 슬롯에 데이터 전송 완료 후 출발지 슬롯은 즉시 초기화되어 데이터 무결성을 유지합니다.

Smart Clearing: 드롭 가능 영역(Slot) 외부나 빈 공간에 드롭할 경우, 명시적으로 데이터를 삭제하여 직관적인 아이템 제거 기능을 제공합니다.

2. 성능 최적화 (Optimization)
UI Object Pooling: 슬롯의 런타임 생성/파괴를 지양하고 풀링 시스템을 통해 재사용함으로써 메모리 점유율과 CPU Peak를 최소화했습니다.

Event-Driven UI Update: Update()를 통한 상시 체크 대신, 데이터 변경 시점에만 SelectCheck()를 호출하여 시각적 상태(Highlight, Cooldown)를 갱신합니다.

3. 하이브리드 이벤트 시스템 (Button + IPointer)
표준 Button의 시각적 피드백은 유지하면서 커스텀 인터페이스를 결합해 복합적인 상호작용 문제를 해결했습니다.

문제: 버튼 컴포넌트가 클릭 이벤트를 선점하여 드래그 판정이 씹히거나 스크롤뷰 작동을 방해하는 현상.

해결: OnPointerDown, OnPointerUp을 활용하여 클릭과 드래그의 상태를 논리적으로 분리했습니다. 이를 통해 버튼의 편리함과 드래그 앤 드롭의 정교함을 동시에 확보했습니다.

문제: Button 컴포넌트가 클릭을 가로채면서 드래그 판정이 발생하지 않음

해결: OnPointerDown과 OnPointerUp 등을 활용해 드래그 시작 전후의 상태를 명확히 구분하여, 버튼 클릭과 드래그 이동이 의도치 않게 겹치는 현상을 방지했습니다.


DragAndDrop: 드래그 앤 드롭의 전역 상태와 데이터 전송 함수를 총괄하는 매니저.
DragIcon: 드래그 중인 아이템의 시각적 피드백을 담당하는 오버레이 아이콘.
DropSlot: IPointer 인터페이스를 통해 드롭 이벤트를 감지하고 데이터를 수신하는 컴포넌트.

InventoryBase<T> (Abstract): 제네릭을 사용하여 다양한 데이터 타입을 수용할 수 있는 인벤토리 최상위 클래스.
Inventory: InventoryBase를 상속받아 실제 게임 로직에 연결된 실체화 클래스.
SlotBase<T> (Abstract): 슬롯의 핵심 기능을 정의한 제네릭 베이스 클래스.
Slot1: SlotBase 상속 및 IPointer 인터페이스를 구현하여 버튼 기능과 드래그 로직이 결합된 실제 UI 슬롯.
