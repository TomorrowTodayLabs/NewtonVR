using UnityEngine;

namespace NewtonVR
{
    public class NVRSlider : NVRInteractable
    {
        [Tooltip("Set to zero when the slider is at StartPoint. Set to one when the slider is at EndPoint.")]
        public float CurrentValue = 0f;

        [Tooltip("A transform at the position of the zero point of the slider")]
        public Transform StartPoint;

        [Tooltip("A transform at the position of the one point of the slider")]
        public Transform EndPoint;
        
        protected float AttachedPositionMagic = 3000f;

        protected Transform PickupTransform;
        protected Vector3 SliderPath;

        protected override void Awake()
        {
            base.Awake();

            if (StartPoint == null)
            {
                Debug.LogError("This slider has no StartPoint.");
            }
            if (EndPoint == null)
            {
                Debug.LogError("This slider has no EndPoint.");
            }

            transform.position = Vector3.Lerp(StartPoint.position, EndPoint.position, CurrentValue);
            SliderPath = EndPoint.position - StartPoint.position;
        }

        public override void OnNewPosesApplied()
        {
            base.OnNewPosesApplied();

            if (IsAttached)
            {
                Vector3 PositionDelta = (PickupTransform.position - transform.position);

                Vector3 velocity = PositionDelta * AttachedPositionMagic * deltaPoses;
                Rigidbody.velocity = ProjectVelocityOnPath(velocity, SliderPath);
            }

            if (transform.hasChanged)
            {
                float totalDistance = Vector3.Distance(StartPoint.position, EndPoint.position);
                float distance = Vector3.Distance(StartPoint.position, transform.position);
                CurrentValue = distance / totalDistance;

                transform.hasChanged = false;
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            PickupTransform = new GameObject(string.Format("[{0}] PickupTransform", gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = transform.position;
            PickupTransform.rotation = transform.rotation;

            ClosestHeldPoint = (PickupTransform.position - transform.position);
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (PickupTransform != null)
                Destroy(PickupTransform.gameObject);
        }

        protected override void DropIfTooFar()
        {
            float distance = Vector3.Distance(AttachedHand.transform.position, (transform.position + ClosestHeldPoint));
            if (distance > DropDistance)
            {
                DroppedBecauseOfDistance();
            }
        }

        protected Vector3 ProjectVelocityOnPath(Vector3 velocity, Vector3 path)
        {
            return Vector3.Project(velocity, path);
        }
    }
}

