using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

namespace NewtonVR
{
    public class NVRPhysicalController : MonoBehaviour
    {
        private NVRHand Hand;
        public bool State = false;
        private Rigidbody Rigidbody;

        private Collider[] Colliders;
        private GameObject PhysicalController;
        private Transform ModelParent;

        protected float DropDistance { get { return 1f; } }
        protected Vector3 ClosestHeldPoint;

        protected float AttachedRotationMagic = 20f;
        protected float AttachedPositionMagic = 3000f;

        public void Initialize(NVRHand trackingHand, bool initialState)
        {
            Hand = trackingHand;

            PhysicalController = GameObject.Instantiate(Hand.gameObject);
            PhysicalController.name = PhysicalController.name.Replace("(Clone)", " [Physical]");

            SteamVR_RenderModel renderModel = PhysicalController.GetComponentInChildren<SteamVR_RenderModel>();
            ModelParent = renderModel.transform;

            GameObject.DestroyImmediate(PhysicalController.GetComponent<NVRPhysicalController>());
            GameObject.DestroyImmediate(PhysicalController.GetComponent<NVRHand>());
            GameObject.DestroyImmediate(PhysicalController.GetComponent<SteamVR_TrackedObject>());
            GameObject.DestroyImmediate(renderModel);
            GameObject.DestroyImmediate(PhysicalController.GetComponent<NVRPhysicalController>());

            Collider[] clonedColliders = PhysicalController.GetComponentsInChildren<Collider>();
            for (int index = 0; index < clonedColliders.Length; index++)
            {
                GameObject.DestroyImmediate(clonedColliders[index]);
            }

            PhysicalController.transform.parent = Hand.transform.parent;
            PhysicalController.transform.position = Hand.transform.position;
            PhysicalController.transform.rotation = Hand.transform.rotation;
            PhysicalController.transform.localScale = Hand.transform.localScale;

            string controllerModel = Hand.GetDeviceName();
            switch (controllerModel)
            {
                case "vr_controller_05_wireless_b":
                    Transform dk1Trackhat = ModelParent.transform.Find("trackhat");
                    Collider dk1TrackhatCollider = dk1Trackhat.gameObject.GetComponent<BoxCollider>();
                    if (dk1TrackhatCollider == null)
                        dk1TrackhatCollider = dk1Trackhat.gameObject.AddComponent<BoxCollider>();

                    Transform dk1Body = ModelParent.transform.Find("body");
                    Collider dk1BodyCollider = dk1Body.gameObject.GetComponent<BoxCollider>();
                    if (dk1BodyCollider == null)
                        dk1BodyCollider = dk1Body.gameObject.AddComponent<BoxCollider>();

                    Colliders = new Collider[] { dk1TrackhatCollider, dk1BodyCollider };
                    break;

                case "vr_controller_vive_1_5":
                    Transform dk2TrackhatColliders = ModelParent.transform.FindChild("VivePreColliders");
                    if (dk2TrackhatColliders == null)
                    {
                        dk2TrackhatColliders = GameObject.Instantiate(Resources.Load<GameObject>("VivePreColliders")).transform;
                        dk2TrackhatColliders.parent = ModelParent.transform;
                        dk2TrackhatColliders.localPosition = Vector3.zero;
                        dk2TrackhatColliders.localRotation = Quaternion.identity;
                        dk2TrackhatColliders.localScale = Vector3.one;
                    }

                    Colliders = dk2TrackhatColliders.GetComponentsInChildren<Collider>();
                    break;

                case "Custom":
                    Transform customCollidersTransform = PhysicalController.transform.FindChild("VivePreColliders");
                    if (customCollidersTransform == null)
                    {
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

                        }
                        else
                        {
                            GameObject customColliders = GameObject.Instantiate(Hand.CustomPhysicalColliders);
                            customColliders.name = "CustomColliders";
                            customCollidersTransform = customColliders.transform;

                            customCollidersTransform.parent = PhysicalController.transform;
                            customCollidersTransform.localPosition = Vector3.zero;
                            customCollidersTransform.localRotation = Quaternion.identity;
                            customCollidersTransform.localScale = Vector3.one;
                        }
                    }

                    Colliders = customCollidersTransform.GetComponentsInChildren<Collider>();
                    break;

                default:
                    Debug.LogError("Error. Unsupported device type: " + controllerModel);
                    break;
            }

            Rigidbody = PhysicalController.GetComponent<Rigidbody>();
            Rigidbody.isKinematic = false;
            Rigidbody.maxAngularVelocity = float.MaxValue;

            Renderer[] renderers = PhysicalController.GetComponentsInChildren<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                NVRHelpers.SetOpaque(renderers[index].material);
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