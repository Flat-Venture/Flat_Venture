# Flat Venture 시드 시스템 팀 설명서

> 대상: 맵, 몬스터, 아이템, 상점, 제련, 이벤트 등 랜덤 콘텐츠를 구현하는 팀원

## 1. 시드 시스템을 사용하는 이유

시드는 한 번의 던전 진행에서 어떤 랜덤 결과가 나올지 다시 재현하기 위한 시작값입니다.

예를 들어 RunSeed가 `123456`이고 상점 후보 목록과 난수 호출 순서가 같다면 다음 실행에서도 같은 상점 결과를 만들 수 있습니다. 버그 제보에 시드값을 함께 기록하면 다른 팀원이 같은 랜덤 상황을 다시 확인할 수 있습니다.

시드가 콘텐츠 결과 자체를 저장하는 것은 아닙니다. 시드는 같은 순서로 난수를 다시 생성하게 해주는 시작점입니다.

```text
RunSeed + 스트림 이름 + 호출 순서
→ 같은 난수열
→ 같은 후보 목록에 적용
→ 같은 콘텐츠 결과
```

따라서 같은 시드를 사용해도 후보 목록의 구성·정렬 또는 난수 호출 순서가 바뀌면 최종 결과가 달라질 수 있습니다.

## 2. 프로젝트의 확정 규칙

- 난수 알고리즘: MT19937 메르센 트위스터
- 알고리즘 버전: `SeedService.SeedAlgorithmVersion` 1
- 사용자에게 표시하는 시드: 숫자 6자리
- 자동 생성 시드: `100000`~`999999`
- 직접 입력 가능한 시드: `000000`~`999999`
- 같은 이름의 스트림 내부에서는 난수를 순서대로 소비
- 스트림끼리는 서로 독립
- Unity 업데이트 후에도 알고리즘 버전 1의 결과를 유지
- 콘텐츠 담당자는 `UnityEngine.Random`이나 `System.Random` 대신 제공된 API를 사용

## 3. 시드의 생명주기

```text
마을 진입
→ 다음 런에 사용할 6자리 시드 생성
→ 필요하면 디버그 입력으로 시드 교체
→ 던전 입장 시 SeedService 생성
→ 던전 진행 중 같은 SeedService 유지
→ 성공 또는 실패 후 마을 귀환
→ 다음 런을 위한 새 시드 생성
```

던전 진행 중에는 RunSeed를 변경하지 않습니다. 다른 시드를 사용하려면 새로운 런을 시작하면서 `SeedService`를 새로 만들어야 합니다.

```csharp
int runSeed = 123456;
ISeedService seedService = new SeedService(runSeed);
```

시드 입력 문자열이 비어 있다면 무작위 시드를 생성합니다.

```csharp
int runSeed;

if (string.IsNullOrEmpty(seedInputText))
{
    runSeed = SeedValue.Generate();
}
else if (!SeedValue.TryParse(seedInputText, out runSeed))
{
    // 사용자에게 "숫자 6자리를 입력해 주세요" 오류 표시
    return;
}

ISeedService seedService = new SeedService(runSeed);
```

화면에 표시하거나 복사할 때는 항상 `SeedValue.Format` 또는 `FormattedRunSeed`를 사용합니다. 정수 `1`도 화면에는 `000001`로 표시됩니다.

```csharp
string displayedSeed = seedService.FormattedRunSeed;
```

## 4. 스트림이란 무엇인가

스트림은 특정 콘텐츠 영역만 사용하는 독립된 난수열입니다.

```text
RunSeed 123456
├─ Map
├─ Monster
├─ Item
├─ Shop
├─ Forge
└─ Event
```

상점이 `Shop` 스트림에서 난수를 추가로 사용해도 `Map`이나 `Event` 스트림의 다음 결과는 바뀌지 않습니다.

모든 시스템이 전역 난수 하나를 함께 사용하면 상점 코드에 난수 호출 한 번을 추가했을 때 이후 맵·몬스터·아이템 결과가 전부 밀릴 수 있습니다. 이를 방지하기 위해 담당 영역별 스트림을 나눕니다.

기본 스트림은 상수로 제공합니다.

| 담당 영역 | 사용할 상수 | 실제 이름 |
|---|---|---|
| 맵·방·노드 | `SeedStreamNames.Map` | `Map` |
| 몬스터 구성 | `SeedStreamNames.Monster` | `Monster` |
| 보상·아이템 후보 | `SeedStreamNames.Item` | `Item` |
| 상점 | `SeedStreamNames.Shop` | `Shop` |
| 제련·강화 | `SeedStreamNames.Forge` | `Forge` |
| 이벤트 | `SeedStreamNames.Event` | `Event` |

기능을 더 세밀하게 분리해야 한다면 임의의 영문 이름을 사용할 수 있습니다.

```csharp
IRandomStream secretRoomRandom = seedService.GetStream("SecretRoom");
IRandomStream potionDropRandom = seedService.GetStream("PotionDrop");
```

스트림 이름은 대소문자를 구분합니다. `Shop`, `shop`, `SHOP`은 서로 다른 스트림입니다. 배포한 이름은 이후에 변경하지 않습니다.

## 5. 스트림을 가져오는 방법

담당 클래스는 생성 또는 초기화 시점에 필요한 스트림을 받아 보관하는 방식을 권장합니다.

```csharp
public sealed class ShopInventoryGenerator
{
    private readonly IRandomStream random;

    public ShopInventoryGenerator(ISeedService seedService)
    {
        random = seedService.GetStream(SeedStreamNames.Shop);
    }

    public void Generate()
    {
        // random을 정해진 순서로 사용
    }
}
```

같은 `SeedService`에서 같은 이름을 요청하면 새 스트림이 생기는 것이 아니라 기존 스트림이 반환됩니다. 따라서 소비 위치도 이어집니다.

```csharp
IRandomStream first = seedService.GetStream(SeedStreamNames.Shop);
IRandomStream second = seedService.GetStream(SeedStreamNames.Shop);

// first와 second는 같은 스트림 인스턴스입니다.
```

## 6. 기본 API 사용법

### 6.1 다음 32비트 값

특별한 변환이 필요한 경우에만 원본 값을 사용합니다.

```csharp
uint value = random.NextUInt32();
```

일반 콘텐츠에서는 아래 범위·선택 API를 우선 사용합니다.

### 6.2 정수 범위

최솟값은 포함하고 최댓값은 제외합니다.

```csharp
// 0, 1, 2 중 하나
int index = random.Range(0, 3);
```

목록 인덱스에는 다음과 같이 사용합니다.

```csharp
int index = random.Range(0, candidates.Count);
ItemData selected = candidates[index];
```

### 6.3 실수 범위

실수도 최솟값을 포함하고 최댓값을 제외합니다.

```csharp
float priceRate = random.Range(0.8f, 1.2f);
```

### 6.4 확률 판정

`0`은 0%, `1`은 100%입니다.

```csharp
bool success = random.Chance(0.25f);
```

호출 순서를 일정하게 유지하기 위해 `Chance(0f)`와 `Chance(1f)`도 난수를 한 번 소비합니다.

### 6.5 목록에서 하나 선택

```csharp
RoomData selectedRoom = random.Pick(roomCandidates);
```

빈 목록은 사용할 수 없습니다. 후보가 있는지 먼저 확인해야 합니다.

### 6.6 목록 순서 섞기

`Shuffle`은 전달한 목록 자체의 순서를 변경합니다.

```csharp
random.Shuffle(nodeCandidates);
```

원본 목록을 보존해야 한다면 복사본을 만들어 전달합니다.

```csharp
List<NodeData> shuffled = new List<NodeData>(nodeCandidates);
random.Shuffle(shuffled);
```

### 6.7 가중치 선택

후보와 가중치를 같은 인덱스 순서로 전달합니다.

```csharp
List<ItemData> candidates = new List<ItemData>();
List<float> weights = new List<float>();

for (int i = 0; i < sortedItems.Count; i++)
{
    ItemData item = sortedItems[i];
    candidates.Add(item);
    weights.Add(item.ShopWeight);
}

ItemData selected = random.WeightedPick(candidates, weights);
```

- 후보와 가중치 개수는 같아야 합니다.
- 가중치는 0 이상이어야 합니다.
- 가중치가 0인 후보는 선택되지 않습니다.
- 모든 가중치가 0이면 사용할 수 없습니다.

후보를 직접 선택하고 싶다면 인덱스만 받을 수 있습니다.

```csharp
int selectedIndex = random.WeightedIndex(weights);
```

## 7. 시스템별 적용 예시

### 7.1 상점 품목 생성

```csharp
public void GenerateShopItems(
    ISeedService seedService,
    IReadOnlyList<ItemData> sortedCandidates,
    List<ItemData> result)
{
    IRandomStream random = seedService.GetStream(SeedStreamNames.Shop);
    List<float> weights = new List<float>(sortedCandidates.Count);

    for (int i = 0; i < sortedCandidates.Count; i++)
        weights.Add(sortedCandidates[i].ShopWeight);

    result.Clear();
    for (int slot = 0; slot < 4; slot++)
        result.Add(random.WeightedPick(sortedCandidates, weights));
}
```

### 7.2 이벤트 성공 여부

```csharp
public bool DetermineForgeSuccess(ISeedService seedService, float successRate)
{
    IRandomStream random = seedService.GetStream(SeedStreamNames.Forge);
    return random.Chance(successRate);
}
```

### 7.3 랜덤 방 순서

```csharp
public List<RoomData> CreateRoomOrder(
    ISeedService seedService,
    IReadOnlyList<RoomData> sortedRooms)
{
    List<RoomData> result = new List<RoomData>(sortedRooms);
    IRandomStream random = seedService.GetStream(SeedStreamNames.Map);
    random.Shuffle(result);
    return result;
}
```

## 8. 호출 순서가 중요한 이유

스트림은 난수를 순서대로 소비합니다.

```text
Shop 스트림
#1 → 첫 번째 판매 아이템
#2 → 두 번째 판매 아이템
#3 → 세 번째 판매 아이템
```

중간에 가격 변동용 난수 호출을 추가하면 이후 호출 번호가 밀립니다.

```text
변경 후
#1 → 첫 번째 판매 아이템
#2 → 첫 번째 가격 변동
#3 → 두 번째 판매 아이템
```

이 변화는 `Shop` 스트림 안의 이후 결과에는 영향을 줍니다. 그러나 독립된 `Map`, `Monster`, `Event` 스트림에는 영향을 주지 않습니다.

호출 순서를 유지하려면 다음 규칙을 지킵니다.

1. 필요한 난수는 콘텐츠 생성 함수 안에서 정해진 순서로 소비합니다.
2. `Update`, `LateUpdate`, 물리 프레임에서 런 콘텐츠 스트림을 소비하지 않습니다.
3. 프레임 수, 오브젝트 탐색 순서, 비동기 완료 순서에 따라 호출 횟수가 달라지지 않게 합니다.
4. 기능 하나의 변경이 다른 결과에 영향을 주면 별도 스트림으로 분리합니다.
5. 조건에 따라 난수를 소비할지 결정하는 코드는 변경 시 이후 결과가 달라진다는 점을 인지합니다.

## 9. 후보 목록 정렬 규칙

같은 난수 인덱스가 나와도 후보 순서가 다르면 다른 콘텐츠가 선택됩니다.

```text
인덱스 1

[검, 활, 지팡이] → 활
[활, 검, 지팡이] → 검
```

후보 목록은 난수를 적용하기 전에 변하지 않는 고유 ID 기준으로 정렬해야 합니다.

```csharp
// 예시: 데이터 로딩 단계에서 StableId 오름차순으로 정렬
sortedCandidates.Sort(CompareByStableId);
```

`FindObjectsByType`, `Dictionary`, `HashSet`의 순회 결과를 그대로 후보 순서로 사용하지 않습니다. 실행 환경이나 콘텐츠 구성에 따라 순서가 달라질 수 있습니다.

후보의 추가·삭제 또는 정렬 기준 변경은 기존 시드 결과를 바꿀 수 있습니다.

## 10. 시드를 적용할 대상

다음은 런 콘텐츠 재현용 시드 적용 대상입니다.

- 막과 지역 테마
- 노드 지도 형태와 연결
- 방 종류와 배치
- 일반·엘리트·보스 방 후보
- 몬스터 종류, 등급, 수량, 스폰 위치
- 엘리트 특성
- 방 구조와 환경 오브젝트
- 보상 아이템 후보
- 아이템 등급, 옵션, 속성 후보
- 상점 품목과 가격 변동
- 제련·강화·승급 결과
- 이벤트 종류와 선택 결과
- 미지 노드 결과
- 포션과 구조물 드롭
- 리롤 결과

## 11. 시드를 적용하지 않을 대상

다음 실시간 전투·연출 확률은 런 콘텐츠 스트림에서 제외합니다.

- 치명타
- 회피
- 화상 등 전투 중 상태 이상 부여 확률
- 공격 중 실시간 행운 판정
- 장식용 이펙트 무작위
- 사운드 피치 변화

이 확률들이 `Map`, `Shop`, `Item` 같은 콘텐츠 스트림을 소비하면 전투 횟수에 따라 이후 콘텐츠 결과가 달라집니다.

## 12. 상태 저장과 복원

시드만 저장하면 스트림의 처음부터 같은 결과를 만들 수 있습니다. 런 중간 위치에서 이어하려면 각 스트림의 현재 상태도 저장해야 합니다.

```csharp
RandomStreamState state = random.CaptureState();
random.RestoreState(state);
```

생성된 모든 스트림 상태를 한 번에 받을 수도 있습니다.

```csharp
IReadOnlyList<RandomStreamState> states = seedService.CaptureAllStreamStates();
seedService.RestoreStreamStates(states);
```

`RandomStreamState`에는 다음 정보가 포함됩니다.

- 알고리즘 버전
- RunSeed
- 스트림 이름
- MT19937 내부 상태
- 다음 값을 읽을 위치
- 누적 호출 횟수

다른 RunSeed, 다른 스트림 이름, 다른 알고리즘 버전의 상태는 복원할 수 없습니다.

상태 DTO를 파일로 저장하고 불러오는 작업은 세이브 담당자의 영역입니다. 난수 모듈은 상태 생성과 복원 API까지만 제공합니다.

## 13. 디버그 씬 사용법

검증 씬:

```text
Assets/_Scenes/NUH/Test_08_SeedDebug.unity
```

확인 순서:

1. 씬을 실행합니다.
2. RunSeed에 `123456`을 입력하고 `시드 적용`을 누릅니다.
3. 스트림에서 `Shop`을 선택합니다.
4. `다음 값 5개`를 누릅니다.
5. `동일 시드 재실행`을 누릅니다.
6. 재실행 후 출력된 값이 첫 결과와 같은지 확인합니다.

기준 시드 `123456`, `Shop` 스트림의 첫 값 5개는 다음과 같습니다.

```text
2857546384, 4238085333, 2201865542, 2819321446, 3862857544
```

`Map` 값을 여러 번 소비한 뒤 동일 시드의 `Shop` 첫 값을 확인해도 위 결과가 유지되어야 합니다.

## 14. 버그 제보에 포함할 정보

- 화면에 표시된 RunSeed 6자리
- `SeedService.SeedAlgorithmVersion`
- 문제가 발생한 스트림 이름
- 문제가 발생한 막, 방, 노드 또는 상점 슬롯
- 가능하면 해당 스트림의 `CallCount`
- 후보 목록의 고유 ID와 정렬 순서

디버그 출력 예시:

```text
RunSeed=123456
Version=1
Stream=Shop
CallCount=5
Node=Shop_02
```

## 15. 자주 하는 실수

### 같은 시드인데 결과가 다름

다음을 순서대로 확인합니다.

1. 스트림 이름과 대소문자가 같은가?
2. 후보 목록의 개수와 정렬 순서가 같은가?
3. 난수 호출 순서와 횟수가 같은가?
4. 프레임이나 오브젝트 탐색 순서에 따라 호출하고 있지 않은가?
5. 알고리즘 버전이 같은가?

### 시드를 다시 넣었는데 중간부터 이어지지 않음

시드는 스트림의 처음을 재현합니다. 중간부터 이어하려면 `RandomStreamState`도 복원해야 합니다.

### 다른 시스템을 수정했는데 내 결과도 바뀜

같은 스트림을 여러 기능이 공유하고 있는지 확인합니다. 서로 영향을 주면 별도 스트림 이름으로 분리합니다.

### 목록 선택 결과가 에디터와 빌드에서 다름

정렬되지 않은 `Dictionary`, `HashSet`, 씬 오브젝트 탐색 결과를 후보로 사용했는지 확인합니다. 고유 ID로 명시적으로 정렬해야 합니다.

## 16. 최종 체크리스트

- [ ] 던전 입장 시 전달받은 하나의 `ISeedService`를 사용하는가?
- [ ] 담당 기능에 맞는 스트림 이름을 사용하는가?
- [ ] `UnityEngine.Random`과 `System.Random`을 사용하지 않는가?
- [ ] 후보 목록을 안정적인 고유 ID로 정렬했는가?
- [ ] 난수를 프레임마다 소비하지 않는가?
- [ ] 정수·실수 범위의 최댓값이 제외된다는 점을 반영했는가?
- [ ] 가중치 목록과 후보 목록의 순서·개수가 같은가?
- [ ] 기존 호출 순서를 바꿀 때 재현 결과가 달라짐을 확인했는가?
- [ ] 버그 로그에 RunSeed, 버전, 스트림 이름을 남기는가?

빠른 API 확인은 [Seed_API_Team_Guide.md](./Seed_API_Team_Guide.md), 확정 설계 근거는 [Seed_Design_Decisions.md](./Seed_Design_Decisions.md)를 참고합니다.
