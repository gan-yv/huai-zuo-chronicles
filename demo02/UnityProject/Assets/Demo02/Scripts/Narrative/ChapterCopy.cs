namespace Demo02.Narrative
{
    public static class ChapterCopy
    {
        public static string GetObjective(InvestigationState state)
        {
            if (state == null)
            {
                return "调查案卷尚未初始化";
            }

            if (state.SelectedConclusion == ConclusionChoice.Preserve)
            {
                return "盐引巷重新入册，沈砚的名字回到城市记录中";
            }

            if (state.SelectedConclusion == ConclusionChoice.Seal)
            {
                return "盐引巷被永久封存，沈砚只留在你的修复记录里";
            }

            return state.Stage switch
            {
                ChapterStage.Arrival => "前往旧仓内院，询问守仓人沈伯",
                ChapterStage.Investigate =>
                    $"调查盐引巷留下的记录  {state.CollectedClueCount}/{state.RequiredClueCount}",
                ChapterStage.ReviewEvidence => "把三处记录带回旧仓，让沈伯辨认笔迹",
                ChapterStage.StabilizeAnchors =>
                    $"按 Q 切换时间层，稳定锚点  {state.StabilizedTimeAnchorCount}/{state.RequiredTimeAnchorCount}",
                ChapterStage.RepairRift => "前往街尾，压下被撕开的记录",
                ChapterStage.FinalDecision => "在街尾决定盐引巷与沈砚的命运",
                _ => "调查已经结束"
            };
        }

        public static string GetReasoning(InvestigationState state)
        {
            if (state == null)
            {
                return "没有可读取的调查记录。";
            }

            return state.Stage switch
            {
                ChapterStage.Arrival =>
                    "沈砚失踪后，官署和街坊同时忘记了他。先询问沈伯，确认最后留下的三处记录。",
                ChapterStage.Investigate =>
                    "沈砚带走了七页私盐账簿。门牌、账牌与官府封条必须互相印证，才能证明盐引巷曾经存在。",
                ChapterStage.ReviewEvidence =>
                    "三处证据已经指向同一个夜晚。沈伯认得儿子的笔迹，下一步应让他确认账牌签名。",
                ChapterStage.StabilizeAnchors =>
                    "陆崇的封条比盐引巷消失早一天，说明除名是预谋。旧时层的三处锚点记录了那一夜。",
                ChapterStage.RepairRift =>
                    "完整证据链已经形成。街尾时缝是盐引巷被撕掉的缺口，压下它才能读取最后一页。",
                ChapterStage.FinalDecision =>
                    "恢复旧街会让真相重新进入官册，也会引来陆崇的势力；封存旧街能换来稳定，却会再次抹去沈砚。",
                _ => state.SelectedConclusion == ConclusionChoice.Preserve
                    ? "盐引巷重新入册，沈砚的名字恢复。陆崇私盐案将成为下一场冲突的入口。"
                    : "盐引巷被封存，城市记录恢复稳定。沈砚仍只存在于你的私人修复记录中。"
            };
        }

        public static string GetStanceLabel(InvestigationState state)
        {
            return state?.Stance switch
            {
                WitnessStance.EvidenceFirst => "证据优先",
                WitnessStance.PersonFirst => "先保住沈砚",
                _ => "尚未向沈伯表态"
            };
        }

        public static string GetPlanLabel(InvestigationState state)
        {
            return state?.Plan switch
            {
                EvidencePlan.PublicRecord => "公开证据",
                EvidencePlan.PrivateProtection => "私下保护",
                _ => "尚未决定证据去向"
            };
        }

        public static string GetChoiceConsequence(InvestigationState state)
        {
            if (state == null)
            {
                return string.Empty;
            }

            if (state.Stance == WitnessStance.PersonFirst)
            {
                return "你答应先保住沈砚，因此沈伯愿意把最私人的记忆交给你。";
            }

            if (state.Stance == WitnessStance.EvidenceFirst)
            {
                return "你坚持以记录为先，沈伯开始把你视作能查清此案的人。";
            }

            return string.Empty;
        }

        public static string GetEndingDetail(InvestigationState state)
        {
            if (state == null)
            {
                return string.Empty;
            }

            if (state.Plan == EvidencePlan.PrivateProtection)
            {
                return "你没有让官署先看到完整副本。沈伯和证人暂时安全，但真相只能由少数人继续保存。";
            }

            if (state.Plan == EvidencePlan.PublicRecord)
            {
                return "公开的账页副本让官署无法再装作不知情，沈伯也必须面对陆崇势力的追查。";
            }

            return "你尚未决定证据的去向，旧街的命运仍悬在记录之外。";
        }
    }
}
