﻿using UnityEngine;

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

        private Vector3 InitialLocalPosition;
        private Vector3 ConstrainedPosition;

        private Quaternion InitialLocalRotation;
        private Quaternion ConstrainedRotation;

        private void Awake()
        {
            InitialPosition = new GameObject(string.Format("[{0}] Initial Position", gameObject.name)).transform;
            InitialPosition.parent = transform.parent;
            InitialPosition.localPosition = Vector3.zero;
            InitialPosition.localRotation = Quaternion.identity;

            if (Rigidbody == null)
                Rigidbody = GetComponent<Rigidbody>();

            if (Rigidbody == null)
            {
                Debug.LogError("There is no rigidbody attached to this button.");
            }

            InitialLocalPosition = transform.localPosition;
            ConstrainedPosition = InitialLocalPosition;

            InitialLocalRotation = transform.localRotation;
            ConstrainedRotation = transform.localRotation;
        }

        private void FixedUpdate()
        {
            ConstrainPosition();

            CurrentDistance = Vector3.Distance(transform.position, InitialPosition.position);

            Vector3 PositionDelta = InitialPosition.position - transform.position;
            Rigidbody.velocity = PositionDelta * PositionMagic * Time.fixedDeltaTime;
        }

        private void Update()
        {
            ButtonWasPushed = ButtonIsPushed;
            ButtonIsPushed = CurrentDistance > DistanceToEngage;

            if (ButtonWasPushed == false && ButtonIsPushed)
                ButtonDown = true;
            else
                ButtonDown = false;

            if (ButtonWasPushed && ButtonIsPushed == false)
                ButtonUp = true;
            else
                ButtonUp = false;
        }

        private void ConstrainPosition()
        {
            ConstrainedPosition.y = transform.localPosition.y;
            transform.localPosition = ConstrainedPosition;
            transform.localRotation = ConstrainedRotation;
        }

        private void LateUpdate()
        {
            ConstrainPosition();
        }
    }
}