using Demo02.Combat;
using Demo02.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo02.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerPartyController : MonoBehaviour
    {
        public const int MaxPartySlots = 4;

        [SerializeField] private Transform[] characters = new Transform[MaxPartySlots];
        [SerializeField] private int activeIndex;

        public Transform ActiveCharacter =>
            characters.Length > activeIndex ? characters[activeIndex] : null;
        public int ActiveIndex => activeIndex;
        public int PartySlotCount => characters.Length;
        public int UnlockedSlotCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < characters.Length; index++)
                {
                    if (characters[index] != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
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
            characters = new Transform[MaxPartySlots];
            characters[0] = first;
            characters[1] = second;
            activeIndex = 0;
        }

        public Transform GetCharacter(int index)
        {
            return characters != null && index >= 0 && index < characters.Length
                ? characters[index]
                : null;
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
            if (Time.timeScale > 0f && Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    SwitchCharacter(0);
                }
                if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    SwitchCharacter(1);
                }
                if (Keyboard.current.digit3Key.wasPressedThisFrame)
                {
                    SwitchCharacter(2);
                }
                if (Keyboard.current.digit4Key.wasPressedThisFrame)
                {
                    SwitchCharacter(3);
                }
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    CycleCharacter();
                }
            }

            Combatant activeHealth = ActiveCharacter != null
                ? ActiveCharacter.GetComponent<Combatant>()
                : null;

            if (activeHealth == null || activeHealth.IsAlive)
            {
                return;
            }

            for (int offset = 1; offset < characters.Length; offset++)
            {
                int nextIndex = (activeIndex + offset) % characters.Length;
                Combatant nextHealth = characters[nextIndex] != null
                    ? characters[nextIndex].GetComponent<Combatant>()
                    : null;
                if (nextHealth != null && nextHealth.IsAlive)
                {
                    SetActiveCharacter(nextIndex, moveToActivePosition: true);
                    return;
                }
            }
        }

        public bool SwitchCharacter(int targetIndex)
        {
            if (characters == null
                || targetIndex < 0
                || targetIndex >= characters.Length
                || characters[targetIndex] == null
                || targetIndex == activeIndex)
            {
                return false;
            }

            Combatant targetHealth = characters[targetIndex].GetComponent<Combatant>();
            if (targetHealth != null && targetHealth.IsAlive)
            {
                SetActiveCharacter(targetIndex, moveToActivePosition: true);
                return true;
            }

            return false;
        }

        private void CycleCharacter()
        {
            for (int offset = 1; offset < characters.Length; offset++)
            {
                int nextIndex = (activeIndex + offset) % characters.Length;
                if (SwitchCharacter(nextIndex))
                {
                    return;
                }
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
                SetEnabled<CharacterAbilityController>(character, isActive);
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
