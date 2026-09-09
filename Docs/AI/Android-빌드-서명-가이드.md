# Android 개발 빌드와 릴리스 서명

## 개발 빌드

현재 브랜치는 커스텀 키스토어 사용을 끈 상태다. Unity 기본 개발 서명으로 APK 검증을 수행하며, 회전한 릴리스 키나 비밀번호가 필요하지 않다.

Unity 2021.3.45f2의 Android Build Support, SDK/NDK Tools, OpenJDK를 설치한다.
Build Settings에서 Android와 Development Build를 선택하고, 활성 씬 3개(MainWindow, GameWindow, EndingCredit)를 유지한다.
출력은 `Builds/` 하위에 둔다. 이 디렉터리는 Git에서 제외되어 있다.

## 릴리스 서명

릴리스 키스토어는 프로젝트 외부의 접근 제한된 디렉터리 또는 CI 비밀 파일 저장소에 둔다.
비밀번호는 문서, 명령 인자, 프로젝트 설정 파일, 일반 로그에 넣지 않는다.

로컬 빌드에서는 Unity Publishing Settings에 일시적으로 키스토어를 지정한다.
빌드 뒤 커스텀 키스토어 사용을 끄고, ProjectSettings의 변경을 검토하여 개인 경로와 서명 설정을 커밋하지 않는다.

CI에서는 빌드 프로세스 안에서만 아래 값을 비밀 환경 변수로 전달한다. 값을 출력하지 않는다.

- `ANDROID_KEYSTORE_PATH`: CI가 다운로드한 비밀 파일 경로.
- `ANDROID_KEYSTORE_PASSWORD`: 키스토어 암호.
- `ANDROID_KEY_ALIAS`: 업로드 키 별칭.
- `ANDROID_KEY_PASSWORD`: 별칭 암호.

향후 CI 빌드 진입점은 이 값을 읽어 `PlayerSettings.Android`의 서명 속성에 일시 적용하고, `finally`에서 기존 설정으로 복구해야 한다. 이 문서는 운영 규약이며 CI 워크플로를 실제 구축하거나 릴리스 서명을 검증했다는 의미는 아니다.

## 이력 정리 후 협업

사용자가 키 회전과 일반 브랜치 이력 재작성을 수행했다. 과거 클론의 커밋을 다시 병합하면 민감 파일이 재유입될 수 있으므로 새 이력을 기준으로 작업한다.
GitHub의 숨겨진 PR 참조와 캐시 제거 여부는 별도로 확인해야 한다. 일반 브랜치 강제 푸시 성공만으로 모든 복사본에서 제거되었다고 판단하지 않는다.

## 검증 범위

- 개발 APK 성공과 Play 배포/릴리스 서명 성공은 별개다.
- Google Play Games 로그인은 기기·앱 등록·인증서 설정이 필요한 별도 검증이다.
- 의존성 다운로드 실패 시 실제 Gradle 오류를 보존하고 원인을 확인한다. 검증을 통과시키려고 플러그인이나 씬을 임의로 제외하지 않는다.
