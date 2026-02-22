using UnityEngine;

public class Tower : BaseBuilding
{
    public enum TowerState { Idle, Attacking }
    public TowerState currentState = TowerState.Idle;
    [Header("Perception")]
    public float attackRange = 10;
    public LayerMask enemyLayer;
    public Transform turretTransform;

    [Header("Range")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 20f;
    public int bulletDamage = 10;
    public float shootCooldown = 1f;
    float shootTimer =0;
    GameObject currentTarget;

    void Update()
    {
        switch (currentState)
        {
            case TowerState.Idle:
                LookForTargets();
                break;
            case TowerState.Attacking:
                turretTransform.LookAt(currentTarget.transform);
                AttackTarget();
                break;
        }
    }
    public void LookForTargets()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        if (enemiesInRange.Length > 0)
        {
            currentTarget = enemiesInRange[0].gameObject;
            currentState = TowerState.Attacking;
        }
    }
    public void AttackTarget()
    {
        RaycastHit hit;
        if (currentTarget == null)
        {
            currentState = TowerState.Idle;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distanceToTarget > attackRange)
        {
            currentState = TowerState.Idle;
            currentTarget = null;
            return;
        }

        shootTimer += Time.deltaTime;
        if (shootTimer >= shootCooldown)
        {
            if (Physics.Raycast(bulletSpawnPoint.position, bulletSpawnPoint.forward, out hit, attackRange, enemyLayer))
            {
            Shoot();
            }
            shootTimer = 0f;
        }
    }
    public void Shoot()
    {
        Debug.Log("Enemy Hit");
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Vector3 shootDirection = (currentTarget.transform.position - transform.position).normalized;
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null && currentTarget != null)
        {
            bulletComponent.SetTarget(currentTarget.transform, bulletDamage, bulletSpeed);
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.DrawLine(bulletSpawnPoint.position, bulletSpawnPoint.position + bulletSpawnPoint.forward * attackRange);
        //Debug.DrawRay(transform.position, bulletSpawnPoint.forward * attackRange, Color.yellow);
    }
}
