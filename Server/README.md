Unity Netcode for GameObjects를 기반으로 한 서버 및 네트워크 관리 모듈입니다.
=======
https://github.com/user-attachments/assets/c7db0d45-54cb-407e-a2e5-5c20f695db31

Unity Netcode for GameObjects를 기반으로 한 서버 및 네트워크 관리 모듈입니다.

1 권한 기반 제어 (Authority Management)<br>
Client Authority: ClientNetworkTransform과 OwnerNetworkAnimator를 사용하여 서버의 확인을 기다리지 않고 소유자(Owner)가 즉시 이동과 애니메이션을 실행하도록 설정(조작 지연 해제).<br>
Server Authoritative: 데미지 판정, 스킬 생성, 플레이어 스탯 수정은 반드시 서버(IsServer)에서만 실행되도록 설계하여 클라이언트의 변조(핵) 방지.

2 동기화 최적화 (Data Synchronization)<br>
Serialized Struct: INetworkSerializable을 상속받은 PlayerData 구조체를 통해 여러 스탯 데이터를 하나의 패킷으로 묶어 비트 단위로 효율적인 전송 수행.<br>
NetworkVariable: 델리게이트(OnValueChanged)를 활용하여 값이 변경될 때만 UI를 갱신하는 이벤트 기반 동기화 구현.<br>
FixedString: 동적 할당이 발생하는 string 대신 고정 크기인 FixedString32Bytes를 사용하여 가비지 컬렉션(GC) 발생 억제 및 대역폭 최적화.

3 네트워크 오브젝트 풀링 (Network Object Pooling)<br>
Custom Prefab Handler: INetworkPrefabInstanceHandler를 구현하여 NGO의 기본 생성/파괴 로직을 커스텀 풀링 로직으로 대체.<br>
Pre-warm Strategy: 게임 시작 시점에 인스턴스를 미리 생성하여 대규모 전투 시 발생하는 프레임 드랍 방지.<br>
Safe Return: Despawn(false)를 호출하여 오브젝트 파괴 없이 풀(Pool)로 반환 및 재사용 유도.

ClientNetworkTransform : 소유자 클라이언트 기반 위치 동기화(NetworkTransform 상속)
OwnerNetworkAnimator : 소유자 클라이언트 기반 애니메이션 동기화(NetworkAnimator 상속)
NetworkSkillPool : 네트워크 오브젝트 풀링 관리 및 핸들러 등록(INetworkPrefabInstanceHandler 상속)
PlayerData : 플레이어 상태 데이터 직렬화 및 패킷 최적화(INetworkSerializable 상속)
ServerSkill : 서버 중심의 투사체 로직 및 클라이언트 연출 지시