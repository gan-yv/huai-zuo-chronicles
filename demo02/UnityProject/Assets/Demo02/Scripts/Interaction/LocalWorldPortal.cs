using Demo02.Player;
using Demo02.Core;
using Demo02.UI;
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
            GameAudio.PlayPortal();
            ScreenFade fade = Demo02.Core.GameServices.Resolve<ScreenFade>();
            if (fade != null)
            {
                if (!fade.PlayTransition(() =>
                {
                    TeleportParty(
                        destination,
                        destinationAreaId,
                        destinationAreaName,
                        out _
                    );
                }))
                {
                    return $"正在前往{destinationAreaName}。";
                }

                return $"正在前往{destinationAreaName}。";
            }

            return TeleportParty(destination, destinationAreaId, destinationAreaName, out string message)
                ? message
                : "无法找到可用的街区入口。";
        }

        public static bool TeleportParty(
            Transform target,
            string areaId,
            string areaName,
            out string message)
        {
            message = string.Empty;
            PlayerPartyController party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
            Transform activeCharacter = party != null ? party.ActiveCharacter : null;
            if (activeCharacter == null || target == null)
            {
                return false;
            }

            CharacterController controller = activeCharacter.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            activeCharacter.SetPositionAndRotation(
                target.position + Vector3.up,
                target.rotation
            );

            if (controller != null)
            {
                controller.enabled = true;
            }

            WorldRuntime.Instance?.SetCurrentArea(areaId, areaName);
            message = $"抵达{areaName}。";
            return true;
        }
    }
}
