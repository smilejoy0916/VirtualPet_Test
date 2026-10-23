using UnityEngine;
using UnityEngine.EventSystems;
using VirtualPet.Core;
using VirtualPet.UI;

namespace VirtualPet.Home
{
    /// <summary>
    /// The 3D part of the Home screen: camera, light, ground and the pet. Deactivated
    /// while another tab is visible so it costs nothing off-screen.
    /// </summary>
    public sealed class PetStage : MonoBehaviour
    {
        public PetView Pet { get; private set; }

        public static PetStage Create(AppConfig config)
        {
            var stage = new GameObject("PetStage").AddComponent<PetStage>();
            SetUpCamera();

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(stage.transform, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.58f, 0.64f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "Ground";
            ground.transform.SetParent(stage.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            ground.transform.localScale = new Vector3(3.4f, 0.04f, 3.4f);
            Destroy(ground.GetComponent<Collider>());
            ground.GetComponent<MeshRenderer>().sharedMaterial = config.groundMaterial != null
                ? config.groundMaterial
                : MaterialFactory.CreateLit("Ground", new Color(0.55f, 0.82f, 0.50f), 0.1f);

            stage.Pet = config.petPrefab != null
                ? Instantiate(config.petPrefab, stage.transform).GetComponent<PetView>()
                : PetBuilder.Build(stage.transform, PetBuilder.CreateRuntimePalette());

            return stage;
        }

        static void SetUpCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UITheme.Sky;
            camera.fieldOfView = 38f;
            camera.transform.position = new Vector3(0f, 1.9f, -7.2f);
            camera.transform.LookAt(new Vector3(0f, 0.95f, 0f));
            if (camera.GetComponent<PhysicsRaycaster>() == null) camera.gameObject.AddComponent<PhysicsRaycaster>();
        }
    }
}
