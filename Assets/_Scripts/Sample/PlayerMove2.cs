using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove2 : MonoBehaviour
{
    [Header("Move Settings")]
    public float moveSpeed = 5f;          // 이동 속도 (목표 속도)
    public float rotationSpeed = 150f;    // 회전 속도
    public float acceleration = 25f;      // 추가: 가속도 조절 (부드러운 움직임에 사용)

    [Header("Jump Settings")]
    public float jumpForce = 5f;          // 점프 힘
    public float groundCheckDistance = 1.1f; // 바닥 체크 거리

    private Rigidbody rb;
    private bool isGrounded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 넘어지지 않도록 설정
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        CheckGround();
        Move();
        Rotate();
    }

    void Update()
    {
        Jump();
    }

    // -----------------------------
    // 이동 (실무 방식: AddForce로 가속도 제어)
    // -----------------------------
    void Move()
    {
        float v = Input.GetAxis("Vertical");

        // 목표 방향 벡터
        Vector3 targetDirection = transform.forward * v;

        // 현재 XZ 평면의 속도 (Y축 속도(중력)는 유지)
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // 목표 속도
        Vector3 targetVelocity = targetDirection * moveSpeed;

        // 목표 속도와 현재 속도의 차이 (필요한 속도 변화량)
        Vector3 velocityDifference = targetVelocity - currentVelocity;

        // 가속도를 적용할 힘 계산
        // F = ma 공식에서 F = (속도 변화량 / 시간) * 질량
        // Time.fixedDeltaTime으로 나눠야 하지만, acceleration 변수를 도입하여
        // 이 값을 조절하여 부드러운 가속/감속 효과를 만듭니다.
        Vector3 force = velocityDifference * acceleration;

        // XZ 평면의 움직임에만 힘을 적용하고 Y축은 건드리지 않음
        rb.AddForce(force, ForceMode.Acceleration);
        // ForceMode.Acceleration: 질량을 무시하고 바로 가속도 값으로 적용 (매우 유용)
    }

    // -----------------------------
    // 회전
    // -----------------------------
    void Rotate()
    {
        float h = Input.GetAxis("Horizontal");

        // 회전은 그대로 MoveRotation을 사용하는 것이 가장 안정적입니다.
        Quaternion rot = Quaternion.Euler(0f, h * rotationSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * rot);
    }

    // -----------------------------
    // 점프
    // -----------------------------
    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // -----------------------------
    // 바닥 체크 (Raycast)
    // -----------------------------
    void CheckGround()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    void OnDrawGizmosSelected()
    {
        // Scene 뷰에서 바닥 체크 거리 확인용
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}