using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Interaction
{
    public sealed class WorldPortal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId;
        [SerializeField] private string prompt = "E  前往下一区域";

        public bool CanInteract => !string.IsNullOrWhiteSpace(targetScene);
        public string Prompt => prompt;

        public void Configure(string sceneName, string spawnId, string interactionPrompt)
        {
            targetScene = sceneName;
            targetSpawnId = spawnId;
            prompt = interactionPrompt;
        }

        public string Interact(PlayerInteractor interactor)
        {
            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime == null)
            {
                return "世界运行时尚未初始化。";
            }

            runtime.RequestSceneLoad(targetScene, targetSpawnId);
            return "正在前往下一区域。";
        }
    }
}
