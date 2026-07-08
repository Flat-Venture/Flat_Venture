# 팀원용 시드 API 적용 가이드

> 시드 개념과 적용 원칙을 처음부터 확인하려면 [Flat Venture 시드 시스템 팀 설명서](./Seed_System_Team_Manual.md)를 먼저 읽어 주세요.

## 적용 범위

이 모듈은 런 콘텐츠를 재현하기 위한 MT19937 난수만 제공합니다. 맵, 몬스터, 아이템, 상점, 제련, 이벤트 담당자는 자신의 생성 코드에서 API를 호출합니다.

치명타, 회피, 화상 부여처럼 전투 도중 매번 판정하는 확률과 장식용 이펙트·사운드 난수에는 이 모듈을 사용하지 않습니다.

## 런 시작 시 서비스 만들기

마을에서는 다음 런에 사용할 6자리 문자열을 준비합니다.

```csharp
int nextSeed;
if (!SeedValue.TryParse(seedInputText, out nextSeed))
    nextSeed = SeedValue.Generate();

string displayedSeed = SeedValue.Format(nextSeed);
```

던전 입장 시 시드를 확정하고 서비스 인스턴스를 한 번 만듭니다.

```csharp
ISeedService seedService = new SeedService(nextSeed);
```

던전 진행 중에는 이 인스턴스를 교체하지 않습니다. 성공 또는 실패 후 마을로 돌아오면 다음 런용 새 시드를 준비합니다.

## 스트림 가져오기

담당 영역에 맞는 이름으로 스트림을 가져옵니다. 같은 서비스에서 같은 이름을 요청하면 소비 상태가 이어지는 같은 인스턴스가 반환됩니다.

```csharp
IRandomStream mapRandom = seedService.GetStream(SeedStreamNames.Map);
IRandomStream shopRandom = seedService.GetStream(SeedStreamNames.Shop);
IRandomStream secretRoomRandom = seedService.GetStream("SecretRoom");
```

기본 이름은 다음과 같습니다.

| 담당 영역 | 상수 | 실제 이름 |
|---|---|---|
| 지도 | `SeedStreamNames.Map` | `Map` |
| 몬스터 | `SeedStreamNames.Monster` | `Monster` |
| 아이템 | `SeedStreamNames.Item` | `Item` |
| 상점 | `SeedStreamNames.Shop` | `Shop` |
| 제련 | `SeedStreamNames.Forge` | `Forge` |
| 이벤트 | `SeedStreamNames.Event` | `Event` |

새 스트림 이름은 영문 PascalCase를 권장합니다. 이름은 대소문자를 구분하며 배포 후에는 변경하지 않습니다.

## 기본 API

```csharp
// uint 전체 범위
uint rawValue = shopRandom.NextUInt32();

// 정수: 0 포함, candidates.Count 제외
int index = shopRandom.Range(0, candidates.Count);

// 실수: 0.5 포함, 1.5 제외
float scale = shopRandom.Range(0.5f, 1.5f);

// 25% 확률
bool success = shopRandom.Chance(0.25f);

// 목록에서 하나 선택
ItemData selected = shopRandom.Pick(candidates);

// 전달한 목록 자체의 순서를 섞음
shopRandom.Shuffle(candidates);
```

`Chance(0)`과 `Chance(1)`도 호출 순서를 명확히 유지하기 위해 난수를 한 번 소비합니다.

## 가중치 선택

후보와 가중치를 같은 인덱스 순서로 전달합니다. 가중치가 0인 후보는 선택되지 않습니다.

```csharp
List<ItemData> candidates = new List<ItemData>();
List<float> weights = new List<float>();

for (int i = 0; i < sortedItemData.Count; i++)
{
    ItemData item = sortedItemData[i];
    candidates.Add(item);
    weights.Add(item.ShopWeight);
}

ItemData selected = shopRandom.WeightedPick(candidates, weights);
```

후보 목록을 직접 관리해야 한다면 인덱스만 받을 수 있습니다.

```csharp
int selectedIndex = shopRandom.WeightedIndex(weights);
```

## 호출 순서 규칙

한 스트림 내부에서는 호출 순서가 결과를 결정합니다. 생성 함수가 호출될 때 필요한 값을 정해진 순서로 한 번에 소비합니다.

`Update`, `LateUpdate`, 물리 프레임처럼 실행 횟수가 달라질 수 있는 위치에서는 런 콘텐츠 스트림을 소비하지 않습니다.

기능 하나의 호출 추가가 다른 콘텐츠 결과를 바꾸면 안 되는 경우 새 스트림으로 분리합니다. 예를 들어 상점과 비밀방은 각각 `Shop`, `SecretRoom` 스트림을 사용할 수 있습니다.

후보는 재현 가능한 고유 ID로 먼저 정렬한 뒤 난수를 적용합니다. 같은 난수여도 후보 순서가 달라지면 선택 결과가 바뀝니다.

## 상태 저장과 복원

시드만 다시 넣으면 스트림의 처음으로 돌아갑니다. 런 중간부터 이어야 할 때는 상태도 함께 저장합니다.

```csharp
RandomStreamState shopState = shopRandom.CaptureState();
shopRandom.RestoreState(shopState);
```

현재까지 생성된 모든 스트림 상태를 다룰 수도 있습니다.

```csharp
IReadOnlyList<RandomStreamState> states = seedService.CaptureAllStreamStates();
seedService.RestoreStreamStates(states);
```

이 모듈은 상태 DTO까지만 제공합니다. 파일에 기록하고 불러오는 작업은 세이브 담당자가 수행합니다. 복원할 때는 저장 당시와 같은 RunSeed로 `SeedService`를 만들어야 합니다.

## 버그 제보 시 필요한 정보

- RunSeed 6자리 값
- `SeedService.SeedAlgorithmVersion`
- 스트림 이름
- 문제가 발생한 막·방·노드
- 가능하면 `CallCount` 또는 저장된 `RandomStreamState`

팀 공통 재현 확인 시드는 `123456`입니다.

## 제공되는 검증 씬

`Assets/_Scenes/NUH/Test_08_SeedDebug.unity`에서 다음 항목을 콘텐츠 연결 없이 확인할 수 있습니다.

- 무작위 6자리 시드 생성
- 숫자 6자리 직접 입력과 적용
- 현재 시드 복사
- 기본 스트림 또는 임의 이름 스트림 선택
- 스트림의 다음 MT19937 값 5개와 호출 횟수 확인
- 동일 시드로 모든 스트림을 초기화한 뒤 첫 결과 재현

기준 시드 `123456`, `Shop` 스트림의 첫 5개 값은 다음과 같습니다.

```text
2857546384, 4238085333, 2201865542, 2819321446, 3862857544
```
