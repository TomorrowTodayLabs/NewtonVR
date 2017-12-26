using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRVignette : MonoBehaviour
    {
        public Shader vignetteShader;

        private int vignetteProperty;

        private Material material;

        public static NVRVignette instance;

        void Awake()
        {
            instance = this;

            if (vignetteShader == null)
                vignetteShader = Shader.Find("Vignette");

            material = new Material(vignetteShader);

            vignetteProperty = Shader.PropertyToID("_VignettePower");
        }

        public void SetAmount(float newFeather)
        {
            material.SetFloat(vignetteProperty, newFeather);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, material);
        }

        private void OnDestroy()
        {
            Destroy(material);
        }
    }
}