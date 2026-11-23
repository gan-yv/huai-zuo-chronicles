using UnityEngine;

namespace Demo02.Player
{
    [DisallowMultipleComponent]
    public sealed class CharacterRuntime : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition definition;
        [SerializeField, Min(0f)] private float currentStamina;
        [SerializeField, Min(0f)] private float currentEnergy;

        private float staminaRecoveryBlockedUntil;
        private float staminaBonus;
        private float energyGainMultiplier = 1f;

        public CharacterDefinition Definition => definition;
        public float MaxStamina =>
            (definition != null ? definition.MaxStamina : 100f)
            + staminaBonus;
        public float CurrentStamina => currentStamina;
        public float StaminaRatio =>
            Mathf.Clamp01(currentStamina / Mathf.Max(1f, MaxStamina));
        public float MaxEnergy => definition != null ? definition.MaxEnergy : 100f;
        public float CurrentEnergy => currentEnergy;
        public float EnergyRatio =>
            Mathf.Clamp01(currentEnergy / Mathf.Max(1f, MaxEnergy));
        public bool CanUseStamina => currentStamina > 0.01f;
        public float EnergyGainMultiplier => energyGainMultiplier;

        private void Awake()
        {
            if (definition != null && currentStamina <= 0f)
            {
                currentStamina = MaxStamina;
            }
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, MaxEnergy);
        }

        public void Configure(CharacterDefinition characterDefinition)
        {
            definition = characterDefinition;
            currentStamina = MaxStamina;
            currentEnergy = 0f;
            staminaRecoveryBlockedUntil = 0f;
        }

        public bool DrainStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }
            if (currentStamina <= 0f)
            {
                return false;
            }

            currentStamina = Mathf.Max(0f, currentStamina - amount);
            staminaRecoveryBlockedUntil = Time.time
                + (definition != null ? definition.StaminaRecoveryDelay : 0.65f);
            return currentStamina > 0f;
        }

        public void RecoverStamina(float deltaTime)
        {
            if (deltaTime <= 0f
                || Time.time < staminaRecoveryBlockedUntil
                || currentStamina >= MaxStamina)
            {
                return;
            }

            float recoveryPerSecond = definition != null
                ? definition.StaminaRecoveryPerSecond
                : 30f;
            currentStamina = Mathf.Min(
                MaxStamina,
                currentStamina + recoveryPerSecond * deltaTime
            );
        }

        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Clamp(
                amount,
                0f,
                MaxStamina
            );
            staminaRecoveryBlockedUntil = 0f;
        }

        public void GainEnergy(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentEnergy = Mathf.Min(
                MaxEnergy,
                currentEnergy + amount * energyGainMultiplier
            );
        }

        public void SetContinuationBonuses(
            float maxStaminaBonus,
            float newEnergyGainMultiplier)
        {
            float previousMax = MaxStamina;
            bool wasFull = currentStamina >= previousMax - 0.01f;
            staminaBonus = Mathf.Max(0f, maxStaminaBonus);
            energyGainMultiplier = Mathf.Max(0.1f, newEnergyGainMultiplier);
            currentStamina = wasFull
                ? MaxStamina
                : Mathf.Min(currentStamina, MaxStamina);
        }

        public void RestoreEnergy(float amount)
        {
            currentEnergy = Mathf.Clamp(amount, 0f, MaxEnergy);
        }

        public bool TrySpendEnergy(float amount)
        {
            if (amount < 0f || currentEnergy < amount)
            {
                return false;
            }

            currentEnergy -= amount;
            return true;
        }
    }
}
