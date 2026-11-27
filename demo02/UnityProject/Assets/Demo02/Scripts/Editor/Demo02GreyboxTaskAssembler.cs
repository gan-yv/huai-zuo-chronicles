using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Editor
{
    /// <summary>
    /// Builds clues, objectives, interactive story props and rewards.
    /// </summary>
    internal static class Demo02GreyboxTaskAssembler
    {
        public static void BuildOfficialStreet(
            Transform root,
            CaseDefinition chapterContent,
            out GameObject publicRecordVisual,
            out GameObject privateRecordVisual)
        {
            GameObject clue = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Clue_OldStreetSign",
                new Vector3(4.35f, 1.35f, -6f),
                new Vector3(1.6f, 1.5f, 0.22f),
                new Color(0.95f, 0.72f, 0.25f)
            );
            clue.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            EvidenceDefinition oldStreetEvidence = RequireEvidence(
                chapterContent,
                "old-street-sign"
            );
            clue.AddComponent<ClueInteractable>().Configure(
                oldStreetEvidence.Id,
                oldStreetEvidence.InteractionPrompt,
                oldStreetEvidence.DiscoveredText
            );

            GameObject saltLedger = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Clue_SaltLedger",
                new Vector3(-4.35f, 1.1f, 2f),
                new Vector3(1.2f, 0.9f, 0.22f),
                new Color(0.82f, 0.56f, 0.25f)
            );
            saltLedger.transform.rotation =
                Quaternion.Euler(0f, -90f, 0f);
            EvidenceDefinition ledgerEvidence = RequireEvidence(
                chapterContent,
                "salt-ledger"
            );
            saltLedger.AddComponent<ClueInteractable>().Configure(
                ledgerEvidence.Id,
                ledgerEvidence.InteractionPrompt,
                ledgerEvidence.DiscoveredText
            );

            GameObject officialSeal =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Clue_OfficialSeal",
                    new Vector3(4.35f, 1.3f, 18f),
                    new Vector3(1.15f, 1.45f, 0.22f),
                    new Color(0.82f, 0.28f, 0.2f)
                );
            officialSeal.transform.rotation =
                Quaternion.Euler(0f, 90f, 0f);
            EvidenceDefinition sealEvidence = RequireEvidence(
                chapterContent,
                "official-seal"
            );
            officialSeal.AddComponent<ClueInteractable>().Configure(
                sealEvidence.Id,
                sealEvidence.InteractionPrompt,
                sealEvidence.DiscoveredText
            );

            CreateInventoryPickup(
                root,
                "Pickup_SaltToken",
                new Vector3(-1.8f, 0.55f, -4.8f),
                new Vector3(0.35f, 0.12f, 0.35f),
                new Color(0.92f, 0.68f, 0.2f),
                "pickup-salt-token",
                "salt-token-charm",
                1
            );
            CreateInventoryPickup(
                root,
                "Pickup_BlankLedgerPage",
                new Vector3(-4.2f, 0.6f, 4.5f),
                new Vector3(0.7f, 0.08f, 0.9f),
                new Color(0.82f, 0.78f, 0.62f),
                "pickup-blank-ledger-page",
                "blank-ledger-page",
                2
            );

            CreateMemoryEcho(
                root,
                "Echo_SaltToken",
                new Vector3(3.8f, 0.55f, -2.5f),
                "salt-token",
                "盐引铜钱",
                "铜钱边缘刻着沈砚的名字。茶肆掌柜说，这是沈砚失踪前留在桌上的最后一件事物。"
            );
            CreateMemoryEcho(
                root,
                "Echo_FireAsh",
                new Vector3(13.8f, 0.55f, 12f),
                "warehouse-ash",
                "烧焦的账角",
                "灰烬里残留着二十年前的盐仓火印。那场火没有被记录，却一直留在旧仓的墙里。"
            );
            CreateMemoryEcho(
                root,
                "Echo_SevenPages",
                new Vector3(0f, 0.62f, 25f),
                "seven-pages",
                "被撕走的七页",
                "七道纸影从街尾一直延伸到时缝。沈砚没有逃跑，他把账页藏进了每个人都能看见、却没人肯相信的地方。"
            );

            GameObject repairSite = Demo02SceneObjectFactory.CreateBlock(
                root,
                "RepairSite_TimeRift",
                new Vector3(0f, 1.5f, 29f),
                new Vector3(2f, 3f, 0.6f),
                new Color(0.58f, 0.22f, 0.82f)
            );
            repairSite.AddComponent<RepairSite>();
            repairSite
                .AddComponent<WorldObjectId>()
                .Configure("RepairSite_TimeRift");

            GameObject preserveOutcome =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Outcome_OldStreetPreserved",
                    new Vector3(0f, 0.15f, 30.5f),
                    new Vector3(9f, 0.3f, 1.4f),
                    new Color(0.28f, 0.7f, 0.42f)
                );
            preserveOutcome.SetActive(false);

            GameObject sealOutcome = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Outcome_OldStreetSealed",
                new Vector3(0f, 2f, 31f),
                new Vector3(14f, 4f, 0.5f),
                new Color(0.6f, 0.18f, 0.18f)
            );
            sealOutcome.SetActive(false);

            GameObject preserveChoice =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Choice_Preserve",
                    new Vector3(-3f, 1.1f, 26f),
                    new Vector3(1.3f, 2.2f, 0.3f),
                    new Color(0.86f, 0.72f, 0.3f)
                );
            preserveChoice
                .AddComponent<EndingChoiceInteractable>()
                .Configure(
                    ConclusionChoice.Preserve,
                    preserveOutcome,
                    sealOutcome
                );
            preserveChoice
                .AddComponent<WorldObjectId>()
                .Configure("Choice_Preserve");

            GameObject sealChoice = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Choice_Seal",
                new Vector3(3f, 1.1f, 26f),
                new Vector3(1.3f, 2.2f, 0.3f),
                new Color(0.55f, 0.58f, 0.62f)
            );
            sealChoice
                .AddComponent<EndingChoiceInteractable>()
                .Configure(
                    ConclusionChoice.Seal,
                    preserveOutcome,
                    sealOutcome
                );
            sealChoice
                .AddComponent<WorldObjectId>()
                .Configure("Choice_Seal");

            publicRecordVisual =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Consequence_PublicRecord",
                    new Vector3(-4.4f, 1.25f, 7.5f),
                    new Vector3(1.8f, 2.15f, 0.22f),
                    new Color(0.88f, 0.68f, 0.28f)
                );
            publicRecordVisual
                .AddComponent<NpcDialogueInteractable>()
                .Configure(
                    "官署抄录告示",
                    "E  查看已经送交官署的记录",
                    new DialogueLine(
                        "官署抄录告示",
                        "盐引巷七车盐账已抄录三份。沈砚的名字重新出现在官署门外的告示上，来往行人都能看见。"
                    )
                );
            publicRecordVisual.SetActive(false);

            privateRecordVisual =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Consequence_ProtectedEvidence",
                    new Vector3(14.5f, 0.7f, 10.5f),
                    new Vector3(1.4f, 1.4f, 1.4f),
                    new Color(0.3f, 0.19f, 0.1f)
                );
            privateRecordVisual
                .AddComponent<NpcDialogueInteractable>()
                .Configure(
                    "封存证物箱",
                    "E  查看封存的账页副本",
                    new DialogueLine(
                        "封存证物箱",
                        "账页副本藏在旧仓最暗的角落。只有沈伯和你知道这里，但真相也因此暂时无法被官署销毁。"
                    )
                );
            privateRecordVisual.SetActive(false);
        }

        public static void BuildWarehouse(Transform root)
        {
            GameObject notice = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Courtyard_NoticeBoard",
                new Vector3(8.8f, 1.25f, 11f),
                new Vector3(1.8f, 2.2f, 0.25f),
                new Color(0.9f, 0.7f, 0.3f)
            );
            notice.transform.rotation =
                Quaternion.Euler(0f, -90f, 0f);
            notice.AddComponent<NpcDialogueInteractable>().Configure(
                "旧仓告示",
                "E  查看旧仓告示",
                new DialogueLine(
                    "旧仓告示",
                    "“仓中货物，只记其来处，不记其去处。”告示右下角还有一行被刮掉的小字。"
                )
            );

            GameObject ledger = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_MissingLedger",
                new Vector3(23f, 1.12f, 9f),
                new Vector3(1.3f, 0.16f, 0.8f),
                new Color(0.92f, 0.7f, 0.24f)
            );
            ledger.AddComponent<NpcDialogueInteractable>().Configure(
                "缺页账簿",
                "E  调查缺页账簿",
                new DialogueLine(
                    "缺页账簿",
                    "账簿中间少了整整七页。残留的装订线上，还能看出被人匆忙撕走的痕迹。"
                ),
                new DialogueLine(
                    "主角",
                    "这些页不是自然损坏。有人先改写了这里，再让整条旧街从记录里消失。"
                )
            );
        }

        public static void BuildDongguanStory(
            Transform areaRoot,
            GameObject storyteller,
            GlideChallengeController glideChallenge)
        {
            GameObject paperKiteRoot =
                new("Story_DongguanPaperKite");
            paperKiteRoot.transform.SetParent(areaRoot);
            paperKiteRoot.transform.position =
                new Vector3(60f, 0f, 28f);

            GameObject presentTrace = Demo02SceneObjectFactory.CreateBlock(
                paperKiteRoot.transform,
                "Trace_PaperKiteCord_Present",
                new Vector3(56.8f, 1.15f, 16f),
                new Vector3(0.22f, 1.6f, 0.22f),
                new Color(0.3f, 0.68f, 0.64f)
            );

            GameObject pastTrace = Demo02SceneObjectFactory.CreateBlock(
                paperKiteRoot.transform,
                "Trace_PaperKiteBell_Past",
                new Vector3(63.2f, 1.05f, 27f),
                new Vector3(0.28f, 1.35f, 0.28f),
                new Color(0.78f, 0.64f, 0.32f)
            );
            pastTrace
                .AddComponent<TimeLayerVisibility>()
                .Configure(TimeLayer.Past);

            GameObject windlineKnot =
                Demo02SceneObjectFactory.CreateBlock(
                    paperKiteRoot.transform,
                    "Anchor_OldWindlineKnot",
                    new Vector3(60f, 1.05f, 54f),
                    new Vector3(0.3f, 1.55f, 0.3f),
                    new Color(0.3f, 0.72f, 0.76f)
                );

            GameObject publicConsequence =
                new("Consequence_PaperKitePublicName");
            publicConsequence.transform.SetParent(
                paperKiteRoot.transform
            );
            Demo02SceneObjectFactory.CreateBlock(
                publicConsequence.transform,
                "PublicNoticePost",
                new Vector3(62.6f, 0.62f, 35f),
                new Vector3(0.14f, 1.24f, 0.14f),
                new Color(0.28f, 0.18f, 0.1f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                publicConsequence.transform,
                "PublicNoticeBoard",
                new Vector3(62.6f, 1.42f, 35f),
                new Vector3(0.92f, 0.58f, 0.1f),
                new Color(0.68f, 0.46f, 0.2f)
            );
            publicConsequence.SetActive(false);

            GameObject privateConsequence =
                new("Consequence_PaperKitePrivateMemory");
            privateConsequence.transform.SetParent(
                paperKiteRoot.transform
            );
            Demo02SceneObjectFactory.CreateBlock(
                privateConsequence.transform,
                "PrivateMemoryPost",
                new Vector3(57.4f, 0.72f, 35f),
                new Vector3(0.13f, 1.44f, 0.13f),
                new Color(0.25f, 0.2f, 0.14f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                privateConsequence.transform,
                "PrivateMemorySilk",
                new Vector3(57.4f, 1.18f, 35.18f),
                new Vector3(0.08f, 0.7f, 0.38f),
                new Color(0.28f, 0.62f, 0.72f)
            );
            privateConsequence.SetActive(false);

            PaperKiteStoryController paperKiteStory =
                paperKiteRoot.AddComponent<PaperKiteStoryController>();
            paperKiteStory.Configure(
                storyteller.transform,
                presentTrace.transform,
                pastTrace.transform,
                windlineKnot.transform,
                glideChallenge,
                publicConsequence,
                privateConsequence
            );

            storyteller
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.Storyteller
                );
            presentTrace
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.PresentTrace
                );
            pastTrace
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.PastTrace
                );
            windlineKnot
                .AddComponent<PaperKiteStoryInteractable>()
                .Configure(
                    paperKiteStory,
                    PaperKiteInteractionKind.WindlineKnot
                );
        }

        public static void BuildDongguanRewards(Transform areaRoot)
        {
            CreateInventoryPickup(
                areaRoot,
                "Pickup_GingerTea",
                new Vector3(58f, 0.62f, 111.5f),
                new Vector3(0.42f, 0.3f, 0.42f),
                new Color(0.55f, 0.26f, 0.12f),
                "pickup-ginger-tea",
                "ginger-tea",
                2
            );
        }

        public static void BuildStreetStory(
            Transform root,
            WorldEncounterZone zone,
            string encounterId)
        {
            string storyId;
            string npcName;
            string rewardItemId;
            Vector3 npcPosition;
            Color outfit;
            Color accent;
            DialogueLine[] intro;
            DialogueLine[] ready;
            DialogueLine[] completed;

            if (encounterId == "dongguan-patrol")
            {
                storyId = "dongguan-apprentice-ledger";
                npcName = "新来的脚夫";
                rewardItemId = "ginger-tea";
                npcPosition = new Vector3(57.5f, 1f, 40.5f);
                outfit = new Color(0.34f, 0.28f, 0.2f);
                accent = new Color(0.74f, 0.52f, 0.25f);
                intro = new[]
                {
                    new DialogueLine(
                        npcName,
                        "那队巡哨把一只旧账袋拖进盐仓，说里面只剩废纸。可我看见封签上盖的是官印。"
                    ),
                    new DialogueLine(
                        "主角",
                        "巡逻若真是查案，就不会把封签藏起来。"
                    )
                };
                ready = new[]
                {
                    new DialogueLine(
                        npcName,
                        "队伍散了。账袋还在供桌后，我没敢碰。"
                    ),
                    new DialogueLine(
                        npcName,
                        "这壶热姜汤你先拿着，潮气重，别在旧仓里病倒。"
                    )
                };
                completed = new[]
                {
                    new DialogueLine(
                        npcName,
                        "街口今天安静多了，我也该回去搬货了。"
                    )
                };
            }
            else if (encounterId == "caiyi-lookout")
            {
                storyId = "caiyi-apprentice-ledger-page";
                npcName = "裁衣学徒";
                rewardItemId = "blank-ledger-page";
                npcPosition = new Vector3(62.5f, 1f, 216f);
                outfit = new Color(0.38f, 0.22f, 0.28f);
                accent = new Color(0.78f, 0.44f, 0.28f);
                intro = new[]
                {
                    new DialogueLine(
                        npcName,
                        "每晚都有人替望风的人改衣，袖口藏着纸条。师父说别问，可我不信他们只是在巡夜。"
                    ),
                    new DialogueLine(
                        "主角",
                        "纸条既然能缝进衣服，就也能从这里带出去。"
                    )
                };
                ready = new[]
                {
                    new DialogueLine(
                        npcName,
                        "望风的人走了。我在裁衣案板下找到一张空白账页。"
                    ),
                    new DialogueLine(
                        npcName,
                        "你比我更需要它。若真能把街名写回来，记得别写错。"
                    )
                };
                completed = new[]
                {
                    new DialogueLine(
                        npcName,
                        "针脚已经拆干净了。希望下次来，这里只剩正常做买卖的人。"
                    )
                };
            }
            else
            {
                return;
            }

            GameObject npc =
                Demo02GreyboxNpcAssembler.CreateCharacter(
                    root,
                    $"NPC_Story_{encounterId}",
                    npcPosition,
                    outfit,
                    accent,
                    addNpcCollider: true
                );
            WorldContentEvent contentEvent =
                root.gameObject.AddComponent<WorldContentEvent>();
            contentEvent.Configure(
                storyId,
                npcName,
                zone,
                rewardItemId,
                1,
                "巡哨线索已交付并领取街区奖励。"
            );
            npc.AddComponent<WorldStoryNpcInteractable>().Configure(
                npcName,
                contentEvent,
                intro,
                ready,
                completed
            );
        }

        private static EvidenceDefinition RequireEvidence(
            CaseDefinition content,
            string evidenceId)
        {
            EvidenceDefinition evidence =
                content.FindEvidence(evidenceId);
            if (evidence == null)
            {
                throw new System.InvalidOperationException(
                    $"Case content is missing evidence '{evidenceId}'."
                );
            }
            return evidence;
        }

        private static void CreateInventoryPickup(
            Transform parent,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Color tint,
            string pickupId,
            string itemId,
            int amount)
        {
            GameObject pickup =
                Demo02SceneObjectFactory.CreateBlock(
                    parent,
                    objectName,
                    position,
                    scale,
                    tint
                );
            pickup.AddComponent<InventoryItemPickup>().Configure(
                pickupId,
                itemId,
                amount
            );
        }

        private static void CreateMemoryEcho(
            Transform parent,
            string name,
            Vector3 position,
            string echoId,
            string title,
            string text)
        {
            GameObject echo =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            echo.name = name;
            echo.transform.SetParent(parent);
            echo.transform.position = position;
            echo.transform.localScale =
                new Vector3(0.34f, 0.34f, 0.34f);
            echo.AddComponent<ColorTint>().Configure(
                new Color(0.98f, 0.76f, 0.3f),
                0.55f
            );

            SphereCollider trigger = echo.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.35f;
            echo
                .AddComponent<MemoryEchoInteractable>()
                .Configure(echoId, title, text);
        }
    }
}
