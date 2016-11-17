using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace NewtonVR
{
    public class NVRInteractableItem : NVRInteractable
    {
        private const float MaxVelocityChange = 10f;
        private const float MaxAngularVelocityChange = 20f;
        private const float VelocityMagic = 6000f;
        private const float AngularVelocityMagic = 50f;

        [Tooltip("If you have a specific point you'd like the object held at, create a transform there and set it to this variable")]
        public Transform InteractionPoint;

        public UnityEvent OnUseButtonDown;
        public UnityEvent OnUseButtonUp;

        public UnityEvent OnBeginInteraction;
        public UnityEvent OnEndInteraction;

        protected Transform PickupTransform;

        protected Vector3 VelocityToAdd;
        protected Vector3 AngularVelocityToAdd;

        protected Vector3?[] VelocityHistory;
        protected Vector3?[] AngularVelocityHistory;
        protected int CurrentVelocityHistoryStep = 0;

        protected override void Awake()
        {
            base.Awake();

            this.Rigidbody.maxAngularVelocity = 100f;
        }

        protected override void Start()
        {
            base.Start();
        }

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                CheckForDrop();

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
                    if (float.IsNaN(AngularTarget.x) == false)
                    {
                        AngularTarget = (AngularTarget * AngularVelocityMagic) * Time.fixedDeltaTime;
                        this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, AngularTarget, MaxAngularVelocityChange);
                    }
                }

                Vector3 VelocityTarget = (PositionDelta * VelocityMagic) * Time.fixedDeltaTime;
                if (float.IsNaN(VelocityTarget.x) == false)
                {
                    this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, VelocityTarget, MaxVelocityChange);
                }

                AddExternalVelocities();

                if (VelocityHistory != null)
                {
                    CurrentVelocityHistoryStep++;
                    if (CurrentVelocityHistoryStep >= VelocityHistory.Length)
                    {
                        CurrentVelocityHistoryStep = 0;
                    }

                    VelocityHistory[CurrentVelocityHistoryStep] = this.Rigidbody.velocity;
                    AngularVelocityHistory[CurrentVelocityHistoryStep] = this.Rigidbody.angularVelocity;
                }
            }
        }

        protected virtual void AddExternalVelocities()
        {
            if (VelocityToAdd != Vector3.zero)
            {
                this.Rigidbody.velocity += VelocityToAdd;
                VelocityToAdd = Vector3.zero;
            }

            if (AngularVelocityToAdd != Vector3.zero)
            {
                this.Rigidbody.angularVelocity += AngularVelocityToAdd;
                AngularVelocityToAdd = Vector3.zero;
            }
        }

        public override void AddVelocity(Vector3 velocity)
        {
            VelocityToAdd += velocity;
        }

        public override void AddAngularVelocity(Vector3 angularVelocity)
        {
            AngularVelocityToAdd += angularVelocity;
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            PickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", this.gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;


            if (hand.Player.VelocityHistorySteps > 0)
            {
                VelocityHistory = new Vector3?[hand.Player.VelocityHistorySteps];
                AngularVelocityHistory = new Vector3?[hand.Player.VelocityHistorySteps];
            }

            if (OnBeginInteraction != null)
            {
                OnBeginInteraction.Invoke();
            }
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

                CurrentVelocityHistoryStep = 0;

                for (int index = 0; index < VelocityHistory.Length; index++)
                {
                    VelocityHistory[index] = null;
                    AngularVelocityHistory[index] = null;
                }
            }

            if (OnEndInteraction != null)
            {
                OnEndInteraction.Invoke();
            }
        }

        public override void Reset()
        {
            EndInteraction();
            base.Reset();
        }

        public override void UseButtonDown()
        {
            base.UseButtonDown();

            if (OnUseButtonDown != null)
            {
                OnUseButtonDown.Invoke();
            }
        }

        public override void UseButtonUp()
        {
            base.UseButtonUp();

            if (OnUseButtonUp != null)
            {
                OnUseButtonUp.Invoke();
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