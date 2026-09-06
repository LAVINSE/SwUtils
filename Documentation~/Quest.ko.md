# SWUtils 퀘스트와 업적

`SW.Quest`는 작업 묶음, 조건, 보상, 진행 보고와 저장 복원을 조합할 수 있는 SWUtils 퀘스트 및 업적 모듈입니다.

> [!WARNING]
> 이 모듈은 아직 전체 설계 검토와 실제 프로젝트 검증이 끝나지 않은 실험적 시스템입니다. 검토 결과에 따라 공개 기능과 저장 데이터 형식이 변경될 수 있으므로 실제 서비스에 적용하기 전에 프로젝트 환경에서 충분히 검증하세요.

## 설계 구조

| 역할 | 타입 | 책임 |
| --- | --- | --- |
| 정의와 런타임 | `SWQuest`, `SWAchievement` | 표시 정보, 작업 묶음, 조건과 보상을 보유하고 독립 런타임 복제본을 생성합니다. |
| 진행 단위 | `SWQuestTask` | 카테고리와 대상을 비교하고 보고 변화량을 진행량으로 반영합니다. |
| 진행 순서 | `SWQuestTaskGroup` | 묶음 안의 작업을 동시에 진행하며 묶음 사이의 순서는 퀘스트가 관리합니다. |
| 진행 계산 | `SWQuestTaskAction` | 더하기, 값 교체, 양수·음수 전용과 연속 성공 계산을 교체할 수 있게 합니다. |
| 대상 비교 | `SWQuestTarget` | 문자열 또는 Unity 오브젝트를 진행 보고 대상과 비교합니다. |
| 규칙 확장 | `SWQuestCondition`, `SWQuestReward` | 프로젝트별 수락·취소 조건과 보상 지급을 구현합니다. |
| 정의 조회 | `SWQuestDatabase`, `SWAchievementDatabase` | 일반 퀘스트와 업적을 서로 분리된 에셋과 코드명 영역으로 저장하고 조회합니다. |
| 런타임 관리 | `SWQuestSystem` | 등록, 보고, 상태 목록, 이벤트, 완료, 취소와 저장 복원을 담당합니다. |
| 제작 도구 | `SWQuestSystemWindow` | 관련 에셋 생성·복제·삭제·검색·편집과 데이터베이스 동기화·검증을 제공합니다. |
| 저장 위치 | `ISWQuestSaveStore` | 직렬화 문자열의 실제 저장 위치를 교체합니다. 기본 구현은 암호화된 `SWPlayerPrefs`를 사용합니다. |
| 장면 연결 | `SWQuestGiver`, `SWQuestReporter` | 장면 시작과 물리 트리거를 퀘스트 시스템 호출로 연결합니다. |

일반 퀘스트와 업적은 같은 코드명을 사용할 수 있습니다. 같은 종류의 목록 안에서는 저장 복원과 조회가 모호해지지 않도록 코드명을 고유하게 지정해야 합니다.

## 에셋 제작 순서

1. `Assets > Create > SWBase > Category`에서 `KILL`, `COLLECT`, `LOCATION` 같은 카테고리를 만듭니다.
2. 대상 구분이 필요하면 `Assets > Create > SWUtils > Quest > Target`에서 문자열 또는 Unity 오브젝트 대상을 만듭니다.
3. `Assets > Create > SWUtils > Quest > Task`에서 코드명, 설명, 카테고리, 대상과 필요 진행량을 설정합니다.
4. 특별한 계산이 필요하면 `Task Action`을 연결합니다. 비워 두면 보고 변화량을 현재 진행량에 더합니다.
5. `Quest` 또는 `Achievement` 에셋을 만들고 `Task Groups`에 작업을 연결합니다. 각 묶음에도 퀘스트 안에서 고유한 코드명을 지정합니다. 같은 묶음의 작업은 동시에 진행하고 다음 묶음은 현재 묶음이 모두 끝난 뒤 시작합니다.
6. 필요한 수락 조건, 취소 조건과 보상을 연결합니다.
7. `SWTools > Utils > Data > Quest System Editor`를 열어 `퀘스트 데이터베이스`와 `업적 데이터베이스` 에셋을 각각 만듭니다.
8. 각 데이터베이스를 선택해 `프로젝트 정의 동기화`, `구성 검증`을 차례로 실행합니다.
9. 시작 장면에 `SWQuestSystem`을 배치하고 두 데이터베이스를 연결합니다.

업적은 시스템 초기화 시 자동 등록되며 모든 일반 퀘스트와 같은 진행 보고를 받습니다. 업적은 작업을 모두 채우는 즉시 완료되고 취소할 수 없으며 항상 저장됩니다.

## 퀘스트 시스템 편집기

`SWTools > Utils > Data > Quest System Editor`에서 퀘스트, 업적, 작업, 대상, 조건, 보상, 진행 계산, 시작 진행값과 두 데이터베이스를 탭별로 관리할 수 있습니다.

- `생성`: 현재 탭의 기본 타입 또는 프로젝트에서 구현한 파생 타입 에셋을 만듭니다.
- `복제`, `삭제`, `위치 표시`: 선택한 에셋을 관리합니다.
- 검색과 정렬: 코드명, 표시명, 에셋 이름과 타입 이름을 기준으로 목록을 찾고 정렬합니다.
- `프로젝트 정의 동기화`: 일반 퀘스트는 퀘스트 데이터베이스에, 업적은 업적 데이터베이스에 수집합니다.
- `구성 검증`: 비어 있거나 중복된 코드명과 잘못된 작업 구성을 콘솔에 출력합니다.

퀘스트 또는 업적 에셋을 창에서 생성·복제·삭제하면 관련 데이터베이스는 자동으로 다시 동기화됩니다. 외부에서 에셋을 변경했다면 데이터베이스 탭의 동기화 버튼을 사용합니다.

## 진행 보고

게임 코드에서 카테고리와 대상 값을 전달합니다.

```csharp
using SW.Quest;

SWQuestSystem.Instance.ReceiveReport(killCategory, slimeTarget.Value, 1);
```

문자열 코드로도 보고할 수 있습니다.

```csharp
SWQuestSystem.Instance.ReceiveReport("KILL", defeatedEnemy, 1);
```

대상이 없는 작업은 카테고리만 일치하면 보고를 받습니다. 카테고리도 없는 작업은 모든 카테고리 보고를 받을 수 있으므로 의도하지 않은 진행이 생기지 않도록 정의를 검토해야 합니다.

`SWQuestReporter`를 사용하면 메서드 직접 호출과 삼차원·이차원 트리거 진입을 같은 설정으로 연결할 수 있습니다. 충돌체 태그 목록이 비어 있으면 모든 충돌체를 허용합니다.

## 진행 계산 전략

| 타입 | 계산 방식 |
| --- | --- |
| 연결 없음 또는 `SWQuestAddProgressAction` | 현재 진행량에 변화량을 더합니다. |
| `SWQuestSetProgressAction` | 변화량을 현재 진행량으로 사용합니다. |
| `SWQuestPositiveProgressAction` | 양수 변화량만 더합니다. |
| `SWQuestNegativeProgressAction` | 음수 변화량만 더해 진행량을 감소시킵니다. |
| `SWQuestContinuousProgressAction` | 양수 변화량은 누적하고 0 이하이면 진행량을 0으로 되돌립니다. |

결과 진행량은 항상 0과 필요 진행량 사이로 제한됩니다. 프로젝트별 계산은 `SWQuestTaskAction`을 상속하고 `Calculate`를 구현합니다.

## 완료와 취소

일반 퀘스트가 모든 작업을 끝내면 `WaitingForCompletion` 상태가 됩니다. 자동 완료를 사용하거나 `Complete()`를 호출하면 보상을 지급하고 완료 목록으로 이동합니다.

```csharp
if (runtimeQuest.IsWaitingForCompletion)
{
    runtimeQuest.Complete();
}
```

`ForceComplete()`는 남은 모든 작업을 완료한 뒤 보상까지 지급하므로 개발 도구나 명시적인 건너뛰기 기능에서만 사용합니다.

취소 가능 설정과 모든 취소 조건을 충족한 일반 퀘스트만 `Cancel()`할 수 있습니다. 업적의 취소 호출은 항상 실패합니다.

## 사용자 정의 조건

조건은 시스템과 판정 대상 퀘스트를 모두 받습니다. 다른 퀘스트 완료 여부는 기본 `SWQuestCompletedCondition`으로 검사할 수 있습니다. 자동 초기화 전에 외부 문맥이 필요한 업적 조건이 있다면 시스템의 자동 불러오기를 끄고 문맥 연결 후 `Initialize`를 직접 호출합니다.

게임 데이터가 필요한 조건은 시스템 문맥을 사용합니다.

```csharp
using SW.Quest;

[UnityEngine.CreateAssetMenu(fileName = "MinimumLevelCondition_", menuName = "Game/Quest/Minimum Level Condition")]
public sealed class MinimumLevelCondition : SWQuestCondition
{
    public override bool IsMet(SWQuestSystem questSystem, SWQuest quest)
    {
        return questSystem.TryGetContext(out PlayerProgress progress)
            && progress.Level >= 10;
    }
}
```

시작 장면에서 `questSystem.SetContext(playerProgress)`처럼 문맥을 연결합니다.

## 사용자 정의 보상

보상 하나에서 예외가 발생해도 나머지 보상 지급은 계속됩니다. 완료 복원 과정에서는 보상을 다시 지급하지 않습니다.

```csharp
using SW.Quest;

[UnityEngine.CreateAssetMenu(fileName = "GoldReward_", menuName = "Game/Quest/Gold Reward")]
public sealed class GoldReward : SWQuestReward
{
    public override void Grant(SWQuestSystem questSystem, SWQuest quest)
    {
        if (questSystem.TryGetContext(out PlayerWallet wallet))
        {
            wallet.AddGold(Quantity);
        }
    }
}
```

`SWQuestRewardGrantedEvent`를 구독하면 보상 에셋을 프로젝트 서비스에 직접 연결하지 않고 별도 보상 처리기로 전달할 수도 있습니다.

## 이벤트

`SWQuestSystem`은 다음 지역 이벤트를 제공합니다.

- `QuestRegistered`, `QuestCompleted`, `QuestCanceled`
- `AchievementRegistered`, `AchievementUnlocked`
- `TaskProgressChanged`, `RewardGranted`

서로 직접 참조하면 안 되는 시스템에서는 `SWEventBus`로 발행되는 다음 이벤트 데이터를 구독합니다.

- `SWQuestRegisteredEvent`
- `SWQuestCompletedEvent`, `SWQuestCanceledEvent`
- `SWAchievementUnlockedEvent`
- `SWQuestTaskProgressChangedEvent`
- `SWQuestRewardGrantedEvent`

## 저장과 복원

간단한 게임은 암호화된 `SWPlayerPrefs` 기반 기능을 바로 사용할 수 있습니다.

```csharp
SWQuestSystem.Instance.Save();
SWQuestSystem.Instance.Load();
```

다른 저장소를 사용할 때는 자동 불러오기를 끄고 시스템 초기화 전에 `SetSaveStore`로 `ISWQuestSaveStore` 구현을 연결합니다.

게임 전체 저장 루트가 따로 있으면 퀘스트 데이터를 그 안에 포함합니다.

```csharp
[System.Serializable]
public sealed class GameSaveData
{
    public SWQuestSystemSaveData quests;
}

saveData.quests = SWQuestSystem.Instance.CreateSaveData();
SWQuestSystem.Instance.RestoreSaveData(saveData.quests);
```

저장 데이터에는 현재 작업 묶음뿐 아니라 모든 묶음과 모든 작업의 상태가 들어갑니다. 묶음과 작업을 코드명으로 대응하므로 정의에 항목이 추가되거나 순서가 달라져도 가능한 진행량을 복원하고, 사라진 항목은 경고합니다. 코드명이 없던 이전 저장 데이터는 배열 인덱스로 복원합니다. 완료된 퀘스트와 업적을 불러올 때는 이미 지급된 보상을 다시 지급하지 않습니다. 현재 구현보다 높은 형식 버전의 저장 데이터는 상태를 지우기 전에 거부합니다.

## 현재 검토가 필요한 부분

- 퀘스트와 업적 정의를 변경한 뒤 이전 저장 데이터가 모든 변경 사례에서 안전하게 복원되는지 추가 검증이 필요합니다.
- 대량의 퀘스트와 업적을 동시에 진행할 때 보고 처리 비용과 저장 데이터 크기를 측정해야 합니다.
- 프로젝트별 조건, 보상과 외부 문맥 객체의 생명주기 규칙을 실제 게임 흐름에서 검토해야 합니다.
- 화면 표시와 알림은 런타임 모듈에 포함하지 않습니다. 지역 이벤트, `SWEventBus` 이벤트와 읽기 전용 목록을 이용해 프로젝트 표현 계층에서 구현해야 합니다.
