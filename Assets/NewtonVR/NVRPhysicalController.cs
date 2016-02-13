using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

        public void Initialize(NVRHand trackingHand, bool initialState)
        {
            Hand = trackingHand;

            PhysicalController = GameObject.Instantiate(Hand.gameObject);
            PhysicalController.name = PhysicalController.name.Replace("(Clone)", " [Physical]");

            GameObject.DestroyImmediate(PhysicalController.GetComponent<NVRPhysicalController>());
            GameObject.DestroyImmediate(PhysicalController.GetComponent<NVRHand>());
            GameObject.DestroyImmediate(PhysicalController.GetComponent<SteamVR_TrackedObject>());
            GameObject.DestroyImmediate(PhysicalController.GetComponent<SteamVR_RenderModel>());
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

            Rigidbody = PhysicalController.GetComponent<Rigidbody>();
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = false;
            Rigidbody.angularDrag = 0;
            Rigidbody.maxAngularVelocity = 100f;

            string controllerModel = Hand.GetDeviceName();
            switch (controllerModel)
            {
                case "vr_controller_05_wireless_b":
                    Transform dk1Trackhat = PhysicalController.transform.FindChild("trackhat");
                    Collider dk1TrackhatCollider = dk1Trackhat.gameObject.AddComponent<BoxCollider>();

                    Transform dk1Body = PhysicalController.transform.FindChild("body");
                    Collider dk1BodyCollider = dk1Body.gameObject.AddComponent<BoxCollider>();

                    Colliders = new Collider[] { dk1TrackhatCollider, dk1BodyCollider };
                    break;

                case "vr_controller_vive_1_5":
                    List<Collider> colliderList = new List<Collider>();

                    Transform dk2Trackhat = PhysicalController.transform.FindChild("trackhat");

                    Transform dk2TrackhatColliders = dk2Trackhat.FindChild("VivePreTrackhatColliders");
                    if (dk2TrackhatColliders == null)
                    {
                        dk2TrackhatColliders = GameObject.Instantiate(Resources.Load<GameObject>("VivePreTrackhatColliders")).transform;
                        dk2TrackhatColliders.parent = dk2Trackhat;
                        dk2TrackhatColliders.localPosition = Vector3.zero;
                        dk2TrackhatColliders.localRotation = Quaternion.identity;
                        dk2TrackhatColliders.localScale = Vector3.one * 0.1f;
                    }


                    Transform dk2Body = dk2Trackhat.FindChild("body collider");
                    if (dk2Body == null)
                    {
                        dk2Body = new GameObject("body collider").transform;
                        dk2Body.parent = PhysicalController.transform;
                        dk2Body.localPosition = new Vector3(0, -0.01f, -0.083f);
                        dk2Body.localScale = new Vector3(0.044f, 0.035f, 0.18f);
                    }

                    Collider dk2BodyCollider = dk2Body.gameObject.GetComponent<BoxCollider>();
                    if (dk2BodyCollider == null)
                    {
                        dk2BodyCollider = dk2Body.gameObject.AddComponent<BoxCollider>();
                    }

                    colliderList.AddRange(dk2TrackhatColliders.GetComponents<Collider>());
                    colliderList.Add(dk2BodyCollider);

                    Colliders = colliderList.ToArray();
                    break;

                default:
                    Debug.LogError("Error. Unsupported device type: " + controllerModel);
                    break;
            }

            Transform trackhat = PhysicalController.transform.FindChild("trackhat");
            Collider trackhatCollider = trackhat.gameObject.AddComponent<BoxCollider>();

            Transform body = PhysicalController.transform.FindChild("body");
            Collider bodyCollider = body.gameObject.AddComponent<BoxCollider>();

            Colliders = new Collider[] { trackhatCollider, bodyCollider };

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
        
        private void Movement()
        {
            Vector3 PositionDelta;
            Quaternion RotationDelta;

            float angle;
            Vector3 axis;

            RotationDelta = Hand.transform.rotation * Quaternion.Inverse(PhysicalController.transform.rotation);
            PositionDelta = (Hand.transform.position - PhysicalController.transform.position);

            RotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0)
            {
                Vector3 AngularTarget = angle * axis * AttachedRotationMagic;
                AngularTarget = AngularTarget * Time.fixedDeltaTime;
                this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, AngularTarget, 10f);
            }

            Vector3 VelocityTarget = PositionDelta * AttachedPositionMagic * Time.fixedDeltaTime;

            this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, VelocityTarget, 10f);
        }

        protected virtual void FixedUpdate()
        {
            if (State == true)
            {
                CheckForDrop();

                Movement();
            }
        }

        protected virtual void DroppedBecauseOfDistance()
        {
            Hand.ForceGhost();

            Debug.Log("Dropped");
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