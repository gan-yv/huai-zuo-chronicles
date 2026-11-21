using UnityEngine;

namespace Demo02.Core
{
    public sealed class ColorTint : MonoBehaviour
    {
        [SerializeField] private Color tint = Color.white;
        [SerializeField, Range(0f, 1f)] private float smoothness = 0.18f;
        [SerializeField, Range(0f, 1f)] private float metallic;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            CacheAndApply();
        }

        public void Configure(Color color, float surfaceSmoothness = 0.18f, float surfaceMetallic = 0f)
        {
            tint = color;
            smoothness = surfaceSmoothness;
            metallic = surfaceMetallic;
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

                if (item.sharedMaterial != null && item.sharedMaterial.HasProperty("_Smoothness"))
                {
                    propertyBlock.SetFloat("_Smoothness", smoothness);
                }

                if (item.sharedMaterial != null && item.sharedMaterial.HasProperty("_Metallic"))
                {
                    propertyBlock.SetFloat("_Metallic", metallic);
                }

                item.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
