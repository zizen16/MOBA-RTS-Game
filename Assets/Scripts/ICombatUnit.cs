using UnityEngine;

public interface ICombatUnit 
{
    CombatState currentCombatState { get; set; }
    void StartMoving();
    void ForceAttackTarget(GameObject target);
    void AttackMove(Vector3 destination);
}
public enum CombatState
{
    Idle, // scanning for enemies
    Moving, // moving to a location, ignoring enemies
    Chasing, // pursuing an enemy with auto detection
    Attacking, //attacking with auto detection
    ForcedAttacking, // attacking a specific target(ignore other enemies)
    AttackMoving, // moving to a location but attacking enemies on the way
    AttackMoveChasing, //chasing enemy encountered during attack move
    AttackMoveAttacking // attacking enemy encountered during attack move

}
