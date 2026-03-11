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

        // AI Hero: Use skills strategically during combat
        if (isEnemyUnit && currentCombatState != CombatState.Idle)
        {
            HandleAISkillUsage();
        }

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

    // ====================================================================
    // AI HERO SKILL SYSTEM
    // ====================================================================
    // These methods allow AI heroes to use skills during combat

    /// <summary>
    /// Handle AI skill usage during combat based on current state and conditions.
    /// </summary>
    private void HandleAISkillUsage()
    {
        switch (currentCombatState)
        {
            case CombatState.Attacking:
                HandleAttackingSkillUsage();
                break;
            case CombatState.ForcedAttacking:
                HandleForcedAttackingSkillUsage();
                break;
            case CombatState.AttackMoveAttacking:
                HandleAttackMoveSkillUsage();
                break;
        }
    }

    /// <summary>
    /// Handle skill usage during normal attacking.
    /// </summary>
    private void HandleAttackingSkillUsage()
    {
        // Use skill 3 (barrage) regularly for burst damage
        if (!skill3OnCooldown && UnityEngine.Random.value > 0.7f)
        {
            UseSkill3();
        }
        
        // Use skill 4 (damage buff) when health is above 60%
        if (!skill4OnCooldown && HealthPercent > 0.6f && UnityEngine.Random.value > 0.75f)
        {
            UseSkill4();
        }
        
        // Use skill 2 (healing) when health drops below 40%
        if (!skill2OnCooldown && HealthPercent < 0.4f)
        {
            UseSkill2();
        }
        
        // Use skill 1 (speed boost) to chase or escape when needed
        if (!skill1OnCooldown && UnityEngine.Random.value > 0.8f)
        {
            UseSkill1();
        }
    }

    /// <summary>
    /// Handle skill usage during forced target engagement.
    /// </summary>
    private void HandleForcedAttackingSkillUsage()
    {
        // Use skill 3 (barrage) for burst damage
        if (!skill3OnCooldown && UnityEngine.Random.value > 0.75f)
        {
            UseSkill3();
        }
        
        // Use skill 4 (damage buff) when in forced attack
        if (!skill4OnCooldown && UnityEngine.Random.value > 0.8f)
        {
            UseSkill4();
        }
        
        // Use skill 2 (healing) when health is critical
        if (!skill2OnCooldown && HealthPercent < 0.3f)
        {
            UseSkill2();
        }
    }

    /// <summary>
    /// Handle skill usage during attack-move combat.
    /// </summary>
    private void HandleAttackMoveSkillUsage()
    {
        // Use skill 3 (barrage) for area damage
        if (!skill3OnCooldown && UnityEngine.Random.value > 0.75f)
        {
            UseSkill3();
        }
        
        // Use skill 4 (damage buff) occasionally
        if (!skill4OnCooldown && UnityEngine.Random.value > 0.8f)
        {
            UseSkill4();
        }
        
        // Use skill 2 (healing) when health is low
        if (!skill2OnCooldown && HealthPercent < 0.35f)
        {
            UseSkill2();
        }
    }

    /// <summary>
    /// AI hero uses skill 1 (speed boost).
    /// </summary>
    public bool UseSkill1()
    {
        if (skill1OnCooldown) return false;
        Skill1();
        return true;
    }

    /// <summary>
    /// AI hero uses skill 2 (healing).
    /// </summary>
    public bool UseSkill2()
    {
        if (skill2OnCooldown) return false;
        Skill2();
        return true;
    }

    /// <summary>
    /// AI hero uses skill 3 (projectile barrage).
    /// </summary>
    public bool UseSkill3()
    {
        if (skill3OnCooldown) return false;
        if (currentTarget == null) return false;
        Skill3();
        return true;
    }

    /// <summary>
    /// AI hero uses skill 4 (damage buff).
    /// </summary>
    public bool UseSkill4()
    {
        if (skill4OnCooldown) return false;
        Skill4();
        return true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(track.position, track.position + track.forward * 50f);
    }
}
