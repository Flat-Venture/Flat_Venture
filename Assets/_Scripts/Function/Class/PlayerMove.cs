using JetBrains.Annotations;
using System.Collections;
using UnityEngine;
using static Enums;

public class PlayerMove : MonoBehaviour
{

    CharacterController cc;
    Transform cam;
    PlayerAttack pa;

    public bool canMove = true;         //외부에서 사용해야해서 public 생성

    //Player 캐싱
    Player player;

    //대쉬 거리, 대쉬 시간
    public float dashCooldown = 1f;
    public float dashDuration = 0.125f;
    public float dashSpeed = 30f;
    float lastDashTime = -999f;
    bool isDashing;

    const float face_threshold = 0.2f;          //캐릭터 떨림 방지

    void Start()
    {
        cc = GetComponent<CharacterController>();

        pa = GetComponent<PlayerAttack>();
        cam = Camera.main.transform;
        player = Player.Instance;
    }

    void Update()
    {
        if (!GameStateManager.Instance.CanPlayerControl()) return;  // 게임 플레이 상태가 아니면(일시정지나 레벨업 도중이면 움직이지 못함)
        Move();
    }

    void Move()
    {
        if (!canMove)
        {
            player.animCtrl.ChangeState(PlayerAnimState.Idle);
            return;       //canMove상태가 아니면 못움직임
        }


        float mvSpd = player.finalStats.moveSpeed;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        if (player != null && player.IsInputReversed)
        {
            x *= -1;
            z *= -1;
        }

        //카메라 기준 방향 계산
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;



        //위 / 아래 각도 무시
        camForward.y = 0f;
        camRight.y = 0f;

        //노멀라이즈
        camForward.Normalize();
        camRight.Normalize();

        //카메라 기준 이동
        Vector3 dir = camForward * z + camRight * x;

        cc.Move(dir * mvSpd * Time.deltaTime);


        //움직일 경우 Move로 바뀌기
        if (x != 0 || z != 0)
        {
            player.animCtrl.ChangeState(PlayerAnimState.Move);
        }
        else
        {
            player.animCtrl.ChangeState(PlayerAnimState.Idle);
        }

        //sprite돌리기
        if (dir.sqrMagnitude > 0.1f)
        {
            float dot = Vector3.Dot(dir, cam.right);

            if(dot > face_threshold)
            {
                player.SetFacing(1f);
            }
            else if( dot < -face_threshold)
            {
                player.SetFacing(-1f);
            }
               //player.SetFacing(dot);
        }

        //대쉬기능 = 스페이스바
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //이동키를 안누르면 대쉬 안됨
            if (dir.sqrMagnitude > 0.01f)
            {
                //방향만 전달해주고, Dash()에서 크기 알아서 적용함
                Dash(dir.normalized);
            }
        }
    }

    public void Dash(Vector3 dir)
    {
        //대쉬중, 쿨타임중이면 return
        if (isDashing || GetDashCooldownRemaining() > 0f) return;

        lastDashTime = Time.time;
        StartCoroutine(CoDash(dir));
    }

    IEnumerator CoDash(Vector3 dir)
    {
        isDashing = true;

        player.AddInvincible(InvincibleReason.Dash);

        //사운드
        SoundManager.Instance.PlaySFX("dash");

        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;
            cc.Move(dir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        player.RemoveInvincible(InvincibleReason.Dash);
        isDashing = false;
    }

    public float GetDashCooldownRemaining()
    {
        float endTime = lastDashTime + dashCooldown;
        return Mathf.Max(0f, endTime - Time.time);
    }
}
