using UnityEngine;

public class Altar : MonoBehaviour
{
    public LayerMask FriendlyUnit;
    public float friendlyUnitDetectionRadius = 5f;

    void Update()
    {

    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, friendlyUnitDetectionRadius);
    }
}
