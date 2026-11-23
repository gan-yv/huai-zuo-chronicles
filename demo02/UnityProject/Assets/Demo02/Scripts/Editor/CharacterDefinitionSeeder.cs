using Demo02.Player;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public static class CharacterDefinitionSeeder
    {
        public const string RepairerPath =
            "Assets/Demo02/Data/Characters/CHR_Repairer.asset";
        public const string GuidePath =
            "Assets/Demo02/Data/Characters/CHR_PathGuide.asset";

        [MenuItem("Demo02/Content/Rebuild Character Definitions")]
        public static void BuildAll()
        {
            LoadOrConfigure(
                RepairerPath,
                "repairer",
                "修复者",
                "近战 · 时空修复",
                "游历者",
                ResonanceMark.Continuation,
                new Color(0.93f, 0.76f, 0.36f),
                120f,
                18f,
                14f,
                30f,
                0.65f,
                100f,
                "续·截时",
                "续·原缝回响",
                "向前斩开一段时缝，对近身敌人造成伤害并打断蓄力。",
                CharacterSkillStyle.RiftSlash,
                6f,
                38f,
                2.8f,
                72f,
                5.2f,
                25f
            );
            LoadOrConfigure(
                GuidePath,
                "path-guide",
                "途印向导",
                "游击 · 探索向导",
                "广陵街坊",
                ResonanceMark.Path,
                new Color(0.31f, 0.78f, 0.72f),
                145f,
                15f,
                11f,
                34f,
                0.55f,
                100f,
                "途·折行",
                "百巷归途",
                "借街巷残响快速位移，并获得更持久的探索体力。",
                CharacterSkillStyle.PathDash,
                5f,
                28f,
                2.3f,
                64f,
                4.8f,
                18f
            );
            AssetDatabase.SaveAssets();
        }

        public static CharacterDefinition LoadRepairer()
        {
            return LoadRequired(RepairerPath);
        }

        public static CharacterDefinition LoadPathGuide()
        {
            return LoadRequired(GuidePath);
        }

        public static void ValidateOrThrow()
        {
            foreach (string path in new[] { RepairerPath, GuidePath })
            {
                CharacterDefinition definition = LoadRequired(path);
                if (!definition.IsValid(out string error))
                {
                    throw new System.InvalidOperationException(
                        $"Character definition validation failed: {error}"
                    );
                }
            }

            Debug.Log(
                "Character definitions validated: repairer and path guide."
            );
        }

        private static CharacterDefinition LoadRequired(string path)
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (definition == null)
            {
                throw new System.InvalidOperationException(
                    $"Character definition is missing: {path}"
                );
            }

            return definition;
        }

        private static void LoadOrConfigure(
            string path,
            string id,
            string displayName,
            string role,
            string affiliation,
            ResonanceMark mark,
            Color accent,
            float stamina,
            float sprintCost,
            float glideCost,
            float recovery,
            float recoveryDelay,
            float energy,
            string skill,
            string burst,
            string skillDescription,
            CharacterSkillStyle skillStyle,
            float skillCooldown,
            float skillDamage,
            float skillRadius,
            float burstDamage,
            float burstRadius,
            float burstHealing)
        {
            CharacterDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<CharacterDefinition>();
                definition.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(definition, path);
            }

            if (definition.ContentVersion < 2
                || string.IsNullOrWhiteSpace(definition.CharacterId)
                || definition.CharacterId == "new-character")
            {
                definition.ConfigureForEditor(
                    id,
                    displayName,
                    role,
                    affiliation,
                    mark,
                    accent,
                    stamina,
                    sprintCost,
                    glideCost,
                    recovery,
                    recoveryDelay,
                    energy,
                    skill,
                    burst,
                    skillDescription,
                    skillStyle,
                    skillCooldown,
                    skillDamage,
                    skillRadius,
                    burstDamage,
                    burstRadius,
                    burstHealing
                );
                EditorUtility.SetDirty(definition);
            }
        }
    }
}
