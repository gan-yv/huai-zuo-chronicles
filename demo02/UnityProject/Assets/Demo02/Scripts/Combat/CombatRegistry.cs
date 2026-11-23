using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.Combat
{
    public static class CombatRegistry
    {
        private static readonly List<Combatant> combatants = new();
        private static readonly List<Combatant> enemies = new();

        public static event Action Changed;

        public static IReadOnlyList<Combatant> Combatants => combatants;
        public static IReadOnlyList<Combatant> Enemies => enemies;

        public static void Register(Combatant combatant)
        {
            if (combatant == null || combatants.Contains(combatant))
            {
                return;
            }

            combatants.Add(combatant);
            if (combatant.Team == CombatTeam.Enemy)
            {
                enemies.Add(combatant);
            }
            Changed?.Invoke();
        }

        public static void Unregister(Combatant combatant)
        {
            if (combatant == null)
            {
                return;
            }

            bool removed = combatants.Remove(combatant);
            removed |= enemies.Remove(combatant);
            if (removed)
            {
                Changed?.Invoke();
            }
        }

        public static int CountNearbyEnemies(
            Vector3 position,
            float radius)
        {
            int count = 0;
            float radiusSquared = radius * radius;
            for (int index = 0; index < enemies.Count; index++)
            {
                Combatant enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 offset = enemy.transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
