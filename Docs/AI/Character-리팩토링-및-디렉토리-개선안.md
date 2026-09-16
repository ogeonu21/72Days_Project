# Character 리팩토링 및 디렉토리 개선안

작성: 2026-09-16. 이번 변경은 캐릭터 상태 소유권과 초기화 책임을 정리한다. 전체 프로젝트의 아키텍처 재작성이나 성능 최적화 완료를 의미하지 않는다.

## 1. 반영한 변경

| 대상 | 책임/변경 |
| --- | --- |
| EnemyData | 기존 EnemyDefinition의 이름 변경. 적 원본 설정 ScriptableObject이며 런타임 상태는 저장하지 않는다. |
| Enemy | EnemyData로 초기화. 이전 적의 드롭·상태 효과를 제거하고, 원본 보정치와 부위 확률을 기준으로 재계산한다. |
| Character | 공통 초기화·체력·상태 효과 처리. 스탯 변경은 인스턴스 이벤트로 알린다. |
| Player | 성향(tendency), 경험치, 장비, 플레이어 전용 이벤트 소유. 새 게임/불러오기 초기화 중복을 제거했다. |
| PlayerData / PlayerSaveData | 성향 스냅샷 전달 및 저장. PlayerSaveData에는 Unity 에셋 참조를 추가하지 않았다. |
| GameManager | 성향 필드·증감·리셋 제거. 씬 흐름과 세션 조립만 유지한다. |
| SaveManager | Player.GetCurrentData()에서 성향을 저장하고 PlayerData에 복원한다. |
| MeetingEvent | 기존 성향 변화 방향(+1/-1)을 유지하며 Player.ChangeTendency를 호출한다. |

EnemyData.cs.meta의 GUID는 `de7a43d0e487ed54e8427f341b11fc4c`로 유지했다. 적 데이터 `.asset`, 전투 노드, 프리팹의 GUID 참조를 재생성하지 않는다. 임포터와 콘텐츠 검사기의 타입 검색도 EnemyData로 변경했다.

## 2. 저장 호환성

- 사용자가 변경한 버전 필드의 `float` 타입은 유지하고 새 저장 버전을 `2.0f`로 올렸다.
- 새 저장 위치: `player.tendency`. 루트 성향 필드는 새 저장에 쓰지 않는다.
- v1의 루트 `goodAndEvil`, 중간 변경본의 루트 `tendency`는 v2 플레이어 성향으로 이전한다.
- 두 구 필드에 서로 다른 0이 아닌 값이 있으면 충돌로 거부한다. 하나만 0이 아니면 해당 값을 사용하고, 둘 다 0이면 0을 사용한다.
- 버전 누락·미지원 버전·손상 JSON·플레이어 ID 누락은 실패로 반환한다.
- 이전은 메모리에서 수행하며, 다음 정상 저장 때 v2로 기록한다. 검증 메뉴는 실제 savedata.json을 읽거나 쓰지 않는다.
- v2 파일은 이전 실행 파일에서 읽을 수 없다. Base64 구형 형식의 변환·백업 복구는 이번 범위에 포함하지 않았다.

## 3. 함께 보완한 동작

- 음수/0 피해를 거부하며 무효 피해의 UI 이벤트도 보내지 않는다.
- 체력을 0~최대 체력 범위로 제한한다. 최대 체력이 감소하면 현재 체력도 새 상한을 넘지 않는다.
- UI 참조가 없는 캐릭터도 데이터/전투 로직을 실행할 수 있다.
- Enemy의 스탯 변경이 플레이어 전역 스탯 이벤트를 발행하지 않는다.
- Enemy의 상태 효과 해제 시 원래 공격/회피 보정을 복원한다.
- Enemy를 반복 초기화해도 부위별 피격 확률이 누적 곱셈되지 않는다.
- Player 스냅샷에 성향과 장비 정보를 담고, 불러오기 후 장비를 중복 적용하지 않는다.
- 장비 컨테이너 null, 파손 장비, 카테고리/구체 타입 불일치를 방어한다.
- 큰 경험치 보상에서 필요한 레벨업을 반복 처리한다. 기존 레벨업 이벤트와 경험치 공식은 유지한다.

## 4. 권장 디렉토리 구조 (제안, 일괄 이동하지 않음)

현재 `Data_System/Data/Character`에 런타임 MonoBehaviour와 데이터가 섞여 있고, `Func_System`에 저장·경제·효과가 함께 있어 변경 위치를 찾기 어렵다. 기능 중심으로 모으되 각 기능 내부에서 런타임/데이터를 구분하는 구성을 권한다.

```text
Assets/
  Scripts/
    Runtime/
      App/                 # GameManager, 초기화/씬 흐름
      Characters/
        Core/              # Character, 스탯 값 타입
        Player/            # Player, PlayerData, EquipmentData
        Enemy/             # Enemy, EnemyData
      Combat/              # 계산 규칙, 전투 진행, 보상
      Progression/         # NodeRunner, 노드·선택·이벤트
      Inventory/           # 인벤토리와 아이템
      Economy/             # CurrencyData/Manager/ICurrency
      Persistence/         # 저장 DTO, 버전 이전, 파일 입출력
      UI/                  # 라우터, HUD, 선택지, 안전 영역, 문자 출력
      Common/              # 실제로 여러 기능이 공유하는 소수 도구
    Editor/
      Importing/           # 시트 임포터
      Validation/          # 콘텐츠·캐릭터 검증
      Inspectors/          # 커스텀 인스펙터
    Tests/
      EditMode/            # 저장/전투 규칙/스탯
      PlayMode/            # 씬 흐름/장비/이벤트 구독
  Resources/               # 현재 로드 경로는 유지
```

### 권장 적용 순서

1. 이번 변경을 검증한 뒤 독립 커밋으로 묶는다. 사용자가 이미 이동한 Currency/Function_Effect는 보존한다.
2. 소스 파일만 `.meta`와 함께 기능별로 이동한다. 이 단계에서는 네임스페이스와 클래스 이름을 동시에 바꾸지 않는다.
3. 각 기능 이동마다 컴파일, 직렬화 참조, 새 게임/불러오기 검사를 수행한다.
4. 의존성을 정리한 뒤 런타임·에디터·테스트 asmdef를 도입한다. 현재 Assembly-CSharp 타입을 테스트 asmdef에서 직접 참조할 수 없으므로 순서를 지킨다.
5. Resources 폴더/에셋 이름 변경은 저장 ID·로드 경로 이전을 설계한 별도 작업으로 진행한다.

### 추가로 권장하는 책임 분리

- Character의 UI 필드는 기존 프리팹 연결 보존을 위해 이번에는 유지했다. 후속으로 CharacterView를 붙이고 HP/이름/레벨 표시를 이벤트 구독으로 옮기되 프리팹 마이그레이션 검증을 병행한다.
- PlayerData와 EnemyData는 각각 직렬화 DTO와 ScriptableObject다. 이름이 비슷하다고 같은 기반 클래스로 묶을 필요는 없다.
- Stats.cs의 세 구조체 분리는 탐색 편의 개선이며 저장/초기화 문제보다 우선순위가 낮다.
- 장비 외 임시 버프와 장비 보정의 완전한 분리는 후속 과제다. 이번에는 유효한 기존 보정 계산 흐름을 최대한 유지했다.

## 5. 검증

재실행 메뉴: `Tools > Validation > Character and Save Refactor`.
임시 GameObject/ScriptableObject와 메모리 JSON만 사용하며 난수 상태를 복구한다.

- 성향 증감/저장 왕복, HP 보존, v1 필드 이전, 잘못된 저장 거부, 사망 통지, 적 보정/드롭/피격 확률 초기화, EnemyData GUID/에셋 로드를 검사한다.
- 1차 실행에서 null 플레이어 처리 누락을 발견해 필수 ID 검사로 보완했다.
- 최종 26개 검사 통과, 기존 콘텐츠 검사 오류 0건, Unity Console 오류 0건.
- Play Mode에서 자동 저장을 끄고 새 게임 진입: GameWindow, HP 30/30, 성향 초기값 0 확인. 성향을 -3으로 변경한 뒤 PlayerSaveData JSON 왕복에서 -3을 확인했다. Play Mode 종료 후 Console 오류 0건.
- Codex의 Unity 도구가 로드되지 않은 세션에서도 검증할 수 있도록 로컬 MCP 호출용 `Tools/Invoke-UnityMcp.ps1`을 추가했다. 기본 주소는 `http://127.0.0.1:8080/mcp`이며 에디터 연결이 필요하다.
- 실제 Android 빌드/기기 검증과 전체 전투 UI 회귀는 별도다.
