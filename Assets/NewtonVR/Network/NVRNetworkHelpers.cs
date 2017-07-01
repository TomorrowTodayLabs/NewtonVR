using UnityEngine;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;
using NewtonVR.Network;

namespace NewtonVR
{
    public class NVRNetworkHelpers
    {
        public static string GetHash(byte[] bytes)
        {
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            string hash = System.BitConverter.ToString(md5Provider.ComputeHash(bytes));
            return hash;
        }

        public static void SerializeMaterial(BinaryWriter writer, Material material)
        {
            Shader shader = material.shader;

            writer.Write(shader.name);
            WriteColor32(writer, material.color);
            WriteTexture(writer, material.mainTexture);
        }
        public static Material DeserializeMaterial(BinaryReader reader)
        {
            string name = reader.ReadString();
            Color32 color = ReadColor32(reader);
            Texture texture = ReadTexture(reader);

            Material material = new Material(Shader.Find(name));
            material.mainTexture = texture;
            material.color = color;

            return material;
        }

        public static void SerializePhysicMaterial(BinaryWriter writer, PhysicMaterial material)
        {
            writer.Write(material.name);
            writer.Write((int)material.bounceCombine);
            writer.Write(material.bounciness);
            writer.Write(material.dynamicFriction);
            writer.Write((int)material.frictionCombine);
            writer.Write(material.staticFriction);
        }
        public static PhysicMaterial DeserializePhysicMaterial(BinaryReader reader)
        {
            PhysicMaterial material = new PhysicMaterial(reader.ReadString());
            material.bounceCombine = (PhysicMaterialCombine)reader.ReadInt32();
            material.bounciness = reader.ReadSingle();
            material.dynamicFriction = reader.ReadSingle();
            material.frictionCombine = (PhysicMaterialCombine)reader.ReadInt32();
            material.staticFriction = reader.ReadSingle();

            return material;
        }

        public static void SerializeMesh(BinaryWriter writer, Mesh mesh)
        {
            writer.Write(mesh.name);
            WriteVector3Array(writer, mesh.vertices);
            WriteVector3Array(writer, mesh.normals);
            WriteVector4Array(writer, mesh.tangents);
            WriteVector2Array(writer, mesh.uv);
            WriteVector2Array(writer, mesh.uv2);
            WriteVector2Array(writer, mesh.uv3);
            WriteVector2Array(writer, mesh.uv4);
            WriteColor32Array(writer, mesh.colors32);
        }
        public static Mesh DeserializeMesh(BinaryReader reader)
        {
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

        public static void SerializeCollider(BinaryWriter writer, Collider collider)
        {
            writer.Write(collider.enabled);
            writer.Write(collider.isTrigger);
            string materialHash = NVRNetworkCache.instance.AddAsset(collider.sharedMaterial);
            writer.Write(materialHash);

            if (collider is SphereCollider)
            {
                WriteSphereCollider(writer, (SphereCollider)collider);
            }
            else if (collider is CapsuleCollider)
            {
                WriteCapsuleCollider(writer, (CapsuleCollider)collider);
            }
            else if (collider is BoxCollider)
            {
                WriteBoxCollider(writer, (BoxCollider)collider);
            }
            else if (collider is MeshCollider)
            {
                WriteMeshCollider(writer, (MeshCollider)collider);
            }
        }
        public static void DeserializeCollider(BinaryReader reader, Collider collider)
        {
            collider.enabled = reader.ReadBoolean();
            collider.isTrigger = reader.ReadBoolean();
            string materialHash = reader.ReadString();
            if (string.IsNullOrEmpty(materialHash) == false)
            {
                NVRCacheJob job = new NVRCacheJob(materialHash);
                job.assignment = () => collider.sharedMaterial = NVRNetworkCache.instance.GetCache<PhysicMaterial>(materialHash);
                NVRNetworkCache.instance.AddCacheJob(job);
            }

            if (collider is SphereCollider)
            {
                ReadSphereCollider(reader, (SphereCollider)collider);
            }
            else if (collider is CapsuleCollider)
            {
                ReadCapsuleCollider(reader, (CapsuleCollider)collider);
            }
            else if (collider is BoxCollider)
            {
                ReadBoxCollider(reader, (BoxCollider)collider);
            }
        }

        public static void WriteSphereCollider(BinaryWriter writer, SphereCollider collider)
        {
            WriteVector3(writer, collider.center);
            writer.Write(collider.radius);
        }
        public static void ReadSphereCollider(BinaryReader reader, SphereCollider collider)
        {
            collider.center = ReadVector3(reader);
            collider.radius = reader.ReadSingle();
        }

        public static void WriteCapsuleCollider(BinaryWriter writer, CapsuleCollider collider)
        {
            WriteVector3(writer, collider.center);
            writer.Write(collider.direction);
            writer.Write(collider.height);
            writer.Write(collider.radius);
        }
        public static void ReadCapsuleCollider(BinaryReader reader, CapsuleCollider collider)
        {
            collider.center = ReadVector3(reader);
            collider.direction = reader.ReadInt32();
            collider.height = reader.ReadSingle();
            collider.radius = reader.ReadSingle();
        }

        public static void WriteBoxCollider(BinaryWriter writer, BoxCollider collider)
        {
            WriteVector3(writer, collider.center);
            WriteVector3(writer, collider.size);
        }
        public static void ReadBoxCollider(BinaryReader reader, BoxCollider collider)
        {
            collider.center = ReadVector3(reader);
            collider.size = ReadVector3(reader);
        }

        public static void WriteMeshCollider(BinaryWriter writer, MeshCollider collider)
        {
            writer.Write(collider.convex);
            writer.Write(collider.inflateMesh);
            writer.Write(collider.skinWidth);

            string meshHash = NVRNetworkCache.instance.AddAsset(collider.sharedMesh);
            writer.Write(meshHash);
        }
        public static void ReadMeshCollider(BinaryReader reader, MeshCollider collider)
        {
            bool convex = reader.ReadBoolean();
            bool inflate = reader.ReadBoolean();
            float width = reader.ReadSingle();

            string meshHash = reader.ReadString();
            if (string.IsNullOrEmpty(meshHash) == false)
            {
                NVRCacheJob job = new NVRCacheJob(meshHash);
                job.assignment = () =>
                {
                    collider.sharedMesh = NVRNetworkCache.instance.GetCache<Mesh>(meshHash);
                    collider.convex = convex;
                    collider.inflateMesh = inflate;
                    collider.skinWidth = width;
                };
                NVRNetworkCache.instance.AddCacheJob(job);
            }
        }

        public static void WriteTexture(BinaryWriter writer, Texture texture)
        {
            if (texture is Texture2D)
            {
                Texture2D texture2d = (Texture2D)texture;
                writer.Write(texture2d.name);
                writer.Write(texture2d.width);
                writer.Write(texture2d.height);
                writer.Write((int)texture2d.format);
                writer.Write(texture2d.mipmapCount > 0);
                WriteColor32Array(writer, texture2d.GetPixels32());
            }
            else
            {
                Debug.LogError("[NewtonVR] Unsupported texture type");
            }
        }
        public static Texture ReadTexture(BinaryReader reader)
        {
            string name = reader.ReadString();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            TextureFormat format = (TextureFormat)reader.ReadInt32();
            bool mipped = reader.ReadBoolean();
            Color32[] colors = ReadColor32Array(reader);

            Texture2D texture = new Texture2D(width, height, format, mipped);
            texture.SetPixels32(colors);
            texture.name = name;

            return texture;
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