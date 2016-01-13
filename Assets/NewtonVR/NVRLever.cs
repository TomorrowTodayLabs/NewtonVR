using UnityEngine;
using System.Collections;
using System;

namespace NewtonVR
{
    public class NVRLever : NVRInteractable
    {
        public float LastValue;
        public float CurrentValue;
        public LeverPosition LastLeverPosition;
        public LeverPosition CurrentLeverPosition;
        public bool LeverEngaged = false;
        public float EngageWaitTime = 1f;

        protected virtual float DeltaMagic { get { return 2f; } }
        protected Transform InitialAttachPoint;
        protected HingeJoint HingeJoint;

        protected bool UseMotor;
        protected Quaternion Max, Mid, Min;
        protected float AngleRange;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;

            if (HingeJoint == null)
            {
                HingeJoint = Rigidbody.gameObject.GetComponent<HingeJoint>();
            }

            Mid = HingeJoint.transform.localRotation;
            Max = Mid * Quaternion.AngleAxis(HingeJoint.limits.max, HingeJoint.axis);
            Min = Mid * Quaternion.AngleAxis(HingeJoint.limits.min, HingeJoint.axis);
            UseMotor = this.HingeJoint.useMotor;

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
                Vector3 PositionDelta = (AttachedHand.transform.position - InitialAttachPoint.position) * DeltaMagic;

                this.Rigidbody.AddForceAtPosition(PositionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);
            }
        }

        protected override void Update()
        {
            base.Update();

            LeverEngaged = false;
            LastValue = CurrentValue;
            LastLeverPosition = CurrentLeverPosition;

            CurrentValue = GetValue();
            CurrentLeverPosition = GetPosition();

            if (LastLeverPosition != LeverPosition.On && CurrentLeverPosition == LeverPosition.On)
            {
                LeverEngaged = true;
                Engage();
            }
        }

        protected virtual void Engage()
        {
            if (AttachedHand != null)
                AttachedHand.EndInteraction(this);

            CanAttach = false;

            StartCoroutine(HoldPosition(EngageWaitTime));
        }

        private IEnumerator HoldPosition(float time)
        {
            HingeJoint.useMotor = false;

            yield return new WaitForSeconds(time);

            HingeJoint.useMotor = true;
            CanAttach = true;
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", this.gameObject.name)).transform;
            InitialAttachPoint.position = hand.transform.position;
            InitialAttachPoint.rotation = hand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.parent = this.transform;
            
            HingeJoint.useMotor = false;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            HingeJoint.useMotor = true;

            if (InitialAttachPoint != null)
                Destroy(InitialAttachPoint.gameObject);
        }

        private float GetValue()
        {
            float m_diff = 0.0f;
            if (HingeJoint.useLimits)
            {
                m_diff = HingeJoint.angle - HingeJoint.limits.min;
            }
            return 1 - (m_diff / AngleRange);
        }

        private LeverPosition GetPosition()
        {
            if (CurrentValue <= 0.05)
                return LeverPosition.Off;
            else if (CurrentValue >= 0.95)
                return LeverPosition.On;

            return LeverPosition.Mid;
        }

        public enum LeverPosition
        {
            Off,
            Mid,
            On
        }

        public enum RotationAxis
        {
            XAxis,
            YAxis,
            ZAxis
        }
    }
}
