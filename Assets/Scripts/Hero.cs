using UnityEngine;
using UnityEngine.InputSystem;

public class Hero : HeroUnit, ISkill
{
    float skill1Cooldown = 5f;
    float skill1Timer = 0f;
    bool skill1OnCooldown = true;
    float skill2Cooldown = 5f;
    float skill2Timer = 0f;
    bool skill2OnCooldown = true;
    float skill3Cooldown = 5f;
    float skill3Timer = 0f;
    bool skill3OnCooldown = true;
    float skill4Cooldown = 5f;
    float skill4Timer = 0f;
    bool skill4OnCooldown = true;

    [SerializeField] Transform track;
    HeroUnit heroUnit => this; // Reference to the HeroUnit component for easier access

    protected override void Update()
    {
        base.Update();
        if (isSelected)
        {   
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
{
            Vector3 direction = hit.point - track.position;
            direction.y = 0f; // Ignore vertical difference (Y axis only rotation)

            if (direction != Vector3.zero)
            {
                track.rotation = Quaternion.LookRotation(direction);
            }
}
            if(Keyboard.current.qKey.wasPressedThisFrame && !skill1OnCooldown)
            {
                Skill1();
            }
             if(Keyboard.current.wKey.wasPressedThisFrame && !skill2OnCooldown)
            {
                Skill2();
            }
             if(Keyboard.current.eKey.wasPressedThisFrame && !skill3OnCooldown)
            {
                Skill3();
            }
             if(Keyboard.current.rKey.wasPressedThisFrame && !skill4OnCooldown)
            {
                Skill4();
            }
        }
        SkillCooldownUpdate();
        
    }
    public void Skill1()
    {
        skill1OnCooldown = true;
        skill1Timer = 0f;

        agent.speed += 4f;;
        Invoke(nameof(resetStat), 3f); // Reset speed after 3 seconds
    }

    public void Skill2()
    {
        throw new System.NotImplementedException();
    }

    public void Skill3()
    {
        throw new System.NotImplementedException();
    }

    public void Skill4()
    {
        throw new System.NotImplementedException();
    }

    public void SkillCooldownUpdate()
    {
        if(skill1OnCooldown)
        {
            Debug.Log("Skill 1 is on cooldown.");
            skill1Timer += Time.deltaTime;
            if(skill1Timer >= skill1Cooldown)
            {
                Debug.Log("Skill 1 is ready!");
                skill1OnCooldown = false;
                skill1Timer = 0f;
            }
        }
        if(skill2OnCooldown)
        {
            skill2Timer += Time.deltaTime;
            if(skill2Timer >= skill2Cooldown)
            {
                skill2OnCooldown = false;
                skill2Timer = 0f;
            }
        }
        if(skill3OnCooldown)
        {
            skill3Timer += Time.deltaTime;
            if(skill3Timer >= skill3Cooldown)
            {
                skill3OnCooldown = false;
                skill3Timer = 0f;
            }
        }
        if(skill4OnCooldown)
        {
            skill4Timer += Time.deltaTime;
            if(skill4Timer >= skill4Cooldown)
            {
                skill4OnCooldown = false;
                skill4Timer = 0f;
            }
        }
    }

    public void resetStat()
    {
        agent.speed = maxSpeed;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(track.position, track.position + track.forward * 50f);
    }
}
