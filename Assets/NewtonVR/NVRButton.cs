using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRButton : MonoBehaviour
    {
        public Rigidbody Rigidbody;

        [Tooltip("The (worldspace) distance from the initial position you have to push the button for it to register as pushed")]
        public float DistanceToEngage = 0.075f;

        [Tooltip("Is set to true when the button has been pressed down this update frame")]
        public bool ButtonDown = false;

        [Tooltip("Is set to true when the button has been released from the down position this update frame")]
        public bool ButtonUp = false;

        [Tooltip("Is set to true each frame the button is pressed down")]
        public bool ButtonIsPushed = false;

        [Tooltip("Is set to true if the button was in a pushed state last frame")]
        public bool ButtonWasPushed = false;

        protected Transform InitialPosition;
        protected float MinDistance = 0.001f;

        protected float PositionMagic = 1000f;

        protected float CurrentDistance = -1;

        private void Awake()
        {
            InitialPosition = new GameObject(string.Format("[{0}] Initial Position", this.gameObject.name)).transform;
            InitialPosition.parent = this.transform.parent;
            InitialPosition.localPosition = Vector3.zero;
            InitialPosition.localRotation = Quaternion.identity;

            if (Rigidbody == null)
                Rigidbody = this.GetComponent<Rigidbody>();

            if (Rigidbody == null)
            {
                Debug.LogError("There is no rigidbody attached to this button.");
            }
        }

        private void FixedUpdate()
        {
            CurrentDistance = Vector3.Distance(this.transform.position, InitialPosition.position);

            if (CurrentDistance > MinDistance)
            {
                Vector3 PositionDelta = InitialPosition.position - this.transform.position;
                this.Rigidbody.velocity = PositionDelta * PositionMagic * Time.fixedDeltaTime;
            }
        }

        private void Update()
        {
            ButtonWasPushed = ButtonIsPushed;
            ButtonIsPushed = CurrentDistance > DistanceToEngage;

            if (ButtonWasPushed == false && ButtonIsPushed == true)
                ButtonDown = true;
            else
                ButtonDown = false;

            if (ButtonWasPushed == true && ButtonIsPushed == false)
                ButtonUp = true;
            else
                ButtonUp = false;
        }
    }
}