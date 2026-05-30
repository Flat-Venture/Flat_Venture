using UnityEngine;
using static Enums;

public class PlayerAnimController : MonoBehaviour
{
    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void ChangeState(PlayerAnimState newState)
    {
        //공격중 일때는 공격을 유지해주기
        //if (currentState == PlayerAnimState.Attack && newState != PlayerAnimState.Attack) return;

        //if (currentState == newState) return;
         
        //currentState = newState;

        //일단 bool 초기화
        anim.SetBool("isIdling", false);
        anim.SetBool("isMoving", false);

        switch (newState)
        {
            case PlayerAnimState.Idle:
                anim.SetBool("isIdling", true);
                break;

            case PlayerAnimState.Move:
                anim.SetBool("isMoving", true);
                break;

            case PlayerAnimState.Attack:
                anim.SetTrigger("isAttacking");
                Debug.Log("Attack상태");
                break;
        }
    }
}
