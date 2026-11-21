using System;
using System.Collections.Generic;
using Demo02.Narrative;
using Demo02.WorldState;

namespace Demo02.Core
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public int Version = 1;
        public string SavedAtUtc;
        public string SceneName;
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
        public ChapterStage ChapterStage;
        public bool TestimonyContradictionSolved;
        public int ContradictionAttempts;
        public bool IsRepaired;
        public ConclusionChoice Conclusion;
        public List<CombatantSaveData> Combatants = new();
    }

    [Serializable]
    public sealed class CombatantSaveData
    {
        public string ObjectName;
        public float Health;
        public bool Active;
    }
}
