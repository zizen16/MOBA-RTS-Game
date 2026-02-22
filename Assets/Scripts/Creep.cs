using UnityEngine;

public class Creep : CombatUnit
{
    public bool isAttacked;

    protected override void Update()
    {
        // Call parent Update for combat logic
        base.Update();
    }

    /// <summary>
    /// Register an attacker with this creep. Called when the creep takes damage.
    /// Automatically makes the creep chase and attack the attacker.
    /// </summary>
    public override void RegisterAttacker(BaseUnit attacker)
    {
        lastAttacker = attacker;
        
        isAttacked = true;
        
        // Auto-chase the attacker if we're not already attacking
        if (attacker != null)
        {
            if (state == CombatState.Idle || state == CombatState.Moving)
            {
                ForceAttackTarget(attacker.gameObject);
            }
        }
    }

    /// <summary>
    /// Override TakeDamage to award gold to the last attacker when creep dies
    /// </summary>
    public override void TakeDamage(float damageAmount)
    {
        isAttacked = true;
        base.TakeDamage(damageAmount);
    }

    void OnDestroy()
    {
        // Award gold to the player who dealt the last hit (only if attacker is a player unit)
        if (lastAttacker != null && !lastAttacker.isEnemyUnit)
        {
            PlayerManager.Instance.AddGold(goldReward);
            Debug.Log($"Player received {goldReward} gold for killing creep!");
        }
    }
}
