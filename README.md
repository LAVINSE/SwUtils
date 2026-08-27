# SWUtils

[한국어](README.md) | [English](README.en.md)

![Unity 6.0+](https://img.shields.io/badge/Unity-6.0%2B-222222)
![Package 1.1.1](https://img.shields.io/badge/package-1.1.1-2f80ed)
![Runtime and Editor](https://img.shields.io/badge/runtime%20%2B%20editor-tools-31a36c)

SWUtils는 Unity 프로젝트에서 반복적으로 사용하는 런타임 시스템, 인스펙터 워크플로, 디버깅 도구, 에디터 생산성 창을 모은 유틸리티 패키지입니다.

> [!WARNING]
> SWUtils에서 제공하는 기능은 현재 테스트 단계입니다. 사용 중 예기치 않은 동작이나 버그가 발생할 수 있으므로 실제 프로젝트에 적용하기 전에 충분히 검증하세요.

## 개요

| 영역 | 제공 기능 |
| --- | --- |
| 런타임 기반 | `SWMonoBehaviour`, `SWScriptableObject`, 코루틴 실행기, 풀링, 팝업 흐름, 해상도 보정, 능력치 데이터, 공통 유틸리티를 제공합니다. |
| 그래프 런타임 | 다중 계층 및 스택 State Machine, Behaviour Tree, Blackboard, 그래프 에셋 팩터리와 Runtime Debug를 제공합니다. |
| 데이터와 저장 | 암호화 PlayerPrefs, 저장 슬롯, 파일 저장, 클라우드 저장 진입점, JSON 가져오기와 내보내기 헬퍼를 제공합니다. |
| 인스펙터 도구 | 그룹, 버튼, 조건 표시, 드롭다운, 읽기 전용 필드, `SerializeReference` 타입 선택, 표 가져오기 어트리뷰트를 제공합니다. |
| 디버깅 | 런타임 디버그 콘솔, 명령 등록, 감시 값, 선택적 Input System 지원, 가벼운 성능 오버레이를 제공합니다. |
| 에디터 워크플로 | Shader Graph 스타일 그래프 편집기, Graph List, 사용자 노드 카테고리, 디버그 창, PlayerPrefs 조회, 풀과 이벤트 모니터링, 표 가져오기와 참조 검색을 제공합니다. |

빠른 링크:

- [주요 기능 미리보기](#주요-기능-미리보기)
- [Git 주소로 설치](#git-주소로-설치)
- [빠른 시작](#빠른-시작)
- [네임스페이스 구조](#네임스페이스-구조)
- [런타임 기능](#런타임-기능)
- [에디터 기능](#에디터-기능)
- [조립체 정의](#조립체-정의)

## 주요 기능 미리보기

### 인스펙터 어트리뷰트

그룹, 읽기 전용 필드, 조건부 표시, 드롭다운과 메서드 실행 버튼을 기본 인스펙터에서 사용할 수 있습니다.

<p align="center">
  <img src="Documentation~/Media/SWAttribute.gif" alt="SWUtils 인스펙터 어트리뷰트 사용 화면" width="460">
</p>

### State Machine 그래프

상태와 전이를 시각적으로 구성하고 실행 중인 상태와 최근 전이를 그래프에서 확인할 수 있습니다.

![SWUtils State Machine 그래프 사용 화면](Documentation~/Media/SWStateMachine.gif)

### Behaviour Tree

Blackboard와 노드 인스펙터를 이용해 트리를 구성하고 실행 중인 노드 상태를 확인할 수 있습니다.

![SWUtils Behaviour Tree 사용 화면](Documentation~/Media/SWBehaviourTree.gif)

### 에디터 작업 도구

<table>
  <tr>
    <td width="50%" valign="top">
      <strong>Quick Asset Palette</strong><br>
      자주 사용하는 에셋과 폴더를 등록하고 열기, 선택, 생성 작업을 빠르게 실행합니다.<br><br>
      <img src="Documentation~/Media/SWAssetPalette.png" alt="SWUtils Quick Asset Palette 화면">
    </td>
    <td width="50%" valign="top">
      <strong>Reference Finder</strong><br>
      에셋의 역참조, 의존성과 미사용 후보를 검색합니다.<br><br>
      <img src="Documentation~/Media/ReferenceFinder.png" alt="SWUtils Reference Finder 화면">
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <strong>Excel Table Importer</strong><br>
      탭으로 구분된 표 데이터를 미리 확인한 뒤 ScriptableObject에 적용합니다.<br><br>
      <img src="Documentation~/Media/ExcelTable.png" alt="SWUtils Excel Table Importer 화면">
    </td>
    <td width="50%" valign="top">
      <strong>Amount Format</strong><br>
      큰 숫자의 단위, 소수점과 반올림 방식을 프리셋으로 관리하고 결과를 미리 확인합니다.<br><br>
      <img src="Documentation~/Media/AmountFormat.png" alt="SWUtils Amount Format 화면">
    </td>
  </tr>
</table>

## Git 주소로 설치

Unity Package Manager에서 다음 순서로 설치합니다.

1. Unity 메뉴에서 `Window > Package Manager`를 엽니다.
2. 왼쪽 위의 `+` 버튼을 누릅니다.
3. `Add package from git URL...`을 선택합니다.
4. 다음 주소를 입력합니다.

```text
https://github.com/LAVINSE/SWUtils.git#v1.1.1
```

특정 브랜치나 태그를 설치하려면 주소 뒤에 `#브랜치이름` 또는 `#태그이름`을 붙입니다.

## 의존성

SWUtils가 사용하는 Unity 패키지와 모듈은 Git 주소로 설치할 때 `package.json`을 통해 자동으로 연결됩니다.

- Localization
- TextMeshPro
- Unity UI
- Physics
- Physics 2D

Unity Input System은 자동 설치하지 않습니다. 디버그 콘솔은 프로젝트에 Input System이 이미 있으면 사용할 수 있지만, 필수 패키지 의존성이 생기지 않도록 선택 기능으로 유지합니다.

다음 외부 라이브러리는 관련 기능을 사용할 때만 선택적으로 설치합니다.

- Google Play Games: Android 클라우드 저장
- Steamworks.NET: 데스크톱 클라우드 저장

클라우드 저장 기능에 외부 라이브러리를 사용할 때는 다음 정의 심볼을 추가합니다.

- `SW_GOOGLEPLAY_ENABLE`: Android에서 Google Play Games 저장 기능을 활성화합니다.
- `SW_STEAMWORKS_NET`: 데스크톱에서 Steamworks.NET 저장 기능을 활성화합니다.

## 빠른 시작

1. 예제가 필요하면 프로젝트 창의 `Packages > SWUtils > Samples` 폴더에서 확인합니다.
2. `SWMonoBehaviour`와 `SWScriptableObject`를 사용할 때 `using SW.Base;`를 추가합니다.
3. 인스펙터 어트리뷰트를 사용할 때 `using SW.Attributes;`를 추가합니다.
4. 데이터, 팝업, 해상도, 유틸리티 기능은 각각 `SW.Data`, `SW.Popup`, `SW.ScreenResolution`, `SW.Util`을 사용합니다.
5. 코루틴 실행기는 `using SW.Coroutines;`을 사용합니다.
6. 조립체 정의 파일을 사용하는 프로젝트는 `SWUtils.Runtime` 참조를 추가합니다.

관리자 컴포넌트는 기본적으로 씬이 소유합니다. 씬 전환 후에도 필요한 관리자는 시작 씬에 배치하고 유지되도록 구성합니다.

## 네임스페이스 구조

Runtime과 Editor 코드는 기능별 폴더와 같은 네임스페이스를 사용합니다.

| 폴더 | 네임스페이스 |
| --- | --- |
| `Runtime/Attribute` | `SW.Attributes` |
| `Runtime/Base` | `SW.Base` |
| `Runtime/Coroutine` | `SW.Coroutines` |
| `Runtime/Data` | `SW.Data` |
| `Runtime/Debug` | `SW.Debugging` |
| `Runtime/Pooling` | `SW.Pooling` |
| `Runtime/Popup` | `SW.Popup` |
| `Runtime/Resolution` | `SW.ScreenResolution` |
| `Runtime/Stat` | `SW.Stat` |
| `Runtime/StateMachine` | `SW.StateMachine` |
| `Runtime/Util` | `SW.Util` |
| `Editor/Attribute` | `SW.EditorTools.Attributes` |
| `Editor/<기능>` | `SW.EditorTools.<기능>` |

## 런타임 기능

### 어트리뷰트

- `SWButton`: 인스펙터 버튼으로 메서드를 실행합니다.
- `SWButtonBar`: 여러 메서드 실행 버튼을 한 줄에 표시합니다.
- `SWCondition`: Boolean 필드 값에 따라 필드를 표시하거나 숨깁니다.
- `SWEnumCondition`: 열거형 값에 따라 필드를 표시하거나 숨깁니다.
- `SWDropdown`: 미리 정의한 값을 드롭다운으로 표시합니다.
- `SWGroup`: 인스펙터 필드를 접을 수 있는 그룹으로 묶습니다.
- `SWReadOnly`: 필드를 읽기 전용으로 표시합니다.
- `SWSubClassSelector`: `SerializeReference` 필드의 구현 타입을 검색하여 선택합니다.
- `SWTable`, `SWTableSheet`: 표 데이터를 직렬화 필드에 연결합니다.
- `SWCommand`: 메서드를 런타임 디버그 콘솔 명령으로 노출합니다.

### 기본 타입

- `SWMonoBehaviour`: SWUtils 인스펙터 기능을 사용하는 컴포넌트 기본 클래스입니다.
- `SWScriptableObject`: 같은 인스펙터 기능을 사용하는 데이터 에셋 기본 클래스입니다.
- `SWIdentifiedObject`: 식별자, 코드명, 표시명, 설명, 카테고리와 에디터 전용 스프라이트 아이콘을 가진 데이터 에셋입니다.
- `SWIODatabase`: `SWIdentifiedObject` 목록을 관리하고 빠르게 조회합니다.
- `SWCategory`: 데이터 에셋 분류에 사용합니다.

### 코루틴

- `ICoroutineRunner`: 코루틴 실행 기능의 추상 인터페이스입니다.
- `SWCoroutineRunner`: 지연 호출, 다음 프레임 호출, 조건 대기와 반복 실행을 제공합니다.

```csharp
using SW.Coroutines;
using UnityEngine;

public class DelayExample : MonoBehaviour
{
    [SerializeField] private SWCoroutineRunner coroutineRunner;

    private void Start()
    {
        coroutineRunner.DelayedCall(1f, () => Debug.Log("완료"));
    }
}
```

### 데이터와 저장

- `SWEncrypt<T>`: `SWPlayerPrefs`를 사용하는 암호화 값 래퍼입니다.
- `SWPlayerPrefs`: 키와 값을 암호화하여 Unity PlayerPrefs에 저장합니다.
- `SWPlayerPrefsSettings`: 암호화 솔트 설정을 관리합니다.
- `SWSaveDataManager`: 슬롯별 저장, 불러오기, 백업과 복원을 관리합니다.
- `SWSaveSlot`: 기본 저장 슬롯 이름을 제공합니다.
- `SWCloud`: 플랫폼별 클라우드 저장소와 로컬 대체 저장소를 통합합니다.

`SWPlayerPrefs`는 내부적으로 Unity PlayerPrefs를 사용합니다. 따라서 Unity PlayerPrefs 전체 삭제는 SWUtils 암호화 데이터도 함께 삭제합니다.

```csharp
using SW.Data;

SWPlayerPrefs.SetInt("coin", 100);
int coin = SWPlayerPrefs.GetInt("coin");
SWPlayerPrefs.Save();
```

### 디버그

- `SWDebugConsole`: 로그 확인, 명령 실행과 상태 감시를 제공하는 런타임 콘솔입니다.
- `SWDebugConsoleSettings`: 콘솔 열기 입력, 선택적 Input System 확인, 성능 오버레이 표시값을 저장하는 설정 에셋입니다.
- `SWCommand`: 콘솔에서 실행할 메서드를 등록합니다.
- `SWLog`: `SW_DEBUG_MODE` 정의 심볼이 있을 때 로그를 출력합니다.

`SWTools/Debug/Console/Debug Console Settings`에서 현재 빌드 타겟에 `SW_DEBUG_MODE`를 추가한 뒤 사용합니다. 심볼이 없으면 콘솔 호출은 조건부 메서드로 컴파일에서 제거됩니다.

디버그 콘솔 설정 순서:

1. `SWTools/Debug/Console/Debug Console Settings`를 엽니다.
2. `상태` 탭에서 현재 빌드 타겟에 `SW_DEBUG_MODE`를 추가합니다.
3. 프로젝트별 값을 저장하려면 설정 에셋을 생성합니다.
4. `입력` 탭에서 열기 키와 `Control`, `Shift`, `Alt` 조합키를 선택합니다.
5. 모바일에서 콘솔을 여는 동시 터치 개수를 지정합니다.

Input System 패키지는 필수 의존성이 아닙니다. `Input System 확인`을 켜고 프로젝트에 Input System 패키지가 있으면 캐시된 리플렉션으로 먼저 입력을 확인합니다. 패키지가 없으면 Unity 기본 `Input` API로 처리하므로 컴파일 오류가 발생하지 않습니다.

성능 오버레이 설정 순서:

1. `SWTools/Debug/Console/Debug Console Settings`를 엽니다.
2. `오버레이` 탭에서 시작 시 표시 여부, 표시 위치, 크기 배율, 갱신 간격을 설정합니다.
3. FPS, 최소/최대 FPS, 메모리 표시 여부와 FPS 경고 기준을 선택합니다.

런타임 제어 예시:

```csharp
using SW.Debugging;

SWDebugConsole.Show();
SWDebugConsole.ToggleOverlay();
SWDebugConsole.ResetOverlayStats();
```

명령 등록 예시:

```csharp
using SW.Attributes;

public class DebugCommands
{
    [SWCommand("give_gold", "테스트 골드를 추가합니다", "Test")]
    private static void GiveGold(int amount)
    {
    }
}
```

### 오브젝트 풀

- `IPool`, `IPoolable`: 풀 구현과 풀링 대상의 계약입니다.
- `SWPool`: 프리팹별 생성, 예열, 반환, 지연 반환과 유휴 풀 정리를 관리합니다.
- `SWPoolCatalog`: 풀 이름, 그룹과 예열 수량을 데이터로 관리합니다.
- `SWPoolRegistry`: 카탈로그를 실제 풀에 등록합니다.
- `SWPoolSnapshot`: 에디터 모니터에서 사용하는 읽기 전용 상태입니다.

### 팝업

- `SWPopupBase`: 팝업 생명주기와 표시 상태를 관리합니다.
- `SWPopupManager`: 팝업 생성, 표시, 숨김, 캐시와 카탈로그 조회를 관리합니다.
- `SWPopupCatalog`: 문자열 키와 팝업 프리팹을 연결합니다.
- `SWPopupShowEffect`, `SWPopupHideEffect`: 표시 및 숨김 연출 기본 타입입니다.
- `SWPopupScaleShowEffect`, `SWPopupScaleHideEffect`: 크기 변경 기반 기본 연출입니다.
- `SWPopupEffectHandle`: 실행 중인 팝업 연출을 제어합니다.

### 해상도

- `SWCanvasResolution`: 화면 비율에 따라 CanvasScaler 설정을 조정합니다.
- `SWSafeArea`: 노치와 화면 안전 구역에 맞춰 사용자 인터페이스를 배치합니다.
- `SWResolution`: 화면 크기, 비율, 좌표 변환과 카메라 계산을 제공합니다.

### 능력치

- `SWStat`: 기본값과 보너스 값을 조합하는 능력치 데이터입니다.
- `SWStatOverride`: 개체별 기본값 재정의 설정입니다.
- `SWStats`: 게임 오브젝트의 런타임 능력치 목록을 관리합니다.
- `SWStatScaleFloat`: 능력치 비율을 적용한 값을 계산합니다.

### 상태 머신

- `SWStateMachine<TContext>`: Unity 컴포넌트에 의존하지 않는 다중 계층 범용 유한 상태 머신입니다.
- `SWState<TContext>`: 초기화, 진입, 갱신, 종료와 메시지 처리를 정의하는 상태 기본 타입입니다.
- `SWMonoStateMachine<TContext>`: 일반 프레임, 물리 프레임 또는 수동 방식으로 상태 머신을 갱신하는 Unity 컴포넌트 기본 타입입니다.
- `SWStackStateMachine<TContext>`: 이전 상태를 유지하면서 새 상태를 쌓고 제거하는 범용 스택 상태 머신입니다.
- `SWStackState<TContext>`: 진입, 일시 정지, 복귀, 갱신, 종료와 메시지 처리를 정의하는 스택 상태 기본 타입입니다.
- `SWMonoStackStateMachine<TContext>`: 스택 상태 머신을 Unity 갱신 생명주기와 연결하는 컴포넌트 기본 타입입니다.

각 계층은 하나의 현재 상태를 가지며 낮은 계층 번호부터 독립적으로 실행됩니다. 모든 상태 전이는 일반 상태 전이보다 먼저 확인하고, 같은 우선순위에서는 등록 순서가 빠른 전이를 먼저 실행합니다.

```csharp
using SW.StateMachine;

SWStateMachine<Player> stateMachine = new SWStateMachine<Player>(player);
stateMachine.AddState<IdleState>(0);
stateMachine.AddState<MovingState>(0);
stateMachine.SetInitialState<IdleState>(0);

stateMachine.AddTransition<IdleState, MovingState>(
    state => state.Context.IsMoving,
    0);
stateMachine.AddAnyTransition<IdleState>(PlayerStateCommand.ReturnToIdle, layer: 0);

stateMachine.Start();
stateMachine.Tick(deltaTime);
```

그래프 기반 다중 계층 상태의 통합 예제는 `SWGraphAssetsExample`에서 확인할 수 있습니다.

스택 상태 머신에서는 최상단 상태만 갱신됩니다. 새 상태를 추가하면 기존 상태가 일시 정지되고, 최상단 상태를 제거하면 아래 상태가 종료되지 않은 채 다시 활성화됩니다.

```csharp
SWStackStateMachine<GameFlow> stackStateMachine =
    new SWStackStateMachine<GameFlow>(gameFlow);

stackStateMachine.AddState<GameplayState>();
stackStateMachine.AddState<PauseState>();
stackStateMachine.AddState<SettingsState>();
stackStateMachine.Start<GameplayState>();

stackStateMachine.Push<PauseState>();
stackStateMachine.Push<SettingsState>();
stackStateMachine.Pop();
```

그래프 기반 스택 상태와 실행 제어도 `SWGraphAssetsExample`에서 함께 확인할 수 있습니다.

#### 상태 머신 그래프 편집기

- `SWStateMachineGraphAsset`: 상태 노드와 전이 정보를 저장하는 `ScriptableObject` 그래프 에셋입니다.
- `Layered`: 여러 계층을 독립적으로 실행하며 같은 계층의 상태와 `Any State`를 연결하는 그래프 형식입니다.
- `Stack`: 상태를 쌓고 제거하는 그래프 형식입니다. 입력 전용 `Return State`로 현재 상태를 제거하고 이전 상태에 복귀합니다.
- `SWStateMachineGraphFactory`: 그래프 에셋으로 다중 계층 상태 머신 또는 스택 상태 머신 제어기를 생성합니다.
- `SWStateMachineGraphCondition<TContext>`: 프로젝트 문맥에 맞는 전이 조건을 구현하는 기본 타입입니다.
- `SWStackStateMachineGraphController<TContext>`: 스택 그래프의 갱신, 명령, 메시지, 중지와 상태 전이를 제어합니다.

##### 편집 방법

1. `Assets > Create > SWTools > State Machine Graph`에서 그래프 에셋을 생성합니다.
2. 그래프 에셋을 두 번 클릭하거나 인스펙터의 `상태 머신 그래프 편집` 버튼을 누릅니다.
3. `SWTools > Utils > State Machine > Graph Editor`에서도 편집기 창을 열 수 있습니다.
4. 왼쪽 `Blackboard`의 `Graph Type`에서 `Layered` 또는 `Stack`을 선택합니다.
5. 가운데 빈 공간을 마우스 오른쪽 버튼으로 누른 뒤 `Create Node...`를 선택합니다. 상단 `Create Node` 버튼과 `Space` 키도 같은 검색 창을 엽니다.
6. 검색 창의 `States`에서 `SWState<TContext>` 또는 `SWStackState<TContext>` 구현을 선택합니다. `Flow Control`에서는 `Any State` 또는 `Return State`를 생성할 수 있습니다. 이름이 같은 중첩 상태는 선언 클래스 이름으로 구분됩니다.
7. 노드의 `Out` 연결점에서 다른 노드의 `In` 연결점까지 끌어 전이를 만듭니다. 연결선 위 표식에서 동작, 명령과 우선순위를 바로 확인할 수 있습니다.
8. 노드를 선택하면 오른쪽에서 표시 이름, 초기 상태와 `Layer`를 편집합니다. 연결선을 선택하면 동작, 명령, 조건, 재진입과 우선순위를 편집합니다.
9. 노드 또는 연결선을 선택하고 `Delete` 키를 누르면 삭제됩니다. `Blackboard`의 검색 가능한 `States`와 `Transitions` 목록에서도 선택·이동·추가·삭제할 수 있고, 각 목록은 접을 수 있습니다.
10. 아래 `Graph Validation`을 펼쳐 구체적인 오류 내용을 확인한 뒤 `Save Asset`으로 저장합니다.

##### 편집 및 디버깅 기능

- `Graph List`: 그래프 에셋을 검색하고 빠르게 전환하며 패널을 왼쪽으로 접을 수 있습니다.
- `Blackboard`: 그래프 형식, 상태와 전이 목록을 관리하고 각 목록을 검색하거나 접을 수 있습니다. 아래 모서리를 끌어 패널 크기를 조절합니다.
- `Graph Inspector`: 선택한 상태의 표시 이름, 초기 상태와 계층 또는 전이의 동작, 명령, 조건, 재진입과 우선순위를 편집합니다. 아래 모서리를 끌어 패널 크기를 조절합니다.
- `Graph Validation`: 저장 전에 잘못된 노드 구성과 전이 연결을 검사합니다.
- `Auto Layout`: 계층과 초기 상태를 기준으로 노드를 자동 정렬합니다.
- `New Script`: 수정 가능한 텍스트 템플릿으로 다중 계층 상태, 스택 상태와 전이 조건 스크립트를 생성합니다.
- `전이 요약`: 노드 이동을 따라가며 동작, 명령과 우선순위를 표시합니다. `Alt` 키를 누른 채 끌면 사용자 지정 위치를 에셋에 저장합니다.
- `Settings`: 전이 요약 표시, 노드와 패널 크기, 격자 맞춤과 간격을 설정합니다. 프로젝트 공통 설정은 `Project Settings > SWUtils > State Machine Graph`에서 변경합니다.
- `Runtime`: 그래프 팩터리로 만든 상태 머신을 Play Mode 디버거에 자동 등록합니다. 문맥 게임 오브젝트를 선택하면 활성 상태, 실행 시간과 최근 전이 이력을 표시합니다.
- `단축키`: `Ctrl+C` 복사, `Ctrl+V` 붙여넣기, `Ctrl+D` 복제, `A` 전체 보기, `O` 원점 보기, `Space` 노드 검색을 지원합니다.

일반 상태는 초록색, `Any State`는 보라색, `Return State`는 청록색 카드로 구분됩니다. 복사한 노드 사이의 전이와 전이 설정도 함께 복사되며 같은 그래프 형식의 다른 에셋에도 붙여넣을 수 있습니다. 그래프 화면 위치와 확대 비율은 에셋별로, 편집기 배치는 사용자별로 저장됩니다. 자세한 설명은 `Documentation~/StateMachineGraph.ko.md`를 참고하세요.

그래프 에셋으로 런타임 상태 머신을 생성할 수 있습니다.

```csharp
SWStateMachine<Player> stateMachine =
    SWStateMachineGraphFactory.CreateLayered(graphAsset, player);

SWStackStateMachineGraphController<GameFlow> stackController =
    SWStateMachineGraphFactory.CreateStack(graphAsset, gameFlow);
```

조건 연결은 문맥 타입에 맞는 `SWStateMachineGraphCondition<TContext>`를 구현하고 연결선 상세 영역의 `조건 타입 선택`에서 선택합니다.

```csharp
public sealed class IsMovingCondition : SWStateMachineGraphCondition<Player>
{
    public override bool Evaluate(Player context)
    {
        return context.IsMoving;
    }
}
```

다중 계층 그래프 팩터리는 완성된 `SWStateMachine<TContext>`를 반환합니다. 스택 그래프 제어기는 `Tick`, `ExecuteCommand`, `SendMessage`, `Stop`을 제공하며 그래프에 지정된 상태 추가, 교체와 이전 상태 복귀 연결을 실행합니다.

### Behaviour Tree

- `SWBehaviourTreeAsset`: 노드와 연결, Blackboard 기본값을 저장하고 `Running`, `Success`, `Failure`, `Aborted` 실행 결과를 관리합니다.
- `SWBehaviourTreeRunner`: 에셋의 독립 실행 복제본을 생성하고 게임 오브젝트의 생명주기에 맞춰 실행합니다.
- `SWBehaviourActionNode`: 실제 작업을 수행하며 자식을 가지지 않는 노드 기본 타입입니다.
- `SWBehaviourCompositeNode`: 여러 자식을 가지며 실행 순서를 정의하는 노드 기본 타입입니다.
- `SWBehaviourDecoratorNode`: 하나의 자식을 감싸 실행 결과나 실행 방식을 바꾸는 노드 기본 타입입니다.
- `SWBehaviourBlackboard`: 트리에서 공유하는 타입별 값을 키로 관리합니다.
- `SWBehaviourNodeProperty<T>`: 고정값 또는 같은 타입의 Blackboard 키를 노드 필드에 연결합니다.
- `SWBehaviourSubTreeNode`: 다른 Behaviour Tree 에셋을 현재 트리의 일부로 실행합니다.

##### 편집 방법

1. `Assets > Create > SWTools > Behaviour Tree`에서 에셋을 생성합니다.
2. 에셋 Inspector의 `Behaviour Tree 편집` 또는 `SWTools > Utils > Behaviour > Tree Editor`를 엽니다.
3. 빈 공간을 마우스 오른쪽 버튼으로 누르거나 `Create Node`를 눌러 Action, Composite, Decorator 노드를 검색합니다.
4. 부모의 `Out`에서 자식의 `In`으로 연결합니다. Composite는 여러 자식, Decorator는 하나의 자식, Action은 자식을 가질 수 없습니다.
5. 자식은 그래프의 왼쪽에서 오른쪽 순서로 실행됩니다. 노드를 가로로 이동하면 실행 순서도 자동 정렬됩니다.
6. 왼쪽 Blackboard에서 값 타입을 선택해 키를 추가하고 이름과 기본값을 편집합니다.
7. 오른쪽 Node Inspector에서 표시 이름, 설명, 노드별 값을 편집하고 `Set as Root`로 시작 노드를 지정합니다.
8. `SWBehaviourTreeRunner`에 에셋을 연결하면 활성화 시 독립 복제본을 실행합니다. Play Mode에서 해당 게임 오브젝트를 선택하면 Running은 노란색, Success는 초록색, Failure는 빨간색으로 표시됩니다.

##### 편집 및 실행 기능

- `Graph List`: Behaviour Tree 에셋을 검색하고 빠르게 전환하며 패널을 접을 수 있습니다.
- `Blackboard`: 기본 타입과 사용자 정의 타입의 키, 기본값과 Runner별 재정의 값을 관리합니다.
- `Node Inspector`: 표시 이름, 설명, 노드별 값과 시작 노드를 편집합니다.
- `Auto Layout`: 시작 노드를 기준으로 트리 구조를 자동 정렬합니다.
- `SubTree`: 다른 트리를 선택해 재사용하고 부모 Blackboard의 공유 여부를 설정합니다.
- `New Script`: `Editor/Behaviour/Templates`의 수정 가능한 텍스트 템플릿으로 Action, Composite와 Decorator 노드 스크립트를 생성합니다.
- `Settings`: 노드 크기, 간격, 패널 크기와 실행 상태 갱신 간격을 설정합니다. 프로젝트 공통 설정은 `Project Settings > SWUtils > Behaviour Tree`에서 변경합니다.
- `Runtime Debug`: Runner, 노드 상태와 Blackboard 값을 확인합니다. 실행 중은 노란색, 성공은 초록색, 실패는 빨간색, 중단은 회색으로 표시됩니다.
- `단축키`: 복사, 붙여넣기, 복제, 키보드 탐색, 전체 보기, 원점 보기와 노드 검색을 지원합니다.

`Set Property`와 `Compare Property`는 기본 타입과 사용자 정의 Blackboard 값을 처리합니다. `SWBehaviourTreeRunner`는 외부 `MonoBehaviour`에서 사용하는 `GetBlackboardValue`, `SetBlackboardValue`, `FindBlackboardKey`를 제공하며 사용자 정의 키도 Runner별 재정의에서 선택할 수 있습니다. 자세한 설명은 `Documentation~/BehaviourTree.ko.md`를 참고하세요.

##### 사용자 정의 분류

- `SWStateMachineNodeCategory`: 상태와 전이 조건을 슬래시 경로 기반 생성 메뉴 카테고리로 분류합니다.
- `SWBehaviourNodeCategory`: Behaviour 노드를 슬래시 경로 기반 생성 메뉴 카테고리로 분류합니다.

예를 들어 `SWStateMachineNodeCategory("Combat/Movement")` 또는 `SWBehaviourNodeCategory("Combat/Actions")`처럼 경로를 지정할 수 있습니다.

사용자 노드는 다음처럼 추가합니다. 별도 등록 없이 노드 검색 창에 나타납니다.

```csharp
using SW.BehaviourTree;

[Serializable]
public sealed class HasTargetNode : SWBehaviourActionNode
{
    protected override SWBehaviourStatus OnUpdate(
        SWBehaviourContext context,
        SWBehaviourTreeAsset tree)
    {
        return context.Blackboard.GetValue<GameObject>("Target") != null
            ? SWBehaviourStatus.Success
            : SWBehaviourStatus.Failure;
    }
}
```

### 유틸리티

- `SWAmountFormat`, `SWAmountFormatProfile`: 큰 숫자의 단위와 소수점 표시를 관리합니다.
- `SWAudioLibrary`, `SWAudioManager`: 음악과 효과음 등록, 재생과 볼륨을 관리합니다.
- `SWButtonExtension`: 연타 방지, 길게 누르기, 반복 실행과 클릭 효과음을 제공합니다.
- `SWCooldown`, `SWTimer`, `SWRefillTimer`, `SWTime`: 시간과 타이머 기능을 제공합니다.
- `SWEventBus`: 타입 기반 이벤트 발행과 구독을 제공합니다.
- `SWRandom`, `SWShuffleBag`: 가중치 선택, 섞기와 반복 없는 무작위 선택을 제공합니다.
- `SWSceneLoader`: 씬 로딩 진행률과 전환 상태를 관리합니다.
- `SWSingleton`, `SWSingletonScene`: 전역 또는 씬 단위 싱글톤을 제공합니다.
- `SWFactory`, `SWExtension`, `SWString`, `SWUtility`: 생성, 확장 메서드, 문자열과 공통 기능을 제공합니다.
- `SWRectDummy`: 메시 없는 사용자 인터페이스 레이캐스트 영역을 제공합니다.
- `SWTriggerDispatcher`: 2차원 및 3차원 트리거 이벤트를 외부로 전달합니다.
- `SWVibration`: 플랫폼 진동 기능을 제공합니다.

## 에디터 기능

### 인스펙터

Runtime 어트리뷰트에 대응하는 프로퍼티 서랍과 `SWMonoBehaviour`, `SWScriptableObject` 사용자 지정 인스펙터를 제공합니다.

### 에디터 창

디버깅 도구는 `SWTools/Debug`, 일반 도구는 `SWTools/Utils` 메뉴에서 엽니다.

- `SWTools/Debug/Build/Build Report Viewer`
- `SWTools/Debug/Console/Debug Console Settings`
- `SWTools/Debug/Event/EventBus Debugger Window`
- `SWTools/Debug/Input/Input Debugger Window`
- `SWTools/Debug/PlayerPrefs/PlayerPrefs Viewer`
- `SWTools/Debug/Pool/Pool Monitor Window`
- `SWTools/Debug/Test/Test Tools Window`
- `SWTools/Utils/Asset/Quick Asset Palette`
- `SWTools/Utils/Behaviour/Tree Editor`
- `SWTools/Utils/Asset/Reference Finder`
- `SWTools/Utils/Asset/TMP Font Asset Manager`
- `SWTools/Utils/Data/Amount Format Window`
- `SWTools/Utils/Data/Excel Table Importer`
- `SWTools/Utils/Data/Localization Tools`
- `SWTools/Utils/Data/Stat System Editor`: `SWCategory`, `SWStat` 같은 `SWIdentifiedObject` 에셋을 생성, 편집, 정렬, 이름 변경하고 목록 아이콘과 표시 크기를 조정합니다.
- `SWTools/Utils/Hierarchy/Hierarchy Tools`
- `SWTools/Utils/Project/Define Symbol Window`
- `SWTools/Utils/Project/PlayerPrefs Salt Settings`
- `SWTools/Utils/Screen/Resolution Window`
- `SWTools/Utils/Simulation/Random Simulator`
- `SWTools/Utils/State Machine/Graph Editor`

#### `SWTools/Debug/Console/Debug Console Settings`

디버그 콘솔 설정 창은 탭으로 필요한 항목만 보여줍니다.

- `상태`: Resources 설정 에셋을 연결하거나 생성하고 현재 빌드 타겟의 `SW_DEBUG_MODE`를 추가 또는 제거합니다.
- `입력`: 자동 생성, 열기 키, 조합키, 터치 개수, 선택적 Input System 확인을 설정합니다.
- `오버레이`: 시작 시 표시, 표시 위치, 크기 배율, 갱신 간격, 표시 항목, FPS 경고 색상을 설정합니다.
- `플레이`: 플레이 중 콘솔 열기와 닫기, 오버레이 토글, 오버레이 기록 초기화를 실행합니다.

### 엑셀 표 가져오기

`SWTableSheet`가 적용된 리스트, 배열 또는 일반 클래스 필드에 탭으로 구분된 데이터를 적용합니다.

1. 대상 `ScriptableObject` 필드에 `SWTableSheet`를 추가합니다.
2. 행 데이터 타입의 필드에 `SWTable`을 추가합니다.
3. `SWTools > Utils > Data > Excel Table Importer`를 엽니다.
4. 표 데이터를 붙여 넣고 미리보기 후 적용합니다.

### 하이어라키 도구

게임 오브젝트의 배경색, 아이콘, 활성 상태와 누락된 컴포넌트 경고를 하이어라키에 표시합니다.

## 샘플

- `SWAttributeExample`: 인스펙터 어트리뷰트 사용 예제
- `SWSubClassSelectorExample`: `SerializeReference` 구현 타입 선택 예제
- `SWGraphAssetsExample`: Behaviour Tree, 다중 계층 상태 머신, 스택 상태 머신과 사용자 정의 노드 카테고리를 한 파일에서 보여주는 통합 예제
- `SWExampleBehaviourTree`: 실행 가능한 Behaviour Tree 예제 에셋
- `SWExampleStateMachine`: 실행 가능한 다중 계층 State Machine 예제 에셋
- `SWExampleStackStateMachine`: Gameplay, Pause와 Return State를 사용하는 Stack State Machine 예제 에셋
- `SWPool`, `SWPoolRegistry`, `SWPopupManager`, `SWStats` 프리팹

예제는 프로젝트 창의 `Packages > SWUtils > Samples` 폴더에서 바로 확인할 수 있습니다.

## 조립체 정의

- `SWUtils.Runtime`: 런타임 코드
- `SWUtils.Editor`: 에디터 코드
- `SWUtils.Samples`: 샘플 코드

스크립트 파일을 이동하거나 이름을 변경할 때는 Unity 메타 식별자를 유지해야 기존 씬과 프리팹 참조가 보존됩니다.
