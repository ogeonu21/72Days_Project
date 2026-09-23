# 이벤트 시트 작성 및 가져오기

작성일: 2026-09-23

## 현재 연결

- 원본: [72Days_DataSheet](https://docs.google.com/spreadsheets/d/1KIn585a8qHi8Jr7f1g4C7PrfJ8YO25Wd1qA-6irMKhs/edit)
- 기존 NodeData, ItemData, StatusData는 그대로 유지했다.
- 추가: EventData, EventChoiceData, RewardData.
- 기존 Apps Script는 새 시트의 **3행을 헤더**, 4행부터 데이터로 읽는다. 1행은 한글 안내, 2행은 표 도구 표시 공간이다. 헤더 행을 이동하지 않는다.
- 기존 Apps Script 주소에서 신규 시트 데이터가 반환되므로 재배포나 주소 변경은 필요하지 않다.
- 시트 편집만으로 실행 중인 게임이 갱신되지는 않는다. Unity에서 가져오기를 실행해야 한다.

## 가장 간단한 사용 예: 물약 상점

1. EventData의 `Demo_Shop` 행을 확인한다. 종류는 Shop, 떠나기 노드는 Main_03이다.
2. EventChoiceData에서 `Demo_Shop / buy_potion`을 확인한다. 100골드로 물약 2개를 구매하고 현재 이벤트에 남는 예제다.
3. RewardData의 `Demo_Potions`는 `Potion_01`을 2개, 확률 1로 지급한다.
4. 원하는 **EventNode 행**의 NodeData → EventDefinitionID에 `Demo_Shop`을 입력한다. 예를 들어 Event_01을 상점으로 바꾸려면 그 행의 해당 셀에 입력한다. 본문도 상점 설명으로 맞춘다.
5. Unity에서 `Tools > Validate Remote Game Data (No Changes)`로 검사한다. 이 메뉴는 에셋을 변경하지 않는다.
6. `Tools > Update All Game Data`를 실행하고 미리보기에서 적용한다.
7. `Assets/Resources/EventDefinitions/Demo_Shop.asset`이 자동 생성·갱신되고 해당 EventNode에 연결된다. Inspector에서 이벤트 에셋을 미리 만들 필요가 없다.

기존 노드 연결과 플레이 흐름은 이번 작업에서 바꾸지 않았다. 현재 예제는 시트에만 작성되어 있으며 실제 콘텐츠로 사용할 때 4~6번을 실행한다. 기존 Inspector에서 지정한 definition도 NodeData의 EventDefinitionID가 비어 있으면 전체 임포트 시 해제되므로, 유지하려면 반드시 시트에도 ID를 적는다.

## 시트 관계

`NodeData.EventDefinitionID → EventData.EventID → EventChoiceData.EventID`

`EventChoiceData.RewardID → RewardData.RewardID`

ID는 대소문자와 공백까지 동일하게 작성한다. 확장자 `.asset`을 붙이지 않는다. 새 EventID는 파일명으로 사용할 수 있는 이름이어야 하며 하위 폴더 경로를 지원하지 않는다. 기존 수동 에셋을 참조하는 NodeData의 하위 폴더 경로 호환은 유지한다.

### EventData — A3:D11

| 열 | 의미 |
| --- | --- |
| EventID | 이벤트 에셋 이름. 중복 금지 |
| Kind | Reward / Shop / Encounter / Quest |
| Title | 이벤트 UI 제목 |
| ExitNodeID | 떠나기 버튼의 노드 ID. 필수 |

### EventChoiceData — A3:N16

| 열 | 의미 |
| --- | --- |
| EventID | 부모 이벤트 ID |
| ChoiceID | 이벤트 안에서 고유한 선택지 ID. 저장 후 임의 변경 주의 |
| SortOrder | 표시 순서. 0 이상, 작은 값부터 |
| Text | 선택지 문구 |
| GoldCost | 지급 전 필요한 골드. 부족하면 실행하지 않음 |
| GoldLoss | 보유 골드 한도 내 강제 손실 |
| Repeatable | 체크하면 반복 실행. 해제하면 노드/선택지별 저장 게임당 1회 |
| RequiredQuestID | 먼저 수락해야 하는 퀘스트 ID. 없으면 빈 셀 |
| RequiredItemID | 소지 조건 아이템 ID. **소비하지 않음** |
| RequiredQuantity | 조건 아이템 수량. 조건이 없으면 0 |
| Action | None / AcceptQuest / CompleteQuest / Combat |
| QuestID | 수락·완료 대상 퀘스트 ID |
| NextNodeID | 성공 결과 확인 후 이동. 빈 셀이면 현재 이벤트 유지. Combat은 CombatNode 필수 |
| RewardID | 지급할 보상 묶음 ID. 보상이 없으면 빈 셀 |

### RewardData — A3:F14

| 열 | 의미 |
| --- | --- |
| RewardID | 보상 묶음. 여러 행에서 같은 값을 사용할 수 있음 |
| EntryID | 동일 보상 묶음 안에서 고유한 항목 ID |
| Kind | Item / Gold / Heal / Exp / STR / DEX / CON |
| ItemID | Item일 때 아이템 ID. 다른 종류에서는 빈 셀 |
| Amount | 지급 수량 또는 증가량. 양의 정수. 아이템은 최대 1000 |
| Probability | Item은 0~1. 다른 보상은 반드시 1 |

예: 같은 RewardID에 Gold/100과 Item/Potion_01/2를 두 행으로 작성하면 골드 100과 물약 2개를 함께 지급한다. GoldLoss는 보상이 아니므로 EventChoiceData에 작성한다.

## 입력된 예제

| EventID | 내용 |
| --- | --- |
| Demo_Training | STR/DEX/CON 각각 +1, 확정 지급 선택지 |
| Demo_Shop | 골드 100으로 물약 2개 반복 구매 |
| Demo_Encounter | 떠나기 또는 기존 Combat_02 진입 |
| Demo_Quest | 의뢰 수락, 물약 1개 소지 확인 후 골드 200·경험치 10 지급 |
| Demo_Heal | 체력 25 회복 |
| Demo_Exp | 경험치 10 지급 |
| Demo_Gold | 골드 100 획득 또는 최대 50 손실 |
| Demo_Item | 물약 2개 확정, 식칼 1개 50% 확률 |

임의 예제 수치이며 게임 밸런스 확정값은 아니다. 기존 ItemData의 Potion_01/Weapon_01과 NodeData의 Main_03/Combat_02를 참조한다. 전투 보상·성공·실패 분기는 기존 적/전투 노드 설정을 따른다. Combat_02의 실패 경로 Ending_01 등 기존 콘텐츠는 이번 작업에서 변경하지 않았다.

## 매핑 수정 사항

- StatusData.DodgeBonus: 정수에서 실수로 수정. 0.07 같은 회피율을 그대로 읽는다.
- ItemData.Durability: 액세서리에도 적용한다.
- StatusData.DropItemCategory: 별도 중복 저장하지 않고 DropItemID의 실제 종류와 일치하는지 검증한다.
- 빈 숫자 셀은 0, 빈 불리언 셀은 false. 문자열 TRUE/FALSE도 처리한다.
- 잘못된 숫자, 정수 열의 소수, NaN/Infinity, 누락된 필수 열은 임포트 전에 거부한다.
- 문구의 `\n` 및 여러 역슬래시 뒤의 n을 실제 줄바꿈으로 변환한다. 시트 원문은 보존한다.
- Potion만 Consumable=true인지 검사한다. 인벤토리 수량은 런타임/세이브 데이터이므로 Item SO에 덮어쓰지 않는다.
- ItemIcon 주소는 그대로 사용하며 빈 셀은 기존 Inspector 주소를 유지한다.
- 이벤트 정의·선택지·복합 보상을 시트에서 생성한다. 선택지는 SortOrder로 정렬한다.
- 전체 검증 후 아이템 → 적 → 노드 생성 → 이벤트 정의 생성 → 노드 참조 연결 순서로 적용한다.
- 재임포트는 기존 에셋을 갱신하여 GUID를 유지한다. 시트에 없는 에셋은 삭제하지 않는다.
- 실패 시 Undo로 변경을 되돌리고 그 실행에서 새로 만든 에셋 파일만 정리한다.

## 현재 지원하지 않는 기능

- 아이템 제출 시 수량 차감, 퀘스트 처치/탐험 목표, 상점 판매·재고.
- 스탯·골드·경험치 보상의 확률 판정. 현재 확률은 아이템 항목에만 적용한다.
- NodeData의 기존 Event_01 설명에 있는 50% 스탯 상승은 구현된 동작과 다르다. 새 훈련 예제는 확정 상승으로 명시했다.
- 기존 조우 설명의 통조림 차감/보상과 실제 동작 불일치는 콘텐츠·런타임 기능 문제이며, 시트 매핑만으로 구현되지 않는다.

## 검증

- 원격 여섯 시트를 기존 Apps Script에서 읽어 전체 연결 검증: 아이템 9, 적 6, 노드 9, 이벤트 8, 선택지 13, 보상 행 11.
- 신규 임포터 33개 검사 통과. 임시 에셋으로 가져오기, 재가져오기, GUID 보존, 모든 아이템 종류 및 이벤트 복합 보상 매핑과 검사 파일 정리를 Edit Mode에서 확인했다.
- 기존 이벤트 14개, 캐릭터/저장 26개, 아이템/저장 51개 검사 통과.
- 실제 콘텐츠의 전체 재임포트 및 Play Mode 검증은 실행하지 않았다. 실제 세이브는 변경하지 않았다.
