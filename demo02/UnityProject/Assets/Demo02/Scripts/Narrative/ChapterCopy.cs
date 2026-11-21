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

            CaseDefinition content = state.CaseContent;
            if (content == null)
            {
                return "案件内容资产尚未加载";
            }

            if (state.SelectedConclusion == ConclusionChoice.Preserve)
            {
                return content.GetConclusionObjective(ConclusionChoice.Preserve);
            }

            if (state.SelectedConclusion == ConclusionChoice.Seal)
            {
                return content.GetConclusionObjective(ConclusionChoice.Seal);
            }

            return state.Stage switch
            {
                ChapterStage.Investigate => content.GetObjective(
                    state.Stage,
                    state.CollectedClueCount,
                    state.RequiredClueCount
                ),
                ChapterStage.StabilizeAnchors => content.GetObjective(
                    state.Stage,
                    TimeAnchorPuzzleRules.OrderHint,
                    state.StabilizedTimeAnchorCount,
                    state.RequiredTimeAnchorCount
                ),
                _ => content.GetObjective(state.Stage)
            };
        }

        public static string GetReasoning(InvestigationState state)
        {
            if (state == null)
            {
                return "没有可读取的调查记录。";
            }

            CaseDefinition content = state.CaseContent;
            if (content == null)
            {
                return "案件内容资产尚未加载。";
            }

            if (state.SelectedConclusion != ConclusionChoice.None)
            {
                return content.GetConclusionReasoning(
                    state.SelectedConclusion
                );
            }

            return content.GetReasoning(state.Stage);
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
