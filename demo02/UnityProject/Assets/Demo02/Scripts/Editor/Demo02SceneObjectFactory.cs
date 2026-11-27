using Demo02.Core;
using Demo02.Interaction;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Editor
{
    /// <summary>
    /// Shared greybox primitives used by the specialised scene assemblers.
    /// </summary>
    internal static class Demo02SceneObjectFactory
    {
        public static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color? tint = null)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            if (tint.HasValue)
            {
                block.AddComponent<ColorTint>().Configure(tint.Value, 0.12f);
            }

            return block;
        }

        public static GameObject CreateDecorBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color tint,
            PrimitiveType primitive = PrimitiveType.Cube)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.position = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.AddComponent<ColorTint>().Configure(tint, 0.22f);
            return part;
        }

        public static GameObject CreateAvatarPart(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Color tint)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, worldPositionStays: false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.AddComponent<ColorTint>().Configure(tint, 0.32f);
            return part;
        }

        public static void CreateWorldArea(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            string areaId,
            string displayName)
        {
            GameObject area = new(name);
            area.transform.SetParent(parent);
            area.transform.position = position;

            BoxCollider trigger = area.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;

            Rigidbody body = area.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            area.AddComponent<WorldArea>().Configure(areaId, displayName);
        }

        public static void CreateWorldAnchor(
            Transform parent,
            string name,
            WorldSpawnPoint spawnPoint,
            Color color)
        {
            GameObject anchor = new(name);
            anchor.transform.SetParent(parent);
            anchor.transform.position = spawnPoint.transform.position;
            SphereCollider trigger = anchor.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.45f;
            CreateAvatarPart(
                anchor.transform,
                "Anchor_Core",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.28f, 0f),
                new Vector3(0.34f, 0.24f, 0.34f),
                color
            );
            anchor.AddComponent<WorldAnchorInteractable>()
                .Configure(spawnPoint);
        }
    }
}
