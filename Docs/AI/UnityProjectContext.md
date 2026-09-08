# Unity 프로젝트 컨텍스트

<!-- unity-onboarding:generated:start -->

## 프로젝트 개요

- 루트: `C:\Users\monke\Desktop\war_LIke2`
- 제품: **Days** (`war_LIke`). 노드 기반 생존·전투 싱글플레이 2D 게임.
- 분석일: 2026-09-08
- 기준 커밋: `91eb3d9aeaebf292be630e198d648bdb90dcaf05` — 골드 드랍 기능 추가

## 환경

- Unity 2021.3.45f2 LTS, Built-in Render Pipeline, Legacy Input Manager.
- Android 주 대상: IL2CPP, ARMv7 + ARM64, 최소 SDK 22.
- 주요 패키지: 2D 기능 묶음, uGUI, TextMeshPro, Timeline, Visual Scripting, Google Play Games, Unity MCP v10.
- 테스트 프레임워크는 설치됐지만 1차 테스트 코드/테스트 asmdef/CI는 없다.

## 폴더 구조

| 경로 | 책임 |
| --- | --- |
| `Assets/Script/Data_System` | 캐릭터·아이템·이벤트·노드 도메인 데이터 |
| `Assets/Script/Main_System` | 전투, 인벤토리, 노드 진행, 캐릭터 생성, 보상 |
| `Assets/Script/UI_System` | 화면 전환과 노드 유형별 UI |
| `Assets/Script/Func_System` | 이벤트, 저장, 화폐, 효과, 유틸, Google 연동 |
| `Assets/Resources` | 런타임 로드 ScriptableObject와 CSV/XLSX 원본 |
| `Assets/Editor` | 시트 데이터 임포터와 커스텀 인스펙터 |
| `Assets/Scenes` | `MainWindow`, `GameWindow`, `EndingCredit` |

## 씬과 실행 흐름

`MainWindow` → `GameWindow` → `EndingCredit` 순으로 빌드에 포함된다. `StartManager`가 새 게임/불러오기를 `GameManager`에 전달하고, `GameManager`는 세션을 생성 또는 복원한 뒤 시작 노드로 진입시킨다. `NodeManager`가 노드 변경을 발행하면 `UIManager`가 스토리·전투·이벤트·엔딩 UI를 선택한다.

## 설계 방식

| 패턴 | 내용 |
| --- | --- |
| 전역 매니저 | `SingleTon<T>` 기반 서비스 로케이터형 매니저 |
| 콘텐츠 | 노드·적·아이템·이벤트는 ScriptableObject |
| 이벤트 | 정적 이벤트로 UI, 노드, 저장, 화폐, 보상 연결 |
| 씬 조립 | 씬 GameObject와 Inspector 연결에 의존 |
| 전투 | 코루틴 기반 교대 턴, 신체 부위별 명중/효과 |

## 코드 관례와 제약

- 1차 코드에 네임스페이스가 없고, public 데이터 필드와 `[SerializeField] private` 필드가 혼용된다.
- 코루틴이 비동기 흐름의 표준이다. 한글 주석이 많으므로 UTF-8 인코딩을 유지한다.
- 기존 미커밋 변경이 Android 플러그인/Gradle, 캐릭터 코드, 패키지, 프로젝트 설정에 존재한다. 수정 또는 빌드 전 분리 검토가 필요하다.
- `Keystore/` 및 프로젝트 설정에 민감한 값이 있다. 값은 문서·로그·커밋에 노출하지 않는다.
- `Tools/Update All Game Data`는 Resources 자산을 교체·삭제·연결할 수 있으므로 실행 전 백업/커밋이 필요하다.

## 미확인 사항

- Play Mode 실제 진행, 전체 Inspector 연결, Android 빌드/성능, Google Play 로그인은 별도 검증이 필요하다.
- 저장은 Unity 객체 참조와 `ItemData`를 포함한다. 새 게임 → 저장 → 불러오기에서 인벤토리/장비까지 검증해야 한다.

<!-- unity-onboarding:generated:end -->
