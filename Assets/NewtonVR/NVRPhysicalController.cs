using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System;

namespace NewtonVR
{
    public class NVRPhysicalController : MonoBehaviour
    {
        private NVRHand Hand;
        public bool State = false;
        private Rigidbody Rigidbody;

        private Collider[] Colliders;
        private GameObject PhysicalController;

        protected float DropDistance { get { return 1f; } }
        protected Vector3 ClosestHeldPoint;

        protected float AttachedRotationMagic = 20f;
        protected float AttachedPositionMagic = 3000f;

        private Type[] KeepTypes = new Type[] {typeof(MeshFilter), typeof(Renderer), typeof(Transform), typeof(Rigidbody)};

        public void Initialize(NVRHand trackingHand, bool initialState)
        {
            Hand = trackingHand;
            
            PhysicalController = GameObject.Instantiate(Hand.gameObject);
            PhysicalController.name = PhysicalController.name.Replace("(Clone)", " [Physical]");

            Component[] components = PhysicalController.GetComponentsInChildren<Component>(true);

            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Type componentType = components[componentIndex].GetType();
                if (KeepTypes.Any(keepType => keepType == componentType || componentType.IsSubclassOf(keepType)) == false)
                {
                    DestroyImmediate(components[componentIndex]);
                }
            }

            PhysicalController.transform.parent = Hand.transform.parent;
            PhysicalController.transform.position = Hand.transform.position;
            PhysicalController.transform.rotation = Hand.transform.rotation;
            PhysicalController.transform.localScale = Hand.transform.localScale;
            
            string controllerModel = Hand.GetDeviceName();
            switch (controllerModel)
            {
                case "Custom":
                    Transform customCollidersTransform = null;
                    if (Hand.CustomPhysicalColliders == null)
                    {
                        GameObject customColliders = GameObject.Instantiate(Hand.CustomModel);
                        customColliders.name = "CustomColliders";
                        customCollidersTransform = customColliders.transform;

                        customCollidersTransform.parent = PhysicalController.transform;
                        customCollidersTransform.localPosition = Vector3.zero;
                        customCollidersTransform.localRotation = Quaternion.identity;
                        customCollidersTransform.localScale = Vector3.one;

                        foreach (Collider col in customColliders.GetComponentsInChildren<Collider>())
                        {
                            col.isTrigger = false;
                        }

                        Colliders = customCollidersTransform.GetComponentsInChildren<Collider>();
                    }
                    else
                    {
                        GameObject customColliders = GameObject.Instantiate(Hand.CustomPhysicalColliders);
                        customColliders.name = "CustomColliders";
                        customCollidersTransform = customColliders.transform;

                        customCollidersTransform.parent = PhysicalController.transform;
                        customCollidersTransform.localPosition = Vector3.zero;
                        customCollidersTransform.localRotation = Quaternion.identity;
                        customCollidersTransform.localScale = Hand.CustomPhysicalColliders.transform.localScale;
                    }

                    Colliders = customCollidersTransform.GetComponentsInChildren<Collider>();
                    break;

                default:
                    Colliders = Hand.SetupDefaultPhysicalColliders(PhysicalController.transform);
                    break;
            }

            if (Colliders == null)
            {
                Debug.LogError("[NewtonVR] Error: Physical colliders on hand not setup properly.");
            }

            Rigidbody = PhysicalController.GetComponent<Rigidbody>();
            Rigidbody.isKinematic = false;
            Rigidbody.maxAngularVelocity = float.MaxValue;

            if (trackingHand.Player.AutomaticallySetControllerTransparency)
            {
                Renderer[] renderers = PhysicalController.GetComponentsInChildren<Renderer>();
                for (int index = 0; index < renderers.Length; index++)
                {
                    NVRHelpers.SetOpaque(renderers[index].material);
                }
            }

            if (initialState == false)
            {
                Off();
            }
            else
            {
                On();
            }
        }

        public void Kill()
        {
            Destroy(PhysicalController);
            Destroy(this);
        }

        private void CheckForDrop()
        {
            float distance = Vector3.Distance(Hand.transform.position, this.transform.position);

            if (distance > DropDistance)
            {
                DroppedBecauseOfDistance();
            }
        }

        private void UpdatePosition()
        {
            Rigidbody.maxAngularVelocity = float.MaxValue; //this doesn't seem to be respected in nvrhand's init. or physical hand's init. not sure why. if anybody knows, let us know. -Keith 6/16/2016

            Quaternion RotationDelta;
            Vector3 PositionDelta;

            float angle;
            Vector3 axis;

            RotationDelta = Hand.transform.rotation * Quaternion.Inverse(PhysicalController.transform.rotation);
            PositionDelta = (Hand.transform.position - PhysicalController.transform.position);

            RotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0)
            {
                Vector3 AngularTarget = angle * axis;
                this.Rigidbody.angularVelocity = AngularTarget;
            }

            Vector3 VelocityTarget = PositionDelta / Time.fixedDeltaTime;
            this.Rigidbody.velocity = VelocityTarget;
        }

        protected virtual void FixedUpdate()
        {
            if (State == true)
            {
                CheckForDrop();

                UpdatePosition();
            }
        }

        protected virtual void DroppedBecauseOfDistance()
        {
            Hand.ForceGhost();
        }

        public void On()
        {
            PhysicalController.transform.position = Hand.transform.position;
            PhysicalController.transform.rotation = Hand.transform.rotation;

            PhysicalController.SetActive(true);

            State = true;
        }

        public void Off()
        {
            PhysicalController.SetActive(false);

            State = false;
        }
    }
}