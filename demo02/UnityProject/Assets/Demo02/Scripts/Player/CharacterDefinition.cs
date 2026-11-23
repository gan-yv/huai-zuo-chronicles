using UnityEngine;

namespace Demo02.Player
{
    public enum ResonanceMark
    {
        Continuation = 0,
        Order = 1,
        Path = 2,
        War = 3,
        Mirror = 4,
        Growth = 5,
        Erosion = 6,
        Balance = 7
    }

    public enum CharacterSkillStyle
    {
        RiftSlash = 0,
        PathDash = 1
    }

    [CreateAssetMenu(
        fileName = "CHR_NewCharacter",
        menuName = "Demo02/Characters/Character Definition"
    )]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private int contentVersion = 1;
        [SerializeField] private string characterId = "new-character";
        [SerializeField] private string displayName = "新角色";
        [SerializeField] private string roleLabel = "近战";
        [SerializeField] private string affiliation = "未归属";
        [SerializeField] private ResonanceMark resonanceMark;
        [SerializeField] private Color accentColor = Color.white;

        [Header("Exploration")]
        [SerializeField, Min(1f)] private float maxStamina = 120f;
        [SerializeField, Min(0f)] private float sprintCostPerSecond = 18f;
        [SerializeField, Min(0f)] private float glideCostPerSecond = 14f;
        [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 30f;
        [SerializeField, Min(0f)] private float staminaRecoveryDelay = 0.65f;

        [Header("Combat Loadout")]
        [SerializeField, Min(1f)] private float maxEnergy = 100f;
        [SerializeField] private string skillName = "核心技能";
        [SerializeField] private string burstName = "终式";
        [SerializeField] private string skillDescription =
            "技能效果等待接入战斗数据。";
        [SerializeField] private CharacterSkillStyle skillStyle;
        [SerializeField, Min(0f)] private float skillCooldown = 6f;
        [SerializeField, Min(0f)] private float skillDamage = 35f;
        [SerializeField, Min(0.1f)] private float skillRadius = 2.6f;
        [SerializeField, Min(0f)] private float burstDamage = 70f;
        [SerializeField, Min(0.1f)] private float burstRadius = 5f;
        [SerializeField, Min(0f)] private float burstHealing = 25f;

        public int ContentVersion => contentVersion;
        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public string RoleLabel => roleLabel;
        public string Affiliation => affiliation;
        public ResonanceMark ResonanceMark => resonanceMark;
        public Color AccentColor => accentColor;
        public float MaxStamina => maxStamina;
        public float SprintCostPerSecond => sprintCostPerSecond;
        public float GlideCostPerSecond => glideCostPerSecond;
        public float StaminaRecoveryPerSecond => staminaRecoveryPerSecond;
        public float StaminaRecoveryDelay => staminaRecoveryDelay;
        public float MaxEnergy => maxEnergy;
        public string SkillName => skillName;
        public string BurstName => burstName;
        public string SkillDescription => skillDescription;
        public CharacterSkillStyle SkillStyle => skillStyle;
        public float SkillCooldown => skillCooldown;
        public float SkillDamage => skillDamage;
        public float SkillRadius => skillRadius;
        public float BurstDamage => burstDamage;
        public float BurstRadius => burstRadius;
        public float BurstHealing => burstHealing;

        public void ConfigureForEditor(
            string id,
            string newDisplayName,
            string newRoleLabel,
            string newAffiliation,
            ResonanceMark mark,
            Color accent,
            float stamina,
            float sprintCost,
            float glideCost,
            float recovery,
            float recoveryDelay,
            float energy,
            string newSkillName,
            string newBurstName,
            string newSkillDescription,
            CharacterSkillStyle newSkillStyle,
            float newSkillCooldown,
            float newSkillDamage,
            float newSkillRadius,
            float newBurstDamage,
            float newBurstRadius,
            float newBurstHealing)
        {
            contentVersion = 2;
            characterId = id;
            displayName = newDisplayName;
            roleLabel = newRoleLabel;
            affiliation = newAffiliation;
            resonanceMark = mark;
            accentColor = accent;
            maxStamina = Mathf.Max(1f, stamina);
            sprintCostPerSecond = Mathf.Max(0f, sprintCost);
            glideCostPerSecond = Mathf.Max(0f, glideCost);
            staminaRecoveryPerSecond = Mathf.Max(0f, recovery);
            staminaRecoveryDelay = Mathf.Max(0f, recoveryDelay);
            maxEnergy = Mathf.Max(1f, energy);
            skillName = newSkillName;
            burstName = newBurstName;
            skillDescription = newSkillDescription;
            skillStyle = newSkillStyle;
            skillCooldown = Mathf.Max(0f, newSkillCooldown);
            skillDamage = Mathf.Max(0f, newSkillDamage);
            skillRadius = Mathf.Max(0.1f, newSkillRadius);
            burstDamage = Mathf.Max(0f, newBurstDamage);
            burstRadius = Mathf.Max(0.1f, newBurstRadius);
            burstHealing = Mathf.Max(0f, newBurstHealing);
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                error = "characterId is empty";
                return false;
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"{name}: displayName is empty";
                return false;
            }
            if (maxStamina <= 0f
                || maxEnergy <= 0f
                || sprintCostPerSecond < 0f
                || glideCostPerSecond < 0f
                || skillCooldown < 0f
                || skillDamage <= 0f
                || skillRadius <= 0f
                || burstDamage <= 0f
                || burstRadius <= 0f)
            {
                error = $"{name}: stamina or energy values are invalid";
                return false;
            }

            error = null;
            return true;
        }
    }
}
