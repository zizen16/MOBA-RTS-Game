using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 5;

    private Vector3 direction;
    private float damage;
    private float speed;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    public void SetDirection(Vector3 newDirection, float bulletDamage, float bulletSpeed)
    {
        direction = newDirection;
        damage = bulletDamage;
        speed = bulletSpeed;
    }
    void OnTriggerEnter(Collider other)
    {
        IDamageable enemy = other.GetComponent<IDamageable>();

        if(enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}