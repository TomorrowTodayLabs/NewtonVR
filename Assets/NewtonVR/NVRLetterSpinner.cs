using UnityEngine;


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

        public override void OnNewPosesApplied()
        {
            base.OnNewPosesApplied();

            if (IsAttached == false)
            {
                float wheelAngle = transform.localEulerAngles.z;

                float rung = Mathf.RoundToInt(wheelAngle / RungAngleInterval);

                float distanceToRung = wheelAngle - (rung * RungAngleInterval);
                float distanceToRungAbs = Mathf.Abs(distanceToRung);

                float velocity = Mathf.Abs(Rigidbody.angularVelocity.z);

                if (velocity > 0.001f && velocity < 0.5f)
                {
                    if (distanceToRungAbs > SnapDistance)
                    {
                        Rigidbody.angularVelocity = LastAngularVelocity;
                    }
                    else
                    {
                        Rigidbody.velocity = Vector3.zero;
                        Rigidbody.angularVelocity = Vector3.zero;

                        Vector3 newRotation = transform.localEulerAngles;
                        newRotation.z = rung * RungAngleInterval;
                        transform.localEulerAngles = newRotation;

                        Rigidbody.isKinematic = true;
                    }
                }
            }

            LastAngularVelocity = Rigidbody.angularVelocity;
        }

        public override void BeginInteraction(NVRHand hand)
        {
            Rigidbody.isKinematic = false;

            base.BeginInteraction(hand);
        }

        public string GetLetter()
        {
            int closest = Mathf.RoundToInt(transform.localEulerAngles.z / RungAngleInterval);
            if (transform.localEulerAngles.z < 0.3)
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