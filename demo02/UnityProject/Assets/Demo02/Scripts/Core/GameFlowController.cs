using Demo02.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Demo02.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        private InvestigationState investigationState;

        private void Awake()
        {
            investigationState = FindAnyObjectByType<InvestigationState>();
        }

        private void Update()
        {
            bool canRestart = investigationState != null && investigationState.IsConclusionReached;
            if (!canRestart
                || Keyboard.current == null
                || !Keyboard.current.rKey.wasPressedThisFrame)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                Debug.LogWarning("Add Greybox to Build Settings before using scene restart.");
            }
        }
    }
}
