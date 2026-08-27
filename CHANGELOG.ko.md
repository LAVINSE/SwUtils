# 변경 기록

[English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

이 프로젝트의 주요 변경사항을 기록합니다.

변경 기록은 버전과 날짜 기준으로 정리합니다. 최신 릴리즈는 Unity Package Manager 확인과 Git 태그 검증이 쉽도록 먼저 요약합니다.

| 최신 버전 | 날짜 | 핵심 내용 |
| --- | --- | --- |
| `v1.1.1` | 2026-08-27 | 플레이어 빌드 호환성 보강과 README 전면 개선 |

## [미배포]

현재 미배포 변경사항이 없습니다.

## [v1.1.1] - 2026-08-27

### 변경
- 영문·한글 README를 기능별 사용법, 코드 예제, 화면 자료와 빠른 탐색 링크 중심으로 전면 개선했습니다.
- 패키지 의존성에 Unity Physics와 Physics 2D 모듈을 명시했습니다.
- 패키지 버전, README 배지와 설치 주소를 `v1.1.1`로 변경했습니다.

### 수정
- `SWIODatabase`가 플레이어 빌드에서 Unity Editor API를 참조해 빌드에 실패하던 문제를 수정했습니다.
- `SWVibration`이 지원하지 않는 플랫폼에서 `Handheld.Vibrate`를 호출하지 않도록 플랫폼 조건을 보강했습니다.

## [v1.1.0] - 2026-07-22

### 추가
- 다중 계층, 조건 전이, 명령 전이, 모든 상태 전이와 상태 메시지를 지원하는 `SWStateMachine<TContext>`를 추가했습니다.
- Unity 갱신 생명주기와 연결하는 `SWMonoStateMachine<TContext>`와 다중 계층 사용 예제를 추가했습니다.
- 상태 쌓기, 제거, 교체, 전체 비우기와 일시 정지·복귀 생명주기를 지원하는 `SWStackStateMachine<TContext>`를 추가했습니다.
- Unity 실행기와 게임 진행, 일시정지, 설정 화면 흐름을 보여주는 스택 상태 머신 예제를 추가했습니다.
- Unity 6 `GraphView` 기반 상태 머신 그래프 에셋과 노드 편집기를 추가했습니다.
- 상태 머신 그래프 편집기를 Shader Graph 스타일의 전체 작업 영역, 떠 있는 정보·인스펙터 패널, 검색 기반 노드 생성과 전이 요약으로 재설계했습니다.
- 편집기 설정 탭에 패널·전이 요약 표시, 공통 크기와 격자 맞춤 설정을 추가하고 노드 생성 좌표 문제를 수정했습니다.
- Blackboard 상태·전이 목록, 자유로운 패널 크기 조절과 펼침형 Graph Validation 목록을 추가했습니다.
- 노드 이동 시 전이 요약 위치가 어긋나는 문제와 같은 이름의 중첩 상태 타입이 중복처럼 보이는 문제를 수정하고 Any State·Return State 표기를 적용했습니다.
- `Alt` 전이 요약 배치, `In`·`Out` 포트 표기와 `Layer` 용어를 적용했습니다.
- `Graph Type`을 Blackboard로 옮기고 검색 및 접기 가능한 상태·전이 데이터 목록을 추가했습니다.
- StateMachine 그래프에 노드 설명, 프로젝트 에셋 전환, 그래프 간 복사·붙여넣기·복제와 탐색 단축키를 추가했습니다.
- 그래프 팩터리 실행 인스턴스의 활성 상태, 실행 시간과 최근 전이를 표시하는 Play Mode Runtime Inspector를 추가했습니다.
- Any State·Return State 중복과 진입할 수 없는 상태에 대한 그래프 검증을 보강했습니다.
- 상태 노드 생성·삭제·이동, 포트 연결, 계층과 초기 상태, 명령과 우선순위, 스택 연결 동작 편집 및 그래프 검사를 추가했습니다.
- 그래프 에셋에서 다중 계층 상태 머신과 스택 상태 머신 제어기를 생성하는 런타임 팩터리와 코드 기반 그래프 조건을 추가했습니다.
- Action·Composite·Decorator 노드, Blackboard, 중단 전파와 런타임 복제를 지원하는 Behaviour Tree를 추가했습니다.
- Unity 6 GraphView 기반 Behaviour Tree 편집기, 검색 생성, Node Inspector, Root 지정, 그래프 검증과 Play Mode 실행 상태 표시를 추가했습니다.
- Behaviour Tree 런타임 네임스페이스를 `SW.BehaviourTree`로 구성했습니다.
- Behaviour Tree에 SubTree, Timeout, Succeeder, Failure, Breakpoint, Random Failure, Set Integer 노드와 `Aborted` 상태를 추가했습니다.
- 고정값과 Blackboard Key를 전환하는 `SWBehaviourNodeProperty<T>`, 사용자 Blackboard Key, Runner별 Blackboard Override를 추가했습니다.
- Behaviour Tree 그래프에 복사·붙여넣기·복제, SubTree 선택, 키보드 탐색, 자동 배치, 에셋 전환과 노드 스크립트 생성을 추가했습니다.
- Behaviour Tree 편집기 프로젝트 설정, 패널 크기와 그래프 화면 저장, 실행 노드와 활성 연결 경로 표시를 추가했습니다.
- 범용 `Set Property`·`Compare Property`, 외부 Blackboard 접근과 캐시 Key API, 사용자 정의 Key Override를 추가했습니다.
- Behaviour Tree와 State Machine에 수정 가능한 스크립트 템플릿과 최초 실행 에셋 선택 화면을 추가했습니다.
- State Machine 그래프에 Layer 기반 자동 배치, 에셋별 화면 복원과 Project Settings 설정 화면을 추가했습니다.
- Unity 6에서 빈 그래프 더블 클릭과 Rectangle Selector가 충돌해 선택 사각형 정리 예외가 발생하는 문제를 수정했습니다.
- 이전 직렬화 데이터의 null Blackboard 항목 때문에 Behaviour 노드 선택·삭제 시 예외가 발생하는 문제를 수정했습니다.
- State Machine과 Behaviour Tree에 검색·생성·삭제·선택 및 왼쪽 접기를 지원하는 공통 Graph List 패널을 추가했습니다.
- 두 그래프 편집기의 상단 도구 모음 크기와 디자인을 통일하고 불필요한 Graph 선택, Open, New Graph 입력을 제거했습니다.
- Behaviour Tree에 Runner 선택, 실행 노드 상태, Blackboard 값을 확인하는 Runtime Debug 탭과 하단 Console 형식의 검증 목록을 추가했습니다.
- `SWStateMachineNodeCategory`와 `SWBehaviourNodeCategory`를 추가해 슬래시 경로 기반 사용자 정의 노드 카테고리를 지원합니다.
- State Machine, Stack State Machine과 Behaviour Tree 타입을 한 파일에서 관리하는 `SWGraphAssetsExample` 통합 예제를 추가했습니다.
- 실행 가능한 Layered, Stack, Behaviour Tree 예제 그래프 에셋을 추가했습니다.
- 조립체 이름이 변경된 그래프 타입도 전체 타입 이름으로 다시 찾는 `SWStateMachineGraphTypeResolver`를 추가했습니다.

### 변경
- 패키지 최소 지원 버전을 Unity 6.0으로 변경했습니다.
- State Machine과 Behaviour Tree의 편집기 구조, Graph List, Inspector, Runtime Debug와 도구 모음 디자인을 통일했습니다.
- 최종 그래프 편집기에서 사용하지 않는 미니맵과 State Machine의 `Offline` 표시를 제거했습니다.
- 중복된 개별 상태 머신 및 Behaviour Tree 예제를 `SWGraphAssetsExample` 하나로 통합했습니다.
- 패키지 메타데이터와 README 설치 주소를 `v1.1.0`으로 변경했습니다.

### 수정
- Unity UI Toolkit에서 지원하지 않는 `:first-child` 선택자를 클래스 선택자로 교체했습니다.
- 그래프 선택 사각형 삭제 중 발생하던 `VisualElement is not my child` 예외를 수정했습니다.
- Behaviour Tree 노드 삭제 시 Blackboard 재구성에서 발생하던 null 참조 예외를 수정했습니다.
- State Machine 예제 타입이 다른 조립체로 이동했을 때 그래프 상태 타입을 찾지 못하던 문제를 수정했습니다.
- 전이 요약이 노드 이동을 따라가지 못하거나 사용자 지정 위치를 잃는 문제를 수정했습니다.

## [v1.0.16] - 2026-07-10

### 추가
- `SWIdentifiedObject`에 에디터 전용 스프라이트 아이콘 필드를 추가하고 Stat System Editor 같은 상속 객체 목록에서 표시하도록 추가했습니다.
- `SWTools/Utils/Data/Stat System Editor`에 정렬 단축 버튼과 선택한 에셋 이름 변경 기능을 추가했습니다.
- Stat System Editor 설정 탭에 목록 행 높이, 아이콘 크기, 삭제 버튼 크기, 글자 크기 조절 옵션과 미리보기를 추가했습니다.

### 변경
- `SWTools/Debug`와 `SWTools/Utils` 에디터 창 메뉴를 더 세부적인 하위 메뉴로 나누었습니다.
- Stat System Editor 목록 행을 고정 폭 기반으로 정리해 삭제 버튼이 잘리지 않도록 변경했습니다.
- 패키지 메타데이터와 README 설치 주소를 `v1.0.16`으로 변경했습니다.

## [v1.0.15] - 2026-07-08

### 추가
- 디버그 콘솔 열기 조합키, 시작 시 오버레이 표시, 오버레이 측정 항목, 선택적 Input System 입력 설정을 추가했습니다.

### 변경
- 디버그 콘솔 설정 창을 상태, 입력, 오버레이, 플레이 제어 탭으로 정리했습니다.
- 필수 Input System 패키지 의존성을 제거하고 캐시된 리플렉션을 통해 선택적 Input System 콘솔 입력만 유지했습니다.
- 패키지 메타데이터와 README 설치 주소를 `v1.0.15`로 변경했습니다.

## [v1.0.14] - 2026-07-07

### 추가
- `SWSubClassSelector` 필드에서 `SWCondition`을 함께 처리하여 `SerializeReference` 필드와 컬렉션의 조건부 비활성화 및 숨김 상태가 동작하도록 추가했습니다.

### 변경
- `SWIdentifiedObject`의 상속된 기본 데이터 정의 필드를 기존 `SWGroup` 인스펙터 디자인의 `데이터 정의` 폴드아웃으로 묶었습니다.
- 패키지 메타데이터와 README 설치 주소를 `v1.0.14`로 변경했습니다.

## [v1.0.13] - 2026-07-07

### 변경
- 패키지 메타데이터와 README 설치 주소를 `v1.0.13`으로 변경했습니다.

## [v1.0.12] - 2026-07-07

### 변경
- Unity 및 .NET 타입 이름과 충돌하기 쉬운 네임스페이스를 `SW.Attributes`, `SW.Coroutines`, `SW.Debugging`, `SW.ScreenResolution`, `SW.EditorTools.Attributes`로 변경했습니다.
- 패키지 메타데이터와 README 설치 주소를 `v1.0.12`로 변경했습니다.

## [v1.0.11] - 2026-07-06

### 변경
- Runtime 네임스페이스를 기능별 `SW.*` 구조로 재편하고 Runtime 및 Editor 폴더를 네임스페이스와 일치시켰습니다.
- 남아 있던 공개 `SWUtils...` 타입을 `SW...` 형식으로 변경하고 기존 저장 키는 유지했습니다.
- 한글 XML 문서 주석을 통일하고 잘못된 어트리뷰트 예제를 수정했습니다.
- 영문·한글 README와 변경 기록 문서를 추가하고 언어 전환 링크를 연결했습니다.
- 패키지 버전과 README 설치 주소를 `v1.0.11`로 변경했습니다.

## [v1.0.10] - 2026-07-02

### 변경
- 패키지 버전과 README 설치 주소를 `v1.0.10`으로 변경했습니다.

### 수정
- 변경된 폴더와 Unity 메타 파일 이름을 일치시켜 읽기 전용 패키지 설치 오류를 수정했습니다.

## [v1.0.9] - 2026-07-02

### 추가
- `SWMonoBehaviour`와 같은 그룹, 버튼, 다시 그리기 어트리뷰트를 지원하는 `SWScriptableObject`를 추가했습니다.
- 기본 Unity PlayerPrefs를 조회, 수정, 삭제할 수 있는 탭을 PlayerPrefs Viewer에 추가했습니다.
- 기본 Unity PlayerPrefs 검색 필터와 전체 삭제 기능을 추가했습니다.
- `SWEncrypt<T>`에 `long`, `double` 지원을 추가했습니다.
- `SWPlayerPrefs`에 `SetLong`, `GetLong`, `SetDouble`, `GetDouble`을 추가했습니다.
- `SWTime.ToDateTime`을 추가했습니다.

### 변경
- PlayerPrefs Viewer 목록에서 값을 직접 수정하고 저장할 수 있게 변경했습니다.
- `SWEventBus`에 전체 로그와 발행별 로그 제어 기능을 추가했습니다.
- `SWUtilsRefillTimer`를 `SWRefillTimer`로 변경하고 기존 저장 키는 유지했습니다.
- `SWTableSheet`가 리스트와 배열 외에 일반 클래스 필드도 지원하도록 확장했습니다.
- Excel Table Importer에 세로형 필드·값 구조를 추가했습니다.
- 주요 Runtime 및 Editor 사용법을 README에 보강했습니다.

## [v1.0.8] - 2026-06-16

### 추가
- 큰 숫자 단위 표시를 위한 `SWAmountFormat`을 추가했습니다.
- 숫자 단위와 소수점 설정을 저장하는 `SWAmountFormatProfile`을 추가했습니다.
- 프리셋 생성, 편집, 미리보기를 제공하는 Amount Format Window를 추가했습니다.
- 이미지 없이 레이캐스트 영역을 제공하는 `SWRectDummy`와 전용 인스펙터를 추가했습니다.

### 변경
- 패키지 버전을 `v1.0.8`로 변경했습니다.
- 숫자 표시 프리셋과 `SWRectDummy` 사용법을 README에 추가했습니다.

## [v1.0.7] - 2026-06-08

### 추가
- EventBus Debugger Window를 추가했습니다.
- `SWEventBus`에 리스너 수, 발행 수, 마지막 발행 시각과 데이터 스냅샷을 추가했습니다.
- Pool Monitor Window를 추가했습니다.
- `SWPool`에 프리팹별 생성, 활성, 대기, 반환과 지연 반환 상태를 추가했습니다.

### 변경
- 에디터 창 메뉴를 `SWTools/Debug`와 `SWTools/Utils`로 분리했습니다.
- 실제 에디터 메뉴 경로를 README에 기록했습니다.
- 패키지 버전을 `v1.0.7`로 변경했습니다.

### 수정
- Steamworks.NET 분기를 에디터에서 제외하여 빌드 오류를 방지했습니다.
- `SWConditionAttribute`의 사용하지 않는 UnityEditor 참조를 제거했습니다.

## [v1.0.6] - 2026-06-02

### 추가
- `SWSubClassSelector`를 추가했습니다.
- `SerializeReference` 필드에서 추상 클래스나 인터페이스 구현 타입을 검색하여 선택할 수 있게 했습니다.
- 타입 선택 경로를 설정하는 `SWAddTypeMenu`를 추가했습니다.
- 특정 타입을 선택 메뉴에서 숨기는 `SWHideInTypeMenu`를 추가했습니다.
- 단일 필드, 배열과 `List<T>` 사용법을 보여주는 샘플을 추가했습니다.

### 변경
- 패키지 버전을 `v1.0.6`으로 변경했습니다.
- 샘플 스크립트에 `SWExample` 네임스페이스를 적용했습니다.

## [v1.0.5] - 2026-05-29

### 추가
- TextMeshPro Font Asset Manager에 성능 탭을 추가했습니다.
- 아틀라스 메모리, 글리프, 문자, 대체 폰트 연결, 동적 아틀라스와 머티리얼 프리셋 검사를 추가했습니다.
- README에 TextMeshPro 폰트 에셋 성능 안내를 추가했습니다.

### 변경
- README와 변경 기록의 버전 표기를 `v1.0.5`로 변경했습니다.

## [1.0.0] - 2026-03-19

### 추가
- SWUtils 첫 버전을 배포했습니다.
