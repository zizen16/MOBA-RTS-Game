using System;
using UnityEngine;

public class CombatUnit : BaseUnit, ICombatUnit
{
    public enum combatType{Melee, Range, Base};
    public combatType currentUnitType;

    [Header("Gold Rewards")]
    public int goldReward = 100; // Gold awarded when this unit is killed

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

    /// <summary>
    /// Register an attacker with this unit. Called when the unit takes damage.
    /// </summary>
    public override void RegisterAttacker(BaseUnit attacker)
    {
        lastAttacker = attacker;
    }

    /// <summary>
    /// Override TakeDamage to track attackers
    /// </summary>
    public override void TakeDamage(float damageAmount)
    {
        // Register the attacker (if called with context)
        base.TakeDamage(damageAmount);
    }

    void OnDestroy()
    {
        // Award gold to the player who dealt the last hit
        if (lastAttacker != null && !lastAttacker.isEnemyUnit)
        {
            PlayerManager.Instance.AddGold(goldReward);
            Debug.Log($"Player received {goldReward} gold for killing enemy unit!");
        }
    }

    protected virtual void Update()
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
        ScanForEnemies();
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
        // Unit stays focused on current target while chasing
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
        // Removed ScanForEnemies() call to maintain attack priority on current target
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
            
            // Register attacker with target if applicable
            Creep creepTarget = currentTarget.GetComponent<Creep>();
            if (creepTarget != null)
            {
                creepTarget.RegisterAttacker(this);
            }
            else
            {
                // Register attacker with other units (HeroUnit, CombatUnit, etc.)
                BaseUnit unitTarget = currentTarget.GetComponent<BaseUnit>();
                if (unitTarget != null)
                {
                    unitTarget.RegisterAttacker(this);
                }
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
    void ClearTarget()
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


}
