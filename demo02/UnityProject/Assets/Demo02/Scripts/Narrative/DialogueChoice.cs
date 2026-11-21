using System;

namespace Demo02.Narrative
{
    [Serializable]
    public sealed class DialogueChoice
    {
        private readonly string text;

        public string Text => text;

        public DialogueChoice(string choiceText)
        {
            text = choiceText;
        }
    }
}
