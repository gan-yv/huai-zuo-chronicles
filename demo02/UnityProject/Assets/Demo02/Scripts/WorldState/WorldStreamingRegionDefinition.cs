using System.Collections.Generic;
using Demo02.Combat;
using UnityEngine;

namespace Demo02.WorldState
{
    [CreateAssetMenu(
        fileName = "STREAM_NewRegion",
        menuName = "Demo02/World/Streaming Region Definition"
    )]
    public sealed class WorldStreamingRegionDefinition : ScriptableObject
    {
        [SerializeField] private string regionId = "streaming-region";
        [SerializeField] private string sceneName = "StreamingRegion";
        [SerializeField] private string displayName = "异步区域";
        [SerializeField, Min(0)] private int buildOrder;
        [SerializeField] private Vector3 worldPosition =
            new(0f, 0.9f, 0f);
        [SerializeField] private Vector3 enemyPosition =
            new(0f, 0.9f, 0f);
        [SerializeField, Min(1f)] private float loadRadius = 18f;
        [SerializeField, Min(1f)] private float preloadRadius = 32f;
        [SerializeField, Min(1f)] private float unloadRadius = 44f;
        [SerializeField] private bool preloadOnStart;
        [SerializeField] private string streamingRootName =
            "Streaming_Region";
        [SerializeField] private string encounterObjectName =
            "Encounter_Region";
        [SerializeField] private string enemyObjectName = "Enemy_Region";
        [SerializeField] private WorldEncounterDefinition encounterContent;
        [SerializeField] private string contentEventId;

        public string RegionId => regionId;
        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public int BuildOrder => buildOrder;
        public Vector3 WorldPosition => worldPosition;
        public Vector3 EnemyPosition => enemyPosition;
        public float LoadRadius => loadRadius;
        public float PreloadRadius => preloadRadius;
        public float UnloadRadius => unloadRadius;
        public bool PreloadOnStart => preloadOnStart;
        public string StreamingRootName => streamingRootName;
        public string EncounterObjectName => encounterObjectName;
        public string EnemyObjectName => enemyObjectName;
        public WorldEncounterDefinition EncounterContent =>
            encounterContent;
        public string ContentEventId => contentEventId;

        public List<string> Validate()
        {
            List<string> errors = new();
            if (string.IsNullOrWhiteSpace(regionId))
            {
                errors.Add($"{name}: region ID is empty.");
            }
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                errors.Add($"{name}: scene name is empty.");
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add($"{name}: display name is empty.");
            }
            if (string.IsNullOrWhiteSpace(streamingRootName)
                || string.IsNullOrWhiteSpace(encounterObjectName)
                || string.IsNullOrWhiteSpace(enemyObjectName))
            {
                errors.Add($"{name}: streaming object names are incomplete.");
            }
            if (loadRadius < 1f
                || preloadRadius <= loadRadius
                || unloadRadius < preloadRadius)
            {
                errors.Add(
                    $"{name}: radii must satisfy load < preload <= unload."
                );
            }
            if (encounterContent == null)
            {
                errors.Add($"{name}: encounter content is missing.");
            }
            else if (!encounterContent.IsValid(out string encounterError))
            {
                errors.Add($"{name}: {encounterError}");
            }
            return errors;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string id,
            string targetSceneName,
            string regionDisplayName,
            int order,
            Vector3 regionWorldPosition,
            Vector3 streamingEnemyPosition,
            float loadDistance,
            float preloadDistance,
            float unloadDistance,
            bool preload,
            string rootObjectName,
            string zoneObjectName,
            string enemyName,
            WorldEncounterDefinition encounterDefinition,
            string contentId)
        {
            regionId = id;
            sceneName = targetSceneName;
            displayName = regionDisplayName;
            buildOrder = order;
            worldPosition = regionWorldPosition;
            enemyPosition = streamingEnemyPosition;
            loadRadius = Mathf.Max(1f, loadDistance);
            preloadRadius = Mathf.Max(loadRadius + 1f, preloadDistance);
            unloadRadius = Mathf.Max(
                preloadRadius,
                unloadDistance
            );
            preloadOnStart = preload;
            streamingRootName = rootObjectName;
            encounterObjectName = zoneObjectName;
            enemyObjectName = enemyName;
            encounterContent = encounterDefinition;
            contentEventId = contentId;
        }
#endif
    }
}
