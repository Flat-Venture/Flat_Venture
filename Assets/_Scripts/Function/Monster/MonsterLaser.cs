using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MonsterLaser : MonoBehaviour
{
    [Header("Visual")]
    public LineRenderer line;
    public float width = 0.25f;
    public float maxLength = 18f;
    public LayerMask blockMask;

    [Header("Hit")]
    public LayerMask playerMask;
    public float hitHeight = 1.0f;

    bool firing;
    Vector3 origin;
    Vector3 dir;

    void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();

        line.enabled = false;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
    }

    /* ================== 외부 제어 ================== */

    public void BeginCharge()
    {
        firing = false;
        line.enabled = true;
        SetWidth(width * 0.3f); // 차지 중 얇게
    }

    public void BeginFire()
    {
        firing = true;
        SetWidth(width);
    }

    public void EndFire()
    {
        firing = false;
        line.enabled = false;
    }

    public void UpdateAim(Vector3 origin, Vector3 forward)
    {
        this.origin = origin;
        this.dir = forward.normalized;
        UpdateLine();
    }

    public void TryHit(float damage)
    {
        if (!firing) return;

        Vector3 center = origin + dir * (maxLength * 0.5f);
        Vector3 half = new Vector3(width * 0.5f, hitHeight, maxLength * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            center,
            half,
            Quaternion.LookRotation(dir),
            playerMask
        );

        foreach (var h in hits)
        {
            Player p = h.GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(damage);
                return;
            }
        }
    }

    /* ================== 내부 ================== */

    void UpdateLine()
    {
        Vector3 end = origin + dir * maxLength;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxLength, blockMask))
            end = hit.point;

        line.SetPosition(0, origin);
        line.SetPosition(1, end);
    }

    void SetWidth(float w)
    {
        line.startWidth = w;
        line.endWidth = w;
    }
}
