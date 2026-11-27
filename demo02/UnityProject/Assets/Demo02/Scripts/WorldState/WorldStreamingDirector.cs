using System.Collections.Generic;
using Demo02.Player;
using UnityEngine;

namespace Demo02.WorldState
{
    [DefaultExecutionOrder(-30)]
    public sealed class WorldStreamingDirector : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float loadInterval = 0.35f;

        private readonly HashSet<WorldAdditiveSceneRegion> requests = new();
        private readonly HashSet<WorldAdditiveSceneRegion> urgentRequests =
            new();
        private WorldAdditiveSceneRegion activeRegion;
        private PlayerPartyController party;
        private float nextLoadTime;

        public static WorldStreamingDirector Instance { get; private set; }
        public bool IsLoading => activeRegion != null;
        public string ActiveRegionId =>
            activeRegion != null ? activeRegion.RegionId : string.Empty;
        public int PendingRequestCount => requests.Count;
        public Transform PlayerTransform
        {
            get
            {
                if (party == null)
                {
                    party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
                }
                return party != null ? party.ActiveCharacter : null;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RequestLoad(
            WorldAdditiveSceneRegion region,
            bool urgent)
        {
            if (region == null
                || region.IsLoaded
                || region.IsLoading
                || region.IsCompleted)
            {
                return;
            }

            requests.Add(region);
            if (urgent)
            {
                urgentRequests.Add(region);
            }
        }

        public void Cancel(WorldAdditiveSceneRegion region)
        {
            if (region != null)
            {
                requests.Remove(region);
                urgentRequests.Remove(region);
            }
        }

        private void Update()
        {
            if (activeRegion != null)
            {
                if (!activeRegion.IsLoading)
                {
                    activeRegion = null;
                    nextLoadTime = Time.unscaledTime + loadInterval;
                }
                return;
            }

            if (Time.unscaledTime < nextLoadTime)
            {
                return;
            }

            WorldAdditiveSceneRegion next = FindNextRegion();
            if (next == null)
            {
                return;
            }

            requests.Remove(next);
            urgentRequests.Remove(next);
            activeRegion = next;
            if (!next.BeginLoadFromDirector())
            {
                activeRegion = null;
                nextLoadTime = Time.unscaledTime + loadInterval;
            }
        }

        private WorldAdditiveSceneRegion FindNextRegion()
        {
            Transform player = PlayerTransform;
            if (player == null)
            {
                return null;
            }

            WorldAdditiveSceneRegion best = null;
            float bestDistance = float.MaxValue;
            bool bestUrgent = false;
            foreach (WorldAdditiveSceneRegion region in requests)
            {
                if (region == null
                    || region.IsLoaded
                    || region.IsLoading
                    || region.IsCompleted)
                {
                    continue;
                }

                Vector3 offset = player.position - region.transform.position;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                bool urgent = urgentRequests.Contains(region)
                    || distance <= region.LoadRadius * region.LoadRadius;
                if (best == null
                    || (urgent && !bestUrgent)
                    || (urgent == bestUrgent && distance < bestDistance))
                {
                    best = region;
                    bestDistance = distance;
                    bestUrgent = urgent;
                }
            }
            return best;
        }
    }
}
