using UnityEngine;

namespace NewtonVR
{
    public class NVRInteractableRotator : NVRInteractable
    {
        public float CurrentAngle;

        protected virtual float DeltaMagic { get { return 1f; } }
        protected Transform InitialAttachPoint;

        protected override void Awake()
        {
            base.Awake();
            Rigidbody.maxAngularVelocity = 100f;
        }

        public override void OnNewPosesApplied()
        {
            base.OnNewPosesApplied();

            if (IsAttached)
            {
                Vector3 PositionDelta = (AttachedHand.transform.position - InitialAttachPoint.position) * DeltaMagic;

                Rigidbody.AddForceAtPosition(PositionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);
            }

            CurrentAngle = transform.localEulerAngles.z;
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", gameObject.name)).transform;
            //InitialAttachPoint = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            InitialAttachPoint.position = hand.transform.position;
            InitialAttachPoint.rotation = hand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.parent = transform;

            ClosestHeldPoint = (InitialAttachPoint.position - transform.position);
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (InitialAttachPoint != null)
                Destroy(InitialAttachPoint.gameObject);
        }

        protected override void DropIfTooFar()
        {
            float distance = Vector3.Distance(AttachedHand.transform.position, (transform.position + ClosestHeldPoint));
            if (distance > DropDistance)
            {
                DroppedBecauseOfDistance();
            }
        }

    }
}