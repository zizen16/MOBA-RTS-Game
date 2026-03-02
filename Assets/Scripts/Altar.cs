using System.Collections.Generic;
using UnityEngine;

public class Altar : MonoBehaviour
{
    public LayerMask FriendlyUnit;
    public float friendlyUnitDetectionRadius = 5f;

    void Update()
    {
        Collider[] friendlyUnits = Physics.OverlapSphere(transform.position, friendlyUnitDetectionRadius, FriendlyUnit);
        if (friendlyUnits.Length > 0)
        {
            foreach (Collider friendlyUnit in friendlyUnits)
            {
                BaseUnit unit = friendlyUnit.GetComponent<BaseUnit>();
                unit.Heal(1); // Heal the unit by 10 health points
            }
            Debug.Log("Friendly unit detected! Healing or buffing...");
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, friendlyUnitDetectionRadius);
    }
}
