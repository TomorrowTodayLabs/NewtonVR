using UnityEngine;

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
            Rigidbody.maxAngularVelocity = 100f;
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
            if (IsAttached && DoPhysicsStep)
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
                    RotationDelta = PickupTransform.rotation * Quaternion.Inverse(transform.rotation);
                    PositionDelta = (PickupTransform.position - transform.position);
                }

                RotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                if (angle != 0)
                {
                    Vector3 AngularTarget = angle * axis;
                    Rigidbody.angularVelocity = Vector3.MoveTowards(Rigidbody.angularVelocity, AngularTarget, 10f * (deltaPoses * 1000));
                }

                Vector3 VelocityTarget = PositionDelta / Time.fixedDeltaTime;
                Rigidbody.velocity = Vector3.MoveTowards(Rigidbody.velocity, VelocityTarget, 10f);

                if (VelocityHistory != null)
                {
                    VelocityHistoryStep++;
                    if (VelocityHistoryStep >= VelocityHistory.Length)
                    {
                        VelocityHistoryStep = 0;
                    }

                    VelocityHistory[VelocityHistoryStep] = Rigidbody.velocity;
                    AngularVelocityHistory[VelocityHistoryStep] = Rigidbody.angularVelocity;
                }
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            PickupTransform = new GameObject(string.Format("[{0}] NVRPickupTransform", gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = transform.position;
            PickupTransform.rotation = transform.rotation;

            ClosestHeldPoint = (PickupTransform.position - transform.position);
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
                Rigidbody.velocity = GetMeanVector(VelocityHistory);
                Rigidbody.angularVelocity = GetMeanVector(AngularVelocityHistory);

                VelocityHistoryStep = 0;

                VelocityHistory = VelocityHistory.Map<Vector3?, Vector3?>(_ => null);
                AngularVelocityHistory = AngularVelocityHistory.Map<Vector3?, Vector3?>(_ => null);
            }
        }

        protected override void DropIfTooFar()
        {
            float distance = Vector3.Distance(AttachedHand.transform.position, (transform.position + ClosestHeldPoint));
            if (distance > DropDistance)
            {
                DroppedBecauseOfDistance();
            }
        }

        private Vector3 GetMeanVector(Vector3?[] positions)
        {
            return positions.Filter(a => a.HasValue).Map(a => a.Value).Reduce((a, b) => a + b, Vector3.zero);
        }
    }
}