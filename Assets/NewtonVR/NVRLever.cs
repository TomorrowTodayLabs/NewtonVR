using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRLever : NVRInteractable
    {

        public enum LeverPosition { Max, Mid, Min };
        public enum RotationAxis { XAxis, YAxis, ZAxis}

        public float CurrentAngle;
        public float AngleRange;
        public float CurrentValue;
        [Tooltip("Will the lever snap to max, mid (if selected), and min positions. (Default = false)")]
        public bool StickyPositions = false;
        [Tooltip("Does the lever user the mid position? (Default = false)")]
        public bool ThreePosition = false;
        [Tooltip("Are the hinge setting set by this script? Max and Min hinge limits will be set by Max, Mid, and Min Rotations (Default = false)")]
        public bool SetHingeByScript = false;

        [Tooltip("Sets the hinge axis of rotation (Default = X Axis)")]
        public RotationAxis AxisOfRotation = RotationAxis.XAxis;
        [Tooltip("Sets the starting position of the lever (Default = Min")]
        public LeverPosition StartingPositionOfLever = LeverPosition.Min;

        private LeverPosition CurrentPositionOfLever;

        protected virtual float DeltaMagic { get { return 1f; } }
        protected Transform InitialAttachPoint;
        protected HingeJoint HingeJoint;

        public Transform MaxRotation;
        public Transform MidRotation;
        public Transform MinRotation;

        //private Vector3 Max, Mid, Min;
        private Quaternion Max, Mid, Min;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;

            CurrentPositionOfLever = StartingPositionOfLever;

            if (HingeJoint == null)
            {
                HingeJoint = Rigidbody.gameObject.GetComponent<HingeJoint>();
            }

            if (SetHingeByScript)
            {
                //Finish this later
                /*
                switch (AxisOfRotation)
                {
                    case RotationAxis.XAxis:
                        HingeJoint.axis = Vector3.right;
                        break;
                    case RotationAxis.YAxis:
                        HingeJoint.axis = Vector3.up;
                        break;
                    case RotationAxis.ZAxis:
                        HingeJoint.axis = Vector3.forward;
                        break;
                    default:
                        HingeJoint.axis = Vector3.right;
                        Debug.Log("Error, default RotationAxis set.");
                        break;
                }
                if ((MaxRotation != null) && (MinRotation != null))
                {
                    Max = MaxRotation.localRotation.eulerAngles;
                    Min = MinRotation.localRotation.eulerAngles;
                    if (ThreePosition)
                    {
                        Mid = MidRotation.localRotation.eulerAngles;
                    }
                    HingeJoint.useLimits = true;
                    //set limits
                }
                */
            }
            else
            {

                Mid = HingeJoint.transform.localRotation;
                Max = Mid * Quaternion.AngleAxis(HingeJoint.limits.max, HingeJoint.axis);
                Min = Mid * Quaternion.AngleAxis(HingeJoint.limits.min, HingeJoint.axis);
                Vector3 m_localAxis = HingeJoint.axis.normalized;
                //Max = new Vector3((Rigidbody.transform.localEulerAngles.x + HingeJoint.limits.max) * m_localAxis.x, (Rigidbody.transform.localEulerAngles.y + HingeJoint.limits.max) * m_localAxis.y, (Rigidbody.transform.localEulerAngles.z + HingeJoint.limits.max) * m_localAxis.z);
                //Min = new Vector3((Rigidbody.transform.localEulerAngles.x - HingeJoint.limits.min) * m_localAxis.x, (Rigidbody.transform.localEulerAngles.y - HingeJoint.limits.min) * m_localAxis.y, (Rigidbody.transform.localEulerAngles.z - HingeJoint.limits.min) * m_localAxis.z);
            }

            if (HingeJoint.useLimits)
            {
                AngleRange = (Mathf.Max(HingeJoint.limits.max, HingeJoint.limits.min) - Mathf.Min(HingeJoint.limits.max, HingeJoint.limits.min));
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAttached == true)
            {
                Vector3 PositionDelta = AttachedHand.transform.position - InitialAttachPoint.position * DeltaMagic;

                this.Rigidbody.AddForceAtPosition(PositionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);

            }
            CurrentValue = GetValue();
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            Vector3 closestPoint = Vector3.zero;
            float shortestDistance = float.MaxValue;
            for (int index = 0; index < Colliders.Length; index++)
            {
                Vector3 closest = Colliders[index].bounds.ClosestPoint(AttachedHand.transform.position);
                float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPoint = closest;
                }
            }

            InitialAttachPoint = new GameObject(string.Format("[{0}] PickupTransform", this.gameObject.name)).transform;
            //InitialAttachPoint = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            InitialAttachPoint.position = hand.transform.position;
            InitialAttachPoint.rotation = hand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.parent = this.transform;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (InitialAttachPoint != null)
                Destroy(InitialAttachPoint.gameObject);
        }

        private float GetValue()
        {
            float diff = 0.0f;
            if (HingeJoint.useLimits)
            {
                diff = HingeJoint.angle - HingeJoint.limits.min;
            }
            return diff / AngleRange;
        }
    }

}
