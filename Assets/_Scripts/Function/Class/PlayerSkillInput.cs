using UnityEngine;

public class PlayerSkillInput : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayerSkillController.Instance.UseSkillSlot(0, player);
        }

        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayerSkillController.Instance.UseSkillSlot(1, player);
        }

        if( Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayerSkillController.Instance.UseSkillSlot(2, player);
        }
    }
}
