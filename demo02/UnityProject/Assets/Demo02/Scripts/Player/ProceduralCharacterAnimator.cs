using UnityEngine;

namespace Demo02.Player
{
    [DefaultExecutionOrder(20)]
    public sealed class ProceduralCharacterAnimator : MonoBehaviour
    {
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform body;
        private Transform head;

        private Quaternion leftArmBase;
        private Quaternion rightArmBase;
        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Quaternion bodyBase;
        private Vector3 bodyPositionBase;
        private Vector3 headPositionBase;
        private Vector3 lastPosition;
        private float phase;
        private float movementBlend;
        private float attackRemaining;
        private bool isNpc;

        public void Configure(bool npc)
        {
            isNpc = npc;
            CacheParts();
        }

        private void Awake()
        {
            CacheParts();
        }

        private void Start()
        {
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || body == null)
            {
                return;
            }

            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = transform.position;
            if (delta.sqrMagnitude > 9f)
            {
                movementBlend = 0f;
                return;
            }

            float speed = delta.magnitude / Time.deltaTime;
            float targetBlend = Mathf.Clamp01(speed / 3.2f);
            movementBlend = Mathf.MoveTowards(
                movementBlend,
                targetBlend,
                Time.deltaTime * 5f
            );

            if (speed > 0.15f)
            {
                phase += Time.deltaTime * Mathf.Lerp(7f, 12f, Mathf.Clamp01(speed / 7f));
            }
            else
            {
                phase += Time.deltaTime * 1.6f;
            }

            float stride = Mathf.Sin(phase) * 24f * movementBlend;
            float armSwing = Mathf.Sin(phase) * 18f * movementBlend;
            ApplySwing(leftArm, leftArmBase, armSwing);
            ApplySwing(rightArm, rightArmBase, -armSwing);
            ApplySwing(leftLeg, leftLegBase, -stride);
            ApplySwing(rightLeg, rightLegBase, stride);

            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.035f * movementBlend;
            float idleBreath = Mathf.Sin(Time.unscaledTime * 1.55f) * (isNpc ? 0.01f : 0.016f);
            body.localPosition = bodyPositionBase + Vector3.up * (bob + idleBreath);
            head.localPosition = headPositionBase + Vector3.up * (bob * 0.55f + idleBreath * 0.7f);

            if (attackRemaining > 0f)
            {
                attackRemaining = Mathf.Max(0f, attackRemaining - Time.deltaTime);
                float progress = 1f - attackRemaining / 0.24f;
                float attackSwing = Mathf.Sin(progress * Mathf.PI) * 72f;
                ApplySwing(rightArm, rightArmBase, -attackSwing);
                body.localRotation = bodyBase * Quaternion.Euler(0f, attackSwing * 0.18f, 0f);
            }
            else
            {
                body.localRotation = bodyBase;
            }
        }

        public void PlayAttack()
        {
            attackRemaining = 0.24f;
        }

        private void CacheParts()
        {
            if (body != null)
            {
                return;
            }

            leftArm = transform.Find("LeftArm");
            rightArm = transform.Find("RightArm");
            leftLeg = transform.Find("LeftLeg");
            rightLeg = transform.Find("RightLeg");
            body = transform.Find("Body");
            head = transform.Find("Head");

            leftArmBase = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            rightArmBase = rightArm != null ? rightArm.localRotation : Quaternion.identity;
            leftLegBase = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            rightLegBase = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            bodyBase = body != null ? body.localRotation : Quaternion.identity;
            bodyPositionBase = body != null ? body.localPosition : Vector3.zero;
            headPositionBase = head != null ? head.localPosition : Vector3.zero;
        }

        private static void ApplySwing(Transform part, Quaternion baseRotation, float angle)
        {
            if (part != null)
            {
                part.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
            }
        }

        private void OnDisable()
        {
            RestorePose();
        }

        private void RestorePose()
        {
            if (leftArm != null) leftArm.localRotation = leftArmBase;
            if (rightArm != null) rightArm.localRotation = rightArmBase;
            if (leftLeg != null) leftLeg.localRotation = leftLegBase;
            if (rightLeg != null) rightLeg.localRotation = rightLegBase;
            if (body != null)
            {
                body.localRotation = bodyBase;
                body.localPosition = bodyPositionBase;
            }
            if (head != null) head.localPosition = headPositionBase;
        }
    }
}
