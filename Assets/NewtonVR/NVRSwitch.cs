using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRSwitch : MonoBehaviour
    {
        public bool CurrentState = true;
        public bool LastState = true;
        private bool FixedState = true;

        public Transform OnButton;
        public Renderer OnButtonRenderer;

        public Transform OffButton;
        public Renderer OffButtonRenderer;

        private Rigidbody Rigidbody;
        private float ForceMagic = 100f;

        private void Awake()
        {
            Rigidbody = this.GetComponent<Rigidbody>();
            SetRotation(CurrentState);
        }

        private void FixedUpdate()
        {
            float angle = this.transform.localEulerAngles.z;
            if (angle > 180)
                angle -= 360;

            if (angle > -7.5f)
            {
                if (angle < -0.2f)
                {
                    Rigidbody.AddForceAtPosition(-this.transform.right * ForceMagic, OnButton.position);
                }
                else if ((angle > -0.2f && angle < -0.1f) || angle > 0.1f)
                {
                    SetRotation(true);
                }
            }
            else if (angle < -7.5f)
            {
                if (angle > -14.8f)
                {
                    Rigidbody.AddForceAtPosition(-this.transform.right * ForceMagic, OffButton.position);
                }
                else if ((angle < -14.8f && angle > -14.9f) || angle < -15.1)
                {
                    SetRotation(false);
                }
            }
        }

        private void Update()
        {
            LastState = CurrentState;
            CurrentState = FixedState;
        }

        private void SetRotation(bool forState)
        {
            FixedState = forState;
            if (FixedState == true)
            {
                this.transform.localEulerAngles = Vector3.zero;
                OnButtonRenderer.material.color = Color.yellow;
                OffButtonRenderer.material.color = Color.white;
            }
            else
            {
                this.transform.localEulerAngles = new Vector3(0, 0, -15);
                OnButtonRenderer.material.color = Color.white;
                OffButtonRenderer.material.color = Color.red;
            }

            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.velocity = Vector3.zero;
        }
    }
}