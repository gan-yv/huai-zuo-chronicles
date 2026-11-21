using System.Collections.Generic;
using System.Linq;
using Demo02.Narrative;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class CaseDefinitionSeeder
    {
        public const string Chapter01AssetPath =
            "Assets/Demo02/Data/Quests/Chapter01_Case.asset";

        [MenuItem("Demo02/Validate Case Content")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
        }

        public static CaseDefinition LoadOrCreateChapter01()
        {
            CaseDefinition existing = AssetDatabase.LoadAssetAtPath<CaseDefinition>(
                Chapter01AssetPath
            );
            if (existing != null)
            {
                return existing;
            }

            CaseDefinition definition = ScriptableObject.CreateInstance<CaseDefinition>();
            definition.name = "Chapter01_Case";
            definition.ConfigureForEditor(
                "chapter-01",
                "消失的盐引",
                "CHAPTER 01",
                "official-seal",
                CreateEvidence(),
                CreateTestimonies(),
                CreateContradiction(),
                CreateStageCopy(),
                CreateConclusionCopy()
            );
            AssetDatabase.CreateAsset(definition, Chapter01AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created chapter content asset at {Chapter01AssetPath}");
            return definition;
        }

        public static void ValidateOrThrow()
        {
            CaseDefinition definition = LoadOrCreateChapter01();
            List<string> errors = definition.Validate();
            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException(
                    "Case content validation failed:\n"
                    + string.Join("\n", errors.Select(error => $"- {error}"))
                );
            }

            Debug.Log(
                $"Case content validated: {definition.CaseId}, "
                + $"{definition.Evidence.Count} evidence, "
                + $"{definition.Testimonies.Count} testimonies."
            );
        }

        private static List<EvidenceDefinition> CreateEvidence()
        {
            return new List<EvidenceDefinition>
            {
                new()
                {
                    Id = "old-street-sign",
                    Title = "盐引巷门牌",
                    Detail =
                        "旧漆下藏着“盐引巷”三字。这里是盐船入城后的第一处验引点。",
                    InteractionPrompt = "E  调查旧街门牌",
                    DiscoveredText =
                        "门牌背面刮着一层旧漆。对着光能认出“盐引巷”，"
                        + "三枚铜钉下还刻着：西仓、东验、中册。"
                },
                new()
                {
                    Id = "salt-ledger",
                    Title = "七车盐账牌",
                    Detail =
                        "七车私盐于初七后半夜入城，复核签字为沈砚。",
                    InteractionPrompt = "E  调查盐仓账牌",
                    DiscoveredText =
                        "账牌背面残留七车盐的押运编号。复核签字是“沈砚”，"
                        + "入城日期为初七后半夜。"
                },
                new()
                {
                    Id = "official-seal",
                    Title = "陆崇除名封条",
                    Detail =
                        "封条日期早于旧街消失一天，背面写着“知情人沈砚，宜除名”。",
                    InteractionPrompt = "E  调查官府封条",
                    DiscoveredText =
                        "封条属于转运副使陆崇，日期比盐引巷从地图消失早一天。"
                        + "背面还有一行小字：知情人沈砚，宜除名。"
                }
            };
        }

        private static List<TestimonyDefinition> CreateTestimonies()
        {
            return new List<TestimonyDefinition>
            {
                new()
                {
                    Id = "shenbo-seal-date",
                    Source = "沈伯 · 守仓人",
                    Text =
                        "封条是初五傍晚贴上的。那天阿砚还在仓里，"
                        + "门外还没有那七车盐。"
                },
                new()
                {
                    Id = "teahouse-salt-carts",
                    Source = "茶铺伙计 · 街口",
                    Text =
                        "初五到初八都见过盐车，我记不清七车究竟是在哪一夜卸下的。"
                },
                new()
                {
                    Id = "clerk-removal-order",
                    Source = "官署抄书吏 · 册页旁证",
                    Text =
                        "旧街的名字是在封仓之后，才从官署册页上一起消失的。"
                }
            };
        }

        private static ContradictionDefinition CreateContradiction()
        {
            return new()
            {
                StatementId = "shenbo-seal-date",
                EvidenceId = "salt-ledger",
                Explanation =
                    "初五的封条已经写明要把沈砚除名，七车盐却在初七夜里才入城。"
                    + "除名发生在私盐入城之前，说明陆崇不是查案后处置，"
                    + "而是提前抹掉知情人。",
                FirstHint =
                    "这两份记录可以同时成立。寻找一份写有具体日期、"
                    + "另一份记录同一事件先后的内容。",
                RepeatedHint =
                    "再比对一次时间。沈伯说的是封条何时贴上，"
                    + "账牌记录的是七车盐何时入城。"
            };
        }

        private static List<StageCopyDefinition> CreateStageCopy()
        {
            return new List<StageCopyDefinition>
            {
                new()
                {
                    Stage = ChapterStage.Arrival,
                    Objective = "前往旧仓内院，询问守仓人沈伯",
                    Reasoning =
                        "沈砚失踪后，官署和街坊同时忘记了他。"
                        + "先询问沈伯，确认最后留下的三处记录。"
                },
                new()
                {
                    Stage = ChapterStage.Investigate,
                    Objective = "调查盐引巷留下的记录  {0}/{1}",
                    Reasoning =
                        "沈砚带走了七页私盐账簿。门牌、账牌与官府封条"
                        + "必须互相印证，才能证明盐引巷曾经存在。"
                },
                new()
                {
                    Stage = ChapterStage.ReviewEvidence,
                    Objective = "把三处记录带回旧仓，让沈伯辨认笔迹",
                    Reasoning =
                        "三处证据已经指向同一个夜晚。沈伯认得儿子的笔迹，"
                        + "下一步应让他确认账牌签名。"
                },
                new()
                {
                    Stage = ChapterStage.CrossExamineTestimony,
                    Objective =
                        "按 T 打开证词对照，找出封条与盐车记录的时间矛盾",
                    Reasoning =
                        "沈伯说封条在初五傍晚贴上，账牌却记录七车盐"
                        + "直到初七夜里才入城。如果两句话都成立，"
                        + "除名就发生在私盐入城之前。"
                },
                new()
                {
                    Stage = ChapterStage.StabilizeAnchors,
                    Objective =
                        "按记录顺序稳定锚点：{0} / {1}/{2}",
                    Reasoning =
                        "陆崇的封条比盐引巷消失早一天，说明除名是预谋。"
                        + "门牌背面的铜钉顺序是“西仓、东验、中册”，"
                        + "错序会重置锚点。"
                },
                new()
                {
                    Stage = ChapterStage.RepairRift,
                    Objective = "前往街尾，压下被撕开的记录",
                    Reasoning =
                        "完整证据链已经形成。街尾时缝是盐引巷被撕掉的缺口，"
                        + "压下它才能读取最后一页。"
                },
                new()
                {
                    Stage = ChapterStage.FinalDecision,
                    Objective = "在街尾决定盐引巷与沈砚的命运",
                    Reasoning =
                        "恢复旧街会让真相重新进入官册，也会引来陆崇的势力；"
                        + "封存旧街能换来稳定，却会再次抹去沈砚。"
                },
                new()
                {
                    Stage = ChapterStage.Resolved,
                    Objective = "调查已经结束",
                    Reasoning = "盐引巷的命运已经确定。"
                }
            };
        }

        private static List<ConclusionCopyDefinition> CreateConclusionCopy()
        {
            return new List<ConclusionCopyDefinition>
            {
                new()
                {
                    Conclusion = ConclusionChoice.Preserve,
                    Objective = "盐引巷重新入册，沈砚的名字回到城市记录中",
                    Reasoning =
                        "盐引巷重新入册，沈砚的名字恢复。"
                        + "陆崇私盐案将成为下一场冲突的入口。"
                },
                new()
                {
                    Conclusion = ConclusionChoice.Seal,
                    Objective = "盐引巷被永久封存，沈砚只留在你的修复记录里",
                    Reasoning =
                        "盐引巷被封存，城市记录恢复稳定。"
                        + "沈砚仍只存在于你的私人修复记录中。"
                }
            };
        }
    }
}
