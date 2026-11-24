using Demo02.Combat;
using UnityEngine;
using Demo02.Core;

namespace Demo02.Player
{
    public sealed class WorkFishCompanion : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float followSpeed = 5.2f;
        [SerializeField, Min(0.1f)] private float sideOffset = 2.1f;
        [SerializeField, Min(0f)] private float backOffset = 0.45f;
        [SerializeField, Min(1f)] private float stopDistance = 1.2f;
        [SerializeField, Min(1f)] private float warpDistance = 12f;
        [SerializeField, Min(1f)] private float combatCheckRadius = 5.5f;
        [SerializeField, Min(0f)] private float combatBackOffset = 3.2f;
        [SerializeField, Min(0f)] private float combatSideOffset = 0.7f;

        private PlayerPartyController party;
        private Transform tail;
        private Quaternion tailBase;
        private Vector3 lastPosition;
        private bool enemiesNearby;
        private float nextCombatCheck;

        public Transform FollowTarget =>
            party != null ? party.ActiveCharacter : null;
        public bool IsInCombat => enemiesNearby;

        private void Start()
        {
            party = FindAnyObjectByType<PlayerPartyController>();
            tail = FindChild(transform, "tail");
            tailBase = tail != null
                ? tail.localRotation
                : Quaternion.identity;
            lastPosition = transform.position;
        }

        private void Update()
        {
            Transform target = FollowTarget;
            if (target == null || Time.deltaTime <= 0f)
            {
                return;
            }

            if (Time.time >= nextCombatCheck)
            {
                nextCombatCheck = Time.time + 0.2f;
                enemiesNearby = CombatRegistry.CountNearbyEnemies(
                    target.position,
                    combatCheckRadius
                ) > 0;
            }

            float lateralOffset = sideOffset
                + (enemiesNearby ? combatSideOffset : 0f);
            float trailingOffset = enemiesNearby
                ? Mathf.Max(backOffset, combatBackOffset)
                : backOffset;
            Vector3 desired = target.position
                + target.right * lateralOffset
                - target.forward * trailingOffset;
            float motionScale = GameSettings.ReduceMotion ? 0.28f : 1f;
            desired.y = target.position.y - 1f
                + Mathf.Sin(Time.time * 2.1f) * 0.025f * motionScale;

            float distance = Vector3.Distance(
                transform.position,
                desired
            );
            if (distance > warpDistance)
            {
                transform.position = desired;
            }
            else if (distance > stopDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    desired,
                    followSpeed * Time.deltaTime
                );
            }

            Vector3 movement = transform.position - lastPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    movement.normalized,
                    Vector3.up
                );
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-8f * Time.deltaTime)
                );
            }
            lastPosition = transform.position;

            if (tail != null)
            {
                float sway = Mathf.Sin(Time.unscaledTime * 2.6f)
                    * 11f
                    * motionScale;
                tail.localRotation = tailBase
                    * Quaternion.Euler(0f, sway, 0f);
            }
        }

        private static Transform FindChild(
            Transform root,
            string name)
        {
            foreach (Transform child in
                     root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
