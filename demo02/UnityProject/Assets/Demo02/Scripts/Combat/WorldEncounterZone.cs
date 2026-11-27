using System.Collections.Generic;
using Demo02.Core;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Combat
{
    [DefaultExecutionOrder(5)]
    public sealed class WorldEncounterZone : MonoBehaviour
    {
        [SerializeField] private WorldEncounterDefinition definition;
        [SerializeField] private Combatant[] enemies = new Combatant[0];

        private bool activated;
        private bool resolved;
        private float nextPlayerCheck;

        public WorldEncounterDefinition Definition => definition;
        public IReadOnlyList<Combatant> Enemies => enemies;
        public bool IsActivated => activated;
        public bool IsResolved => resolved;

        public void Configure(
            WorldEncounterDefinition encounterDefinition,
            IEnumerable<Combatant> encounterEnemies)
        {
            definition = encounterDefinition;
            List<Combatant> configured = new();
            if (encounterEnemies != null)
            {
                foreach (Combatant enemy in encounterEnemies)
                {
                    if (enemy != null)
                    {
                        configured.Add(enemy);
                    }
                }
            }
            enemies = configured.ToArray();
            SetEnemiesActive(definition != null && definition.StartsActive);
        }

        private void Start()
        {
            Demo02.Core.GameServices.Resolve<WorldEncounterDirector>()?.Register(this);
            if (definition != null
                && Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
                && Demo02.Core.GameServices.Resolve<WorldRuntime>().IsEncounterCompleted(
                    definition.EncounterId
                ))
            {
                resolved = true;
                SetEnemiesActive(false);
                return;
            }
            if (definition != null && definition.StartsActive)
            {
                Activate();
            }
        }

        private void Update()
        {
            if (activated
                || resolved
                || definition == null
                || Time.timeScale <= 0f
                || Time.time < nextPlayerCheck)
            {
                return;
            }

            nextPlayerCheck = Time.time + 0.25f;
            Transform player =
                Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.PlayerTransform;
            if (player == null)
            {
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude
                <= definition.ActivationRadius
                    * definition.ActivationRadius)
            {
                Activate();
            }
        }

        public bool Activate()
        {
            if (activated || resolved || definition == null)
            {
                return false;
            }

            activated = true;
            SetEnemiesActive(true);
            foreach (Combatant enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }
                enemy.Died -= OnEnemyDied;
                enemy.Died += OnEnemyDied;
            }
            Demo02.Core.GameServices.Resolve<WorldEncounterDirector>()?.NotifyActivated(this);
            Debug.Log($"Encounter activated: {definition.DisplayName}");
            return true;
        }

        public void RefreshResolvedState()
        {
            bool anyAlive = false;
            foreach (Combatant enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    anyAlive = true;
                    break;
                }
            }

            bool wasResolved = resolved;
            resolved = !anyAlive;
            if (resolved && !wasResolved && definition != null)
            {
                Demo02.Core.GameServices.Resolve<WorldRuntime>()?.MarkEncounterCompleted(
                    definition.EncounterId
                );
            }
        }

        private void OnEnemyDied(Combatant enemy)
        {
            RefreshResolvedState();
        }

        private void OnDestroy()
        {
            Demo02.Core.GameServices.Resolve<WorldEncounterDirector>()?.Unregister(this);
            foreach (Combatant enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.Died -= OnEnemyDied;
                }
            }
        }

        private void SetEnemiesActive(bool active)
        {
            foreach (Combatant enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(active);
                }
            }
        }
    }
}
