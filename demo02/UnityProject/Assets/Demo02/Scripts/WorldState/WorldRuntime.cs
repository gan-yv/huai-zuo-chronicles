using System.Collections;
using System.Collections.Generic;
using Demo02.Core;
using Demo02.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.WorldState
{
    [DefaultExecutionOrder(-100)]
    public sealed class WorldRuntime : MonoBehaviour
    {
        private readonly Dictionary<string, WorldSpawnPoint> spawnPoints = new();

        public static WorldRuntime Instance { get; private set; }

        public string CurrentWorldId { get; private set; } = "huai-zuo";
        public string CurrentDistrictId { get; private set; }
        public string CurrentAreaId { get; private set; }
        public string CurrentAreaName { get; private set; }
        public WorldSaveData PendingSave { get; private set; }
        public bool ShouldShowOpening { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentDistrictId = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        public void RegisterSpawnPoint(WorldSpawnPoint spawnPoint)
        {
            if (spawnPoint != null && !string.IsNullOrWhiteSpace(spawnPoint.SpawnId))
            {
                spawnPoints[spawnPoint.SpawnId] = spawnPoint;
            }
        }

        public void UnregisterSpawnPoint(WorldSpawnPoint spawnPoint)
        {
            if (spawnPoint != null && spawnPoints.TryGetValue(spawnPoint.SpawnId, out WorldSpawnPoint current))
            {
                if (current == spawnPoint)
                {
                    spawnPoints.Remove(spawnPoint.SpawnId);
                }
            }
        }

        public void SetCurrentArea(string areaId, string displayName)
        {
            CurrentAreaId = areaId;
            CurrentAreaName = displayName;
        }

        public void BeginNewSession()
        {
            ShouldShowOpening = true;
            PendingSave = null;
        }

        public bool ConsumeOpeningRequest()
        {
            bool shouldShow = ShouldShowOpening;
            ShouldShowOpening = false;
            return shouldShow;
        }

        public void QueueSaveLoad(WorldSaveData saveData)
        {
            PendingSave = saveData;
        }

        public void ClearPendingSave()
        {
            PendingSave = null;
        }

        public void RequestSceneLoad(string sceneName, string spawnId)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("WorldRuntime cannot load an empty scene name.");
                return;
            }

            pendingSpawnId = spawnId;
            SceneManager.LoadScene(sceneName);
        }

        private string pendingSpawnId;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentDistrictId = scene.name;
            if (!string.IsNullOrWhiteSpace(pendingSpawnId))
            {
                StartCoroutine(PlacePlayerAtPendingSpawn());
            }
        }

        private IEnumerator PlacePlayerAtPendingSpawn()
        {
            yield return null;
            string spawnId = pendingSpawnId;
            pendingSpawnId = null;

            if (!spawnPoints.TryGetValue(spawnId, out WorldSpawnPoint spawnPoint))
            {
                Debug.LogWarning($"Spawn point '{spawnId}' was not found in the loaded scene.");
                yield break;
            }

            PlayerPartyController party = FindAnyObjectByType<PlayerPartyController>();
            if (party == null || party.ActiveCharacter == null)
            {
                Debug.LogWarning("WorldRuntime could not find the player party after scene load.");
                yield break;
            }

            CharacterController controller = party.ActiveCharacter.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null)
            {
                controller.enabled = false;
            }

            party.ActiveCharacter.SetPositionAndRotation(
                spawnPoint.transform.position,
                spawnPoint.transform.rotation
            );

            if (controller != null)
            {
                controller.enabled = wasEnabled;
            }
        }
    }
}
