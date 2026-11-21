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
                if (existing.DialogueScripts.Count == 0
                    || existing.ContentVersion < 3)
                {
                    existing.ConfigureDialogueScriptsForEditor(
                        CreateDialogueScripts()
                    );
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Upgraded chapter asset to content version 3.");
                }
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
                CreateConclusionCopy(),
                CreateDialogueScripts()
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

        private static List<DialogueScriptDefinition> CreateDialogueScripts()
        {
            return new List<DialogueScriptDefinition>
            {
                Script(
                    "shenbo-arrival",
                    "E  询问沈砚失踪的事",
                    new[] { StageCondition(ChapterStage.Arrival) },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "你也是为那条不存在的街来的？今早我去官署报官，"
                            + "他们说我从来没有儿子。"
                        ),
                        Line(
                            "主角",
                            "官署收到一批盐引，交货地点写着‘盐引巷’。"
                            + "我在广陵地图上找不到这个名字。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "盐引巷就是这条旧街从前的名字。我儿子沈砚守着盐仓账。"
                            + "昨夜他带走了七页账簿，再没回来。"
                        ),
                        Line(
                            "主角",
                            "带我去看他最后留下的东西。旧街招牌、盐仓账牌、"
                            + "官府封条——三处记录若指向同一个人，就能把他找回来。"
                        )
                    },
                    new[]
                    {
                        Choice(
                            "我按记录办事。证据齐了，才有资格谈救人。",
                            Effect(
                                DialogueEffectType.SetWitnessStance,
                                WitnessStance.EvidenceFirst
                            )
                        ),
                        Choice(
                            "如果沈砚还活着，我会先把他带回来。",
                            Effect(
                                DialogueEffectType.SetWitnessStance,
                                WitnessStance.PersonFirst
                            )
                        )
                    },
                    new[]
                    {
                        Effect(DialogueEffectType.CompleteWitnessBriefing)
                    }
                ),
                Script(
                    "shenbo-investigate-person",
                    "E  询问沈砚最后留下的记录",
                    new[]
                    {
                        StageCondition(ChapterStage.Investigate),
                        StanceCondition(WitnessStance.PersonFirst)
                    },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "你肯先想着阿砚，我就把知道的都告诉你。"
                            + "后墙上的旧漆、账台的背面，还有官署那枚封条，都别漏。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "沈砚不会无缘无故带走账页。去找旧街招牌、盐仓账牌"
                            + "和官府封条。三张纸放在一起，才有人肯信。"
                        )
                    }
                ),
                Script(
                    "shenbo-investigate-evidence",
                    "E  询问沈砚最后留下的记录",
                    new[]
                    {
                        StageCondition(ChapterStage.Investigate),
                        StanceCondition(WitnessStance.EvidenceFirst)
                    },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "你说得对，先让三张纸彼此印证。"
                            + "官署不会信一个父亲的话，但会怕一份完整的账。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "沈砚不会无缘无故带走账页。去找旧街招牌、盐仓账牌"
                            + "和官府封条。三张纸放在一起，才有人肯信。"
                        )
                    }
                ),
                Script(
                    "shenbo-evidence-review",
                    "E  把三处记录交给沈伯辨认",
                    new[] { StageCondition(ChapterStage.ReviewEvidence) },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "这三笔……都是阿砚的字。"
                            + "七车盐从盐引巷入城，复核签字也是他。"
                        ),
                        Line(
                            "主角",
                            "封条是转运副使陆崇的，日期却在旧街消失前一天。"
                            + "他不是事后掩盖，而是提前写好了结局。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "所以阿砚不是逃了。是有人把私盐的账、盐引巷，"
                            + "还有知情的阿砚一起从纸上抹掉了。"
                        ),
                        Line(
                            "主角",
                            "等等。封条和账牌的日期对不上。把沈伯的说法"
                            + "与七车盐账牌放在一起，应该能看出陆崇"
                            + "究竟在什么时候决定抹掉阿砚。"
                        )
                    },
                    new[]
                    {
                        Choice(
                            "把复制记录送入官署，让所有人都看见。",
                            Effect(
                                DialogueEffectType.SetEvidencePlan,
                                evidencePlan: EvidencePlan.PublicRecord
                            )
                        ),
                        Choice(
                            "先藏住证据，保护沈伯和剩余证人。",
                            Effect(
                                DialogueEffectType.SetEvidencePlan,
                                evidencePlan: EvidencePlan.PrivateProtection
                            )
                        )
                    },
                    new[]
                    {
                        Effect(DialogueEffectType.CompleteEvidenceReview)
                    }
                ),
                Script(
                    "shenbo-cross-examine",
                    "E  请沈伯重述封条贴上的时间",
                    new[] { StageCondition(ChapterStage.CrossExamineTestimony) },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "封条是初五傍晚贴上的。那天阿砚还在仓里，"
                            + "他亲手数过门外的空盐车。"
                        ),
                        Line(
                            "主角",
                            "可账牌写着七车盐初七夜才入城。"
                            + "封条比私盐早了两天，他们要除掉的不是"
                            + "已经被抓住的把柄，而是预定的知情人。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "按 T，把这两句话并排放在案卷上。"
                            + "别只看它们各自写了什么，要看哪一句先发生。"
                        )
                    }
                ),
                Script(
                    "shenbo-anchor-person",
                    "E  询问如何稳定时间锚点",
                    new[]
                    {
                        StageCondition(ChapterStage.StabilizeAnchors),
                        StanceCondition(WitnessStance.PersonFirst)
                    },
                    AnchorGuidance(
                        "阿砚若还听得见，他会记得你刚才的话。"
                        + "去把那些门一扇扇找出来。"
                    )
                ),
                Script(
                    "shenbo-anchor-evidence",
                    "E  询问如何稳定时间锚点",
                    new[]
                    {
                        StageCondition(ChapterStage.StabilizeAnchors),
                        StanceCondition(WitnessStance.EvidenceFirst)
                    },
                    AnchorGuidance(
                        "先把旧时的路钉住。只有亲眼看见那一夜，"
                        + "你才知道这案子究竟被谁写下。"
                    )
                ),
                Script(
                    "shenbo-repair-private",
                    "E  询问街尾被撕开的记录",
                    new[]
                    {
                        StageCondition(ChapterStage.RepairRift),
                        PlanCondition(EvidencePlan.PrivateProtection)
                    },
                    RepairGuidance(
                        "你没把账页立刻交出去，"
                        + "陆崇的人就还不知道我们已经走到哪一步。小心些。"
                    )
                ),
                Script(
                    "shenbo-repair-public",
                    "E  询问街尾被撕开的记录",
                    new[]
                    {
                        StageCondition(ChapterStage.RepairRift),
                        PlanCondition(EvidencePlan.PublicRecord)
                    },
                    RepairGuidance(
                        "官署那边迟早会听见风声。"
                        + "你既然选择了公开，就不能只拿到一半真相。"
                    )
                ),
                Script(
                    "shenbo-final-person",
                    "E  请沈伯说出他的选择",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision),
                        StanceCondition(WitnessStance.PersonFirst)
                    },
                    FinalGuidance(
                        "你从一开始就说会先救阿砚。现在轮到你决定，"
                        + "是让他回来，还是让整条街继续安静下去。"
                    )
                ),
                Script(
                    "shenbo-final-evidence",
                    "E  请沈伯说出他的选择",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision),
                        StanceCondition(WitnessStance.EvidenceFirst)
                    },
                    FinalGuidance(
                        "你说过记录要先说话。现在记录已经说完了，"
                        + "剩下的不是证据，是人的选择。"
                    )
                ),
                EpilogueScript(
                    "shenbo-epilogue-preserve-public",
                    ConclusionChoice.Preserve,
                    EvidencePlan.PublicRecord,
                    WitnessStance.Undecided,
                    "门口的盐引旗升起来了。你把账页送进官署，"
                    + "这回整条街都听得见阿砚的算盘。"
                ),
                EpilogueScript(
                    "shenbo-epilogue-preserve-private",
                    ConclusionChoice.Preserve,
                    EvidencePlan.PrivateProtection,
                    WitnessStance.Undecided,
                    "门口的盐引旗升起来了。账页仍藏在暗处，"
                    + "但至少阿砚的名字回到了街上。"
                ),
                EpilogueScript(
                    "shenbo-epilogue-seal-person",
                    ConclusionChoice.Seal,
                    EvidencePlan.Undecided,
                    WitnessStance.PersonFirst,
                    "街口又空了。我知道你已经尽力，可阿砚还是没能回来。"
                    + "至少别忘记他。"
                ),
                EpilogueScript(
                    "shenbo-epilogue-seal-evidence",
                    ConclusionChoice.Seal,
                    EvidencePlan.Undecided,
                    WitnessStance.EvidenceFirst,
                    "街口又空了。记录稳定了，阿砚却仍不在册。"
                    + "或许这就是只相信纸张的代价。"
                ),
                Script(
                    "shenbo-epilogue-fallback",
                    "E  与沈伯告别",
                    new[] { StageCondition(ChapterStage.Resolved) },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "盐引巷又安静下来了。无论记录怎么写，"
                            + "我都会记得阿砚曾经在这里。"
                        )
                    }
                ),
                Script(
                    "teahouse-epilogue-preserve-public",
                    "E  向掌柜打听盐引旗",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        PlanCondition(EvidencePlan.PublicRecord)
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "今早盐引旗一升，整条街都在谈七车盐。"
                            + "官署的人来过又走了，这回他们没法只当没听见。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "沈砚那张靠窗的桌子我留着。"
                            + "等茶客再提起他的名字，我不会再忘。"
                        )
                    },
                    npcId: "teahouse"
                ),
                Script(
                    "teahouse-epilogue-preserve-private",
                    "E  向掌柜打听盐引旗",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        PlanCondition(EvidencePlan.PrivateProtection)
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "盐引旗升起来了，可你知道账页还在暗处。"
                            + "我会让伙计收好茶炉，真有人来问，这里什么都没看见。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "沈砚的名字回来了。只要名字还在，"
                            + "就总有能说出口的一天。"
                        )
                    },
                    npcId: "teahouse"
                ),
                Script(
                    "teahouse-epilogue-seal",
                    "E  与掌柜告别",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "街口安静得像什么都没发生过。"
                            + "只是那张靠窗的桌子，我还是不敢让别人坐。"
                        )
                    },
                    npcId: "teahouse"
                ),
                Script(
                    "teahouse-rumor-seal",
                    "E  询问初七夜里的盐车",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.Resolved),
                        HasEvidenceCondition("official-seal")
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "你找到那枚封条了？初五傍晚他们封的是空仓，"
                            + "门口连一辆盐车都没有。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "初七后半夜，七辆车才从后巷过去。"
                            + "沈砚那晚坐在这里，反复数七张账页。"
                            + "他走时脸白得厉害。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "封条比盐车早两天，这账怎么算都不对。"
                            + "你若还能找到沈砚，替我把这壶凉茶热上。"
                        )
                    },
                    npcId: "teahouse"
                ),
                Script(
                    "teahouse-early",
                    "E  与茶肆掌柜交谈",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Arrival),
                        StageBeforeCondition(ChapterStage.ReviewEvidence)
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "昨夜打烊前，沈砚坐在靠窗那张桌，"
                            + "反复数七张账页。他走时把一枚盐引铜钱落在桌上。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "今早伙计问我铜钱是哪来的。"
                            + "我竟一时想不起沈砚是谁。茶凉了还能续，"
                            + "人从纸上消失，可就续不回来了。"
                        )
                    },
                    npcId: "teahouse"
                )
            };
        }

        private static DialogueScriptDefinition Script(
            string id,
            string prompt,
            DialogueConditionDefinition[] conditions,
            DialogueLine[] lines,
            DialogueChoiceDefinition[] choices = null,
            DialogueEffectDefinition[] completionEffects = null,
            string npcId = "shenbo")
        {
            return new DialogueScriptDefinition
            {
                Id = id,
                NpcId = npcId,
                InteractionPrompt = prompt,
                Conditions = new List<DialogueConditionDefinition>(conditions),
                Lines = new List<DialogueLine>(lines),
                Choices = choices == null
                    ? new List<DialogueChoiceDefinition>()
                    : new List<DialogueChoiceDefinition>(choices),
                CompletionEffects = completionEffects == null
                    ? new List<DialogueEffectDefinition>()
                    : new List<DialogueEffectDefinition>(completionEffects)
            };
        }

        private static DialogueLine[] AnchorGuidance(string response)
        {
            return new[]
            {
                Line("守仓人 沈伯", response),
                Line(
                    "守仓人 沈伯",
                    "阿砚小时候说，盐仓的火光照到旧墙上，"
                    + "会多出一排本不存在的门。"
                ),
                Line(
                    "主角",
                    "那是旧时层留下的影子。门牌上写着“西仓、东验、中册”。"
                    + "按这个顺序稳定锚点，错序会让已经稳定的部分重新漂移。"
                )
            };
        }

        private static DialogueLine[] RepairGuidance(string response)
        {
            return new[]
            {
                Line("守仓人 沈伯", response),
                Line(
                    "守仓人 沈伯",
                    "锚点安静下来了。可街尾那块地方……"
                    + "像被人从纸上硬生生撕开。"
                ),
                Line(
                    "主角",
                    "沈砚的名字还在裂缝另一侧。"
                    + "压下时缝，我就能看见最后那一页。"
                )
            };
        }

        private static DialogueLine[] FinalGuidance(string response)
        {
            return new[]
            {
                Line("守仓人 沈伯", response),
                Line(
                    "守仓人 沈伯",
                    "我已经很多年没听见有人叫阿砚了。"
                    + "你若恢复盐引巷，陆崇的人会知道；"
                    + "你若封存这里，阿砚也许再也回不来。"
                ),
                Line(
                    "主角",
                    "记录不会替我们选。要保住一个人、一条街，"
                    + "总得有人承担它重新出现后的代价。"
                )
            };
        }

        private static DialogueScriptDefinition EpilogueScript(
            string id,
            ConclusionChoice conclusion,
            EvidencePlan plan,
            WitnessStance stance,
            string response)
        {
            List<DialogueConditionDefinition> conditions = new()
            {
                StageCondition(ChapterStage.Resolved),
                ConclusionCondition(conclusion)
            };
            if (plan != EvidencePlan.Undecided)
            {
                conditions.Add(PlanCondition(plan));
            }
            if (stance != WitnessStance.Undecided)
            {
                conditions.Add(StanceCondition(stance));
            }

            return Script(
                id,
                "E  与沈伯告别",
                conditions.ToArray(),
                new[]
                {
                    Line("守仓人 沈伯", response)
                }
            );
        }

        private static DialogueLine Line(string speaker, string text)
        {
            return new DialogueLine(speaker, text);
        }

        private static DialogueChoiceDefinition Choice(
            string text,
            DialogueEffectDefinition effect)
        {
            return new DialogueChoiceDefinition
            {
                Text = text,
                Effects = new List<DialogueEffectDefinition> { effect }
            };
        }

        private static DialogueEffectDefinition Effect(
            DialogueEffectType type,
            WitnessStance stance = WitnessStance.Undecided,
            EvidencePlan evidencePlan = EvidencePlan.Undecided)
        {
            return new DialogueEffectDefinition
            {
                Type = type,
                Stance = stance,
                EvidencePlan = evidencePlan
            };
        }

        private static DialogueConditionDefinition StageCondition(
            ChapterStage stage)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.StageEquals,
                Stage = stage
            };
        }

        private static DialogueConditionDefinition StageAtLeastCondition(
            ChapterStage stage)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.StageAtLeast,
                Stage = stage
            };
        }

        private static DialogueConditionDefinition StageBeforeCondition(
            ChapterStage stage)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.StageBefore,
                Stage = stage
            };
        }

        private static DialogueConditionDefinition HasEvidenceCondition(
            string evidenceId)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.HasEvidence,
                EvidenceId = evidenceId
            };
        }

        private static DialogueConditionDefinition StanceCondition(
            WitnessStance stance)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.StanceEquals,
                Stance = stance
            };
        }

        private static DialogueConditionDefinition PlanCondition(
            EvidencePlan plan)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.EvidencePlanEquals,
                EvidencePlan = plan
            };
        }

        private static DialogueConditionDefinition ConclusionCondition(
            ConclusionChoice conclusion)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.ConclusionEquals,
                Conclusion = conclusion
            };
        }
    }
}
