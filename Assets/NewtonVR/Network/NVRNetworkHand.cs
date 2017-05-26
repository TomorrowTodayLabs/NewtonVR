using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkHand : NVRHand, NVRNetworkObject
    {
        public abstract bool IsMine();
        protected abstract void SelfOnBeginInteraction(NVRInteractable interactable);
        protected abstract void SelfOnEndInteraction(NVRInteractable interactable);
        protected abstract void ForceRemoteDrop(NVRInteractable interactable);

        public NVRInteractableEvent OnBeginInteractionRemote = new NVRInteractableEvent();
        public NVRInteractableEvent OnEndInteractionRemote = new NVRInteractableEvent();

        public override void PreInitialize(NVRPlayer player)
        {
            if (IsMine())
            {
                base.PreInitialize(player);
                OnBeginInteraction.AddListener(new UnityEngine.Events.UnityAction<NVRInteractable>(SelfOnBeginInteraction));
                OnEndInteraction.AddListener(new UnityEngine.Events.UnityAction<NVRInteractable>(SelfOnEndInteraction));
            }
            else
            {
                PreInitializeSetup(player);
            }
        }


        protected override void Update()
        {
            if (IsMine())
            {
                base.Update();
            }
        }

        public override void BeginInteraction(NVRInteractable interactable)
        {
            if (IsMine())
            {
                if (interactable.CanAttach == true)
                {
                    NVRNetworkInteractable networkInteractable = interactable as NVRNetworkInteractable;

                    if (interactable.AttachedHand != null)
                    {
                        if (networkInteractable != null)
                        {
                            if (networkInteractable.IsRemotelyAttached() == true)
                            {
                                if (NVRNetworkOwnership.Instance.AllowStealingInteraction)
                                {
                                    ForceRemoteDrop(interactable);
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (interactable.AllowTwoHanded == false)
                                {
                                    interactable.AttachedHand.EndInteraction(interactable);
                                }
                            }
                        }
                        else
                        {
                            if (interactable.AllowTwoHanded == false)
                            {
                                interactable.AttachedHand.EndInteraction(interactable);
                            }
                        }
                    }

                    if (networkInteractable != null && networkInteractable.IsMine() == false)
                    {
                        NVRNetworkOwnership.Instance.RequestOwnership(interactable);
                    }

                    CurrentlyInteracting = interactable;
                    CurrentlyInteracting.BeginInteraction(this);

                    if (OnBeginInteraction != null)
                    {
                        OnBeginInteraction.Invoke(interactable);
                    }
                }
            }
        }

        public override void EndInteraction(NVRInteractable item)
        {
            if (IsMine())
            {
                base.EndInteraction(item);
            }
        }

        public override void Initialize()
        {
            if (IsMine())
            {
                InitializeRemoteRenderModel();
            }

            //todo: assess non mine remote init here.

            base.Initialize();

            if (this.PhysicalController != null && this.PhysicalController.PhysicalController != null)
            {
                this.PhysicalController.PhysicalController.AddComponent<NVRPhotonPhysicalHand>();
            }
        }

        /// <summary>
        /// 1. Construct the hierarchy by transform layers
        /// 2. Data structure:
        ///     render model name, [list render model children names], [child index, list 
        /// </summary>
        protected virtual void InitializeRemoteRenderModel()
        {
            List<NVRNetworkGameObjectSerialization> serializedList = new List<NVRNetworkGameObjectSerialization>();
            GetTransformHierarchy(RenderModel.transform.parent, ref serializedList); //todo hopefully this works - I think rendermodel shouldn't have any siblings
            byte[] bytes = GetSerializedHierarchy(serializedList);
            SendRenderModelData(bytes);
        }

        protected abstract void SendRenderModelData(byte[] data);

        protected virtual void ParseRenderModelData(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            int length = reader.ReadInt32();
            List<NVRNetworkGameObjectSerialization> deserializedList = new List<NVRNetworkGameObjectSerialization>();

            if (reader.PeekChar() != -1)
            {
                RenderModel = new GameObject("Remote RenderModel");
                RenderModel.transform.parent = gameObject.transform;

                CreateDeserializedHierarchy(reader, RenderModel, deserializedList);
            }

            if (length != deserializedList.Count)
            {
                Debug.LogError(string.Format("[NewtonVR] We were expecting {0} gameobjects but got {1}", length, deserializedList.Count));
            }
        }

        protected virtual void CreateDeserializedHierarchy(BinaryReader reader, GameObject newGameObject, List<NVRNetworkGameObjectSerialization> deserializedList)
        {
            NVRNetworkGameObjectSerialization deserialized = new NVRNetworkGameObjectSerialization();
            deserialized.Deserialize(reader, newGameObject);

            deserializedList.Add(deserialized);

            for (int childIndex = 0; childIndex < deserialized.ChildCount; childIndex++)
            {
                GameObject child = new GameObject(string.Format("{0} child {1}", name, childIndex));
                child.transform.parent = newGameObject.transform;

                CreateDeserializedHierarchy(reader, newGameObject, deserializedList);
            }
        }

        protected virtual byte[] GetSerializedHierarchy(List<NVRNetworkGameObjectSerialization> serializedList)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(serializedList.Count);
            for (int serializedIndex = 0; serializedIndex < serializedList.Count; serializedIndex++)
            {
                NVRNetworkGameObjectSerialization serialized = serializedList[serializedIndex];
                serialized.Serialize(writer);
            }

            byte[] bytes = stream.ToArray();

            writer.Close();
            return bytes;
        }

        protected virtual void GetTransformHierarchy(Transform parent, ref List<NVRNetworkGameObjectSerialization> serializedList)
        {
            for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                Transform child = parent.GetChild(childIndex);

                NVRNetworkGameObjectSerialization serialized = new NVRNetworkGameObjectSerialization();
                serialized.Name = child.name;
                serialized.GameObject = child.gameObject;
                serialized.ChildCount = child.childCount;

                serializedList.Add(serialized);

                GetTransformHierarchy(child, ref serializedList);
            }
        }
    }
}