using Demo02.Combat;
using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WorldArea : MonoBehaviour
    {
        [SerializeField] private string areaId = "area";
        [SerializeField] private string displayName = "未命名区域";

        private WorldRuntime runtime;

        private void Awake()
        {
            runtime = WorldRuntime.Instance;
        }

        public void Configure(string id, string areaDisplayName)
        {
            areaId = id;
            displayName = areaDisplayName;
        }

        private void OnTriggerEnter(Collider other)
        {
            ReportArea(other);
        }

        private void OnTriggerStay(Collider other)
        {
            ReportArea(other);
        }

        private void ReportArea(Collider other)
        {
            Combatant combatant = other.GetComponentInParent<Combatant>();
            if (combatant == null || combatant.Team != CombatTeam.Player)
            {
                return;
            }

            runtime = runtime != null ? runtime : WorldRuntime.Instance;
            runtime?.SetCurrentArea(areaId, displayName);
        }
    }
}
