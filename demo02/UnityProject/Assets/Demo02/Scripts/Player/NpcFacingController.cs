using UnityEngine;

namespace Demo02.Player
{
    public sealed class NpcFacingController : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float noticeDistance = 5.5f;
        [SerializeField, Min(0f)] private float turnSpeed = 5f;

        private PlayerPartyController party;

        private void Start()
        {
            party = Demo02.Core.GameServices.Resolve<PlayerPartyController>();
        }

        private void Update()
        {
            Transform player = party != null ? party.ActiveCharacter : null;
            if (player == null)
            {
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.04f || offset.sqrMagnitude > noticeDistance * noticeDistance)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-turnSpeed * Time.unscaledDeltaTime)
            );
        }
    }
}
