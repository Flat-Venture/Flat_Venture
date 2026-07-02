# 플레이어 모듈 구조

## 런타임 책임

| 스크립트 | 책임 |
|---|---|
| `PlayerController` | SO 원본으로 런타임 상태 생성, 플레이어 위치와 상태 초기화 |
| `PlayerRuntimeState` | 현재 HP와 런 중 변경되는 능력치·무적 상태 보관 |
| `PlayerInputReader` | Input Actions 값을 읽고 대시·액티브 스킬 입력 이벤트 전달 |
| `PlayerLocomotionController` | 카메라 기준 이동, 대시, 대시 충전 |
| `PlayerHealthController` | 피해, 피격 무적, 사망 시 입력 차단 |
| `PlayerAimResolver` | 마우스 화면 좌표를 XZ 평면 조준 방향으로 변환 |
| `PlayerBasicAttackController` | 자동 대상 탐색, 수동 조준, 근접 공격 판정과 쿨타임 |
| `WarriorSwordWaveController` | 검기 조준, 시전, 연속 발사, 쿨타임과 투사체 풀 관리 |
| `WarriorSwordWaveProjectile` | 검기 이동, 지형 보정, 관통 타격, 벽 충돌과 풀 반환 |
| `ComponentObjectPool<T>` | 검기·화살·마법 투사체에 공통으로 사용할 Component 풀 |

## 흐름

```text
PlayerStatsData (SO 원본)
  → PlayerController
  → PlayerRuntimeState (런타임 복사본)

PlayerInputReader
  ├─ PlayerLocomotionController
  ├─ PlayerBasicAttackController
  └─ WarriorSwordWaveController

PlayerAimResolver
  ├─ PlayerBasicAttackController
  └─ WarriorSwordWaveController

PlayerHealthController
  → PlayerRuntimeState.ApplyDamage
  → 사망 이벤트
  → PlayerInputReader 비활성화

WarriorSwordWaveController
  → ComponentObjectPool<WarriorSwordWaveProjectile>
  → 검기 대여/반환
```

## 구현 규칙

- ScriptableObject 원본은 플레이 중 수정하지 않는다.
- 이동·피격·조준·공격·스킬은 서로의 내부 로직을 직접 수행하지 않는다.
- 기본 공격과 액티브 스킬은 `PlayerAimResolver`의 조준 계산을 공유한다.
- 반복 생성되는 투사체는 `ComponentObjectPool<T>`를 사용한다.
- 런타임 코드에서 익명 람다식을 사용하지 않고 명시적인 메서드와 메서드 그룹을 사용한다.
- 디버그 스크립트는 `Player/Debug`에 격리하며 게임 규칙의 소유자가 되지 않는다.
