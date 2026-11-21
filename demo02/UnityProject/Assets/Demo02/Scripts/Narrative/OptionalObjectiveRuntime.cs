namespace Demo02.Narrative
{
    public static class OptionalObjectiveRuntime
    {
        public static OptionalObjectiveDefinition FindActive(
            CaseDefinition content,
            InvestigationState state)
        {
            if (content == null || state == null)
            {
                return null;
            }

            for (int index = 0;
                index < content.OptionalObjectives.Count;
                index++)
            {
                OptionalObjectiveDefinition objective =
                    content.OptionalObjectives[index];
                if (objective != null
                    && !IsCompleted(objective, state)
                    && DialogueContentRuntime.ConditionsMatch(
                        objective.ActivationConditions,
                        state
                    ))
                {
                    return objective;
                }
            }

            return null;
        }

        public static bool IsCompleted(
            OptionalObjectiveDefinition objective,
            InvestigationState state)
        {
            return objective != null
                && state != null
                && objective.CompletionConditions.Count > 0
                && DialogueContentRuntime.ConditionsMatch(
                    objective.CompletionConditions,
                    state
                );
        }
    }
}
