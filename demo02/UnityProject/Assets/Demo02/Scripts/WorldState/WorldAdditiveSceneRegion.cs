using System.Collections.Generic;
using Demo02.Combat;
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
        [SerializeField, Min(0.1f)] private float retryBaseDelay = 0.75f;
        [SerializeField] private bool loadOnStart;

        private AsyncOperation loadOperation;
        private AsyncOperation unloadOperation;
        private Scene loadedScene;
        private WorldEncounterZone[] sceneZones;
        private WorldContentEvent[] sceneContentEvents;
        private bool loaded;
        private bool completed;
        private float nextCheck;
        private float nextRetryTime;
        private int retryAttempt;
        private string lastLoadError;

        public string RegionId => regionId;
        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public float LoadRadius => loadRadius;
        public float PreloadRadius => preloadRadius;
        public float UnloadRadius => unloadRadius;
        public bool PreloadOnStart => loadOnStart;
        public bool IsLoading =>
            loadOperation != null || unloadOperation != null;
        public bool IsUnloading => unloadOperation != null;
        public bool IsWaitingForRetry =>
            retryAttempt > 0
            && Time.unscaledTime < nextRetryTime
            && !IsLoading;
        public int RetryAttempt => retryAttempt;
        public string LastLoadError => lastLoadError;
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
            if (Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
                && Demo02.Core.GameServices.Resolve<WorldRuntime>().IsStreamingRegionCompleted(
                    regionId
                ))
            {
                completed = true;
            }
            if (loadOnStart)
            {
                Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.RequestLoad(
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

            if (unloadOperation != null)
            {
                if (unloadOperation.isDone)
                {
                    CompleteUnload();
                }
                return;
            }

            if (IsLoading)
            {
                if (loadOperation.isDone)
                {
                    loadOperation = null;
                    loadedScene = SceneManager.GetSceneByName(sceneName);
                    if (!loadedScene.IsValid()
                        || !loadedScene.isLoaded)
                    {
                        RecordLoadFailure(
                            $"Streaming scene failed to load: "
                            + $"{displayName} ({sceneName})"
                        );
                        return;
                    }
                    retryAttempt = 0;
                    nextRetryTime = 0f;
                    lastLoadError = null;
                    loaded = true;
                    CacheSceneContent();
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
            if (Demo02.Core.GameServices.Resolve<WorldRuntime>() != null
                && Demo02.Core.GameServices.Resolve<WorldRuntime>().IsStreamingRegionCompleted(
                    regionId
                ))
            {
                completed = true;
                Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.Cancel(this);
                return;
            }

            Transform player =
                Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.PlayerTransform;
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
                    Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.RequestLoad(
                        this,
                        urgent: true
                    );
                }
                else if (distanceSquared
                         <= preloadRadius * preloadRadius)
                {
                    Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.RequestLoad(
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
            if (loaded
                || loadOperation != null
                || unloadOperation != null
                || completed
                || Time.unscaledTime < nextRetryTime)
            {
                return false;
            }

            loadOperation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );
            if (loadOperation == null)
            {
                RecordLoadFailure(
                    $"Could not start streaming scene: {sceneName}"
                );
                return false;
            }
            return true;
        }

        private void CacheSceneContent()
        {
            List<WorldEncounterZone> zones = new();
            List<WorldContentEvent> contentEvents = new();
            foreach (GameObject rootObject in
                     loadedScene.GetRootGameObjects())
            {
                zones.AddRange(
                    rootObject.GetComponentsInChildren<
                        WorldEncounterZone
                    >(includeInactive: true)
                );
                contentEvents.AddRange(
                    rootObject.GetComponentsInChildren<
                        WorldContentEvent
                    >(includeInactive: true)
                );
            }

            sceneZones = zones.ToArray();
            sceneContentEvents = contentEvents.ToArray();
        }

        private bool CanUnload()
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return false;
            }

            foreach (WorldEncounterZone zone in sceneZones)
            {
                zone.RefreshResolvedState();
                if (!zone.IsResolved)
                {
                    return false;
                }
            }

            foreach (WorldContentEvent contentEvent in sceneContentEvents)
            {
                if (!contentEvent.IsCompleted)
                {
                    return false;
                }
            }
            return true;
        }

        private void Unload()
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                CompleteUnload();
                return;
            }

            unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
            if (unloadOperation == null)
            {
                CompleteUnload();
                return;
            }

            loaded = false;
            Debug.Log(
                $"Streaming region unload started: {displayName}"
            );
        }

        private void CompleteUnload()
        {
            unloadOperation = null;
            completed = true;
            loaded = false;
            loadedScene = default;
            sceneZones = null;
            sceneContentEvents = null;
            Demo02.Core.GameServices.Resolve<WorldRuntime>()?.MarkStreamingRegionCompleted(regionId);
            Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.Cancel(this);
            Debug.Log(
                $"Streaming region unloaded after completion: {displayName}"
            );
        }

        private void RecordLoadFailure(string error)
        {
            retryAttempt++;
            float delay = Mathf.Min(
                8f,
                retryBaseDelay * Mathf.Pow(2f, retryAttempt - 1)
            );
            nextRetryTime = Time.unscaledTime + delay;
            lastLoadError = error;
            Demo02.Core.GameServices.Resolve<WorldStreamingDirector>()?.Cancel(this);
            Debug.LogWarning(
                $"{error}. Retry {retryAttempt} in {delay:0.00}s."
            );
        }

#if UNITY_EDITOR
        public void RecordLoadFailureForTests(string error)
        {
            RecordLoadFailure(error);
        }
#endif
    }
}
