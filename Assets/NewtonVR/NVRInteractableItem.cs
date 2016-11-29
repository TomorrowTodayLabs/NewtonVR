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

        public UnityEvent OnHovering;

        public UnityEvent OnBeginInteraction;
        public UnityEvent OnEndInteraction;

        protected Transform PickupTransform;

        protected Vector3 ExternalVelocity;
        protected Vector3 ExternalAngularVelocity;

        protected Vector3?[] VelocityHistory;
        protected Vector3?[] AngularVelocityHistory;
        protected int CurrentVelocityHistoryStep = 0;

        protected float StartingDrag = -1;
        protected float StartingAngularDrag = -1;

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

                Quaternion rotationDelta;
                Vector3 positionDelta;

                float angle;
                Vector3 axis;

                if (InteractionPoint != null)
                {
                    rotationDelta = AttachedHand.transform.rotation * Quaternion.Inverse(InteractionPoint.rotation);
                    positionDelta = (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    rotationDelta = PickupTransform.rotation * Quaternion.Inverse(this.transform.rotation);
                    positionDelta = (PickupTransform.position - this.transform.position);
                }

                rotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                if (angle != 0)
                {
                    Vector3 angularTarget = angle * axis;
                    if (float.IsNaN(angularTarget.x) == false)
                    {
                        angularTarget = (angularTarget * AngularVelocityMagic) * Time.fixedDeltaTime;
                        this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
                    }
                }

                Vector3 velocityTarget = (positionDelta * VelocityMagic) * Time.fixedDeltaTime;
                if (float.IsNaN(velocityTarget.x) == false)
                {
                    this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
                }


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

            AddExternalVelocities();
        }

        protected virtual void AddExternalVelocities()
        {
            if (ExternalVelocity != Vector3.zero)
            {
                this.Rigidbody.velocity = Vector3.Lerp(this.Rigidbody.velocity, ExternalVelocity, 0.5f);
                ExternalVelocity = Vector3.zero;
            }

            if (ExternalAngularVelocity != Vector3.zero)
            {
                this.Rigidbody.angularVelocity = Vector3.Lerp(this.Rigidbody.angularVelocity, ExternalAngularVelocity, 0.5f);
                ExternalAngularVelocity = Vector3.zero;
            }
        }

        public override void AddExternalVelocity(Vector3 velocity)
        {
            if (ExternalVelocity == Vector3.zero)
            {
                ExternalVelocity = velocity;
            }
            else
            {
                ExternalVelocity = Vector3.Lerp(ExternalVelocity, velocity, 0.5f);
            }
        }

        public override void AddExternalAngularVelocity(Vector3 angularVelocity)
        {
            if (ExternalAngularVelocity == Vector3.zero)
            {
                ExternalAngularVelocity = angularVelocity;
            }
            else
            {
                ExternalAngularVelocity = Vector3.Lerp(ExternalAngularVelocity, angularVelocity, 0.5f);
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            StartingDrag = Rigidbody.drag;
            StartingAngularDrag = Rigidbody.angularDrag;
            Rigidbody.drag = 0;
            Rigidbody.angularDrag = 0.05f;

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

            Rigidbody.drag = StartingDrag;
            Rigidbody.angularDrag = StartingAngularDrag;

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

        public override void HoveringUpdate(NVRHand hand, float forTime)
        {
            base.HoveringUpdate(hand, forTime);

            if (OnHovering != null)
            {
                OnHovering.Invoke();
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