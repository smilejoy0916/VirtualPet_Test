using UnityEngine;

namespace VirtualPet.Home
{
    /// <summary>
    /// Builds the original placeholder pet ("Mochi") from primitives. Used by the editor
    /// setup to author Assets/Prefabs/Pet.prefab, and at runtime as a fallback.
    /// </summary>
    public static class PetBuilder
    {
        public struct Palette
        {
            public Material Body;
            public Material Belly;
            public Material Dark;
            public Material Blush;
        }

        public static Palette CreateRuntimePalette() => new()
        {
            Body = MaterialFactory.CreateLit("Pet_Body", new Color(1f, 0.70f, 0.45f)),
            Belly = MaterialFactory.CreateLit("Pet_Belly", new Color(1f, 0.94f, 0.84f)),
            Dark = MaterialFactory.CreateLit("Pet_Dark", new Color(0.15f, 0.12f, 0.16f), 0.8f),
            Blush = MaterialFactory.CreateLit("Pet_Blush", new Color(1f, 0.55f, 0.62f))
        };

        public static PetView Build(Transform parent, Palette palette)
        {
            var root = new GameObject("Pet");
            root.transform.SetParent(parent, false);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            Part(visual, "Body", palette.Body, new Vector3(0f, 0.75f, 0f), new Vector3(1.5f, 1.35f, 1.35f));
            Part(visual, "Belly", palette.Belly, new Vector3(0f, 0.62f, -0.42f), new Vector3(0.95f, 0.85f, 0.55f));

            for (var side = -1; side <= 1; side += 2)
            {
                var suffix = side < 0 ? "L" : "R";
                Part(visual, "Ear" + suffix, palette.Body, new Vector3(0.48f * side, 1.42f, 0.05f),
                    new Vector3(0.38f, 0.55f, 0.30f), new Vector3(0f, 0f, -25f * side));
                Part(visual, "EarInner" + suffix, palette.Blush, new Vector3(0.50f * side, 1.45f, -0.07f),
                    new Vector3(0.20f, 0.32f, 0.15f), new Vector3(0f, 0f, -25f * side));
                Part(visual, "Eye" + suffix, palette.Dark, new Vector3(0.27f * side, 0.98f, -0.60f),
                    new Vector3(0.17f, 0.21f, 0.10f));
                Part(visual, "EyeShine" + suffix, palette.Belly, new Vector3(0.24f * side, 1.03f, -0.66f),
                    new Vector3(0.06f, 0.06f, 0.03f));
                Part(visual, "Cheek" + suffix, palette.Blush, new Vector3(0.45f * side, 0.85f, -0.53f),
                    new Vector3(0.18f, 0.10f, 0.06f));
                Part(visual, "Foot" + suffix, palette.Body, new Vector3(0.38f * side, 0.10f, -0.25f),
                    new Vector3(0.42f, 0.22f, 0.50f));
            }

            Part(visual, "Nose", palette.Dark, new Vector3(0f, 0.88f, -0.65f), new Vector3(0.08f, 0.05f, 0.05f));

            var collider = root.AddComponent<SphereCollider>();
            collider.center = new Vector3(0f, 0.8f, 0f);
            collider.radius = 0.9f;

            var view = root.AddComponent<PetView>();
            view.SetVisual(visual);
            return view;
        }

        static void Part(Transform parent, string name, Material material, Vector3 position, Vector3 scale,
            Vector3 euler = default)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localEulerAngles = euler;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;

            // One collider on the root handles taps; the per-part colliders are not needed.
            var collider = part.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
    }
}
