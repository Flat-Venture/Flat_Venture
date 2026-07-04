using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 클릭으로 몬스터를 타격하여 넉백과 피격을 테스트하는 임시 스크립트
/// </summary>
public class DummyPlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("한 번 클릭할 때 들어갈 데미지")]
    public float damage = 10f;
    
    [Tooltip("몬스터를 밀쳐낼 넉백 힘")]
    public float knockbackPower = 15f;
    
    [Tooltip("타격할 몬스터의 레이어 (Default 등으로 몬스터 레이어 설정 필수)")]
    public LayerMask monsterLayer;

    private void Update()
    {
        //마우스 좌클릭 시 공격 실행
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ShootRay();
    }

    private void ShootRay()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        // [시각화] 씬 뷰(Scene View)에서 마우스를 클릭한 방향으로 빨간 레이저를 3초간 그립니다.
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 3f);

        // 1단계: 레이저가 Enemy 레이어를 가진 콜라이더를 쳤는가?
        if (Physics.Raycast(ray, out hit, 100f, monsterLayer))
        {
            Debug.Log($"<color=white>[Raycast 확인]</color> '{hit.collider.name}' 오브젝트의 콜라이더를 명중시켰습니다!");

            // 2단계: 맞춘 오브젝트에 MonsterController 몸통이 제대로 붙어있는가?
            MonsterController monster = hit.collider.GetComponent<MonsterController>();
            
            if (monster != null)
            {
                Vector3 pushDirection = (monster.transform.position - transform.position).normalized;
                pushDirection.y = 0;

                monster.ApplyDamageAndKnockback(damage, pushDirection * knockbackPower);
                
                Debug.Log($"<color=yellow>[Dummy Attack]</color> {monster.name} 타격 완벽 성공!");
            }
            else
            {
                // 맞추긴 했는데 컨트롤러가 없을 때 (원인 2번)
                Debug.LogWarning($"<color=orange>[경고]</color> 맞춘 오브젝트에 MonsterController 스크립트가 없습니다!");
            }
        }
        else
        {
            // 아예 허공을 쳤을 때 (원인 1번)
            Debug.Log("<color=grey>[Miss]</color> 허공을 클릭했습니다. (콜라이더에 맞지 않음)");
        }
    }
}