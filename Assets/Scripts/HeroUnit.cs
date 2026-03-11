using System;
using System.Collections;
using UnityEngine;

public class HeroUnit : Builder, ICombatUnit
{
    public enum combatType{Melee, Range, Base};
    public combatType currentUnitType;
    public Transform respawnPoint;

    [Header("Gold Rewards")]
    public int goldReward = 200; // Gold awarded when this hero is killed

    [Header("Respawn")]
    public float respawnDelay = 5f;              // seconds before hero returns

    // internal state
    bool isDead;
    

    [Header("Perception")]
    public float detectionRange = 15f;
    public float attackRange = 10;
    public LayerMask enemyLayer;

    protected BaseUnit lastAttacker; // Track who last hit this unit

    public float attackCooldown = 1f;

    [Header("Melee")]
    public int atkDamage = 10;

    [Header("Range")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 20f;
    public int bulletDamage = 10;
    public float shootCooldown = 1f;

    [Space]
    public float lookRotationSpeed = 5;

    float attackTimer;

    public CombatState state = CombatState.Idle;

    public GameObject currentTarget;

    public CombatState currentCombatState
    {
        get => state;
        set => state = value;
    }
    protected GameObject forcedTarget; // specific target assigned by player
    protected Vector3 attackMoveDestination; // destination assigned by player during attack move
    protected bool hasAttackMoveDestination; // flag to check if attack move destination is set

    /// <summary>
    /// Public property to get the current health percentage (0-1).
    /// </summary>
    public float HealthPercent
    {
        get { return Mathf.Clamp01(currentHealth / maxHealth); }
    }

    /// <summary>
    /// Register an attacker with this unit. Called when the unit takes damage.
    /// </summary>
    public override void RegisterAttacker(BaseUnit attacker)
    {
        lastAttacker = attacker;
    }

    /// <summary>
    /// Intercept damage when the hero is already in the death/respawn state.
    /// </summary>
    public override void TakeDamage(float damageAmount)
    {
        if (isDead) // ignore damage while waiting to respawn
            return;
        base.TakeDamage(damageAmount);
    }

    // NOTE: OnDestroy is still present to cover the (unlikely) case where the
    // object is actually removed from the scene.  Most hero deaths will be handled
    // by <see cref="Die" /> and respawn logic instead.
    void OnDestroy()
    {
        if (!isDead)
            return;

        // Award gold to the player who dealt the last hit.  This duplicates the
        // logic in Die() but ensures rewards are granted even if the object is
        // destroyed for some other reason (e.g. scene unload).
        if (lastAttacker != null && !lastAttacker.isEnemyUnit)
        {
            PlayerManager.Instance.AddGold(goldReward);
            Debug.Log($"Player received {goldReward} gold for killing enemy hero!");
        }
    }

    public void StartMoving()
    {
        ClearAllTargets();
        state = CombatState.Moving;
    }
    public void ForceAttackTarget(GameObject target)
    {
        if (target == null) return;
        forcedTarget = target;
        currentTarget = target;
        hasAttackMoveDestination = false;
        state = CombatState.ForcedAttacking;
    }

    public void AttackMove(Vector3 destination)
    {
        attackMoveDestination = destination;
        hasAttackMoveDestination = true;
        forcedTarget = null;
        currentTarget = null;
        agent.SetDestination(destination);
        state = CombatState.AttackMoving;
    }

    protected override void Update()
    {
        base.Update();

        // Exploration logic
        if (isExploring && currentState == Worker.WorkerState.Idle)
        {
            isExploring = false;
            if (ShouldBuildPylonHere())
            {
                GameObject construction = Instantiate(explorationBuildingData.constructionPrefab, transform.position, Quaternion.identity);
                AssignBuildingTask(transform.position, explorationBuildingData, construction);
            }
        }
         if(isEnemyUnit)
        {
            if (currentTarget != null && forcedTarget == null)
            {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget > detectionRange)
            {
                HandleLostTarget();
                return;
            }
            }
        }
        
        switch (state)
        {
            case CombatState.Idle:
                HandleIdleState();
                break;
            case CombatState.Moving:
                HandleMovingState();
                break;

            case CombatState.Chasing:
                HandleChasingState();
                break;

            case CombatState.Attacking:
                HandleAttackingState();
                break;
            case CombatState.ForcedAttacking:
                HandleForcedAttackingState();
                break;
            case CombatState.AttackMoving:
                HandleAttackMovingState();
                break;
            case CombatState.AttackMoveChasing:
                HandleAttackMoveChasingState();
                break;
            case CombatState.AttackMoveAttacking:
                HandleAttackMoveAttackingState();
                break;

        }
        
    }
    //=============================== STATE HANDLERS ==================================
    protected virtual void HandleIdleState()
    {
         if(isEnemyUnit)
            {
            ScanForEnemies();
            }
    }
    protected virtual void HandleMovingState()
    {
        currentTarget = null;
        if (HasArrived())
        {
            state = CombatState.Idle;
        }
    }
    protected virtual void HandleChasingState()
    {

        if (!IsTargetValid())
        {
            ClearTarget();
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget <= attackRange)
        {
            agent.ResetPath();
            state = CombatState.Attacking;
            return;
        }
        agent.SetDestination(currentTarget.transform.position);
    }
    protected virtual void HandleAttackingState()
    {
        if (!IsTargetValid())
        {
            ClearTarget();
            return;
        }
        RotateTowardsTarget();
        attackTimer += Time.deltaTime;
        if (attackTimer >= shootCooldown)
        {
            Attack();
            attackTimer = 0;
        }
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget > attackRange)
        {
            state = CombatState.Chasing;
            return;
        }
        agent.ResetPath();
        if(isEnemyUnit)
        {
            ScanForEnemies();
        }
    }
    //================================== ADDITIONAL STATE HANDLERS =======================================
    protected virtual void HandleForcedAttackingState()
    {
        if (!isForcedTargetValid())
        {
            ClearAllTargets();
            state = CombatState.Idle;
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget > attackRange)
        {
            agent.SetDestination(currentTarget.transform.position);
        }
        else
        {
            agent.ResetPath();
            RotateTowardsTarget();
            attackTimer += Time.deltaTime;
            if (attackTimer >= shootCooldown)
            {
                Attack();
                attackTimer = 0;
            }
        }
    }
    protected virtual void HandleAttackMovingState()
    {
        if (HasArrived())
        {
            hasAttackMoveDestination = false;
            state = CombatState.Idle;
            return;
        }
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
        if (hits.Length > 0)
        {
            currentTarget = GetClosestFromAttackState(hits);
            if (currentTarget != null)
            {
                state = CombatState.AttackMoveChasing;
            }
        }
    }
    protected virtual void HandleAttackMoveChasingState()
    {
        if (!IsTargetValid())
        {
            ResumeAttackMove();
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget > detectionRange)
        {
            ResumeAttackMove();
            return;
        }
        if (distanceToTarget <= attackRange)
        {
            agent.ResetPath();
            state = CombatState.AttackMoveAttacking;
            return;
        }

    }
    protected virtual void HandleAttackMoveAttackingState()
    {
        //to be implemented
        if (!IsTargetValid())
        {
            ResumeAttackMove();
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget > detectionRange)
        {
            ResumeAttackMove();
            return;
        }
        if (distanceToTarget > attackRange)
        {
            state = CombatState.AttackMoveChasing;
            return;
        }
        agent.ResetPath();
        RotateTowardsTarget();
        attackTimer += Time.deltaTime;
        if (attackTimer >= shootCooldown)
        {
            Attack();
            attackTimer = 0;
        }
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
        if (hits.Length > 0)
        {
            GameObject closestEnemy = GetClosestFromAttackState(hits);
            if(closestEnemy != null && closestEnemy != currentTarget)
            {
                float newDistance = Vector3.Distance(transform.position, closestEnemy.transform.position);
                if(newDistance < distanceToTarget)
                {
                    currentTarget = closestEnemy;
                    state = CombatState.AttackMoveChasing;
                }
            }
        }
    }
    //================================== HELPER =======================================
    void ResumeAttackMove()
    {
        currentTarget = null;
        if (hasAttackMoveDestination)
        {
            agent.SetDestination(attackMoveDestination);
            state = CombatState.AttackMoving;
        }
        else
        {
            state = CombatState.Idle;
        }
    }
    GameObject GetClosestFromAttackState(Collider[] hits)
    {
        float closestDistance = detectionRange;
        GameObject closestEnemy = null;
        foreach (Collider enemy in hits)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.gameObject;
            }
        }
        return closestEnemy;
    }
    void HandleLostTarget()
    {
        currentTarget = null;
        if (hasAttackMoveDestination)
        {
            agent.SetDestination(attackMoveDestination);
            state = CombatState.AttackMoving;
        }
        else
        {
            state = CombatState.Idle;
        }
    }
    bool isForcedTargetValid()
    {
        if (currentTarget == null) return false;
        if (!currentTarget.activeInHierarchy) return false;
        return true;
    }
    void ClearAllTargets()
    {
        currentTarget = null;
        forcedTarget = null;
        hasAttackMoveDestination = false;
    }
    void RotateTowardsTarget()
    {
        Transform aimTransform = GetAimTransform();
        Vector3 faceDirection = (currentTarget.transform.position - aimTransform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(faceDirection);
        aimTransform.rotation = Quaternion.Slerp(aimTransform.rotation, lookRotation, Time.deltaTime * lookRotationSpeed);
    }

    void ScanForEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);

        if (hits.Length == 0) return;
        currentTarget = GetClosest(hits);
        Debug.Log("EnemyFound");
    }

    GameObject GetClosest(Collider[] hits)
    {
        float closestDistance = detectionRange;
        GameObject closestEnemy = null;
        foreach (Collider enemy in hits)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.gameObject;
            }
        }
        if (closestEnemy != null && closestEnemy != currentTarget)
        {
            state = CombatState.Chasing;
        }
        return closestEnemy;
    }

    void Attack()
    {
        if(currentUnitType == combatType.Range){
            Debug.Log("Shoot!");
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            bulletComponent.SetTarget(currentTarget.transform, bulletDamage, bulletSpeed, this);
        }
        else
        {
            Debug.Log("Melee!");
            IDamageable enemy = currentTarget.GetComponent<IDamageable>();
            
            // Register attacker with target
            BaseUnit unitTarget = currentTarget.GetComponent<BaseUnit>();
            if (unitTarget != null)
            {
                unitTarget.RegisterAttacker(this);
            }
            
            enemy.TakeDamage(atkDamage);
        }
        
    }

    bool IsTargetValid()
    {
        //object is not null
        if (currentTarget == null) return false;
        if (!currentTarget.activeInHierarchy) return false;
        return true;
    }

    // =============================================================
    // Respawn helpers
    // =============================================================

    /// <summary>
    /// Override of the base-unit death handler.  Instead of destroying the object,
    /// we begin a respawn timer and temporarily disable the hero's components.
    /// </summary>
    protected override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // award kill gold now
        if (lastAttacker != null && !lastAttacker.isEnemyUnit)
        {
            PlayerManager.Instance.AddGold(goldReward);
            Debug.Log($"Player received {goldReward} gold for killing enemy hero!");
        }

        // disable visuals/collision so the corpse isn't interactable
        DisableComponents();
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    void Respawn()
    {
        // move back to the assigned spawn point
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            if (agent != null)
                agent.Warp(respawnPoint.position);
        }

        currentHealth = maxHealth;
        ClearAllTargets();
        state = CombatState.Idle;
        isDead = false;
        EnableComponents();
    }

    void DisableComponents()
    {
        // turn off the nav agent and collider, hide renderers
        if (agent != null) agent.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }

    void EnableComponents()
    {
        if (agent != null)
        {
            agent.enabled = true;
            agent.ResetPath();
        }
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
    }
    public void ClearTarget()
    {
        currentTarget = null;
        agent.ResetPath();
        state = CombatState.Idle;
    }
    protected virtual Transform GetAimTransform()
    {
        return transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    bool ShouldBuildPylonHere()
    {
        CommandCenter cc = AIManager.Instance.FindCommandCenter();
        if (cc == null) return false;

        float dist = Vector3.Distance(transform.position, cc.transform.position);
        if (dist < 50f) return false; // too close to base

        // check no AI buildings within 20 units
        foreach (var building in AIManager.Instance.GetAllBuildings())
        {
            if (Vector3.Distance(transform.position, building.transform.position) < 20f)
                return false;
        }

        return true;
    }


}


