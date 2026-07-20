using UnityEngine;

/// <summary>카메라 기준 이동, 대시와 대시 충전만 담당합니다.</summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerLocomotionController : MonoBehaviour
{
    // 카메라의 forward/right를 기준으로 WASD 방향을 월드 XZ 방향으로 변환할 때 사용합니다.
    [Tooltip("비어 있으면 Main Camera를 이동 기준으로 사용합니다.")]
    [SerializeField] private Transform movementReference;

    // 같은 플레이어 오브젝트에 있는 모듈 참조입니다.
    private PlayerController player;
    private PlayerInputReader inputReader;
    private CharacterController characterController;
    // 대시가 시작되는 순간의 방향을 저장해 대시 중 방향전환을 막습니다.
    private Vector3 dashDirection;
    // CharacterController는 중력을 자동 적용하지 않으므로 수직 속도를 직접 누적합니다.
    private float verticalVelocity;
    // 0보다 크면 현재 대시 중입니다.
    private float dashTimeRemaining;
    // 다음 대시 충전이 회복되기까지 남은 시간입니다.
    private float dashRechargeRemaining;
    // 지금 즉시 사용할 수 있는 대시 횟수입니다.
    private int dashCharges;

    /// <summary>런타임 능력치에 짧게 접근하기 위한 내부 속성입니다.</summary>
    private PlayerRuntimeState State { get { return player.RuntimeState; } }

    public bool IsDashing { get { return dashTimeRemaining > 0f; } }
    public int DashCharges { get { return dashCharges; } }
    public int MaxDashCharges { get { return State?.MaxDashCharges ?? 0; } }
    public float DashRechargeRemaining { get { return dashRechargeRemaining; } }

    /// <summary>Awake에서 같은 오브젝트의 필수 참조와 카메라 기준을 확보합니다.</summary>
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        inputReader = GetComponent<PlayerInputReader>();
        characterController = GetComponent<CharacterController>();

        if (movementReference == null && Camera.main != null)
            movementReference = Camera.main.transform;
    }

    /// <summary>상태 초기화 이벤트를 구독하고 첫 대시 충전값을 채웁니다.</summary>
    private void Start()
    {
        State.ResetCompleted += ResetLocomotion;
        ResetLocomotion();
    }

    /// <summary>대시 입력 이벤트를 구독합니다.</summary>
    private void OnEnable()
    {
        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
        inputReader.DashPressed += TryStartDash;
    }

    /// <summary>이벤트를 해제하고 대시 무적이 남지 않게 정리합니다.</summary>
    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.DashPressed -= TryStartDash;
        State?.SetInvincibility(InvincibilityReason.Dash, false);
    }

    /// <summary>런타임 상태가 파괴되기 전에 초기화 이벤트 구독을 해제합니다.</summary>
    private void OnDestroy()
    {
        if (State != null)
            State.ResetCompleted -= ResetLocomotion;
    }

    /// <summary>매 프레임 충전을 갱신하고 일반 이동 또는 대시 이동 하나만 실행합니다.</summary>
    private void Update()
    {
        if (State == null || State.IsDead)
            return;

        UpdateDashRecharge();
        if (IsDashing)
            MoveDash();
        else
            MoveNormally();
    }

    /// <summary>테스트 UI에서 최대 대시 수를 바꾸고 즉시 모두 충전합니다.</summary>
    public void SetDebugMaxDashCharges(int value)
    {
        State.SetMaxDashCharges(value);
        dashCharges = State.MaxDashCharges;
        dashRechargeRemaining = 0f;
    }

    /// <summary>대시 시간·중력·충전·무적을 원본 시작 상태로 되돌립니다.</summary>
    private void ResetLocomotion()
    {
        dashTimeRemaining = 0f;
        dashRechargeRemaining = 0f;
        verticalVelocity = 0f;
        dashCharges = State.MaxDashCharges;
        State.SetInvincibility(InvincibilityReason.Dash, false);
    }

    /// <summary>카메라 기준 입력 방향에 이동속도와 수동 중력을 적용합니다.</summary>
    private void MoveNormally()
    {
        Vector3 planarDirection = GetPlanarMoveDirection(inputReader.Move);
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = planarDirection * State.MoveSpeed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>대시 가능 조건을 검사하고 방향·시간·무적·충전 상태를 확정합니다.</summary>
    private void TryStartDash()
    {
        if (State == null || State.IsDead || IsDashing || dashCharges <= 0)
            return;

        Vector3 requestedDirection = GetPlanarMoveDirection(inputReader.Move);
        if (requestedDirection.sqrMagnitude <= 0.0001f)
            return;

        dashDirection = requestedDirection.normalized;
        dashTimeRemaining = State.DashDuration;
        dashCharges--;
        State.SetInvincibility(InvincibilityReason.Dash, true);

        if (dashCharges == State.MaxDashCharges - 1)
            dashRechargeRemaining = State.DashRechargeCooldown;
    }

    /// <summary>고정된 방향으로 남은 대시 시간 동안 CharacterController를 이동시킵니다.</summary>
    private void MoveDash()
    {
        float dashSpeed = State.DashDistance / State.DashDuration;
        characterController.Move(dashDirection * (dashSpeed * Time.deltaTime));
        dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - Time.deltaTime);
        if (dashTimeRemaining <= 0f)
            State.SetInvincibility(InvincibilityReason.Dash, false);
    }

    /// <summary>소모된 대시를 한 번에 하나씩 순차 충전합니다.</summary>
    private void UpdateDashRecharge()
    {
        if (dashCharges >= State.MaxDashCharges)
        {
            dashRechargeRemaining = 0f;
            return;
        }

        dashRechargeRemaining -= Time.deltaTime;
        if (dashRechargeRemaining > 0f)
            return;

        dashCharges++;
        dashRechargeRemaining = dashCharges < State.MaxDashCharges ? State.DashRechargeCooldown : 0f;
    }

    /// <summary>2D 입력을 카메라 기준 월드 XZ 이동 방향으로 변환합니다.</summary>
    private Vector3 GetPlanarMoveDirection(Vector2 input)
    {
        if (movementReference == null)
            return new Vector3(input.x, 0f, input.y);

        Vector3 forward = movementReference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = movementReference.right;
        right.y = 0f;
        right.Normalize();
        return Vector3.ClampMagnitude((right * input.x) + (forward * input.y), 1f);
    }
}
