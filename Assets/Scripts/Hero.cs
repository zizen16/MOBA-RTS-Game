using UnityEngine;
using UnityEngine.InputSystem;

public class Hero : HeroUnit, ISkill
{
    [Header("Skill Cooldowns")]
    public float skill1Cooldown = 5f;
    public float skill1Timer = 0f;
    public bool skill1OnCooldown = true;
    public float skill2Cooldown = 8f;
    public float skill2Timer = 0f;
    public bool skill2OnCooldown = true;
    public float skill3Cooldown = 6f;
    public float skill3Timer = 0f;
    public bool skill3OnCooldown = true;
    public float skill4Cooldown = 10f;
    public float skill4Timer = 0f;
    public bool skill4OnCooldown = true;

    [Header("Skill 2 - Healing")]
    public float healAmount = 50f;
    public float healPercent = 0.2f; // 20% of max health

    [Header("Skill 3 - Projectile Barrage")]
    public int projectileCount = 5;
    public float projectileSpreadAngle = 120f; // Angle spread in degrees
    public float projectileMaxDistance = 30f; // Max distance projectile travels

    [Header("Skill 4 - Damage Buff")]
    public float damageBuffMultiplier = 1.5f; // 50% increase
    public float damageBuffDuration = 5f;
    private float damageBuffTimer = 0f;
    private bool isDamageBuffActive = false;
    private float originalBulletDamage;

    [SerializeField] Transform track;
    HeroUnit heroUnit => this; // Reference to the HeroUnit component for easier access

    protected override void Update()
    {
        base.Update();
        UpdateDamageBuffDuration();

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
        skill2OnCooldown = true;
        skill2Timer = 0f;

        // Heal using both flat amount and percentage
        float totalHeal = healAmount + (maxHealth * healPercent);
        currentHealth = Mathf.Min(currentHealth + totalHeal, maxHealth);
        
        Debug.Log($"Hero healed for {totalHeal}! Current health: {currentHealth}/{maxHealth}");
    }

    public void Skill3()
    {
        skill3OnCooldown = true;
        skill3Timer = 0f;

        // Spawn multiple projectiles in a spread pattern
        float angleStep = projectileSpreadAngle / (projectileCount - 1);
        float startAngle = -projectileSpreadAngle / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion spreadRotation = Quaternion.Euler(0, currentAngle, 0) * bulletSpawnPoint.rotation;

            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, spreadRotation);
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            
            if (bulletComponent != null)
            {
                // Spread projectiles in a cone
                Vector3 spreadDirection = Quaternion.Euler(0, currentAngle, 0) * track.forward;
                bulletComponent.SetTarget(null, bulletDamage, bulletSpeed, this);
                
                // Move kinematic bullet using transform position
                StartCoroutine(MoveBulletTransform(bullet.transform, spreadDirection, projectileMaxDistance, bulletSpeed));
            }
        }

        Debug.Log($"Barrage fired {projectileCount} projectiles!");
    }

    private System.Collections.IEnumerator MoveBulletTransform(Transform bulletTransform, Vector3 direction, float maxDistance, float speed)
    {
        float distanceTraveled = 0f;
        float moveStep = speed * Time.fixedDeltaTime;

        while (bulletTransform != null && distanceTraveled < maxDistance)
        {
            bulletTransform.position += direction * moveStep;
            distanceTraveled += moveStep;
            yield return new WaitForFixedUpdate();
        }

        if (bulletTransform != null)
        {
            Destroy(bulletTransform.gameObject);
        }
    }

    public void Skill4()
    {
        skill4OnCooldown = true;
        skill4Timer = 0f;

        // Activate damage buff
        if (!isDamageBuffActive)
        {
            originalBulletDamage = bulletDamage;
            isDamageBuffActive = true;
            damageBuffTimer = 0f;
        }

        bulletDamage = Mathf.RoundToInt(bulletDamage * damageBuffMultiplier);
        damageBuffTimer = 0f; // Reset timer if re-cast
        
        Debug.Log($"Damage buffed to {bulletDamage}! Buff active for {damageBuffDuration}s");
    }

    private void UpdateDamageBuffDuration()
    {
        if (isDamageBuffActive)
        {
            damageBuffTimer += Time.deltaTime;
            if (damageBuffTimer >= damageBuffDuration)
            {
                bulletDamage = Mathf.RoundToInt(originalBulletDamage);
                isDamageBuffActive = false;
                damageBuffTimer = 0f;
                Debug.Log($"Damage buff expired. Damage reset to {bulletDamage}");
            }
        }
    }

    public void SkillCooldownUpdate()
    {
        if(skill1OnCooldown)
        {
            skill1Timer += Time.deltaTime;
            if(skill1Timer >= skill1Cooldown)
            {
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
