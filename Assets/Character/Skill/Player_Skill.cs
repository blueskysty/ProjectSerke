using UnityEngine;

public class Player_Skill : MonoBehaviour
{
    [SerializeField]
    private SkillManager skillManager;

    void Update()
    {
        SkillInput();
    }

    private void SkillInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            skillManager.UseSkill(0, gameObject);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            skillManager.UseSkill(1, gameObject);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            skillManager.UseSkill(2, gameObject);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            skillManager.UseSkill(3, gameObject);
        }
    }
}
