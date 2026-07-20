using UnityEngine;
using UnityEngine.UI;

/// <summary>마법사 부채꼴 파이어볼의 조준, 시전, 쿨타임, 풀 상태를 표시하는 테스트 전용 UI입니다.</summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class MageActiveSkillDebugView : MonoBehaviour
{
    // 부채꼴 파이어볼 상태를 읽을 컨트롤러, 플레이어 위치, 출력 Text입니다.
    [SerializeField] private MageActiveSkillController skill;
    [SerializeField] private PlayerController player;
    [SerializeField] private Text output;

    // 우클릭 조준 중 중앙 방향과 좌우 경계 방향을 보여줄 선입니다.
    private LineRenderer aimLine;

    /// <summary>조준선의 점 수, 두께, 테스트 재질을 준비합니다.</summary>
    private void Awake()
    {
        aimLine = GetComponent<LineRenderer>();
        aimLine.positionCount = 6;
        aimLine.startWidth = 0.06f;
        aimLine.endWidth = 0.06f;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
    }

    /// <summary>조준선과 상태 Text를 매 프레임 갱신합니다.</summary>
    private void Update()
    {
        if (skill == null || player?.RuntimeState == null)
            return;

        aimLine.enabled = skill.IsAiming;
        if (skill.IsAiming)
            DrawSpreadGuide();

        if (output != null)
        {
            output.text =
                $"Mage Fireball Fan\n" +
                $"Aiming: {skill.IsAiming}\n" +
                $"Casting: {skill.IsCasting}\n" +
                $"Cooldown: {skill.CooldownRemaining:0.00}s\n" +
                $"No Cooldown: {skill.IgnoreCooldown}\n" +
                $"Count: {player.RuntimeState.ActiveSkillProjectileCount} / Spread: {skill.TotalSpreadAngle:0}°\n" +
                $"Range: {player.RuntimeState.ActiveSkillRange:0.0}m / Damage: {player.RuntimeState.ActiveSkillDamage:0}\n" +
                $"Pool: {skill.PoolActiveCount} active / {skill.PoolInactiveCount} inactive / {skill.PoolTotalCount} total";
        }
    }

    /// <summary>중앙 방향과 좌우 30도 경계를 한 LineRenderer로 표시합니다.</summary>
    private void DrawSpreadGuide()
    {
        Vector3 start = player.transform.position + (Vector3.up * 0.2f);
        float range = player.RuntimeState.ActiveSkillRange;
        float halfAngle = skill.TotalSpreadAngle * 0.5f;
        Vector3 left = Quaternion.AngleAxis(-halfAngle, Vector3.up) * skill.AimDirection;
        Vector3 center = skill.AimDirection;
        Vector3 right = Quaternion.AngleAxis(halfAngle, Vector3.up) * skill.AimDirection;

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, start + (left * range));
        aimLine.SetPosition(2, start);
        aimLine.SetPosition(3, start + (center * range));
        aimLine.SetPosition(4, start);
        aimLine.SetPosition(5, start + (right * range));
        aimLine.startColor = aimLine.endColor = new Color(1f, 0.35f, 0.05f);
    }

    /// <summary>UI 버튼에서 마법사 액티브 스킬 쿨타임 무시 상태를 전환합니다.</summary>
    public void ToggleNoCooldown()
    {
        if (skill != null)
            skill.ToggleIgnoreCooldown();
    }
}
