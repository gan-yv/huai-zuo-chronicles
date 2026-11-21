using UnityEngine;

namespace Demo02.Core
{
    public sealed class ColorTint : MonoBehaviour
    {
        [SerializeField] private Color tint = Color.white;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            CacheAndApply();
        }

        public void Configure(Color color)
        {
            tint = color;
            CacheAndApply();
        }

        private void CacheAndApply()
        {
            renderers ??= GetComponentsInChildren<Renderer>(includeInactive: true);
            propertyBlock ??= new MaterialPropertyBlock();

            foreach (Renderer item in renderers)
            {
                item.GetPropertyBlock(propertyBlock);
                if (item.sharedMaterial != null && item.sharedMaterial.HasProperty("_BaseColor"))
                {
                    propertyBlock.SetColor("_BaseColor", tint);
                }

                if (item.sharedMaterial != null && item.sharedMaterial.HasProperty("_Color"))
                {
                    propertyBlock.SetColor("_Color", tint);
                }

                item.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
