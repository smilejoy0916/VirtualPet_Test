using UnityEngine;
using UnityEngine.Rendering;

namespace VirtualPet.Home
{
    /// <summary>Creates simple lit materials for whichever render pipeline is active.</summary>
    public static class MaterialFactory
    {
        public static Material CreateLit(string name, Color color, float smoothness = 0.35f)
        {
            var material = new Material(FindLitShader()) { name = name, color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        static Shader FindLitShader()
        {
            var shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            return shader != null ? shader : Shader.Find("Unlit/Color");
        }
    }
}
