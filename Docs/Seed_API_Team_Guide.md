# 팀원용 시드 API 적용 가이드

> 목적: 각 담당자가 의현의 시드 모듈을 자신의 콘텐츠에 일관되게 적용하기 위한 규칙

## 핵심 규칙

1. `UnityEngine.Random`이나 `System.Random`을 시드 적용 콘텐츠에 사용하지 않는다.
2. 자신이 담당한 시스템 이름으로 독립 스트림을 얻는다.
3. 스트림은 콘텐츠 생성 시점에만 순서대로 사용한다.
4. Update, LateUpdate, 물리 프레임 안에서 콘텐츠 스트림을 소비하지 않는다.
5. 스트림 이름은 배포 후 함부로 바꾸지 않는다.
6. 치명타·회피 등 실시간 전투 확률에는 이 모듈을 사용하지 않는다.
7. 후보 목록은 안정적인 콘텐츠 ID로 정렬한 뒤 난수를 적용한다.

## 담당별 권장 스트림

| 담당 영역 | 스트림 이름 | 주요 용도 |
|---|---|---|
| 지도 | `Map` | 테마, 노드, 방 종류와 연결 |
| 몬스터 | `Monster` | 종류, 등급, 수량, 위치, 엘리트 특성 |
| 아이템 | `Item` | 보상 후보, 등급, 옵션, 속성, 리롤 |
| 상점 | `Shop` | 판매 품목, 가격 변동 후보 |
| 제련소 | `Forge` | 강화·승급 성공 여부와 결과 |
| 이벤트 | `Event` | 이벤트 종류와 선택 결과 |

새 시스템은 의미가 명확하고 변하지 않을 영문 이름을 사용한다. 예: `PotionDrop`, `SecretRoom`.

## 기본 사용 흐름

아래 코드는 최종 클래스명이 확정되기 전의 개념 예시다.

```csharp
// 현재 런에 이미 확정된 시드 서비스에서 담당 스트림을 가져온다.
IRandomStream stream = seedService.GetStream("Shop");

// 정수 범위: 최소 포함, 최대 제외
int index = stream.Range(0, candidates.Count);

// 확률은 0~1
bool success = stream.Chance(0.25f);

// 목록 선택과 셔플은 공통 API 사용
ItemData selected = stream.Pick(candidates);
stream.Shuffle(candidates);

// 가중치 선택
ItemData weighted = stream.WeightedPick(candidates, item => item.Weight);
```

## 올바른 사용 예

```csharp
public void GenerateShopInventory()
{
    IRandomStream random = seedService.GetStream("Shop");

    for (int i = 0; i < slotCount; i++)
    {
        inventory[i] = random.WeightedPick(itemPool, item => item.ShopWeight);
    }
}
```

상점 생성 함수가 한 번 호출될 때 필요한 값을 순서대로 뽑는다.

## 피해야 할 사용 예

```csharp
private void Update()
{
    if (seedService.GetStream("Shop").Chance(0.1f))
    {
        // 프레임 수에 따라 호출 횟수가 달라져 재현되지 않는다.
    }
}
```

```csharp
int index = UnityEngine.Random.Range(0, candidates.Count);
// 시드 서비스와 무관하므로 같은 RunSeed를 재현할 수 없다.
```

## 스트림 호출 순서 규칙

같은 스트림 내부에서는 호출 순서가 결과의 일부다. 이미 출시 또는 테스트 기준으로 사용된 생성 코드 중간에 새 호출을 넣으면 이후 결과가 달라질 수 있다.

변경 영향이 큰 기능은 별도 스트림으로 분리한다.

```text
권장
Shop
Forge
Event

비권장
Content 하나에 상점·제련·이벤트를 모두 넣기
```

## 저장과 복원

런 중 같은 스트림을 이어서 사용해야 한다면 시드만 다시 넣지 말고 모듈이 제공하는 스트림 상태를 저장한다. 상태 DTO 생성과 복원은 시드 API가 담당하고, 파일에 기록하는 작업은 세이브 담당자가 수행한다.

## 디버깅 절차

버그를 전달할 때 다음 정보를 함께 기록한다.

- RunSeed
- SeedAlgorithmVersion
- 스트림 이름
- 문제가 발생한 막·방·노드
- 해당 스트림의 가능하면 호출 순번 또는 저장 상태

기준 시드 `123456`으로 먼저 재현한 뒤 다른 시드에서도 확인한다.
