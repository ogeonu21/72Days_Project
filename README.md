# 72Days · Unity 프로젝트

노드 기반 스토리·전투·이벤트 게임 프로젝트입니다.

## 먼저 찾을 곳

| 하고 싶은 작업 | 안내 |
| --- | --- |
| 폴더와 주요 코드 위치 파악 | [프로젝트 구조 안내](Docs/프로젝트-구조.md) |
| 기능별 사용법·변경 기록 찾기 | [문서 목록](Docs/README.md) |
| 게임 로직 수정 | `Assets/Script/Main_System/` |
| 화면·버튼·아이콘 수정 | `Assets/Script/UI_System/` |
| 데이터 구조 수정 | `Assets/Script/Data_System/` |
| 구글 시트 가져오기 수정 | `Assets/Editor/Importing/DataImporter.cs`, `DataImporter.Events.cs` |
| 씬 열기 | `Assets/Scenes/MainWindow.unity` |

## 폴더 한눈에 보기

```text
war_LIke2/
├─ Assets/           게임 코드·씬·에셋·외부 플러그인
├─ Docs/             한글 사용법·설계·변경 기록
├─ Tools/            인코딩 검사·Unity 연결 보조 도구
├─ Packages/         Unity 패키지 구성
├─ ProjectSettings/  프로젝트 설정
├─ .github/          저장소 자동화 구성
├─ Builds/           로컬 빌드 결과
└─ Library, Temp, Logs, UserSettings/  Unity 생성·로컬 파일
```

## 데이터 작업

- Unity 버전은 `ProjectSettings/ProjectVersion.txt`를 기준으로 맞춥니다.
- 원격 데이터 검사: Unity 메뉴 `Tools > Validate Remote Game Data (No Changes)`.
- 데이터 반영: `Tools > Update All Game Data`. 미리보기를 확인한 뒤 적용합니다.
- 콘텐츠 시트: NodeData, ItemData, StatusData, EventData, EventChoiceData, RewardData.
- 기능별 검사 도구는 `Assets/Editor/Validation/`에서 확인할 수 있습니다.

## 정리할 때 주의

- 에셋은 `.meta`와 함께 관리하고, 이동할 때는 Unity Project 창 또는 AssetDatabase를 사용합니다.
- `Resources` 하위 경로는 로드 코드·가져오기 코드와 연결되므로 임의로 변경하지 않습니다.
- 외부 플러그인과 자동 생성 폴더는 직접 작성한 게임 코드와 구분합니다.
- `Keystore/`는 로컬 서명 자료입니다. 공유하거나 Git에 추가하지 않습니다.
- 상세 변경 이력은 [문서 목록](Docs/README.md)에 정리되어 있습니다.
