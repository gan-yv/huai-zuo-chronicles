using System.Collections;
using System.Collections.Generic;
using Demo02.Core;
using Demo02.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.WorldState
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(WorldStreamingDirector))]
    public sealed class WorldRuntime : MonoBehaviour
    {
        private readonly Dictionary<string, WorldSpawnPoint> spawnPoints = new();
        private readonly Dictionary<string, WorldSpawnPoint> anchors = new();
        private readonly HashSet<string> unlockedAnchorIds = new();
        private readonly HashSet<string> completedStreamingRegionIds =
            new();
        private readonly HashSet<string> completedEncounterIds = new();
        private readonly HashSet<string> completedWorldContentIds = new();
        private readonly HashSet<string> startedWorldContentIds = new();

        public static WorldRuntime Instance { get; private set; }

        public string CurrentWorldId { get; private set; } = "huai-zuo";
        public string CurrentChapterId { get; private set; } = "chapter-01";
        public string CurrentCaseId { get; private set; } = "chapter-01";
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
            if (GetComponent<WorldStreamingDirector>() == null)
            {
                gameObject.AddComponent<WorldStreamingDirector>();
            }
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

        public void RegisterAnchor(WorldSpawnPoint anchor)
        {
            if (anchor == null
                || !anchor.IsFastTravelAnchor
                || string.IsNullOrWhiteSpace(anchor.SpawnId))
            {
                return;
            }

            anchors[anchor.SpawnId] = anchor;
            if (anchor.StartsUnlocked)
            {
                unlockedAnchorIds.Add(anchor.SpawnId);
            }
        }

        public void UnregisterAnchor(WorldSpawnPoint anchor)
        {
            if (anchor != null
                && anchors.TryGetValue(
                    anchor.SpawnId,
                    out WorldSpawnPoint current
                )
                && current == anchor)
            {
                anchors.Remove(anchor.SpawnId);
            }
        }

        public bool IsAnchorUnlocked(string anchorId)
        {
            return !string.IsNullOrWhiteSpace(anchorId)
                && unlockedAnchorIds.Contains(anchorId);
        }

        public bool UnlockAnchor(string anchorId)
        {
            return !string.IsNullOrWhiteSpace(anchorId)
                && unlockedAnchorIds.Add(anchorId);
        }

        public IReadOnlyCollection<string> ExportUnlockedAnchorIds()
        {
            return unlockedAnchorIds;
        }

        public void RestoreUnlockedAnchors(
            IEnumerable<string> anchorIds)
        {
            if (anchorIds == null)
            {
                return;
            }

            unlockedAnchorIds.Clear();
            foreach (string anchorId in anchorIds)
            {
                if (!string.IsNullOrWhiteSpace(anchorId))
                {
                    unlockedAnchorIds.Add(anchorId);
                }
            }

            foreach (WorldSpawnPoint anchor in anchors.Values)
            {
                if (anchor.StartsUnlocked)
                {
                    unlockedAnchorIds.Add(anchor.SpawnId);
                }
            }
        }

        public bool IsStreamingRegionCompleted(string regionId)
        {
            return !string.IsNullOrWhiteSpace(regionId)
                && completedStreamingRegionIds.Contains(regionId);
        }

        public void MarkStreamingRegionCompleted(string regionId)
        {
            if (!string.IsNullOrWhiteSpace(regionId))
            {
                completedStreamingRegionIds.Add(regionId);
            }
        }

        public IReadOnlyCollection<string>
            ExportCompletedStreamingRegionIds()
        {
            return completedStreamingRegionIds;
        }

        public void RestoreCompletedStreamingRegions(
            IEnumerable<string> regionIds)
        {
            completedStreamingRegionIds.Clear();
            if (regionIds == null)
            {
                return;
            }

            foreach (string regionId in regionIds)
            {
                if (!string.IsNullOrWhiteSpace(regionId))
                {
                    completedStreamingRegionIds.Add(regionId);
                }
            }
        }

        public bool IsEncounterCompleted(string encounterId)
        {
            return !string.IsNullOrWhiteSpace(encounterId)
                && completedEncounterIds.Contains(encounterId);
        }

        public void MarkEncounterCompleted(string encounterId)
        {
            if (!string.IsNullOrWhiteSpace(encounterId))
            {
                completedEncounterIds.Add(encounterId);
            }
        }

        public IReadOnlyCollection<string> ExportCompletedEncounterIds()
        {
            return completedEncounterIds;
        }

        public void RestoreCompletedEncounters(
            IEnumerable<string> encounterIds)
        {
            completedEncounterIds.Clear();
            if (encounterIds == null)
            {
                return;
            }

            foreach (string encounterId in encounterIds)
            {
                if (!string.IsNullOrWhiteSpace(encounterId))
                {
                    completedEncounterIds.Add(encounterId);
                }
            }
        }

        public bool IsWorldContentCompleted(string contentId)
        {
            return !string.IsNullOrWhiteSpace(contentId)
                && completedWorldContentIds.Contains(contentId);
        }

        public void MarkWorldContentCompleted(string contentId)
        {
            if (!string.IsNullOrWhiteSpace(contentId))
            {
                completedWorldContentIds.Add(contentId);
            }
        }

        public IReadOnlyCollection<string>
            ExportCompletedWorldContentIds()
        {
            return completedWorldContentIds;
        }

        public void RestoreCompletedWorldContent(
            IEnumerable<string> contentIds)
        {
            completedWorldContentIds.Clear();
            if (contentIds == null)
            {
                return;
            }

            foreach (string contentId in contentIds)
            {
                if (!string.IsNullOrWhiteSpace(contentId))
                {
                    completedWorldContentIds.Add(contentId);
                }
            }
        }

        public bool IsWorldContentStarted(string contentId)
        {
            return !string.IsNullOrWhiteSpace(contentId)
                && startedWorldContentIds.Contains(contentId);
        }

        public void MarkWorldContentStarted(string contentId)
        {
            if (!string.IsNullOrWhiteSpace(contentId))
            {
                startedWorldContentIds.Add(contentId);
            }
        }

        public IReadOnlyCollection<string> ExportStartedWorldContentIds()
        {
            return startedWorldContentIds;
        }

        public void RestoreStartedWorldContent(IEnumerable<string> contentIds)
        {
            startedWorldContentIds.Clear();
            if (contentIds == null)
            {
                return;
            }
            foreach (string contentId in contentIds)
            {
                if (!string.IsNullOrWhiteSpace(contentId))
                {
                    startedWorldContentIds.Add(contentId);
                }
            }
        }

        public void SetCurrentArea(string areaId, string displayName)
        {
            CurrentAreaId = areaId;
            CurrentAreaName = displayName;
        }

        public void BeginChapter(string chapterId, string caseId)
        {
            RestoreChapterIdentity(chapterId, caseId);
            BeginNewSession();
        }

        public void RestoreChapterIdentity(
            string chapterId,
            string caseId)
        {
            if (!string.IsNullOrWhiteSpace(chapterId))
            {
                CurrentChapterId = chapterId;
            }
            if (!string.IsNullOrWhiteSpace(caseId))
            {
                CurrentCaseId = caseId;
            }
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
            if (mode == LoadSceneMode.Single)
            {
                CurrentDistrictId = scene.name;
            }
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
