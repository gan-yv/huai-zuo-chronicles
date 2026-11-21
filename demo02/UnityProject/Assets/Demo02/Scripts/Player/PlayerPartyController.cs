using Demo02.Combat;
using Demo02.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerPartyController : MonoBehaviour
    {
        [SerializeField] private Transform[] characters = new Transform[2];
        [SerializeField] private int activeIndex;

        public Transform ActiveCharacter =>
            characters.Length > activeIndex ? characters[activeIndex] : null;

        public void Configure(Transform first, Transform second)
        {
            characters = new[] { first, second };
            activeIndex = 0;
        }

        private void Start()
        {
            SetActiveCharacter(activeIndex);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SwitchCharacter();
            }

            Combatant activeHealth = ActiveCharacter != null
                ? ActiveCharacter.GetComponent<Combatant>()
                : null;

            if (activeHealth == null || activeHealth.IsAlive)
            {
                return;
            }

            int otherIndex = 1 - activeIndex;
            Combatant otherHealth = characters.Length > otherIndex
                ? characters[otherIndex].GetComponent<Combatant>()
                : null;

            if (otherHealth != null && otherHealth.IsAlive)
            {
                SetActiveCharacter(otherIndex);
            }
        }

        private void SwitchCharacter()
        {
            int targetIndex = 1 - activeIndex;
            if (characters.Length <= targetIndex || characters[targetIndex] == null)
            {
                return;
            }

            Combatant targetHealth = characters[targetIndex].GetComponent<Combatant>();
            if (targetHealth != null && targetHealth.IsAlive)
            {
                SetActiveCharacter(targetIndex);
            }
        }

        private void SetActiveCharacter(int index)
        {
            if (characters == null || characters.Length == 0)
            {
                return;
            }

            activeIndex = Mathf.Clamp(index, 0, characters.Length - 1);
            for (int current = 0; current < characters.Length; current++)
            {
                Transform character = characters[current];
                if (character == null)
                {
                    continue;
                }

                bool isActive = current == activeIndex;
                SetEnabled<ThirdPersonController>(character, isActive);
                SetEnabled<PlayerCombat>(character, isActive);
                SetEnabled<PlayerInteractor>(character, isActive);
                SetEnabled<CompanionFollower>(character, !isActive);
            }
        }

        private static void SetEnabled<T>(Transform character, bool enabled) where T : Behaviour
        {
            T component = character.GetComponent<T>();
            if (component != null)
            {
                component.enabled = enabled;
            }
        }
    }
}
