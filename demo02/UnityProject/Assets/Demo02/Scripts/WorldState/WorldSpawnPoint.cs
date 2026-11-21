using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WorldSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = "default";

        public string SpawnId => spawnId;

        public void Configure(string id)
        {
            spawnId = id;
        }

        private void Start()
        {
            WorldRuntime.Instance?.RegisterSpawnPoint(this);
        }

        private void OnDestroy()
        {
            WorldRuntime.Instance?.UnregisterSpawnPoint(this);
        }
    }
}
