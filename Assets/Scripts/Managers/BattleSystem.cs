using UnityEngine;

public static class BattleSystem
{
    public static void ResolveBattle(PlayerMover attacker, PlayerMover defender, bool isFrontAttack)
    {
        PartnerInstance attackingPartner = isFrontAttack ? attacker.frontPartner : attacker.backPartner;
        PartnerInstance defendingPartner = isFrontAttack ? defender.frontPartner : defender.backPartner;

        int attackPower = attackingPartner?.GetAttackPower() ?? 1;

        Debug.Log($"{attacker.PlayerName} attacks {defender.PlayerName} from {(isFrontAttack ? "front" : "behind")}");

        if (defendingPartner != null)
        {
            int defenseHP = defendingPartner.CurrentHP;

            if (attackPower > defenseHP)
            {
                int excess = attackPower - defenseHP;
                defendingPartner.TakeDamage(defenseHP);
                defender.ModifyHealth(-excess);
            }
            else
            {
                defendingPartner.TakeDamage(attackPower);
            }
        }
        else
        {
            defender.ModifyHealth(-attackPower);
        }

        
    }
}
