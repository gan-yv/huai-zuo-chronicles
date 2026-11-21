using System;
using UnityEngine;

namespace Demo02.Narrative
{
    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private string speaker;
        [SerializeField, TextArea(2, 5)] private string text;

        public string Speaker => speaker;
        public string Text => text;

        public DialogueLine(string speakerName, string lineText)
        {
            speaker = speakerName;
            text = lineText;
        }
    }
}
