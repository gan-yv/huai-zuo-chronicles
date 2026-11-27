using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Player;
using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WorldAnchorInteractable :
        MonoBehaviour,
        IInteractable
    {
        [SerializeField] private WorldSpawnPoint spawnPoint;
        [SerializeField] private ColorTint markerTint;
        [SerializeField] private Color lockedColor =
            new(0.92f, 0.72f, 0.3f);
        [SerializeField] private Color unlockedColor =
            new(0.3f, 0.62f, 0.54f);

        public bool CanInteract =>
            spawnPoint != null
            && spawnPoint.IsFastTravelAnchor
            && Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
            && !Demo02.Core.GameServices.Resolve<WorldRuntime>().IsAnchorUnlocked(
                spawnPoint.SpawnId
            );

        public string Prompt =>
            $"E  记录地区锚点 · {spawnPoint?.AnchorName}";

        public void Configure(WorldSpawnPoint point)
        {
            spawnPoint = point;
            markerTint ??= GetComponentInChildren<ColorTint>(true);
            RefreshVisual();
        }

        private void Start()
        {
            markerTint ??= GetComponentInChildren<ColorTint>(true);
            RefreshVisual();
        }

        public string Interact(PlayerInteractor interactor)
        {
            if (spawnPoint == null)
            {
                return "锚点尚未登记。";
            }

            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
            if (runtime == null)
            {
                return "世界状态尚未就绪。";
            }

            runtime.UnlockAnchor(spawnPoint.SpawnId);
            RestoreParty();
            RefreshVisual();
            return $"已记录锚点：{spawnPoint.AnchorName}";
        }

        public void RefreshVisual()
        {
            bool unlocked = spawnPoint != null
                && Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
                && Demo02.Core.GameServices.Resolve<WorldRuntime>().IsAnchorUnlocked(
                    spawnPoint.SpawnId
                );
            markerTint ??= GetComponentInChildren<ColorTint>(true);
            markerTint?.Configure(
                unlocked ? unlockedColor : lockedColor,
                0.4f,
                0.15f
            );
        }

        private static void RestoreParty()
        {
            PlayerPartyController party =
                Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            if (party == null)
            {
                return;
            }

            for (int index = 0; index < party.PartySlotCount; index++)
            {
                Transform member = party.GetCharacter(index);
                if (member == null)
                {
                    continue;
                }

                Combatant health = member.GetComponent<Combatant>();
                if (health != null && health.IsAlive)
                {
                    health.RestoreHealth(
                        Mathf.Min(
                            health.MaxHealth,
                            health.CurrentHealth
                                + health.MaxHealth * 0.35f
                        )
                    );
                }
                CharacterRuntime runtime =
                    member.GetComponent<CharacterRuntime>();
                if (runtime != null)
                {
                    runtime.RestoreStamina(runtime.MaxStamina);
                    runtime.GainEnergy(20f);
                }
            }
        }
    }
}
