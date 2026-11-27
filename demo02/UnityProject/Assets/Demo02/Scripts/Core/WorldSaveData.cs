using System;
using System.Collections.Generic;
using Demo02.Narrative;
using Demo02.WorldState;

namespace Demo02.Core
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public string SavedAtUtc;
        public string SceneName;
        public string ChapterId;
        public string CaseId;
        public float PlayerX;
        public float PlayerY;
        public float PlayerZ;
        public float PlayerRotationY;
        public int ActivePartyIndex;
        public TimeLayer TimeLayer;
        public string AreaId;
        public string AreaName;
        public WitnessStance Stance;
        public EvidencePlan EvidencePlan;
        public List<string> ClueIds = new();
        public List<string> TimeAnchorIds = new();
        public List<string> EchoIds = new();
        public List<string> StoryFactIds = new();
        public List<InventoryItemSaveData> InventoryItems = new();
        public List<string> CollectedPickupIds = new();
        public List<string> UnlockedAnchorIds = new();
        public List<string> CompletedStreamingRegionIds = new();
        public List<string> CompletedEncounterIds = new();
        public List<string> CompletedWorldContentIds = new();
        public List<string> StartedWorldContentIds = new();
        public ChapterStage ChapterStage;
        public bool TestimonyContradictionSolved;
        public int ContradictionAttempts;
        public bool IsRepaired;
        public ConclusionChoice Conclusion;
        public List<CombatantSaveData> Combatants = new();
    }

    [Serializable]
    internal sealed class SaveFileEnvelope
    {
        public const string CurrentFormat = "Demo02.Save";
        public const int CurrentFormatVersion = 1;

        public string Format = CurrentFormat;
        public int FormatVersion = CurrentFormatVersion;
        public int SaveVersion;
        public string Checksum;
        public string Payload;
    }

    [Serializable]
    public sealed class CombatantSaveData
    {
        public string PersistentId;
        // Retained for version 2 compatibility and diagnostics.
        public string ObjectName;
        public float Health;
        public bool Active;
    }

    [Serializable]
    public sealed class InventoryItemSaveData
    {
        public string ItemId;
        public int Count;
    }
}
