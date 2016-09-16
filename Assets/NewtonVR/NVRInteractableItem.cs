using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableItem : NVRInteractable
    {
        [Tooltip("If you have a specific point you'd like the object held at, create a transform there and set it to this variable")]
        public Transform InteractionPoint;

        protected Transform PickupTransform;

        protected Vector3?[] VelocityHistory;
        protected Vector3?[] AngularVelocityHistory;
        protected int VelocityHistoryStep = 0;

        protected bool DoPhysicsStep = true;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }

        protected override void Start()
        {
            base.Start();

            if (NVRPlayer.Instance.VelocityHistorySteps > 0)
            {
                VelocityHistory = new Vector3?[NVRPlayer.Instance.VelocityHistorySteps];
                AngularVelocityHistory = new Vector3?[NVRPlayer.Instance.VelocityHistorySteps];
            }
        }

        protected override void Update()
        {
            base.Update();

            DoPhysicsStep = true;
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true && DoPhysicsStep == true)
            {
                DoPhysicsStep = false;

                Quaternion RotationDelta;
                Vector3 PositionDelta;

                float angle;
                Vector3 axis;

                if (InteractionPoint != null)
                {
                    RotationDelta = AttachedHand.transform.rotation * Quaternion.Inverse(InteractionPoint.rotation);
                    PositionDelta = (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    RotationDelta = PickupTransform.rotation * Quaternion.Inverse(this.transform.rotation);
                    PositionDelta = (PickupTransform.position - this.transform.position);
                }

                RotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                if (angle != 0)
                {
                    Vector3 AngularTarget = angle * axis;
                    this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, AngularTarget, 10f * (deltaPoses * 1000));
                }

                Vector3 VelocityTarget = PositionDelta / Time.fixedDeltaTime;
                this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, VelocityTarget, 10f);

                if (VelocityHistory != null)
                {
                    VelocityHistoryStep++;
                    if (VelocityHistoryStep >= VelocityHistory.Length)
                    {
                        VelocityHistoryStep = 0;
                    }

                    VelocityHistory[VelocityHistoryStep] = this.Rigidbody.velocity;
                    AngularVelocityHistory[VelocityHistoryStep] = this.Rigidbody.angularVelocity;
                }
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            PickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", this.gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;

            ClosestHeldPoint = (PickupTransform.position - this.transform.position);
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (PickupTransform != null)
            {
                Destroy(PickupTransform.gameObject);
            }

            if (VelocityHistory != null)
            {
                this.Rigidbody.velocity = GetMeanVector(VelocityHistory);
                this.Rigidbody.angularVelocity = GetMeanVector(AngularVelocityHistory);

                VelocityHistoryStep = 0;

                for (int index = 0; index < VelocityHistory.Length; index++)
                {
                    VelocityHistory[index] = null;
                    AngularVelocityHistory[index] = null;
                }
            }
        }

        protected override void DropIfTooFar()
        {
            float distance = Vector3.Distance(AttachedHand.transform.position, (this.transform.position + ClosestHeldPoint));
            if (distance > DropDistance)
            {
                DroppedBecauseOfDistance();
            }
        }

        private Vector3 GetMeanVector(Vector3?[] positions)
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

            int count = 0;
            for (int index = 0; index < positions.Length; index++)
            {
                if (positions[index] != null)
                {
                    x += positions[index].Value.x;
                    y += positions[index].Value.y;
                    z += positions[index].Value.z;

                    count++;
                }
            }

            return new Vector3(x / count, y / count, z / count);
        }
    }
}