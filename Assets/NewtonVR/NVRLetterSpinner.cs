using UnityEngine;
using System.Collections;


namespace NewtonVR
{
    public class NVRLetterSpinner : NVRInteractableRotator
    {
        private static string LETTERLIST = "ABCDEFGHIJKLMNOPQRSTUVWXYZ?";

        private float SnapDistance = 1f;
        private float RungAngleInterval;

        private Vector3 LastAngularVelocity = Vector3.zero;

        protected override void Awake()
        {
            base.Awake();

            RungAngleInterval = 360f / (float)LETTERLIST.Length;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAttached == false)
            {
                float wheelAngle = this.transform.localEulerAngles.z;

                float rung = Mathf.RoundToInt(wheelAngle / RungAngleInterval);

                float distanceToRung = wheelAngle - (rung * RungAngleInterval);
                float distanceToRungAbs = Mathf.Abs(distanceToRung);

                float velocity = Mathf.Abs(this.Rigidbody.angularVelocity.z);

                if (velocity > 0.001f && velocity < 0.5f)
                {
                    if (distanceToRungAbs > SnapDistance)
                    {
                        this.Rigidbody.angularVelocity = LastAngularVelocity;
                    }
                    else
                    {
                        this.Rigidbody.velocity = Vector3.zero;
                        this.Rigidbody.angularVelocity = Vector3.zero;

                        Vector3 newRotation = this.transform.localEulerAngles;
                        newRotation.z = rung * RungAngleInterval;
                        this.transform.localEulerAngles = newRotation;

                        this.Rigidbody.isKinematic = true;
                    }
                }
            }

            LastAngularVelocity = this.Rigidbody.angularVelocity;
        }

        public override void BeginInteraction(NVRHand hand)
        {
            this.Rigidbody.isKinematic = false;

            base.BeginInteraction(hand);
        }

        public string GetLetter()
        {
            int closest = Mathf.RoundToInt(this.transform.localEulerAngles.z / RungAngleInterval);
            if (this.transform.localEulerAngles.z < 0.3)
                closest = LETTERLIST.Length - closest;

            if (closest == 27) //hack
                closest = 0;
            if (closest == -1)
                closest = 26;

            string character = LETTERLIST.Substring(closest, 1);

            return character;
        }
    }
}