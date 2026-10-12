# EventNode 개편 및 스프레드시트 연동

> 2026-09-23 업데이트: EventDefinition도 시트에서 자동 생성·갱신할 수 있도록 확장했다. 아래 내용은 최초 구현 기록이며, 현재 사용법과 신규 시트 구조는 [EventData 시트 작성 가이드](EventData-시트작성-가이드.md)를 따른다.

## 적용 범위

기존 EventNode / BaseEvent / Choice를 삭제하지 않고 새 EventDefinition 연결 방식을 추가했다. 기존 Node SO·Item SO·Character SO GUID는 유지하고 기존 에셋에 대한 일괄 임포트는 실행하지 않았다.

- ItemData 시트: 변경 없음.
- StatusData 시트: 변경 없음.
- NodeData 시트: 선택 열 `EventDefinitionID` 추가.
- EventNode SO: `EventDefinition definition` 참조만 추가.
- 아이템/캐릭터 SO 필드: 변경 없음.

## NodeData 시트 변경 방법

현재 Apps Script가 반환하는 NodeData 행 JSON에 `EventDefinitionID` 문자열이 포함되도록 한다. 헤더를 동적으로 읽는 Apps Script라면 열 추가만으로 충분할 수 있지만 실제 Apps Script 구현은 이번에 확인하지 않았다.

| NodeID | NodeType | EventDefinitionID | 기존 선택지 열 |
|---|---|---|---|
| Event_01 | EventNode | 빈 셀 | 기존 방식 유지 |
| Event_Shop01 | EventNode | Shop01 | 사용하지 않아도 됨 |
| Event_Heal01 | EventNode | Healing01 | 사용하지 않아도 됨 |

`EventDefinitionID = Shop01`은 `Assets/Resources/EventDefinitions/Shop01.asset`을 가리킨다. 확장자는 입력하지 않는다. 하위 폴더가 있다면 `Town/Shop01`처럼 입력한다.

새 열이 비어 있으면 임포터는 definition 참조를 null로 설정하고 기존 EventCategory + Choice1~3_EventName 연결을 사용한다. Inspector에서 수동 연결한 definition도 빈 셀로 재임포트하면 해제되므로 반드시 시트에 ID를 기록한다.

새 ID가 있으면 기존 선택지 검사 대신 EventDefinition의 ID 중복·종류·보상·필수 전투 연결 등을 검사한다. 연결할 EventDefinition 에셋은 임포트 전에 만들어져 있어야 한다. 기존 NodeID/NodeType/WorldLocation 등 공통 필수 열은 그대로 필요하다.

실제 `Tools/Update All Game Data` 실행은 사용자 확인 후 진행한다. 이번 작업에서는 원격 시트를 읽거나 덮어쓰지 않았다.

## 이벤트 콘텐츠 작성

Project 창에서 `Create > Events > Event Definition`으로 생성하고 `Assets/Resources/EventDefinitions` 아래에 저장한다. 에셋 이름과 시트 ID를 일치시킨다.

이번 버전은 시트 호환과 초기 편집 편의성을 위해 이벤트별 SO 하위 클래스 대신 하나의 EventDefinition에 종류와 선택지를 둔다. 이벤트 정의의 세부 내용은 **Inspector에서 관리하며 아직 별도 EventData 시트로 임포트하지 않는다.**

### EventDefinition

- Kind: Reward / Shop / Encounter / Quest.
- Title: 전용 패널 제목.
- Exit Node: 떠나기 버튼의 다음 노드. 재방문에서도 탈출할 수 있도록 필수.
- Options: 가변 개수 선택지. UI는 스크롤 목록으로 표시.

### EventOption

- Id: 에셋 내 고유하고 안정적인 선택지 ID. 수령 상태 저장 키에 쓰므로 출시 후 변경에 주의.
- Text: 버튼 문구.
- Gold Cost: 잔액 부족 시 실행하지 않는 비용.
- Gold Loss: 보유액까지 감소하는 강제 손실. Cost와 별도.
- Repeatable: true면 반복 구매 가능. false면 `노드 에셋 이름/선택지 ID` 단위로 해당 저장 게임에서 한 번만 실행.
- Required Quest: 이미 수락했어야 하는 퀘스트 ID.
- Required Item / Required Quantity: 소지 수량 조건. 아이템을 소모하는 제출 기능은 아님.
- Action: None / AcceptQuest / CompleteQuest / Combat.
- Quest Id: 수락·완료 대상. 퀘스트 수락·완료 선택지는 Repeatable=false.
- Reward: 공통 보상.
- Next Node: 성공 결과 확인 후 이동. 비어 있으면 현재 이벤트 선택 목록으로 복귀. Combat 동작이면 CombatNode 필수.

### 작성 예시

| 콘텐츠 | 권장 설정 |
|---|---|
| 물약 상점 | Kind=Shop, GoldCost=30, Repeatable=true, Reward.Items에 물약 Quantity=2 / Probability=1, NextNode 비움 |
| 치료 | Kind=Reward 또는 Shop, Reward.HpHeal=30, 필요하면 GoldCost 지정 |
| 훈련 | Reward.StatIncrease의 STR/DEX/CON 증가량 설정 |
| 골드 획득 | Reward.DropGold=100 |
| 골드 손실 | GoldLoss=50, Reward 기본값 |
| 경험치 보상 | Reward.Exp 설정 |
| 조우 전투 | Kind=Encounter, Action=Combat, NextNode에 기존 CombatNode 연결 |
| 퀘스트 수락 | Kind=Quest, Action=AcceptQuest, QuestId=Q01 |
| 퀘스트 완료 | Action=CompleteQuest, QuestId=Q01, RequiredItem/Quantity로 보유 조건 설정, Reward 지정 |

새 조우 경로는 공유 CombatNode.successNode를 수정하지 않는다. 성공·실패 경로는 기존 CombatNode와 NodeData의 SuccessNode/FailureNode로 작성한다. 기존 MeetingEvent와 StatUpdateEvent는 호환 경로로 유지한다.

## Reward 확장과 지급 정책

기존 Reward 생성자와 dropItem/itemDropRate/dropGold/hpHeal/exp 필드를 유지했다. `[Serializable]`, `statIncrease`, `List<ItemReward> items`를 추가했다.

- ItemReward: item, quantity(1~1000), probability(0~1).
- 확률은 해당 항목 전체 수량에 한 번 적용한다. 개별 아이템마다 재추첨하지 않는다.
- 기존 dropItem과 새 items가 둘 다 설정되면 둘 다 보상에 포함된다.
- RewardService가 지급하고 RewardResult가 실제 회복량·지급 목록·결과 문구를 반환한다.
- 장비 보상도 런타임 복제하여 원본 SO 내구도를 직접 소모하지 않도록 한다.
- 새 이벤트는 최대 가능 아이템 전부의 공간을 먼저 검사한다. 공간 부족 시 추첨·결제·지급 모두 하지 않는다. 실패 후 재시도로 확률만 다시 굴리는 것을 막는 보수적 정책이다.
- 기존 전투 보상은 가방이 가득 차도 경험치·골드를 지급하고 아이템 실패는 안내하는 정책을 유지한다. 보류 아이템 우편함은 아직 없다.
- 비용·공간 부족과 같은 정상 실패는 사전 검사하지만 외부 이벤트 핸들러 예외까지 전체 롤백하는 트랜잭션 시스템은 아니다.

## UI

기존 EventUIController의 본문 타이핑 완료 후 새 EventUIRouter로 분기한다. 정의가 없는 노드는 기존 선택지 UI를 사용한다.

새 Router는 Reward/Shop/Encounter/Quest별로 별도 런타임 패널을 생성·관리한다. 기존 선택지 버튼 스타일과 본문 폰트를 재사용하며, 현재는 공통 목록 레이아웃이다. 상점 가격 표시, 결과 확인, 명시적 떠나기 버튼을 포함한다. 전용 이미지 중심 상점·퀘스트 목표 진행바 등의 개별 디자인은 후속 작업이다.

## 저장 호환

SaveGameData v4에 eventProgress를 추가했다. claimed, acceptedQuests, completedQuests를 저장한다. v1~v3를 읽을 수 있고 v3 인벤토리 수량은 유지한다. 새 게임은 진행 상태를 초기화한다. 저장 파일을 구버전 프로그램으로 다시 여는 것은 지원하지 않는다.

보상 지급 후 수령 상태와 함께 기존 저장 이벤트를 호출한다. 실제 디스크 저장 실패는 기존 SaveManager 오류 처리에 따르며 UI가 저장 성공까지 보증하는 구조는 아니다.

## 검증 / 남은 범위

- Unity 2021.3.45f2 컴파일 확인.
- EventSystemValidation 14개, Character/Save 26개, Item/Save 51개 Edit Mode 검사 통과.
- 첫 신규 검사에는 플레이어 ID가 없는 잘못된 테스트 저장 데이터가 있어 실패했고, 실제 PlayerData 기반 테스트 데이터로 수정 후 통과했다.
- 외부 시트 임포트, 실제 세이브 쓰기, 기존 SO 에셋 재생성, Play Mode 실행은 하지 않았다.
- 신규 이벤트를 실제 게임에 배치하려면 정의 에셋 작성 및 시트 EventDefinitionID 설정이 필요하다. 기존 콘텐츠를 임의로 새 이벤트로 교체하지 않았다.
- 퀘스트 처치/탐험 카운터, 아이템 제출 소비, 상점 재고·판매, 보상 보류함, 방문 단위 반복 보상 및 실제 UI 클릭 검증은 후속 범위다.
- 앞서 논의한 전투 라운드 CountEffect 순서 변경은 이번 이벤트 개편에 포함하지 않았다.
