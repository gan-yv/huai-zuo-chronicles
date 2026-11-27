using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo02.WorldState
{
    public enum WorldContentStatus
    {
        Undiscovered,
        InProgress,
        ReadyToTurnIn,
        Completed
    }

    [Serializable]
    public sealed class WorldContentEntry
    {
        public string ContentId;
        public string Title;
        public string AreaId;
        public string AreaName;
        public string Hint;
        public string EncounterId;
        public float MapX;
        public float MapY;
    }

    public sealed class WorldContentTracker : MonoBehaviour
    {
        [SerializeField] private List<WorldContentEntry> entries = new();

        public static WorldContentTracker Instance { get; private set; }
        public IReadOnlyList<WorldContentEntry> Entries => entries;

        private void Awake()
        {
            Instance = this;
            Demo02.Core.GameServices.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Demo02.Core.GameServices.Unregister(this);
                Instance = null;
            }
        }

        public void Configure(IEnumerable<WorldContentEntry> contentEntries)
        {
            entries = contentEntries != null
                ? new List<WorldContentEntry>(contentEntries)
                : new List<WorldContentEntry>();
        }

        public WorldContentStatus GetStatus(WorldContentEntry entry)
        {
            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
            if (entry == null || runtime == null)
            {
                return WorldContentStatus.Undiscovered;
            }
            if (runtime.IsWorldContentCompleted(entry.ContentId))
            {
                return WorldContentStatus.Completed;
            }
            if (runtime.IsEncounterCompleted(entry.EncounterId))
            {
                return WorldContentStatus.ReadyToTurnIn;
            }
            return runtime.IsWorldContentStarted(entry.ContentId)
                ? WorldContentStatus.InProgress
                : WorldContentStatus.Undiscovered;
        }

        public void UpdateEntryHint(string contentId, string hint)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index]?.ContentId == contentId)
                {
                    entries[index].Hint = hint ?? string.Empty;
                    return;
                }
            }
        }

        public string GetJournalSummary()
        {
            List<string> lines = new();
            foreach (WorldContentEntry entry in entries)
            {
                WorldContentStatus status = GetStatus(entry);
                string statusText = status switch
                {
                    WorldContentStatus.InProgress => "进行中",
                    WorldContentStatus.ReadyToTurnIn => "可交付",
                    WorldContentStatus.Completed => "已完成",
                    _ => "未发现"
                };
                lines.Add(
                    $"{entry.AreaName} · {entry.Title}\n"
                    + $"{statusText} · {entry.Hint}"
                );
            }
            return lines.Count > 0
                ? "\n\n街区事件\n" + string.Join("\n", lines)
                : string.Empty;
        }

        public WorldContentEntry GetTrackedEntry()
        {
            foreach (WorldContentEntry entry in entries)
            {
                WorldContentStatus status = GetStatus(entry);
                if (status == WorldContentStatus.InProgress
                    || status == WorldContentStatus.ReadyToTurnIn)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}
