using Demo02.Combat;
using Demo02.Player;
using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WindLiftZone : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float liftSpeed = 8f;
        private GlideChallengeController challenge;

        public void Configure(
            GlideChallengeController owner,
            float speed)
        {
            challenge = owner;
            liftSpeed = Mathf.Max(0f, speed);
        }

        private void OnTriggerStay(Collider other)
        {
            Combatant combatant = other.GetComponentInParent<Combatant>();
            if (combatant == null || combatant.Team != CombatTeam.Player)
            {
                return;
            }

            if (challenge != null && !challenge.CanUseLift)
            {
                return;
            }

            other.GetComponentInParent<ThirdPersonController>()
                ?.ApplyWindLift(liftSpeed);
            challenge?.NotifyLiftEntered();
        }
    }
}
