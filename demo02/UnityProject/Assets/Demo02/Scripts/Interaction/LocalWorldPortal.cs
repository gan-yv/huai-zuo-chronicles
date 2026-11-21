using Demo02.Player;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class LocalWorldPortal : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform destination;
        [SerializeField] private string destinationAreaId;
        [SerializeField] private string destinationAreaName;
        [SerializeField] private string prompt = "E  前往其他街区";

        public bool CanInteract => destination != null;
        public string Prompt => prompt;

        public void Configure(
            Transform target,
            string areaId,
            string areaName,
            string interactionPrompt)
        {
            destination = target;
            destinationAreaId = areaId;
            destinationAreaName = areaName;
            prompt = interactionPrompt;
        }

        public string Interact(PlayerInteractor interactor)
        {
            PlayerPartyController party = FindAnyObjectByType<PlayerPartyController>();
            Transform activeCharacter = party != null ? party.ActiveCharacter : null;
            if (activeCharacter == null || destination == null)
            {
                return "无法找到可用的街区入口。";
            }

            CharacterController controller = activeCharacter.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            activeCharacter.SetPositionAndRotation(
                destination.position + Vector3.up,
                destination.rotation
            );

            if (controller != null)
            {
                controller.enabled = true;
            }

            WorldRuntime.Instance?.SetCurrentArea(
                destinationAreaId,
                destinationAreaName
            );
            return $"抵达{destinationAreaName}。";
        }
    }
}
