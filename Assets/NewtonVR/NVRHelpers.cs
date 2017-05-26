using UnityEngine;
using System.Collections;
using System.Reflection;
using System.IO;

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

        //Get an average (mean) from more than two quaternions (with two, slerp would be used).
        //Note: this only works if all the quaternions are relatively close together.
        //Usage:
        //-Cumulative is an external Vector4 which holds all the added x y z and w components.
        //-newRotation is the next rotation to be added to the average pool
        //-firstRotation is the first quaternion of the array to be averaged
        //-addAmount holds the total amount of quaternions which are currently added
        //This function returns the current average quaternion
        public static Quaternion AverageQuaternion(ref Vector4 cumulative, Quaternion newRotation, Quaternion firstRotation, int addAmount)
        {
            float w = 0.0f;
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;

            //Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
            //q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
            if (!AreQuaternionsClose(newRotation, firstRotation))
            {

                newRotation = InverseSignQuaternion(newRotation);
            }

            //Average the values
            float addDet = 1f / (float)addAmount;
            cumulative.w += newRotation.w;
            w = cumulative.w * addDet;
            cumulative.x += newRotation.x;
            x = cumulative.x * addDet;
            cumulative.y += newRotation.y;
            y = cumulative.y * addDet;
            cumulative.z += newRotation.z;
            z = cumulative.z * addDet;

            //note: if speed is an issue, you can skip the normalization step
            return NormalizeQuaternion(x, y, z, w);
        }

        public static Quaternion NormalizeQuaternion(float x, float y, float z, float w)
        {
            float lengthD = 1.0f / (w * w + x * x + y * y + z * z);
            w *= lengthD;
            x *= lengthD;
            y *= lengthD;
            z *= lengthD;

            return new Quaternion(x, y, z, w);
        }

        //Changes the sign of the quaternion components. This is not the same as the inverse.
        public static Quaternion InverseSignQuaternion(Quaternion q)
        {
            return new Quaternion(-q.x, -q.y, -q.z, -q.w);
        }

        //Returns true if the two input quaternions are close to each other. This can
        //be used to check whether or not one of two quaternions which are supposed to
        //be very similar but has its component signs reversed (q has the same rotation as
        //-q)
        public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2)
        {
            float dot = Quaternion.Dot(q1, q2);

            if (dot < 0.0f)
            {
                return false;
            }

            else
            {
                return true;
            }
        }

        public static byte[] SerializeMesh(Mesh mesh)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(mesh.name);
            WriteVector3Array(writer, mesh.vertices);
            WriteVector3Array(writer, mesh.normals);
            WriteVector4Array(writer, mesh.tangents);
            WriteVector2Array(writer, mesh.uv);
            WriteVector2Array(writer, mesh.uv2);
            WriteVector2Array(writer, mesh.uv3);
            WriteVector2Array(writer, mesh.uv4);
            WriteColor32Array(writer, mesh.colors32);

            byte[] bytes = stream.ToArray();
            writer.Close();
            return bytes;
        }

        public static Mesh DeserializeMesh(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);

            string name = reader.ReadString();
            Vector3[] vertices = ReadVector3Array(reader);
            Vector3[] normals = ReadVector3Array(reader);
            Vector4[] tangents = ReadVector4Array(reader);
            Vector2[] uv = ReadVector2Array(reader);
            Vector2[] uv2 = ReadVector2Array(reader);
            Vector2[] uv3 = ReadVector2Array(reader);
            Vector2[] uv4 = ReadVector2Array(reader);
            Color32[] colors32 = ReadColor32Array(reader);

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uv;
            mesh.uv2 = uv2;
            mesh.uv3 = uv3;
            mesh.uv4 = uv4;
            mesh.colors32 = colors32;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static void WriteVector2Array(BinaryWriter writer, Vector2[] array)
        {
            writer.Write(array.Length);
            for (int arrayIndex = 0; arrayIndex < array.Length; arrayIndex++)
            {
                WriteVector2(writer, array[arrayIndex]);
            }
        }
        public static Vector2[] ReadVector2Array(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            Vector2[] array = new Vector2[length];

            for (int arrayIndex = 0; arrayIndex < length; arrayIndex++)
            {
                array[arrayIndex] = ReadVector2(reader);
            }

            return array;
        }

        public static void WriteVector2(BinaryWriter writer, Vector2 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }
        public static Vector2 ReadVector2(BinaryReader reader)
        {
            Vector2 vector = new Vector2();
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            return vector;
        }

        public static void WriteVector3Array(BinaryWriter writer, Vector3[] array)
        {
            writer.Write(array.Length);
            for (int arrayIndex = 0; arrayIndex < array.Length; arrayIndex++)
            {
                WriteVector3(writer, array[arrayIndex]);
            }
        }
        public static Vector3[] ReadVector3Array(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            Vector3[] array = new Vector3[length];

            for (int arrayIndex = 0; arrayIndex < length; arrayIndex++)
            {
                array[arrayIndex] = ReadVector3(reader);
            }

            return array;
        }

        public static void WriteVector3(BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }
        public static Vector3 ReadVector3(BinaryReader reader)
        {
            Vector3 vector = new Vector3();
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            vector.z = reader.ReadSingle();
            return vector;
        }

        public static void WriteVector4Array(BinaryWriter writer, Vector4[] array)
        {
            writer.Write(array.Length);
            for (int arrayIndex = 0; arrayIndex < array.Length; arrayIndex++)
            {
                WriteVector4(writer, array[arrayIndex]);
            }
        }
        public static Vector4[] ReadVector4Array(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            Vector4[] array = new Vector4[length];

            for (int arrayIndex = 0; arrayIndex < length; arrayIndex++)
            {
                array[arrayIndex] = ReadVector4(reader);
            }

            return array;
        }

        public static void WriteVector4(BinaryWriter writer, Vector4 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
            writer.Write(vector.w);
        }
        public static Vector4 ReadVector4(BinaryReader reader)
        {
            Vector4 vector = new Vector4();
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            vector.z = reader.ReadSingle();
            vector.w = reader.ReadSingle();
            return vector;
        }

        public static void WriteColor32Array(BinaryWriter writer, Color32[] array)
        {
            writer.Write(array.Length);
            for (int arrayIndex = 0; arrayIndex < array.Length; arrayIndex++)
            {
                WriteColor32(writer, array[arrayIndex]);
            }
        }
        public static Color32[] ReadColor32Array(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            Color32[] colors = new Color32[length];

            for (int arrayIndex = 0; arrayIndex < length; arrayIndex++)
            {
                colors[arrayIndex] = ReadColor32(reader);
            }

            return colors;
        }

        public static void WriteColor32(BinaryWriter writer, Color32 color)
        {
            writer.Write(color.a);
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
        }
        public static Color32 ReadColor32(BinaryReader reader)
        {
            Color32 color = new Color32();
            color.a = reader.ReadByte();
            color.r = reader.ReadByte();
            color.g = reader.ReadByte();
            color.b = reader.ReadByte();
            return color;
        }
    }
}