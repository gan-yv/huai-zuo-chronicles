using Demo02.Combat;
using Demo02.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Demo02.WorldState
{
    [DefaultExecutionOrder(-20)]
    public sealed class WorldAdditiveSceneRegion : MonoBehaviour
    {
        [SerializeField] private string regionId = "streaming-region";
        [SerializeField] private string sceneName = "StreamingRegion";
        [SerializeField] private string displayName = "异步区域";
        [SerializeField, Min(1f)] private float loadRadius = 18f;
        [SerializeField, Min(1f)] private float preloadRadius = 32f;
        [SerializeField, Min(1f)] private float unloadRadius = 30f;
        [SerializeField] private bool loadOnStart;

        private AsyncOperation loadOperation;
        private Scene loadedScene;
        private bool loaded;
        private bool completed;
        private float nextCheck;

        public string RegionId => regionId;
        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public float LoadRadius => loadRadius;
        public float PreloadRadius => preloadRadius;
        public bool IsLoading => loadOperation != null;
        public bool IsLoaded => loaded;
        public bool IsCompleted => completed;

        public void Configure(
            string id,
            string targetSceneName,
            string regionDisplayName,
            float loadDistance,
            float preloadDistance,
            float unloadDistance,
            bool preload)
        {
            regionId = id;
            sceneName = targetSceneName;
            displayName = regionDisplayName;
            loadRadius = Mathf.Max(1f, loadDistance);
            preloadRadius = Mathf.Max(loadRadius + 1f, preloadDistance);
            unloadRadius = Mathf.Max(loadRadius + 1f, unloadDistance);
            loadOnStart = preload;
        }

        private void Start()
        {
            if (WorldRuntime.Instance != null
                && WorldRuntime.Instance.IsStreamingRegionCompleted(
                    regionId
                ))
            {
                completed = true;
            }
            if (loadOnStart)
            {
                WorldStreamingDirector.Instance?.RequestLoad(
                    this,
                    urgent: true
                );
            }
        }

        private void Update()
        {
            if (Time.time < nextCheck)
            {
                return;
            }
            nextCheck = Time.time + 0.3f;

            if (IsLoading)
            {
                if (loadOperation.isDone)
                {
                    loadOperation = null;
                    loaded = true;
                    loadedScene = SceneManager.GetSceneByName(sceneName);
                    Debug.Log(
                        $"Streaming region loaded: {displayName} "
                        + $"({sceneName})"
                    );
                }
                return;
            }

            if (completed)
            {
                return;
            }
            if (WorldRuntime.Instance != null
                && WorldRuntime.Instance.IsStreamingRegionCompleted(
                    regionId
                ))
            {
                completed = true;
                WorldStreamingDirector.Instance?.Cancel(this);
                return;
            }

            PlayerPartyController party =
                FindAnyObjectByType<PlayerPartyController>();
            Transform player = party != null ? party.ActiveCharacter : null;
            if (player == null)
            {
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (!loaded && loadOperation == null)
            {
                if (distanceSquared <= loadRadius * loadRadius)
                {
                    WorldStreamingDirector.Instance?.RequestLoad(
                        this,
                        urgent: true
                    );
                }
                else if (distanceSquared
                         <= preloadRadius * preloadRadius)
                {
                    WorldStreamingDirector.Instance?.RequestLoad(
                        this,
                        urgent: false
                    );
                }
            }
            else if (loaded
                && distanceSquared >= unloadRadius * unloadRadius
                && CanUnload())
            {
                Unload();
            }
        }

        public bool BeginLoadFromDirector()
        {
            if (loaded || loadOperation != null || completed)
            {
                return false;
            }

            loadOperation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );
            if (loadOperation == null)
            {
                Debug.LogError(
                    $"Could not start streaming scene: {sceneName}"
                );
                completed = true;
                return false;
            }
            return true;
        }

        private bool CanUnload()
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return false;
            }

            WorldEncounterZone[] zones = Object.FindObjectsByType<
                WorldEncounterZone
            >(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool foundZone = false;
            foreach (WorldEncounterZone zone in zones)
            {
                if (zone.gameObject.scene != loadedScene)
                {
                    continue;
                }

                foundZone = true;
                zone.RefreshResolvedState();
                if (!zone.IsResolved)
                {
                    return false;
                }
            }

            WorldContentEvent[] contentEvents = Object.FindObjectsByType<
                WorldContentEvent
            >(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (WorldContentEvent contentEvent in contentEvents)
            {
                if (contentEvent.gameObject.scene != loadedScene)
                {
                    continue;
                }

                foundZone = true;
                if (!contentEvent.IsCompleted)
                {
                    return false;
                }
            }
            return foundZone;
        }

        private void Unload()
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                completed = true;
                return;
            }

            SceneManager.UnloadSceneAsync(loadedScene);
            completed = true;
            loaded = false;
            WorldRuntime.Instance?.MarkStreamingRegionCompleted(regionId);
            WorldStreamingDirector.Instance?.Cancel(this);
            Debug.Log(
                $"Streaming region unloaded after completion: {displayName}"
            );
        }
    }
}
