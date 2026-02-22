using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 5;

    private Vector3 direction;
    private float damage;
    private float speed;
    private Transform target;
    private BaseUnit attacker; // Track who fired this bullet
    public float turnSpeed = 10f; // radians per second for turning toward target

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (target != null)
        {
            if (target == null) // in case target was destroyed
            {
                target = null;
            }
            else
            {
                Vector3 desired = (target.position - transform.position).normalized;
                direction = Vector3.RotateTowards(direction, desired, turnSpeed * Time.deltaTime, 0f);
            }
        }

        transform.position += direction * speed * Time.deltaTime;
    }

    // Homing: assign a moving target for the bullet to follow
    public void SetTarget(Transform newTarget, float bulletDamage, float bulletSpeed, BaseUnit bulletAttacker = null)
    {
        target = newTarget;
        damage = bulletDamage;
        speed = bulletSpeed;
        attacker = bulletAttacker;
        
        if (target != null)
            direction = (target.position - transform.position).normalized;
    }
    void OnTriggerEnter(Collider other)
    {
        IDamageable enemy = other.GetComponent<IDamageable>();
        if(enemy != null)
        {
            Debug.Log("Bullet hit: " + other.name);
            
            // Register attacker with target if applicable
            Creep creepTarget = other.GetComponent<Creep>();
            if (creepTarget != null && attacker != null)
            {
                creepTarget.RegisterAttacker(attacker);
            }
            else
            {
                // Register attacker with other units (HeroUnit, CombatUnit, etc.)
                BaseUnit unitTarget = other.GetComponent<BaseUnit>();
                if (unitTarget != null && attacker != null)
                {
                    unitTarget.RegisterAttacker(attacker);
                }
            }
            
            enemy.TakeDamage(damage);
            Destroy(gameObject);    
        }
    }
}