using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

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

        protected Dictionary<NVRHand, Transform> PickupTransforms = new Dictionary<NVRHand, Transform>();

        protected Vector3 ExternalVelocity;
        protected Vector3 ExternalAngularVelocity;

        protected Vector3?[] VelocityHistory;
        protected Vector3?[] AngularVelocityHistory;
        protected int CurrentVelocityHistoryStep = 0;

        protected float StartingDrag = -1;
        protected float StartingAngularDrag = -1;

        protected Dictionary<Collider, PhysicMaterial> MaterialCache = new Dictionary<Collider, PhysicMaterial>();

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

        protected virtual void FixedUpdate()
        {
            if (IsAttached == true)
            {
                bool dropped = CheckForDrop();

                if (dropped == false)
                {
                    UpdateVelocities();
                }
            }

            AddExternalVelocities();
        }

        protected virtual void GetTargetValues(out Vector3 targetHandPosition, out Quaternion targetHandRotation, out Vector3 targetItemPosition, out Quaternion targetItemRotation)
        {
            if (AttachedHands.Count == 1) //faster path if only one hand, which is the standard scenario
            {
                NVRHand hand = AttachedHands[0];

                if (InteractionPoint != null)
                {
                    targetItemPosition = InteractionPoint.position;
                    targetItemRotation = InteractionPoint.rotation;

                    targetHandPosition = hand.transform.position;
                    targetHandRotation = hand.transform.rotation;
                }
                else
                {
                    targetItemPosition = this.transform.position;
                    targetItemRotation = this.transform.rotation;

                    targetHandPosition = PickupTransforms[hand].position;
                    targetHandRotation = PickupTransforms[hand].rotation;
                }
            }
            else
            {
                Vector3 cumulativeItemVector = Vector3.zero;
                Vector4 cumulativeItemRotation = Vector4.zero;
                Quaternion? firstItemRotation = null;
                targetItemRotation = Quaternion.identity;

                Vector3 cumulativeHandVector = Vector3.zero;
                Vector4 cumulativeHandRotation = Vector4.zero;
                Quaternion? firstHandRotation = null;
                targetHandRotation = Quaternion.identity;

                for (int handIndex = 0; handIndex < AttachedHands.Count; handIndex++)
                {
                    NVRHand hand = AttachedHands[handIndex];

                    if (InteractionPoint != null && handIndex == 0)
                    {
                        targetItemRotation = InteractionPoint.rotation;
                        cumulativeItemVector += InteractionPoint.position;

                        targetHandRotation = hand.transform.rotation;
                        cumulativeHandVector += hand.transform.position;
                    }
                    else
                    {
                        targetItemRotation = this.transform.rotation;
                        cumulativeItemVector += this.transform.position;

                        targetHandRotation = PickupTransforms[hand].rotation;
                        cumulativeHandVector += PickupTransforms[hand].position;
                    }

                    if (firstItemRotation == null)
                    {
                        firstItemRotation = targetItemRotation;
                    }
                    if (firstHandRotation == null)
                    {
                        firstHandRotation = targetHandRotation;
                    }

                    targetItemRotation = NVRHelpers.AverageQuaternion(ref cumulativeItemRotation, targetItemRotation, firstItemRotation.Value, handIndex);
                    targetHandRotation = NVRHelpers.AverageQuaternion(ref cumulativeHandRotation, targetHandRotation, firstHandRotation.Value, handIndex);
                }

                targetItemPosition = cumulativeItemVector / AttachedHands.Count;
                targetHandPosition = cumulativeHandVector / AttachedHands.Count;
            }
        }

        protected virtual void UpdateVelocities()
        {
            Vector3 targetItemPosition;
            Quaternion targetItemRotation;

            Vector3 targetHandPosition;
            Quaternion targetHandRotation;

            GetTargetValues(out targetHandPosition, out targetHandRotation, out targetItemPosition, out targetItemRotation);


            float velocityMagic = VelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);
            float angularVelocityMagic = AngularVelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);

            Vector3 positionDelta;
            Quaternion rotationDelta;

            float angle;
            Vector3 axis;

            positionDelta = (targetHandPosition - targetItemPosition);
            rotationDelta = targetHandRotation * Quaternion.Inverse(targetItemRotation);


            Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.deltaTime;
            if (float.IsNaN(velocityTarget.x) == false)
            {
                this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
            }


            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0)
            {
                Vector3 angularTarget = angle * axis;
                if (float.IsNaN(angularTarget.x) == false)
                {
                    angularTarget = (angularTarget * angularVelocityMagic) * Time.deltaTime;
                    this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
                }
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

            DisablePhysicalMaterials();

            Transform pickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", this.gameObject.name)).transform;
            pickupTransform.parent = hand.transform;
            pickupTransform.position = this.transform.position;
            pickupTransform.rotation = this.transform.rotation;
            PickupTransforms.Add(hand, pickupTransform);

            ResetVelocityHistory();

            if (OnBeginInteraction != null)
            {
                OnBeginInteraction.Invoke();
            }
        }

        public override void EndInteraction(NVRHand hand)
        {
            base.EndInteraction(hand);

            if (hand == null)
            {
                var pickupTransformsEnumerator = PickupTransforms.GetEnumerator();
                while (pickupTransformsEnumerator.MoveNext())
                {
                    var pickupTransform = pickupTransformsEnumerator.Current;
                    if (pickupTransform.Value != null)
                    {
                        Destroy(pickupTransform.Value.gameObject);
                    }
                }

                PickupTransforms.Clear();
            }
            else if (PickupTransforms.ContainsKey(hand))
            {
                Destroy(PickupTransforms[hand].gameObject);
                PickupTransforms.Remove(hand);
            }

            if (PickupTransforms.Count == 0)
            {
                Rigidbody.drag = StartingDrag;
                Rigidbody.angularDrag = StartingAngularDrag;

                EnablePhysicalMaterials();

                ApplyVelocityHistory();
                ResetVelocityHistory();

                if (OnEndInteraction != null)
                {
                    OnEndInteraction.Invoke();
                }
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

        public override void ResetInteractable()
        {
            EndInteraction(null);
            base.ResetInteractable();
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

        protected virtual void ApplyVelocityHistory()
        {
            if (VelocityHistory != null)
            {
                Vector3? meanVelocity = GetMeanVector(VelocityHistory);
                if (meanVelocity != null)
                {
                    this.Rigidbody.velocity = meanVelocity.Value;
                }

                Vector3? meanAngularVelocity = GetMeanVector(AngularVelocityHistory);
                if (meanAngularVelocity != null)
                {
                    this.Rigidbody.angularVelocity = meanAngularVelocity.Value;
                }
            }
        }

        protected virtual void ResetVelocityHistory()
        {
            CurrentVelocityHistoryStep = 0;

            if (VelocityHistory != null && VelocityHistory.Length > 0)
            {
                VelocityHistory = new Vector3?[VelocityHistory.Length];
                AngularVelocityHistory = new Vector3?[VelocityHistory.Length];
            }
        }

        protected Vector3? GetMeanVector(Vector3?[] positions)
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

            if (count > 0)
            {
                return new Vector3(x / count, y / count, z / count);
            }

            return null;
        }

        protected void DisablePhysicalMaterials()
        {
            for (int colliderIndex = 0; colliderIndex < Colliders.Length; colliderIndex++)
            {
                if (Colliders[colliderIndex] == null)
                {
                    continue;
                }

                MaterialCache[Colliders[colliderIndex]] = Colliders[colliderIndex].sharedMaterial;
                Colliders[colliderIndex].sharedMaterial = null;
            }
        }

        protected void EnablePhysicalMaterials()
        {
            for (int colliderIndex = 0; colliderIndex < Colliders.Length; colliderIndex++)
            {
                if (Colliders[colliderIndex] == null)
                {
                    continue;
                }

                if (MaterialCache.ContainsKey(Colliders[colliderIndex]) == true)
                {
                    Colliders[colliderIndex].sharedMaterial = MaterialCache[Colliders[colliderIndex]];
                }
            }
        }

        public override void UpdateColliders()
        {
            base.UpdateColliders();

            for (int colliderIndex = 0; colliderIndex < Colliders.Length; colliderIndex++)
            {
                if (MaterialCache.ContainsKey(Colliders[colliderIndex]) == false)
                {
                    MaterialCache.Add(Colliders[colliderIndex], Colliders[colliderIndex].sharedMaterial);

                    if (IsAttached == true)
                    {
                        Colliders[colliderIndex].sharedMaterial = null;
                    }
                }
            }
        }
    }
}