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
            WorldRuntime runtime = Demo02.Core.GameServices.Resolve<WorldRuntime>();
            if (runtime == null || !runtime.ConsumeOpeningRequest())
            {
                return;
            }

            DialogueController dialogue = Demo02.Core.GameServices.Resolve<DialogueController>();
            if (dialogue == null)
            {
                return;
            }

            dialogue.Begin(new[]
            {
                new DialogueLine(
                    "旁白",
                    "景和十七年，黄昏。广陵官署收到一批无名盐引，交付地点却写着地图上不存在的“盐引巷”。"
                ),
                new DialogueLine(
                    "主角",
                    "每一条被抹掉的记录，都会在时间里留下回声。修复工具正在指向旧仓。"
                ),
                new DialogueLine(
                    "沈伯的口信",
                    "我儿子沈砚是盐仓账房。昨夜他带走七页账簿，今早却再没人记得他。"
                ),
                new DialogueLine(
                    "主角",
                    "去旧仓内院找沈伯。先查清沈砚失踪前最后留下的东西。"
                )
            });
        }
    }
}
