using Demo02.Core;
using UnityEngine;

namespace Demo02.WorldState
{
    public sealed class WorldObjectId : MonoBehaviour
    {
        [SerializeField] private string objectId;

        public string ObjectId => objectId;

        public void Configure(string id)
        {
            objectId = id;
        }

        private void OnEnable()
        {
            GameServices.Resolve<WorldRuntime>()
                ?.RegisterWorldObject(this);
        }

        private void OnDisable()
        {
            GameServices.Resolve<WorldRuntime>()
                ?.UnregisterWorldObject(this);
        }
    }
}
