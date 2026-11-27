using System.Collections.Generic;
using System.Linq;
using Demo02.Narrative;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class CaseDefinitionSeeder
    {
        public const string CaseFolder = "Assets/Demo02/Data/Cases";
        public const string Chapter01AssetPath =
            "Assets/Demo02/Data/Cases/Chapter01_Case.asset";
        private const string LegacyChapter01AssetPath =
            "Assets/Demo02/Data/Quests/Chapter01_Case.asset";

        [MenuItem("Demo02/Validate Case Content")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
        }

        [MenuItem("Demo02/Open Chapter Case Content")]
        public static void OpenChapterCase()
        {
            CaseDefinition definition = LoadOrCreateChapter01();
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        public static CaseDefinition LoadOrCreateChapter01()
        {
            EnsureFolder(CaseFolder);
            MigrateLegacyCaseAsset();
            CaseDefinition existing = AssetDatabase.LoadAssetAtPath<CaseDefinition>(
                Chapter01AssetPath
            );
            if (existing != null)
            {
                if (existing.DialogueScripts.Count == 0
                    || existing.ContentVersion < 16)
                {
                    existing.ConfigureOptionalContentForEditor(
                        CreateDialogueScripts(),
                        CreateStoryFacts(),
                        CreateInventoryItems(),
                        CreateOptionalObjectives()
                    );
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Upgraded chapter asset to content version 16.");
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
                CreateDialogueScripts(),
                CreateStoryFacts(),
                CreateInventoryItems(),
                CreateOptionalObjectives()
            );
            AssetDatabase.CreateAsset(definition, Chapter01AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created chapter content asset at {Chapter01AssetPath}");
            return definition;
        }

        private static void MigrateLegacyCaseAsset()
        {
            CaseDefinition legacy =
                AssetDatabase.LoadAssetAtPath<CaseDefinition>(
                    LegacyChapter01AssetPath
                );
            if (legacy == null)
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<CaseDefinition>(
                    Chapter01AssetPath
                ) != null)
            {
                throw new System.InvalidOperationException(
                    $"Both legacy and target case assets exist: " +
                    $"{LegacyChapter01AssetPath} and " +
                    $"{Chapter01AssetPath}"
                );
            }
            string error = AssetDatabase.MoveAsset(
                LegacyChapter01AssetPath,
                Chapter01AssetPath
            );
            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new System.InvalidOperationException(
                    $"Could not migrate case asset: {error}"
                );
            }
            AssetDatabase.Refresh();
            const string legacyFolder = "Assets/Demo02/Data/Quests";
            if (AssetDatabase.IsValidFolder(legacyFolder)
                && AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { legacyFolder }
                ).Length == 0)
            {
                AssetDatabase.DeleteAsset(legacyFolder);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = System.IO.Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent)
                || string.IsNullOrEmpty(name))
            {
                throw new System.InvalidOperationException(
                    $"Invalid folder: {path}"
                );
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
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

        private static List<StoryFactDefinition> CreateStoryFacts()
        {
            return new List<StoryFactDefinition>
            {
                new()
                {
                    Id = "warehouse-night-shift",
                    Title = "盐仓夜班记录",
                    Detail =
                        "搬运工记得七辆车、四十二袋盐。"
                        + "工头让他把夜班和沈砚一起从点名单上说成从未存在。"
                },
                new()
                {
                    Id = "warehouse-second-meal",
                    Title = "第二份干粮",
                    Detail =
                        "搬运工每天多带一份炊饼，留给女儿阿禾。"
                        + "阿禾的名字已从工册和户册消失，但饭每天都会少一份。"
                },
                new()
                {
                    Id = "warehouse-daughter-public",
                    Title = "阿禾写回公开名册",
                    Detail =
                        "你决定把阿禾的名字写进公开记录。"
                        + "她重新成为官府无法随口否认的人，也会引来陆崇的注意。"
                },
                new()
                {
                    Id = "warehouse-daughter-private",
                    Title = "阿禾留在私人案卷",
                    Detail =
                        "你没有公开阿禾的名字，只把她写进自己的修复记录。"
                        + "这保护了父女二人，也让她的存在再次依赖少数人的记忆。"
                },
                new()
                {
                    Id = "warehouse-daughter-remembered",
                    Title = "有人记得阿禾",
                    Detail =
                        "你听完搬运工与阿禾的故事，并决定把她的名字保存在"
                        + "公开名册或私人案卷。"
                },
                new()
                {
                    Id = "archive-clerk-ink-record",
                    Title = "新浆下的空白",
                    Detail =
                        "抄书吏严槐发现，被抹掉的名字处会留下新浆。"
                        + "纸张知道那里曾经写过什么，只是官府不肯承认。"
                },
                new()
                {
                    Id = "archive-clerk-father-name",
                    Title = "严复留在装订线里的名字",
                    Detail =
                        "严槐的父亲严复也曾守盐仓，并在十年前的旧册中消失。"
                        + "严槐一直保存着一张写着父亲名字的装订残页。"
                },
                new()
                {
                    Id = "archive-clerk-recruited",
                    Title = "严槐加入抄录",
                    Detail =
                        "你让严槐参与证据抄录，并同意把父亲严复的名字写在边栏。"
                        + "他愿意承担被发现的风险。"
                },
                new()
                {
                    Id = "archive-clerk-sheltered",
                    Title = "严槐留在局外",
                    Detail =
                        "你没有让严槐参与抄录，只让他收好父亲留下的拓页。"
                        + "他没有暴露，但仍把一份空白抄本留在抽屉里。"
                },
                new()
                {
                    Id = "lu-chong-threat",
                    Title = "陆崇的空白秩序",
                    Detail =
                        "陆崇当面承认，他需要让一些街和一些人从册页上消失，"
                        + "以维持自己能够控制的秩序。他并未把修复者视作真正的对手。"
                },
                new()
                {
                    Id = "teahouse-cart-testimony",
                    Title = "茶肆盐车旁证",
                    Detail =
                        "茶肆掌柜确认，初五封仓时门外没有盐车，"
                        + "七辆车直到初七后半夜才从后巷经过。"
                },
                new()
                {
                    Id = "teahouse-keeper-protected",
                    Title = "茶肆掌柜保持沉默",
                    Detail =
                        "你没有继续追问盐车细节，避免陆崇的人察觉茶肆"
                        + "也掌握了关键旁证。掌柜会记住这份克制。"
                },
                new()
                {
                    Id = "salt-token-identified",
                    Title = "盐引铜钱暗纹",
                    Detail =
                        "铜钱边缘的暗纹与盐引旧印吻合，"
                        + "说明它不仅是信物，也可能是一枚旧时验引凭证。"
                },
                new()
                {
                    Id = "teahouse-written-record",
                    Title = "茶肆旁证抄录",
                    Detail =
                        "你把茶肆掌柜关于初五空仓和初七盐车的说法"
                        + "抄在空白账页上，形成一份可随时复查的旁证记录。"
                },
                new()
                {
                    Id = "teahouse-event-completed",
                    Title = "茶肆盐车事件结束",
                    Detail =
                        "你已经决定是追问盐车细节，还是保护茶肆不被卷入。"
                },
                new()
                {
                    Id = "paper-kite-present-cord",
                    Title = "茶肆门框上的纸鸢线",
                    Detail =
                        "一段青色纸鸢线今早缠在东关街茶肆门框上，"
                        + "线尾打着旧时才会用的结。"
                },
                new()
                {
                    Id = "paper-kite-past-bell",
                    Title = "旧时风铃里的名字",
                    Detail =
                        "旧时风铃内侧刻着“沈阿绫，十二岁”。"
                        + "她曾替父亲守风铃，也把纸鸢放上东关街的屋顶。"
                },
                new()
                {
                    Id = "paper-kite-trail-secured",
                    Title = "途印追住的风线",
                    Detail =
                        "途印向导在屋顶航线的尽头缚住了旧风线，"
                        + "纸鸢留下的名字终于能够被修复者稳定。"
                },
                new()
                {
                    Id = "paper-kite-public-name",
                    Title = "沈阿绫重新入册",
                    Detail =
                        "你选择把沈阿绫写回公开户册。"
                        + "东关街多了一段能被官府查到的往事，也多了一批追问旧账的人。"
                },
                new()
                {
                    Id = "paper-kite-private-name",
                    Title = "名字留在街坊记忆里",
                    Detail =
                        "你没有把沈阿绫写回官册，"
                        + "而是让这个名字继续留在东关街坊和说书人的故事中。"
                }
            };
        }

        private static List<InventoryItemDefinition> CreateInventoryItems()
        {
            return new List<InventoryItemDefinition>
            {
                new()
                {
                    Id = "salt-token-charm",
                    DisplayName = "盐引铜钱",
                    Category = "信物",
                    Description =
                        "沈砚失踪前留在茶桌上的铜钱，边缘刻着他的名字。"
                        + "它不能直接使用，但可能与旧时盐引有关。",
                    MaxStack = 1,
                    Effect = InventoryItemEffect.GrantStoryFact,
                    UseLabel = "辨认铜钱上的刻字",
                    UseResultText =
                        "铜钱边缘的暗纹与盐引旧印吻合。"
                        + "这枚铜钱与旧时盐引制度有关。",
                    ResultStoryFactId = "salt-token-identified",
                    ConsumeOnUse = false
                },
                new()
                {
                    Id = "ginger-tea",
                    DisplayName = "热姜汤",
                    Category = "消耗品",
                    Description =
                        "茶肆掌柜用旧铜壶温着的姜汤。"
                        + "饮用后恢复 25 点生命。",
                    MaxStack = 5,
                    Effect = InventoryItemEffect.RestoreHealth,
                    EffectAmount = 25f,
                    UseLabel = "饮用",
                    UseResultText = "姜汤驱散了旧仓的寒意。",
                    ConsumeOnUse = true
                },
                new()
                {
                    Id = "blank-ledger-page",
                    DisplayName = "空白账页",
                    Category = "调查用品",
                    Description =
                        "从旧仓账册上拆下的空白页。"
                        + "可以在后续调查中抄录不愿留在原册里的证词。",
                    MaxStack = 3,
                    Effect = InventoryItemEffect.GrantStoryFact,
                    UseLabel = "抄录茶肆盐车旁证",
                    UseResultText =
                        "你把茶肆掌柜的证词抄到空白账页上，"
                        + "这份记录已经进入支线旁证。",
                    RequiredStoryFactId = "teahouse-cart-testimony",
                    ResultStoryFactId = "teahouse-written-record",
                    ConsumeOnUse = true
                },
                new()
                {
                    Id = "paper-kite-letter",
                    DisplayName = "纸鸢旧信",
                    Category = "信物",
                    Description =
                        "一小段青色纸鸢线和风铃上拓下的名字。"
                        + "它记录了沈阿绫留给东关街的最后一声风响。",
                    MaxStack = 1,
                    Effect = InventoryItemEffect.None,
                    ConsumeOnUse = false
                }
            };
        }

        private static List<OptionalObjectiveDefinition> CreateOptionalObjectives()
        {
            return new List<OptionalObjectiveDefinition>
            {
                new()
                {
                    Id = "optional-teahouse-cart-rumor",
                    Title = "东关街的盐车旁证",
                    Description =
                        "封条日期与账牌记录对不上。去东关街找茶肆掌柜，"
                        + "问问初七夜里是否有盐车经过。",
                    TrackerHint = "东关街茶肆",
                    TargetObjectName = "NPC_TeaMaster",
                    ActivationConditions = new List<DialogueConditionDefinition>
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.Resolved),
                        HasEvidenceCondition("official-seal")
                    },
                    CompletionConditions = new List<DialogueConditionDefinition>
                    {
                        HasFactCondition("teahouse-event-completed")
                    }
                },
                new()
                {
                    Id = "optional-record-teahouse-testimony",
                    Title = "把茶肆旁证抄进账页",
                    Description =
                        "你已听到茶肆掌柜关于盐车时间的证词。"
                        + "打开背包，用空白账页抄录这段旁证。",
                    TrackerHint = "打开背包 [B]",
                    TargetObjectName = string.Empty,
                    ActivationConditions = new List<DialogueConditionDefinition>
                    {
                        StageBeforeCondition(ChapterStage.Resolved),
                        HasFactCondition("teahouse-cart-testimony")
                    },
                    CompletionConditions = new List<DialogueConditionDefinition>
                    {
                        HasFactCondition("teahouse-written-record")
                    }
                },
                new()
                {
                    Id = "optional-warehouse-night-shift",
                    Title = "夜班搬运工",
                    Description =
                        "账牌写着七车盐，工头却让搬运工改口说六车。"
                        + "回旧仓内院问问那双记得四十二袋盐的肩膀。",
                    TrackerHint = "旧仓内院 · 盐仓搬运工",
                    TargetObjectName = "NPC_WarehouseWorker",
                    ActivationConditions =
                        new List<DialogueConditionDefinition>
                        {
                            StageAtLeastCondition(ChapterStage.Investigate),
                            StageBeforeCondition(ChapterStage.FinalDecision),
                            HasEvidenceCondition("salt-ledger")
                        },
                    CompletionConditions =
                        new List<DialogueConditionDefinition>
                        {
                            HasFactCondition("warehouse-night-shift")
                        }
                },
                new()
                {
                    Id = "optional-warehouse-second-meal",
                    Title = "第二份干粮",
                    Description =
                        "搬运工每天多带一份饭，却从不说是给谁。"
                        + "再回旧仓内院问问他，那份干粮究竟留给谁。",
                    TrackerHint = "旧仓内院 · 搬运工的第二份饭",
                    TargetObjectName = "NPC_WarehouseWorker",
                    ActivationConditions =
                        new List<DialogueConditionDefinition>
                        {
                            StageAtLeastCondition(ChapterStage.Investigate),
                            StageBeforeCondition(ChapterStage.Resolved),
                            HasFactCondition("warehouse-night-shift"),
                            LacksFactCondition("warehouse-second-meal")
                        },
                    CompletionConditions =
                        new List<DialogueConditionDefinition>
                        {
                            HasFactCondition(
                                "warehouse-daughter-remembered"
                            )
                        }
                }
            };
        }

        private static List<DialogueScriptDefinition> CreateDialogueScripts()
        {
            return new List<DialogueScriptDefinition>
            {
                Script(
                    "lu-chong-preserve",
                    "E  质问陆崇为何留下盐引巷",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve)
                    },
                    new[]
                    {
                        Line(
                            "转运副使 陆崇",
                            "你把街写回册里，很好。可册页归官署保管，"
                            + "今日多出一巷，明日就能多出一条罪名。"
                        ),
                        Line(
                            "转运副使 陆崇",
                            "我会暂时放过这些百姓。不是因为你修好了时缝，"
                            + "而是因为一条活街比一条死巷更容易重新封起来。"
                        )
                    },
                    choices: null,
                    completionEffects: new[]
                    {
                        SetFactEffect("lu-chong-threat")
                    },
                    npcId: "lu-chong"
                ),
                Script(
                    "lu-chong-seal",
                    "E  质问陆崇封街的代价",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "转运副使 陆崇",
                            "你替广陵做了正确的选择。少一条街，多一夜安宁，"
                            + "这笔账在官署里永远划得平。"
                        ),
                        Line(
                            "转运副使 陆崇",
                            "至于你记得的那几个人，不必担心。"
                            + "等下一本册子重抄时，连你也会忘记他们为何重要。"
                        )
                    },
                    choices: null,
                    completionEffects: new[]
                    {
                        SetFactEffect("lu-chong-threat")
                    },
                    npcId: "lu-chong"
                ),
                Script(
                    "lu-chong-final",
                    "E  质问陆崇要封住谁的嘴",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision)
                    },
                    new[]
                    {
                        Line(
                            "转运副使 陆崇",
                            "你终于把那条不存在的街修回来了。"
                            + "现在只要再选一次，七车盐就可以重新变成从未发生。"
                        ),
                        Line(
                            "主角",
                            "你提前写好除名封条，不是为了稳定，"
                            + "是为了让所有知情人都变成没人认识的人。"
                        ),
                        Line(
                            "转运副使 陆崇",
                            "秩序总得留一点空白。街活着，我还能给它一条巷；"
                            + "街进册，我就得让记得它的人一起消失。"
                        ),
                        Line(
                            "途印向导",
                            "他怕的不是这份账。他怕沈砚已经把最后一页交给了别人。"
                        ),
                        Line(
                            "转运副使 陆崇",
                            "选吧。保留，你要替这条街承担陆家的追查；"
                            + "封存，我可以让今夜没人死。"
                        )
                    },
                    choices: null,
                    completionEffects: new[]
                    {
                        SetFactEffect("lu-chong-threat")
                    },
                    npcId: "lu-chong"
                ),
                Script(
                    "ahe-preserve-private",
                    "E  和阿禾说话",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        HasFactCondition("warehouse-daughter-private")
                    },
                    new[]
                    {
                        Line(
                            "阿禾",
                            "爹说不能大声说自己叫什么。那我小声告诉你："
                            + "我叫阿禾，禾苗的禾。"
                        ),
                        Line(
                            "盐仓搬运工",
                            "她今天终于肯坐到桌边。以前我总怕一抬头，"
                            + "对面又只剩一只空碗。"
                        ),
                        Line(
                            "途印向导",
                            "名册可以晚一点补。先让她把这一顿饭吃完。"
                        )
                    },
                    npcId: "ahe"
                ),
                Script(
                    "ahe-preserve-public",
                    "E  和阿禾说话",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        HasFactCondition("warehouse-daughter-public")
                    },
                    new[]
                    {
                        Line(
                            "阿禾",
                            "官署的人来问话时，把我的姓念错了。"
                            + "不过没关系，我知道他们念的是我。"
                        ),
                        Line(
                            "盐仓搬运工",
                            "她今天敢自己走到门口了。街上有人认得她，"
                            + "比册子上那一个名字更要紧。"
                        )
                    },
                    npcId: "ahe"
                ),
                Script(
                    "archive-clerk-preserve",
                    "E  问严槐旧街是否进入新册",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve)
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "新册送进来时，墨还没干。我照着原来的行距抄了一遍，"
                            + "沈砚和盐引巷都在。只是陆崇的名字被人刮掉了半笔。"
                        ),
                        Line(
                            "抄书吏 严槐",
                            "我没补那一笔。少半笔的人，让后来的人自己猜吧。"
                        )
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-seal",
                    "E  问严槐封街后册页如何处理",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "封条送来时，我的笔还没洗。他们让我把旧街写进废册，"
                            + "可废册也要留底。纸不会因为被锁起来，就忘记写过什么。"
                        )
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-final",
                    "E  问严槐两本册子会留下什么",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision)
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "公开会让很多人看见，也会让很多人来抢着改。"
                            + "私藏能保住眼前的人，可下一次重抄时，谁替你记得？"
                        ),
                        Line(
                            "抄书吏 严槐",
                            "我只管纸。哪一种后果更像一条能住人的街，得由你决定。"
                        )
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-investigate",
                    "E  问严槐名字被抹去后会留下什么",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        LacksFactCondition("archive-clerk-ink-record")
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "被刮掉的名字不会完全消失。墨掉了，纸面会起一层新浆，"
                            + "摸上去像一道没有愈合的疤。"
                        ),
                        Line(
                            "抄书吏 严槐",
                            "账牌、封条和旧街招牌都是这样。你若去找搬运工，"
                            + "别只问他看见什么，也问问他每天仍重复做什么。"
                        )
                    },
                    choices: null,
                    completionEffects: new[]
                    {
                        SetFactEffect("archive-clerk-ink-record")
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-father-recruited",
                    "E  问严复的拓页是否还在",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("archive-clerk-recruited")
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "我把证据抄了三份，父亲的拓页夹在最后一份里。"
                            + "若官署看见边栏上的严复，至少会知道有人记得这十年。"
                        )
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-father-sheltered",
                    "E  问严复的拓页是否还在",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("archive-clerk-sheltered")
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "拓页锁在抽屉里。我每天照旧磨墨，也会给父亲留一格空白。"
                            + "不写出来，不代表我忘了。"
                        )
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-father-choice",
                    "E  问严槐为何总在册边留白",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("archive-clerk-ink-record"),
                        LacksFactCondition("archive-clerk-father-name")
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "我父亲严复也守过盐仓。十年前他的名字从册上消失，"
                            + "只在装订线里留下一张残页。我一直把它压在墨匣底下。"
                        ),
                        Line(
                            "主角",
                            "你一直在等一个能把残页夹进正式记录的机会。"
                        ),
                        Line(
                            "抄书吏 严槐",
                            "是。可抄录的人总会先被查到。"
                            + "你可以让我冒险，也可以让我继续留在册边留白。"
                        )
                    },
                    new[]
                    {
                        Choice(
                            "让严槐抄录证据，在边栏写下父亲严复的名字。",
                            SetFactEffect("archive-clerk-recruited")
                        ),
                        Choice(
                            "不让严槐冒险，先保住父亲留下的拓页。",
                            SetFactEffect("archive-clerk-sheltered")
                        )
                    },
                    new[]
                    {
                        SetFactEffect("archive-clerk-father-name")
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "archive-clerk-arrival",
                    "E  问抄书吏官署在抄什么",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.Investigate)
                    },
                    new[]
                    {
                        Line(
                            "抄书吏 严槐",
                            "别在官署门口念沈砚的名字。今早我抄完三页册子，"
                            + "回头再看，装订线里多了一层新浆。"
                        ),
                        Line(
                            "抄书吏 严槐",
                            "纸上原本写过字。只是有人要它看起来，从来都是空白。"
                        )
                    },
                    npcId: "archive-clerk"
                ),
                Script(
                    "caiyi-snack-preserve-daughter",
                    "E  问摊主为何多留一碗热汤",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        HasFactCondition("warehouse-daughter-remembered")
                    },
                    new[]
                    {
                        Line(
                            "小吃摊主",
                            "盐引巷回来后，搬运工每天来买两碗热汤。"
                            + "以前他只买一碗，另一只碗总装进随身的炊饼篮。"
                        ),
                        Line(
                            "小吃摊主",
                            "今早有个小姑娘坐在摊边，把汤喝完了。"
                            + "她说自己叫阿禾，我记住了。"
                        )
                    },
                    npcId: "caiyi-snack-vendor"
                ),
                Script(
                    "caiyi-snack-preserve",
                    "E  问摊主旧街客人是否回来",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve)
                    },
                    new[]
                    {
                        Line(
                            "小吃摊主",
                            "盐引巷的客人今早又来了，热汤多卖了好几碗。"
                            + "他们说话时，已经不必先看看街口有没有官差。"
                        )
                    },
                    npcId: "caiyi-snack-vendor"
                ),
                Script(
                    "caiyi-snack-seal",
                    "E  问摊主盐引巷的客人去了哪里",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "小吃摊主",
                            "盐引巷的客人又不见了。"
                            + "有个搬运工每天还买一碗汤，端着走到街口，再原样端回去。"
                        )
                    },
                    npcId: "caiyi-snack-vendor"
                ),
                Script(
                    "caiyi-snack-default",
                    "E  与小吃摊主交谈",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.Resolved)
                    },
                    new[]
                    {
                        Line(
                            "小吃摊主",
                            "以前盐船靠岸，搬运工会来我这儿买热汤。"
                            + "最近少了盐引巷的客人，生意都冷清了。"
                        )
                    },
                    npcId: "caiyi-snack-vendor"
                ),
                Script(
                    "caiyi-tailor-preserve",
                    "E  问裁衣铺旧街生意是否恢复",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve)
                    },
                    new[]
                    {
                        Line(
                            "裁衣铺掌柜",
                            "今早有人拿着盐引巷的门牌来配窗帘，"
                            + "说那条街终于又能在册上找到。"
                        ),
                        Line(
                            "裁衣铺掌柜",
                            "我量了尺寸。一个地方能不能住人，"
                            + "看有没有人肯为窗户挑布就知道了。"
                        )
                    },
                    npcId: "caiyi-tailor"
                ),
                Script(
                    "caiyi-tailor-seal",
                    "E  问裁衣铺旧街客人去了哪里",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "裁衣铺掌柜",
                            "前些日子还有媳妇来挑青布，今天再问，她们连盐引巷在哪都说不清。"
                            + "街没少一块砖，倒像少了一截尺寸。"
                        )
                    },
                    npcId: "caiyi-tailor"
                ),
                Script(
                    "caiyi-tailor-default",
                    "E  与裁衣铺掌柜交谈",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.Resolved)
                    },
                    new[]
                    {
                        Line(
                            "裁衣铺掌柜",
                            "盐引巷从前就在旧仓后面。那里的媳妇常来我这儿挑青布，"
                            + "说盐灰落上去也不显脏。"
                        ),
                        Line(
                            "裁衣铺掌柜",
                            "街和衣裳一样。少了一段，旁人未必看得出来，"
                            + "住在这里的人却知道。"
                        )
                    },
                    npcId: "caiyi-tailor"
                ),
                Script(
                    "cloth-merchant-preserve",
                    "E  问绸缎商那笔青布买卖",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve)
                    },
                    new[]
                    {
                        Line(
                            "绸缎商",
                            "沈砚那笔青布买卖今早重新出现在账上了。"
                            + "我不在乎一尺布的银子，只在乎收钱的人没被当成没来过。"
                        )
                    },
                    npcId: "cloth-merchant"
                ),
                Script(
                    "cloth-merchant-seal",
                    "E  问绸缎商那笔青布买卖",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "绸缎商",
                            "那尺青布还压在柜台底下，账上仍没有那笔买卖。"
                            + "有人拿走了布，有人拿走了名字，官署说两者都没发生。"
                        )
                    },
                    npcId: "cloth-merchant"
                ),
                Script(
                    "cloth-merchant-default",
                    "E  与绸缎商交谈",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.Resolved)
                    },
                    new[]
                    {
                        Line(
                            "绸缎商",
                            "沈砚前日来扯过一尺青布，说要给父亲缝个新账袋。"
                            + "钱是我亲手收的，账上却找不到这笔买卖。"
                        ),
                        Line(
                            "绸缎商",
                            "若连买卖都能被改，东关街还算得上是谁的街？"
                            + "去旧仓看看吧，他父亲一直守在那里。"
                        )
                    },
                    npcId: "cloth-merchant"
                ),
                Script(
                    "warehouse-worker-preserve-public",
                    "E  问阿禾是否还认得住这条街",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        HasFactCondition("warehouse-daughter-public")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "盐引巷回来后，有人拿着旧户册来认门。"
                            + "阿禾的名字排在最后一页，字很丑，可没人再问那是谁。"
                        ),
                        Line(
                            "盐仓搬运工",
                            "她还是不敢去官署。不过今天收工时，她先喊了我一声。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-preserve-private",
                    "E  问阿禾是否还认得住这条街",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve),
                        HasFactCondition("warehouse-daughter-private")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "新街没有阿禾的户册，我还是每天多带一份饭。"
                            + "现在我不用对空凳解释，因为你和阿禾都听见了。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-seal-public",
                    "E  问封街后阿禾去了哪里",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal),
                        HasFactCondition("warehouse-daughter-public")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "街又封了。官署收走公开名册时，我把阿禾那一页"
                            + "藏进了炊饼篮。至少这一次，不是他们决定她存不存在。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-seal-private",
                    "E  问封街后阿禾去了哪里",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal),
                        HasFactCondition("warehouse-daughter-private")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "街没了，阿禾的名字也没写出去。可她昨晚坐到我对面。"
                            + "我没告诉别人，怕一开口，她又被当成一条错记录。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-final-public",
                    "E  问阿禾的名字会落到谁手里",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision),
                        HasFactCondition("warehouse-daughter-public")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "你让官署看见了阿禾。陆崇若还掌着册，就能再抹她一次。"
                            + "最后一个选择，别只保住一条街，也别只保住一份名单。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-final-private",
                    "E  问阿禾的名字会落到谁手里",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision),
                        HasFactCondition("warehouse-daughter-private")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "你替阿禾藏了一次名字，也替我背了一次风险。"
                            + "最后一个选择，别让更多人像我一样只能靠肩膀记住。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-daughter-public-repeat",
                    "E  问阿禾的近况",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("warehouse-daughter-public")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "我把阿禾写在点名单背面，工头看见也没敢擦。"
                            + "晚上她坐过的板凳，还是只我一个人收拾。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-daughter-private-repeat",
                    "E  问阿禾的近况",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("warehouse-daughter-private")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "只有你的私人案卷写着阿禾。没人找我们麻烦，"
                            + "可我每天收工，还是会先喊她一声。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-daughter-choice",
                    "E  问搬运工为何多带一份干粮",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("warehouse-night-shift"),
                        LacksFactCondition("warehouse-second-meal")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "你注意到我每顿都多拿一份炊饼吗？"
                            + "阿禾吃不了这么多，可我还是每天都带。"
                        ),
                        Line("主角", "阿禾是谁？"),
                        Line(
                            "盐仓搬运工",
                            "我女儿。工册和户册里没有她。"
                            + "街坊说我对着空凳子说话，可每天收工，饭还是会少一份。"
                        ),
                        Line(
                            "途印向导",
                            "被抹掉的是她在城里的位置，不是你对她的记忆。"
                            + "名字若没人敢写，就会一直空着。"
                        ),
                        Line(
                            "盐仓搬运工",
                            "别急着替我作证。先告诉我，阿禾的名字该给官署看，"
                            + "还是只留给还认得她的人。"
                        )
                    },
                    new[]
                    {
                        Choice(
                            "把阿禾写进公开记录，让官署不能再装作没有她。",
                            SetFactEffect("warehouse-daughter-public")
                        ),
                        Choice(
                            "先把阿禾写进私人案卷，保护她和你的证人。",
                            SetFactEffect("warehouse-daughter-private")
                        )
                    },
                    new[]
                    {
                        SetFactEffect("warehouse-second-meal"),
                        SetFactEffect("warehouse-daughter-remembered")
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-preserve",
                    "E  问搬运工盐引巷的夜班",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Preserve)
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "盐引巷回来了，工头今早第一次没敢让我改口。"
                            + "七车盐、四十二袋，我都还能一件件数出来。"
                        ),
                        Line(
                            "盐仓搬运工",
                            "你若要查陆崇，就来问我。我的肩膀比点名单记得清楚。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-seal",
                    "E  问搬运工盐引巷的夜班",
                    new[]
                    {
                        StageCondition(ChapterStage.Resolved),
                        ConclusionCondition(ConclusionChoice.Seal)
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "工头又说昨夜没人搬盐。我明明还记得肩膀磨破的地方，"
                            + "可没有人肯听一个苦力说什么。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-final",
                    "E  问搬运工是否愿意作证",
                    new[]
                    {
                        StageCondition(ChapterStage.FinalDecision)
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "我可以作证，但你得先决定让谁听见。"
                            + "官署会问我姓名，商帮会问我下一顿饭在哪里。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-repeat",
                    "E  复核盐仓夜班",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.ReviewEvidence),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        HasFactCondition("warehouse-night-shift")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "七辆车，四十二袋。沈砚亲手在最后一袋上画了记号，"
                            + "说若少一袋，就把账翻过来看。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-testimony",
                    "E  问搬运工初七夜里的盐车",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.FinalDecision),
                        LacksFactCondition("warehouse-night-shift")
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "沈砚昨夜问我盐仓少了几车。我说七车，"
                            + "工头当场让我改口，说只进过六车。"
                        ),
                        Line(
                            "盐仓搬运工",
                            "一辆车七袋，七辆就是四十二袋。"
                            + "今早的点名单上没有夜班，也没有沈砚，可我的肩膀记得。"
                        )
                    },
                    completionEffects: new[]
                    {
                        SetFactEffect("warehouse-night-shift")
                    },
                    npcId: "warehouse-worker"
                ),
                Script(
                    "warehouse-worker-arrival",
                    "E  问搬运工沈砚去了哪里",
                    new[]
                    {
                        StageBeforeCondition(ChapterStage.Investigate)
                    },
                    new[]
                    {
                        Line(
                            "盐仓搬运工",
                            "守仓人若是问你沈砚，你先听他说完。"
                            + "我们这些搬货的，名字最容易从纸上少一笔。"
                        )
                    },
                    npcId: "warehouse-worker"
                ),
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
                            "途印向导",
                            "官署不是没找到沈砚。他们是在把“沈砚”两个字"
                            + "从每一页上擦掉。名字一没，人就像从未站在这里。"
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
                            "途印向导",
                            "这三张纸能证明有人改过册。可只要送出去，"
                            + "写纸的人也会立刻知道是谁先发现了。"
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
                    "shenbo-cross-examine-teahouse",
                    "E  把茶肆旁证告诉沈伯",
                    new[]
                    {
                        StageCondition(ChapterStage.CrossExamineTestimony),
                        HasFactCondition("teahouse-cart-testimony")
                    },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "茶肆掌柜也看见了？初五封仓时门外没有车，"
                            + "初七后半夜那七辆车才从后巷过去……"
                        ),
                        Line(
                            "主角",
                            "两处证词对上了。封条先写好，盐车后入城。"
                            + "现在只需要让沈伯的说法和账牌并排出现。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "按 T。别让那两个人只是各自说了一段话，"
                            + "要让他们在时间上互相撞破。"
                        )
                    }
                ),
                Script(
                    "shenbo-cross-examine-protected",
                    "E  请沈伯重述封条贴上的时间",
                    new[]
                    {
                        StageCondition(ChapterStage.CrossExamineTestimony),
                        HasFactCondition("teahouse-keeper-protected")
                    },
                    new[]
                    {
                        Line(
                            "守仓人 沈伯",
                            "茶肆那边你没继续追问？做得对。"
                            + "陆崇的人已经在街口打听谁还记得沈砚。"
                        ),
                        Line(
                            "主角",
                            "掌柜知道的已经够多了。现在把你说的封条日期"
                            + "和账牌上的入城日期放在一起。"
                        ),
                        Line(
                            "守仓人 沈伯",
                            "初五傍晚贴的封条，初七后半夜才到的盐车。"
                            + "按 T，让他们并排撞一次。"
                        )
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
                            "途印向导",
                            "初五写除名，初七才运盐。顺序反过来就说明，"
                            + "陆崇不是追着案子走，他是在案子发生前先清掉人。"
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
                    "teahouse-after-cart-testimony",
                    "E  与掌柜核对盐车旁证",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.Resolved),
                        HasFactCondition("teahouse-cart-testimony")
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "我把能记住的都说了：初五封仓时没有车，"
                            + "初七后半夜七辆车从后巷过去。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "沈砚坐在那张桌旁数账页，数到第七张时手一直在抖。"
                            + "要是这些能给沈伯一个交代，你就去说。"
                        )
                    },
                    npcId: "teahouse"
                ),
                Script(
                    "teahouse-after-protected",
                    "E  与掌柜交换一个眼神",
                    new[]
                    {
                        StageAtLeastCondition(ChapterStage.Investigate),
                        StageBeforeCondition(ChapterStage.Resolved),
                        HasFactCondition("teahouse-keeper-protected")
                    },
                    new[]
                    {
                        Line(
                            "茶肆掌柜",
                            "你没继续问，是对的。"
                            + "今天已经有两拨人来问过盐车，我不想让伙计记住他们的脸。"
                        ),
                        Line(
                            "茶肆掌柜",
                            "该说的话我记在心里。等陆崇的人不再盯着这里，"
                            + "你再来听。"
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
                        HasEvidenceCondition("official-seal"),
                        LacksFactCondition("teahouse-cart-testimony"),
                        LacksFactCondition("teahouse-keeper-protected")
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
                    new[]
                    {
                        Choice(
                            "请掌柜细说那七辆车经过的经过。",
                            SetFactEffect("teahouse-cart-testimony"),
                            SetFactEffect("teahouse-event-completed")
                        ),
                        Choice(
                            "到此为止，别让茶肆卷进陆崇的视线。",
                            SetFactEffect("teahouse-keeper-protected"),
                            SetFactEffect("teahouse-event-completed")
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
                    "途印向导",
                    "旧时层会把路改得像另一条街。别跟着新出现的门走，"
                    + "认门牌背面的铜钉。那才是这座城真正的次序。"
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
                    "途印向导",
                    "你想找回家的路，沈伯只想再叫一声儿子的名字。"
                    + "这一次，纸外的人和纸上的人，只能先保住一边。"
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
                    Line("守仓人 沈伯", response),
                    Line(
                        "途印向导",
                        conclusion == ConclusionChoice.Preserve
                            ? "地图上会多出一条街。以后有人再想把它抹掉，"
                                + "至少得先经过一群记得这个名字的人。"
                            : "官署的册页今天没有多出一条街。"
                                + "但走过这里的人会记得，我们曾经看见过它。"
                    )
                }
            );
        }

        private static DialogueLine Line(string speaker, string text)
        {
            return new DialogueLine(speaker, text);
        }

        private static DialogueChoiceDefinition Choice(
            string text,
            params DialogueEffectDefinition[] effects)
        {
            return new DialogueChoiceDefinition
            {
                Text = text,
                Effects = new List<DialogueEffectDefinition>(effects)
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

        private static DialogueEffectDefinition SetFactEffect(string factId)
        {
            return new DialogueEffectDefinition
            {
                Type = DialogueEffectType.SetFact,
                StoryFactId = factId
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

        private static DialogueConditionDefinition HasFactCondition(string factId)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.HasFact,
                StoryFactId = factId
            };
        }

        private static DialogueConditionDefinition LacksFactCondition(string factId)
        {
            return new DialogueConditionDefinition
            {
                Type = DialogueConditionType.HasFact,
                StoryFactId = factId,
                Negate = true
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
