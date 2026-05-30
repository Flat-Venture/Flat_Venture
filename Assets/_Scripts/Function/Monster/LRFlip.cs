using UnityEngine;

public class LRFlip : MonoBehaviour
{
    [Header("Detect")]
    public string playerLayerName = "Player";
    public float detectRange = 10f;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;
    public bool invertFlip = false;

    int playerLayer;
    int playerMask;

    Player player;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        playerMask = 1 << playerLayer;

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        player = Player.Instance;
    }

    void Update()
    {
        LookAtPCam();

        Transform playerTr = FindPlayerInRange();
        if (playerTr != null) FlipToward(playerTr.position);
    }

    void LookAtPCam()
    {
        if (player == null || player.pCam == null) return;

        Vector3 lookDir = player.pCam.transform.forward;
        spriteRenderer.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    Transform FindPlayerInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRange, playerMask);

        if (hits == null || hits.Length == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestTr = null;

        for (int i = 0; i < hits.Length; i++)
        {
            float d =
                (hits[i].transform.position - transform.position).sqrMagnitude;

            if (d < best)
            {
                best = d;
                bestTr = hits[i].transform;
            }
        }

        return bestTr;
    }

    void FlipToward(Vector3 targetPos)
    {
        bool targetIsLeft = targetPos.x < transform.position.x;
        bool flip = invertFlip ? !targetIsLeft : targetIsLeft;

        spriteRenderer.flipX = flip;
    }

}
