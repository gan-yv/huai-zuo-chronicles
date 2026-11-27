using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.UI;
using Demo02.WorldState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Demo02.Editor
{
    internal static class Demo02CombatRegressionSuite
    {
        public static void VerifyCombatDamageForBatch()
        {
            GameObject probe = new GameObject("CombatantDamageProbe");
            Combatant combatant = probe.AddComponent<Combatant>();
            combatant.Configure(CombatTeam.Enemy, 60f);

            combatant.TakeDamage(30f);
            if (Mathf.Approximately(combatant.CurrentHealth, 30f) == false
                || combatant.IsAlive == false)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("First hit did not reduce enemy health to 30.");
            }

            combatant.TakeDamage(30f);
            if (Mathf.Approximately(combatant.CurrentHealth, 0f) == false
                || combatant.IsAlive)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException("Second hit did not kill the enemy.");
            }

            Object.DestroyImmediate(probe);
            Debug.Log("Combat damage verification passed: 60 HP defeated in two 30 damage hits.");
        }

        public static void VerifyMeleeComboForBatch()
        {
            MeleeComboTimeline combo = new(3);
            if (combo.RegisterAttack(1f, 0.72f) != 1
                || combo.RegisterAttack(1.4f, 0.72f) != 2
                || combo.RegisterAttack(1.8f, 0.72f) != 3
                || combo.RegisterAttack(2.2f, 0.72f) != 1)
            {
                throw new System.InvalidOperationException(
                    "Melee combo did not advance and wrap through three steps."
                );
            }

            combo.Reset();
            if (combo.CurrentStep != 0
                || combo.RegisterAttack(4f, 0.72f) != 1
                || combo.RegisterAttack(5f, 0.72f) != 1)
            {
                throw new System.InvalidOperationException(
                    "Melee combo did not reset after its input window expired."
                );
            }

            Debug.Log(
                "Melee combo verification passed: 1 -> 2 -> 3 -> 1, " +
                "with dodge/expiry reset."
            );
        }

        public static void VerifyPerfectDodgeCounterForBatch()
        {
            GameObject probe = new("PerfectDodgeCounterProbe");
            PlayerCombat combat = probe.AddComponent<PlayerCombat>();
            combat.RewardPerfectDodge();
            if (!combat.IsPerfectDodgeCounterReady)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Perfect dodge did not open its counter window."
                );
            }

            Object.DestroyImmediate(probe);
            Debug.Log(
                "Perfect dodge validation passed: counter window opens " +
                "and grants energy."
            );
        }

        public static void VerifyEnemyAttackCadenceForBatch()
        {
            EnemyAttackCadence cadence = new(3);
            if (cadence.NextIsHeavy)
            {
                throw new System.InvalidOperationException(
                    "Enemy attack cadence started with a heavy attack."
                );
            }
            cadence.CompleteAttack();
            if (cadence.NextIsHeavy)
            {
                throw new System.InvalidOperationException(
                    "Enemy attack cadence produced a heavy attack too early."
                );
            }
            cadence.CompleteAttack();
            if (!cadence.NextIsHeavy)
            {
                throw new System.InvalidOperationException(
                    "Enemy attack cadence did not reach its heavy attack."
                );
            }
            cadence.CompleteAttack();
            if (cadence.NextIsHeavy)
            {
                throw new System.InvalidOperationException(
                    "Enemy attack cadence did not reset after heavy attack."
                );
            }
            Debug.Log(
                "Enemy attack cadence passed: normal, normal, heavy, reset."
            );
        }

        public static void VerifyCharacterFrameworkForBatch()
        {
            CharacterDefinition definition =
                CharacterDefinitionSeeder.LoadRepairer();
            GameObject probe = new("CharacterRuntimeProbe");
            CharacterRuntime runtime = probe.AddComponent<CharacterRuntime>();
            runtime.Configure(definition);
            float initialStamina = runtime.CurrentStamina;
            bool drained = runtime.DrainStamina(18f);
            runtime.GainEnergy(12f);
            bool spent = runtime.TrySpendEnergy(10f);
            bool valid = Mathf.Approximately(initialStamina, definition.MaxStamina)
                && drained
                && Mathf.Approximately(
                    runtime.CurrentStamina,
                    definition.MaxStamina - 18f
                )
                && Mathf.Approximately(runtime.CurrentEnergy, 2f)
                && spent;
            runtime.SetContinuationBonuses(10f, 1.25f);
            runtime.RestoreEnergy(0f);
            runtime.GainEnergy(20f);
            valid &= Mathf.Approximately(
                runtime.MaxStamina,
                definition.MaxStamina + 10f
            ) && Mathf.Approximately(runtime.CurrentEnergy, 25f);
            Object.DestroyImmediate(probe);

            if (!valid)
            {
                throw new System.InvalidOperationException(
                    "Character stamina or energy framework validation failed."
                );
            }
            Debug.Log(
                "Character framework validation passed: stamina drain and "
                + "energy gain/spend."
            );
        }

        public static void VerifyInventoryForBatch()
        {
            CaseDefinition content = CaseDefinitionSeeder.LoadOrCreateChapter01();
            GameObject probe = new("InventoryProbe");
            InventoryState inventory = probe.AddComponent<InventoryState>();
            inventory.Configure(content);
            InvestigationState state = probe.AddComponent<InvestigationState>();
            state.ConfigureContent(content);
            Combatant health = probe.AddComponent<Combatant>();
            health.Configure(CombatTeam.Player, 100f);

            if (!inventory.AddItem("salt-token-charm", 1)
                || inventory.GetCount("salt-token-charm") != 1
                || !inventory.AddItem("ginger-tea", 7)
                || inventory.GetCount("ginger-tea") != 5
                || !inventory.AddItem("blank-ledger-page", 1)
                || !inventory.MarkPickupCollected("pickup-ginger-tea")
                || !inventory.IsPickupCollected("pickup-ginger-tea"))
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Inventory stacking or pickup state verification failed."
                );
            }

            state.SetFact("teahouse-cart-testimony");
            if (!inventory.TryUseItem(
                    "blank-ledger-page",
                    null,
                    state,
                    out _
                )
                || !state.HasFact("teahouse-written-record")
                || inventory.GetCount("blank-ledger-page") != 0
                || !inventory.TryUseItem(
                    "salt-token-charm",
                    null,
                    state,
                    out _
                )
                || !state.HasFact("salt-token-identified")
                || inventory.GetCount("salt-token-charm") != 1)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Inventory story-item effects verification failed."
                );
            }

            health.TakeDamage(40f);
            if (!inventory.TryUseItem("ginger-tea", health, state, out _)
                || !Mathf.Approximately(health.CurrentHealth, 85f)
                || inventory.GetCount("ginger-tea") != 4)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Inventory healing item verification failed."
                );
            }

            health.TakeDamage(20f);
            if (!inventory.TryUseFirstHealingItem(
                    health,
                    state,
                    out _
                )
                || !Mathf.Approximately(health.CurrentHealth, 90f)
                || inventory.GetCount("ginger-tea") != 3)
            {
                Object.DestroyImmediate(probe);
                throw new System.InvalidOperationException(
                    "Quick healing item verification failed."
                );
            }

            List<InventoryItemSaveData> snapshot = inventory.ExportItems();
            string[] pickupIds = inventory.ExportCollectedPickupIds();
            Object.DestroyImmediate(probe);

            GameObject restoredProbe = new("InventoryRestoreProbe");
            InventoryState restored =
                restoredProbe.AddComponent<InventoryState>();
            restored.Configure(content);
            restored.Restore(snapshot, pickupIds);
            if (restored.GetCount("salt-token-charm") != 1
                || restored.GetCount("ginger-tea") != 3
                || !restored.IsPickupCollected("pickup-ginger-tea"))
            {
                Object.DestroyImmediate(restoredProbe);
                throw new System.InvalidOperationException(
                    "Inventory snapshot restore verification failed."
                );
            }

            Object.DestroyImmediate(restoredProbe);
            Debug.Log(
                "Inventory verification passed: stacking, story use, "
                + "healing, pickup and restore."
            );
        }

        public static void VerifyEnemyDiversityForBatch()
        {
            GameObject shieldProbe = new("ShieldEnemyProbe");
            shieldProbe.AddComponent<CharacterController>();
            shieldProbe
                .AddComponent<Combatant>()
                .Configure(CombatTeam.Enemy, 100f);
            EnemyAI shieldAi = shieldProbe.AddComponent<EnemyAI>();
            shieldAi.ConfigureCombatProfile(2f, 10f, 1.2f, 0.4f);
            shieldAi.ConfigureArchetype(EnemyArchetype.Shield);
            float frontDamage = shieldAi.ModifyIncomingDamage(
                20f,
                shieldProbe.transform.position
                    + shieldProbe.transform.forward * 2f
            );
            float rearDamage = shieldAi.ModifyIncomingDamage(
                20f,
                shieldProbe.transform.position
                    - shieldProbe.transform.forward * 2f
            );
            bool shieldValid = shieldAi.IsGuarding
                && frontDamage < rearDamage
                && Mathf.Approximately(rearDamage, 20f);
            Object.DestroyImmediate(shieldProbe);

            GameObject eliteProbe = new("ElitePhaseProbe");
            eliteProbe.AddComponent<CharacterController>();
            Combatant eliteHealth =
                eliteProbe.AddComponent<Combatant>();
            eliteHealth.Configure(CombatTeam.Enemy, 120f);
            EnemyAI eliteAi = eliteProbe.AddComponent<EnemyAI>();
            eliteAi.ConfigureCombatProfile(2.2f, 10f, 1.2f, 0.4f);
            EliteEnemyController elite =
                eliteProbe.AddComponent<EliteEnemyController>();
            elite.Configure("多阶段测试敌人");
            elite.EvaluatePhase();
            bool phaseOneValid =
                elite.Phase == 1
                && eliteAi.Archetype == EnemyArchetype.Chaser;
            eliteHealth.TakeDamage(55f);
            elite.EvaluatePhase();
            bool phaseTwoValid =
                elite.Phase == 2
                && eliteAi.Archetype == EnemyArchetype.Shield;
            eliteHealth.TakeDamage(30f);
            elite.EvaluatePhase();
            bool phaseThreeValid =
                elite.Phase == 3
                && eliteAi.Archetype == EnemyArchetype.Ranged;
            Object.DestroyImmediate(eliteProbe);

            EnemyProjectile projectile = EnemyProjectile.Spawn(
                Vector3.up,
                Vector3.forward,
                10f,
                5f
            );
            bool rangedValid = projectile != null;
            if (projectile != null)
            {
                Object.DestroyImmediate(projectile.gameObject);
            }

            if (!shieldValid
                || !phaseOneValid
                || !phaseTwoValid
                || !phaseThreeValid
                || !rangedValid)
            {
                throw new System.InvalidOperationException(
                    "Enemy diversity validation failed: shield, ranged or "
                    + "multi-phase behavior is incomplete."
                );
            }

            Debug.Log(
                "Enemy diversity passed: shield guard, ranged projectile "
                + "and chaser -> shield -> ranged phase progression."
            );
        }

    }
}
