using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Core
{
    [DefaultExecutionOrder(60)]
    public sealed class OpeningSequence : MonoBehaviour
    {
        private void Start()
        {
            WorldRuntime runtime = WorldRuntime.Instance;
            if (runtime == null || !runtime.ConsumeOpeningRequest())
            {
                return;
            }

            DialogueController dialogue = FindAnyObjectByType<DialogueController>();
            if (dialogue == null)
            {
                return;
            }

            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "旁白",
                    "黄昏钟声响起后，广陵官方地图上不存在的一条旧街重新出现在城墙之内。"
                ),
                new DialogueLine(
                    "主角",
                    "我的修复工具在发烫。这里的时空正在互相覆盖。"
                ),
                new DialogueLine(
                    "旁白",
                    "调查三处异常记录，穿过时间层稳定锚点，再决定这条旧街是否能继续存在。"
                )
            });
        }
    }
}
