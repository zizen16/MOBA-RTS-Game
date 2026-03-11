using UnityEngine;

public class Canon : MonoBehaviour
{
    public GameObject explosionVFX; // Drag your VFX Prefab here
    public float explosionRadius = 5f;
    public float explosionForce = 700f;

    void OnCollisionEnter(Collision collision)
    {
        // 1. Spawn the Visuals
        Instantiate(explosionVFX, transform.position, Quaternion.identity);

        // 2. Physical Blast (Optional)
       // ApplyExplosionForce();

        // 3. Remove the bomb
        Destroy(gameObject);
    }

   /*  void ApplyExplosionForce()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }
    } */
}
