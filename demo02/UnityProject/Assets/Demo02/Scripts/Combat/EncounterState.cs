using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Demo02.Combat
{
    public sealed class EncounterState : MonoBehaviour
    {
        [SerializeField] private Combatant[] enemies = new Combatant[0];

        public int TotalEnemyCount => enemies.Length;
        public int AliveEnemyCount => enemies.Count(enemy => enemy != null && enemy.IsAlive);
        public bool IsCleared => TotalEnemyCount > 0 && AliveEnemyCount == 0;
        public IReadOnlyList<Combatant> Enemies => enemies;

        public void Configure(IEnumerable<Combatant> encounterEnemies)
        {
            enemies = encounterEnemies.Where(enemy => enemy != null).ToArray();
        }
    }
}
