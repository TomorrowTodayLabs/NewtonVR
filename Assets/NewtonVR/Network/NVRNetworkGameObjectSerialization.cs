using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NewtonVR.Network
{
    public class NVRNetworkGameObjectSerialization
    {
        public string Name;
        public int ChildCount;
        public GameObject GameObject;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name);
            NVRNetworkHelpers.WriteVector3(writer, GameObject.transform.localPosition);
            NVRNetworkHelpers.WriteVector3(writer, GameObject.transform.localScale);
            NVRNetworkHelpers.WriteVector3(writer, GameObject.transform.localEulerAngles);
            writer.Write(ChildCount);

            MeshFilter meshFilter = GameObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GameObject.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                
                NVRNetworkHelpers.SerializeMesh(writer, meshFilter.sharedMesh);

                Material[] materials = meshRenderer.sharedMaterials;
                writer.Write(materials.Length);
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    NVRNetworkHelpers.SerializeMaterial(writer, materials[materialIndex]);
                }
            }

            Collider[] colliders = GameObject.GetComponents<Collider>();
            if (colliders == null || colliders.Length == 0)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(colliders.Length);

                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    Collider collider = colliders[colliderIndex];
                    NVRColliderTypes colliderType = NVRColliderTypes.Sphere;

                    if (collider is SphereCollider)
                    {
                        colliderType = NVRColliderTypes.Sphere;
                    }
                    else if (collider is CapsuleCollider)
                    {
                        colliderType = NVRColliderTypes.Capsule;
                    }
                    else if (collider is BoxCollider)
                    {
                        colliderType = NVRColliderTypes.Box;
                    }

                    writer.Write((int)colliderType);
                    NVRNetworkHelpers.SerializeCollider(writer, collider);
                }
            }
        }

        public void Deserialize(BinaryReader reader, GameObject gameObject)
        {
            string name = reader.ReadString();
            Vector3 localPosition = NVRNetworkHelpers.ReadVector3(reader);
            Vector3 localScale = NVRNetworkHelpers.ReadVector3(reader);
            Vector3 localEulerAngles = NVRNetworkHelpers.ReadVector3(reader);
            int childCount = reader.ReadInt32();
            bool hasMesh = reader.ReadBoolean();
            bool hasColliders = reader.ReadBoolean();

            gameObject.name = name;
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.transform.localEulerAngles = localEulerAngles;

            this.Name = name;
            this.ChildCount = childCount;
            this.GameObject = gameObject;

            if (hasMesh)
            {
                Mesh mesh = NVRNetworkHelpers.DeserializeMesh(reader);

                int materialsCount = reader.ReadInt32();
                Material[] materials = new Material[materialsCount];

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = NVRNetworkHelpers.DeserializeMaterial(reader);
                }

                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = materials;
            }

            if (hasColliders)
            {
                int collidersCount = reader.ReadInt32();
                
                for (int colliderIndex = 0; colliderIndex < collidersCount; colliderIndex++)
                {
                    NVRColliderTypes colliderType = (NVRColliderTypes)reader.ReadInt32();
                    Collider collider = AddCollider(gameObject, colliderType);
                    NVRNetworkHelpers.DeserializeCollider(reader, collider);
                }
            }
        }

        protected Collider AddCollider(GameObject gameObject, NVRColliderTypes colliderType)
        {
            Collider collider = null;

            if (colliderType == NVRColliderTypes.Sphere)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }
            else if(colliderType == NVRColliderTypes.Capsule)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }
            else if (colliderType == NVRColliderTypes.Box)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            return collider;
        }

        protected enum NVRColliderTypes
        {
            Sphere,
            Box,
            Capsule,
            Mesh,
        }
    }
}
