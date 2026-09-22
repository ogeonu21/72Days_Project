# StatsChanged 기반 UI 자동 갱신

작성일: 2026-09-22

## 변경 사항

- Character의 스탯 변경 통지 시 HP UI를 먼저 갱신한다. 피해·회복·HP 복원에도 실제 값이 달라지면 StatsChanged를 발행한다.
- 최대 HP 감소 시 현재 HP를 상한으로 제한한다. 장비 변경으로 최대 HP가 증가해도 자동 회복하지 않는 기존 정책은 유지한다.
- UIManager는 현재 플레이어의 StatsChanged를 직접 구독하여 일반 능력치 UI를 갱신한다. 이전 전역 PlayerEvent는 다른 소비자와의 호환성을 위해 유지한다.
- CombatUIController는 플레이어와 적 양쪽의 StatsChanged를 구독하여 예상 피해량과 명중률 표시를 즉시 갱신한다. 기존 턴 갱신 경로도 유지한다.
- UI 활성화 시 현재 캐릭터를 연결하고 즉시 표시한다. 캐릭터 교체 및 UI 비활성화 시 기존 구독을 해제한다.
- 씬·프리팹의 필드 이름과 참조는 변경하지 않았다. 기존 InventoryUI의 StatsChanged 구독도 그대로 활용한다.

## 검증

Unity 2021.3.45f2의 Edit Mode에서 컴파일 및 임시 오브젝트 기반 검사를 실행했다.

- CharacterStatsUIValidation: 11개 통과 (공격력·적 회피율·일반 스탯·HP 표시, 피해/회복 통지, 무변경 통지 억제, 캐릭터 교체, 구독 해제, HP 상한).
- CharacterRefactorValidation: 26개 통과.
- ItemSaveValidation: 51개 통과. 임시 파일 왕복만 수행하며 실제 세이브는 변경하지 않았다.
- 이번 변경에 대한 Play Mode 실게임 조작 및 플랫폼 빌드는 실행하지 않았다.

## 사용 시 주의

baseStats 또는 tuningStats 필드를 직접 수정하는 코드에서는 수정 후 UpdateStats를 호출해야 한다. 필드 대입 자체는 이벤트를 발행하지 않는다. 장비·상태 효과의 기존 UpdateTuningStats 경로는 그대로 사용할 수 있다.

사용자가 수정 중이던 Inventory Dynamic SDF.asset은 변경 대상에서 제외했다.
