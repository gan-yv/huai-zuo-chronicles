using Demo02.Player;
using UnityEngine;

namespace Demo02.Combat
{
    public sealed class EliteArenaBoundary : MonoBehaviour
    {
        [SerializeField] private EliteEnemyController elite;
        [SerializeField, Min(1f)] private float radius = 10.5f;

        private bool engaged;

        public bool IsEngaged => engaged;

        public void Configure(EliteEnemyController enemy, float arenaRadius)
        {
            elite = enemy;
            radius = Mathf.Max(1f, arenaRadius);
        }

        private void Update()
        {
            if (elite == null || !elite.IsActive || Time.timeScale <= 0f)
            {
                engaged = false;
                return;
            }

            PlayerPartyController party =
                FindAnyObjectByType<PlayerPartyController>();
            Transform player = party != null ? party.ActiveCharacter : null;
            if (player == null)
            {
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (!engaged)
            {
                engaged = offset.magnitude <= radius;
            }
            if (!engaged || offset.magnitude <= radius)
            {
                return;
            }

            Vector3 target = transform.position + offset.normalized * radius;
            CharacterController controller =
                player.GetComponent<CharacterController>();
            bool enabled = controller != null && controller.enabled;
            if (controller != null)
            {
                controller.enabled = false;
            }
            player.position = target;
            if (controller != null)
            {
                controller.enabled = enabled;
            }
        }
    }
}
