using System.Collections.Generic;
using Demo02.Core;
using UnityEngine;

namespace Demo02.UI
{
    public static class GameplayUiState
    {
        private static readonly List<UnityEngine.Object> ModalStack = new();

        public static int OpenModalCount
        {
            get
            {
                PruneDestroyedModals();
                return ModalStack.Count;
            }
        }

        public static UnityEngine.Object ActiveModal
        {
            get
            {
                PruneDestroyedModals();
                return ModalStack.Count > 0
                    ? ModalStack[ModalStack.Count - 1]
                    : null;
            }
        }

        public static bool IsModalOpen => OpenModalCount > 0;

        public static bool IsOpen(UnityEngine.Object owner)
        {
            PruneDestroyedModals();
            return owner != null && ModalStack.Contains(owner);
        }

        public static bool HasOpenModalExcept(UnityEngine.Object owner)
        {
            for (int index = ModalStack.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object candidate = ModalStack[index];
                if (candidate == null)
                {
                    ModalStack.RemoveAt(index);
                    continue;
                }
                if (candidate != owner)
                {
                    return true;
                }
            }
            return false;
        }

        public static void RegisterModal(UnityEngine.Object owner)
        {
            if (owner == null)
            {
                return;
            }
            PruneDestroyedModals();
            ModalStack.Remove(owner);
            ModalStack.Add(owner);
        }

        public static void UnregisterModal(UnityEngine.Object owner)
        {
            if (owner == null)
            {
                PruneDestroyedModals();
                return;
            }
            ModalStack.Remove(owner);
        }

        public static void ResetModals()
        {
            ModalStack.Clear();
        }

        public static bool IsGameplayBlocked =>
            IsModalOpen
            || ResolveGameFlow() is { } flow
                && (flow.IsPaused || flow.IsGameEnding);

        private static void PruneDestroyedModals()
        {
            ModalStack.RemoveAll(owner => owner == null);
        }

        private static IGameFlowState ResolveGameFlow()
        {
            return GameServices.GetOrNull<IGameFlowState>()
                ?? GameFlowController.Instance;
        }
    }
}
