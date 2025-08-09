using UnityEngine;

public static class BattleSystem
{
    public static void ResolveBattle(PlayerMover attacker, PlayerMover defender)
    {
        PartnerInstance attackingPartner = attacker.frontPartner;

        // If the attacker has no front partner, no attack occurs
        if (attackingPartner == null)
        {
            Debug.Log($"{attacker.PlayerName} has no front partner to attack.");
            return;
        }

        // Defender's defending partner is either front or back (in that order)
        PartnerInstance defendingPartner = defender.frontPartner ?? defender.backPartner;

        int attackPower = attackingPartner.GetAttackPower();
        int defenseHP = defendingPartner != null ? defendingPartner.CurrentHP : 0;

        Debug.Log($"{attacker.PlayerName} attacks {defender.PlayerName} with {attackPower} power.");

        if (defendingPartner != null)
        {
            if (attackPower > defenseHP)
            {
                int excessDamage = attackPower - defenseHP;
                defendingPartner.TakeDamage(defenseHP); // Defeat the partner
                Debug.Log($"{defender.PlayerName}'s partner was defeated!");
                defender.ModifyHealth(-excessDamage);
                Debug.Log($"{defender.PlayerName} took {excessDamage} excess damage!");
            }
            else
            {
                defendingPartner.TakeDamage(attackPower);
                Debug.Log($"{defender.PlayerName}'s partner took {attackPower} damage.");
            }
        }
        else
        {
            // No defending partner — attack player directly
            defender.ModifyHealth(-attackPower);
            Debug.Log($"{defender.PlayerName} had no partner. Took {attackPower} direct damage!");
        }

        // Future: Add animation, sound, UI flash, or camera shake
    }
}