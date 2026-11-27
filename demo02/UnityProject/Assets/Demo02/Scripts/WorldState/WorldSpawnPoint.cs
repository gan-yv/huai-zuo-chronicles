using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WorldSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = "default";
        [SerializeField] private bool isFastTravelAnchor;
        [SerializeField] private string anchorName;
        [SerializeField] private bool startsUnlocked;

        public string SpawnId => spawnId;
        public bool IsFastTravelAnchor => isFastTravelAnchor;
        public string AnchorName => anchorName;
        public bool StartsUnlocked => startsUnlocked;

        public void Configure(string id)
        {
            spawnId = id;
        }

        public void ConfigureAsAnchor(
            string displayName,
            bool unlockedByDefault)
        {
            isFastTravelAnchor = true;
            anchorName = displayName;
            startsUnlocked = unlockedByDefault;
        }

        private void Awake()
        {
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RegisterSpawnPoint(this);
            if (isFastTravelAnchor)
            {
                Demo02.Core.GameServices.Resolve<WorldRuntime>()?.RegisterAnchor(this);
            }
        }

        private void OnDestroy()
        {
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.UnregisterSpawnPoint(this);
            if (isFastTravelAnchor)
            {
                Demo02.Core.GameServices.Resolve<WorldRuntime>()?.UnregisterAnchor(this);
            }
        }
    }
}
