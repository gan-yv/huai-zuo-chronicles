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
        public int ActiveIndex => activeIndex;
        public bool IsPartyDefeated
        {
            get
            {
                for (int index = 0; index < characters.Length; index++)
                {
                    Combatant health = characters[index] != null
                        ? characters[index].GetComponent<Combatant>()
                        : null;
                    if (health != null && health.IsAlive)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Configure(Transform first, Transform second)
        {
            characters = new[] { first, second };
            activeIndex = 0;
        }

        public void RestoreActiveCharacter(int index)
        {
            if (characters == null || index < 0 || index >= characters.Length)
            {
                return;
            }

            SetActiveCharacter(index, moveToActivePosition: false);
        }

        private void Start()
        {
            SetActiveCharacter(activeIndex, moveToActivePosition: false);
        }

        private void Update()
        {
            if (Time.timeScale > 0f
                && Keyboard.current != null
                && Keyboard.current.tabKey.wasPressedThisFrame)
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
                SetActiveCharacter(otherIndex, moveToActivePosition: true);
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
                SetActiveCharacter(targetIndex, moveToActivePosition: true);
            }
        }

        private void SetActiveCharacter(int index, bool moveToActivePosition)
        {
            if (characters == null || characters.Length == 0)
            {
                return;
            }

            Transform previousCharacter = ActiveCharacter;
            Vector3 previousPosition = previousCharacter != null
                ? previousCharacter.position
                : Vector3.zero;
            Quaternion previousRotation = previousCharacter != null
                ? previousCharacter.rotation
                : Quaternion.identity;

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

                CharacterController controller = character.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.enabled = false;
                    if (isActive && moveToActivePosition)
                    {
                        character.SetPositionAndRotation(previousPosition, previousRotation);
                    }

                    controller.enabled = isActive;
                }

                Renderer[] renderers = character.GetComponentsInChildren<Renderer>(includeInactive: true);
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = isActive;
                }
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
