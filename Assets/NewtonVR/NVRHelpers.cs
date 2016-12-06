using UnityEngine;
using System.Collections;
using System.Reflection;

namespace NewtonVR
{
    public class NVRHelpers
    {
        private static Shader standardShader;
        private static Shader StandardShader
        {
            get
            {
                if (standardShader == null)
                {
                    standardShader = Shader.Find("Standard");
                }
                return standardShader;
            }
        }

        public static void SetTransparent(Material material, Color? newcolor = null)
        {
            if (material.shader != StandardShader)
                Debug.LogWarning("Trying to set transparent mode on non-standard shader. Please use the Standard Shader instead or modify this method.");

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0f);
            material.renderQueue = 3000;
            material.mainTexture = null;

            if (newcolor != null)
            {
                material.color = newcolor.Value;
            }
        }
        
        public static void SetOpaque(Material material)
        {
            if (material.shader != StandardShader)
                Debug.LogWarning("Trying to set opaque mode on non-standard shader. Please use the Standard Shader instead or modify this method.");

            material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }

        public static void SetProperty(object obj, string propertyName, object value, bool isPublic)
        {
            BindingFlags flags = BindingFlags.Instance;
            if (isPublic)
                flags = flags | BindingFlags.Public;
            else
                flags = flags | BindingFlags.NonPublic;

            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName, flags);
            propertyInfo.SetValue(obj, value, null);
        }

        public static void SetField(object obj, string fieldName, object value, bool isPublic)
        {
            BindingFlags flags = BindingFlags.Instance;
            if (isPublic)
                flags = flags | BindingFlags.Public;
            else
                flags = flags | BindingFlags.NonPublic;

            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, flags);
            fieldInfo.SetValue(obj, value);
        }

        public static void LineRendererSetColor(LineRenderer lineRenderer, Color startColor, Color endColor)
        {
            #if UNITY_5_5_OR_NEWER
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
            #else
            lineRenderer.SetColors(startColor, endColor);
            #endif
        }

        public static void LineRendererSetWidth(LineRenderer lineRenderer, float startWidth, float endWidth)
        {
            #if UNITY_5_5_OR_NEWER
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            #else
            lineRenderer.SetWidth(startWidth, endWidth);
            #endif
        }
    }
}