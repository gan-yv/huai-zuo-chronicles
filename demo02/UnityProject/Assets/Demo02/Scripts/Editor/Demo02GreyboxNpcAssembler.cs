using Demo02.Combat;
using Demo02.Core;
using Demo02.Interaction;
using Demo02.Narrative;
using Demo02.Player;
using Demo02.WorldState;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    /// <summary>
    /// Owns character visuals, NPC placement and NPC-only components.
    /// </summary>
    internal static class Demo02GreyboxNpcAssembler
    {
        public static void BuildOfficialStreet(
            Transform root,
            out GameObject worker,
            out GameObject archiveClerk,
            out GameObject ahe,
            out GameObject luChong)
        {
            worker = CreateNpc(
                root,
                "NPC_WarehouseWorker",
                new Vector3(-4.7f, 1f, 1f),
                new Color(0.62f, 0.37f, 0.18f),
                new Color(0.85f, 0.58f, 0.24f),
                "warehouse-worker"
            );

            archiveClerk = CreateNpc(
                root,
                "NPC_ArchiveClerk",
                new Vector3(-3.8f, 1f, -9.4f),
                new Color(0.2f, 0.3f, 0.38f),
                new Color(0.72f, 0.63f, 0.42f),
                "archive-clerk"
            );

            ahe = CreateNpc(
                root,
                "NPC_AHe",
                new Vector3(-3.15f, 1f, 0.25f),
                new Color(0.44f, 0.58f, 0.52f),
                new Color(0.84f, 0.67f, 0.32f),
                "ahe"
            );
            ahe.transform.localScale = Vector3.one * 0.82f;
            GameObject aheController = new("ConditionalNPC_AHe");
            aheController.transform.SetParent(root);
            aheController
                .AddComponent<WorldNpcAvailability>()
                .Configure(
                    ahe,
                    ChapterStage.Resolved,
                    ConclusionChoice.Preserve,
                    new[] { "warehouse-daughter-remembered" }
                );
            ahe.SetActive(false);

            luChong = CreateNpc(
                root,
                "NPC_LuChong",
                new Vector3(3.6f, 1f, 22.5f),
                new Color(0.3f, 0.14f, 0.12f),
                new Color(0.82f, 0.63f, 0.24f),
                "lu-chong"
            );
            GameObject luChongController = new("ConditionalNPC_LuChong");
            luChongController.transform.SetParent(root);
            luChongController
                .AddComponent<WorldNpcAvailability>()
                .Configure(
                    luChong,
                    ChapterStage.FinalDecision,
                    ConclusionChoice.None
                );
            luChong.SetActive(false);
        }

        public static GameObject BuildWarehouse(Transform root)
        {
            GameObject keeper = CreateCharacter(
                root,
                "NPC_ShenBo",
                new Vector3(16.5f, 1f, 8f),
                new Color(0.48f, 0.38f, 0.22f),
                new Color(0.76f, 0.6f, 0.32f),
                addNpcCollider: true
            );
            keeper.AddComponent<ShenBoInteractable>();
            return keeper;
        }

        public static void BuildDongguan(
            Transform areaRoot,
            out GameObject storyteller,
            out GameObject clothMerchant,
            out GameObject teaMaster)
        {
            storyteller = CreateCharacter(
                areaRoot,
                "NPC_DongguanStoryteller",
                new Vector3(56f, 1f, -4f),
                new Color(0.2f, 0.4f, 0.58f),
                new Color(0.72f, 0.58f, 0.3f),
                addNpcCollider: true
            );

            clothMerchant = CreateNpc(
                areaRoot,
                "NPC_ClothMerchant",
                new Vector3(63.5f, 1f, 64f),
                new Color(0.48f, 0.22f, 0.32f),
                new Color(0.85f, 0.55f, 0.28f),
                "cloth-merchant"
            );

            teaMaster = CreateNpc(
                areaRoot,
                "NPC_TeaMaster",
                new Vector3(56.5f, 1f, 112f),
                new Color(0.24f, 0.46f, 0.27f),
                new Color(0.78f, 0.62f, 0.32f),
                "teahouse"
            );
        }

        public static void BuildCaiyi(
            Transform areaRoot,
            out GameObject tailor,
            out GameObject snackVendor)
        {
            tailor = CreateNpc(
                areaRoot,
                "NPC_CaiyiTailor",
                new Vector3(56f, 1f, 190f),
                new Color(0.55f, 0.24f, 0.26f),
                new Color(0.8f, 0.58f, 0.3f),
                "caiyi-tailor"
            );

            snackVendor = CreateNpc(
                areaRoot,
                "NPC_CaiyiSnackVendor",
                new Vector3(64f, 1f, 245f),
                new Color(0.66f, 0.36f, 0.13f),
                new Color(0.88f, 0.65f, 0.3f),
                "caiyi-snack-vendor"
            );
        }

        public static void CreateExplorationVisuals(GameObject character)
        {
            bool partner = character.name.Contains("Partner");
            CreateGliderVisual(
                character.transform,
                partner
                    ? new Color(0.28f, 0.55f, 0.5f)
                    : new Color(0.78f, 0.67f, 0.43f),
                partner
                    ? new Color(0.31f, 0.78f, 0.72f)
                    : new Color(0.93f, 0.76f, 0.36f)
            );
            CreateAbilityPulse(
                character.transform,
                partner
                    ? new Color(0.31f, 0.78f, 0.72f)
                    : new Color(0.93f, 0.76f, 0.36f)
            );
        }

        public static void CreateWorkFishCompanion(
            Transform player,
            Transform root)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AnimeFishPilotSetup.PrefabPath
                );
            if (prefab == null)
            {
                throw new System.InvalidOperationException(
                    "Work fish companion prefab is missing."
                );
            }

            GameObject companion =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            companion.name = "Companion_WorkFish";
            companion.transform.SetParent(root, false);
            companion.transform.position = player.position
                + player.right * 2.1f
                - player.forward * 0.45f
                + Vector3.down;
            companion.transform.rotation = player.rotation;

            CapsuleCollider interactionCollider =
                companion.AddComponent<CapsuleCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 0.9f, 0f);
            interactionCollider.height = 1.9f;
            interactionCollider.radius = 0.55f;

            WorkFishCompanion follower =
                companion.AddComponent<WorkFishCompanion>();
            companion
                .AddComponent<WorkFishCompanionInteractable>()
                .Configure(follower);
        }

        public static GameObject CreateCharacter(
            Transform parent,
            string name,
            Vector3 position,
            Color outfit,
            Color accent,
            bool addNpcCollider)
        {
            GameObject character = new(name);
            character.transform.SetParent(parent);
            character.transform.position = position;

            if (TryAttachBlockyCharacter(character, name))
            {
                if (addNpcCollider)
                {
                    ConfigureNpcCollider(character);
                }
                return character;
            }

            Color skin = new(0.82f, 0.66f, 0.52f);
            Color hair = new(0.13f, 0.09f, 0.075f);
            Color trousers = Color.Lerp(outfit, Color.black, 0.38f);

            Demo02SceneObjectFactory.CreateAvatarPart(
                character.transform,
                "Body",
                PrimitiveType.Capsule,
                new Vector3(0f, -0.15f, 0f),
                new Vector3(0.72f, 0.64f, 0.72f),
                outfit
            );
            Demo02SceneObjectFactory.CreateAvatarPart(
                character.transform,
                "Sash",
                PrimitiveType.Cube,
                new Vector3(0f, -0.08f, -0.37f),
                new Vector3(0.18f, 0.58f, 0.05f),
                accent
            );
            Demo02SceneObjectFactory.CreateAvatarPart(
                character.transform,
                "Belt",
                PrimitiveType.Cube,
                new Vector3(0f, -0.24f, 0f),
                new Vector3(0.74f, 0.1f, 0.74f),
                Color.Lerp(accent, Color.black, 0.28f)
            );
            Demo02SceneObjectFactory.CreateAvatarPart(
                character.transform,
                "Head",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.67f, 0f),
                new Vector3(0.58f, 0.62f, 0.58f),
                skin
            );
            Demo02SceneObjectFactory.CreateAvatarPart(
                character.transform,
                "Hair",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.88f, -0.03f),
                new Vector3(0.6f, 0.25f, 0.6f),
                hair
            );

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject arm = Demo02SceneObjectFactory.CreateAvatarPart(
                    character.transform,
                    side < 0 ? "LeftArm" : "RightArm",
                    PrimitiveType.Capsule,
                    new Vector3(0.43f * side, -0.03f, 0f),
                    new Vector3(0.17f, 0.39f, 0.17f),
                    outfit
                );
                arm.transform.localRotation =
                    Quaternion.Euler(0f, 0f, -13f * side);

                Demo02SceneObjectFactory.CreateAvatarPart(
                    character.transform,
                    side < 0 ? "LeftLeg" : "RightLeg",
                    PrimitiveType.Capsule,
                    new Vector3(0.2f * side, -0.63f, 0f),
                    new Vector3(0.2f, 0.38f, 0.2f),
                    trousers
                );
                Demo02SceneObjectFactory.CreateAvatarPart(
                    character.transform,
                    side < 0 ? "LeftShoe" : "RightShoe",
                    PrimitiveType.Cube,
                    new Vector3(0.2f * side, -0.89f, 0.08f),
                    new Vector3(0.3f, 0.13f, 0.45f),
                    new Color(0.1f, 0.08f, 0.065f)
                );
            }

            if (name.Contains("Player"))
            {
                Demo02SceneObjectFactory.CreateAvatarPart(
                    character.transform,
                    "RepairTool",
                    PrimitiveType.Cylinder,
                    new Vector3(0.47f, -0.22f, -0.08f),
                    new Vector3(0.13f, 0.34f, 0.13f),
                    new Color(0.52f, 0.4f, 0.18f)
                );
                Demo02SceneObjectFactory.CreateAvatarPart(
                    character.transform,
                    "RecordSatchel",
                    PrimitiveType.Cube,
                    new Vector3(-0.42f, -0.2f, -0.08f),
                    new Vector3(0.3f, 0.34f, 0.18f),
                    new Color(0.2f, 0.14f, 0.09f)
                );
            }
            else if (name.Contains("Partner"))
            {
                Demo02SceneObjectFactory.CreateAvatarPart(
                    character.transform,
                    "TravelBag",
                    PrimitiveType.Cube,
                    new Vector3(-0.43f, -0.2f, -0.08f),
                    new Vector3(0.28f, 0.3f, 0.16f),
                    new Color(0.4f, 0.25f, 0.12f)
                );
            }

            if (addNpcCollider)
            {
                ConfigureNpcCollider(character);
            }

            character
                .AddComponent<ProceduralCharacterAnimator>()
                .Configure(addNpcCollider);

            return character;
        }

        private static GameObject CreateNpc(
            Transform parent,
            string name,
            Vector3 position,
            Color outfit,
            Color accent,
            string dialogueId)
        {
            GameObject npc = CreateCharacter(
                parent,
                name,
                position,
                outfit,
                accent,
                addNpcCollider: true
            );
            npc.AddComponent<NpcDialogueInteractable>()
                .ConfigureData(dialogueId);
            return npc;
        }

        private static bool TryAttachBlockyCharacter(
            GameObject character,
            string name)
        {
            string prefabPath =
                StylizedCharacterSetup.ResolveCharacterPrefab(name);
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                prefabPath =
                    BlockyCharacterSetup.ResolveCharacterPrefab(name);
                prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return false;
                }
            }

            GameObject visual =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.name = "BlockyVisual";
            visual.transform.SetParent(character.transform, false);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            return true;
        }

        private static void ConfigureNpcCollider(GameObject character)
        {
            CapsuleCollider collider =
                character.AddComponent<CapsuleCollider>();
            collider.center = Vector3.zero;
            collider.height = 2f;
            collider.radius = 0.48f;
            character.AddComponent<NpcFacingController>();
        }

        private static void CreateGliderVisual(
            Transform character,
            Color canopyColor,
            Color frameColor)
        {
            GameObject root = new("ExplorationGlider");
            root.transform.SetParent(character, false);
            root.transform.localPosition = new Vector3(0f, 0.82f, -0.12f);

            GameObject leftWing =
                Demo02SceneObjectFactory.CreateAvatarPart(
                    root.transform,
                    "Canopy_Left",
                    PrimitiveType.Cube,
                    new Vector3(-0.61f, 0.08f, 0f),
                    new Vector3(1.16f, 0.055f, 0.72f),
                    canopyColor
                );
            leftWing.transform.localRotation =
                Quaternion.Euler(0f, 0f, -9f);
            GameObject rightWing =
                Demo02SceneObjectFactory.CreateAvatarPart(
                    root.transform,
                    "Canopy_Right",
                    PrimitiveType.Cube,
                    new Vector3(0.61f, 0.08f, 0f),
                    new Vector3(1.16f, 0.055f, 0.72f),
                    canopyColor
                );
            rightWing.transform.localRotation =
                Quaternion.Euler(0f, 0f, 9f);
            Demo02SceneObjectFactory.CreateAvatarPart(
                root.transform,
                "Canopy_Ridge",
                PrimitiveType.Cube,
                new Vector3(0f, 0.11f, 0f),
                new Vector3(0.08f, 0.09f, 0.76f),
                frameColor
            );
            Demo02SceneObjectFactory.CreateAvatarPart(
                root.transform,
                "Glider_Crossbar",
                PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(2.36f, 0.045f, 0.055f),
                frameColor
            );
            root.SetActive(false);
        }

        private static void CreateAbilityPulse(
            Transform character,
            Color color)
        {
            GameObject pulse =
                Demo02SceneObjectFactory.CreateAvatarPart(
                    character,
                    "AbilityPulse",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, -0.88f, 0f),
                    new Vector3(0.3f, 0.035f, 0.3f),
                    color
                );
            pulse.AddComponent<AbilityPulseVisual>();
            pulse.SetActive(false);
        }
    }
}
