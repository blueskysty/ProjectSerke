# ProjectSerke

ScriptableObject 기반 스킬 시스템

이 프로젝트는 ScriptableObject 기반 스킬 데이터 관리,<br>
그리고 SkillManager를 통한 런타임 스킬 실행 및 쿨타임 처리,<br>
마지막으로 UI(쿨타임/스테이터스) 연동 예제가 포함된 범용 스킬 시스템입니다.

스킬 데이터는 별도의 ScriptableObject로 관리되기 때문에,<br>
코드를 수정하지 않고도 새로운 스킬을 손쉽게 추가할 수 있습니다.

Data & Event (ScriptableObject)<br>
SkillData : 스킬의 이름, 데미지, 설명 등 데이터를 관리합니다.<br>
SkillEventChannelSO : 시스템 전반의 신호 중계소입니다. 스킬 사용 성공/실패(SP 부족 등), 효과 메시지 출력 등을 이벤트를 통해 전파합니다.

Logic & Controller<br>
SkillManager : 플레이어의 스킬을 전체관리하는 매니저입니다.<br>
Skill : 개별 스킬의 실체화된 인스턴스입니다. SkillData를 참조하여 현재 쿨타임 상태, 사용 가능 여부 등 '상태 데이터'를 관리합니다.
Player_Skill : 간단한 플레이어 스크립트입니다.

UI System<br>
UI_Status : 플레이어의 스태이터스와 스킬 메세지를 표시합니다.<br>
UI_SkillIcon : 스킬 쿨타임일 경우 쿨타임 이미지는 표시합니다.
