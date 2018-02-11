using UnityEngine;

namespace NewtonVR
{
    public class NVRButton : MonoBehaviour
    {
        public Rigidbody Rigidbody;

        [Tooltip("The direction of button movement")]
        public ButtonDirections ButtonDirection = ButtonDirections.y;

        [Tooltip("The limit direction of button movement")]
        public ButtonLimitDirections ButtonLimitDirection = ButtonLimitDirections.None;

        [Tooltip("The (worldspace) distance from the initial position you have to push the button for it to register as pushed")]
        public float DistanceToEngage = 0.075f;

        [Tooltip("Maximum button distance from start position (based on axis selected on ButtonDirection variable)")]
        public float MaxDistance;

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
        private float RealMaxDistance;

        private Vector3 InitialLocalPosition;
        private Vector3 ConstrainedPosition;

        private Quaternion InitialLocalRotation;
        private Quaternion ConstrainedRotation;

        private void Awake()
        {
            InitialPosition = new GameObject(string.Format("[{0}] Initial Position", gameObject.name)).transform;
            InitialPosition.parent = transform.parent;
            InitialPosition.localPosition = transform.localPosition;
            InitialPosition.localRotation = transform.localRotation;

            if (Rigidbody == null)
                Rigidbody = GetComponent<Rigidbody>();

            if (Rigidbody == null)
            {
                Debug.LogError("There is no rigidbody attached to this button.");
            }

            InitialLocalPosition = transform.localPosition;
            ConstrainedPosition = InitialLocalPosition;

            InitialLocalRotation = transform.localRotation;
            ConstrainedRotation = InitialLocalRotation;
        }

        private void FixedUpdate()
        {
            ConstrainPosition();

            CurrentDistance = Vector3.Distance(transform.position, InitialPosition.position);

            Vector3 PositionDelta = InitialPosition.position - transform.position;
            Rigidbody.velocity = PositionDelta * PositionMagic * Time.deltaTime;
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

        private void ConstrainPosition()
        {
            // Calculates Real Max Distance

            RealMaxDistance =
                MaxDistance == 0
                    ? Mathf.Infinity
                    : Mathf.Abs(MaxDistance);

            if (ButtonLimitDirection == ButtonLimitDirections.Negative)
                RealMaxDistance *= -1f;

            switch (ButtonDirection)
            {
                case ButtonDirections.x:
                    RealMaxDistance = InitialLocalPosition.x + RealMaxDistance;
                    break;

                case ButtonDirections.y:
                    RealMaxDistance = InitialLocalPosition.y + RealMaxDistance;
                    break;

                case ButtonDirections.z:
                    RealMaxDistance = InitialLocalPosition.z + RealMaxDistance;
                    break;

                default:
                    break;
            }

            // Check which direction the button will move to and update the constrained position
            switch (ButtonDirection)
            {
                case ButtonDirections.x:
                    if (ValidateButtonMovement(transform.localPosition.x, InitialLocalPosition.x))
                    {
                        if (ButtonLimitDirection == ButtonLimitDirections.None)
                        {
                            ConstrainedPosition.x =
                                Mathf.Clamp(
                                    transform.localPosition.x,
                                    -RealMaxDistance,
                                    RealMaxDistance
                                );
                        }
                        else
                        {
                            ConstrainedPosition.x =
                                Mathf.Clamp(
                                    transform.localPosition.x,
                                    Mathf.Min(InitialLocalPosition.x, RealMaxDistance),
                                    Mathf.Max(InitialLocalPosition.x, RealMaxDistance)
                                );
                        }
                    }

                    break;

                case ButtonDirections.y:
                    if (ValidateButtonMovement(transform.localPosition.y, InitialLocalPosition.y))
                    {
                        if (ButtonLimitDirection == ButtonLimitDirections.None)
                        {
                            ConstrainedPosition.y =
                                Mathf.Clamp(
                                    transform.localPosition.y,
                                    -RealMaxDistance,
                                    RealMaxDistance
                                );
                        }
                        else
                        {
                            ConstrainedPosition.y =
                            Mathf.Clamp(
                                transform.localPosition.y,
                                Mathf.Min(InitialLocalPosition.y, RealMaxDistance),
                                Mathf.Max(InitialLocalPosition.y, RealMaxDistance)
                            );
                        }
                    }

                    break;

                case ButtonDirections.z:
                    if (ValidateButtonMovement(transform.localPosition.z, InitialLocalPosition.z))
                    {
                        if (ButtonLimitDirection == ButtonLimitDirections.None)
                        {
                            ConstrainedPosition.z =
                                Mathf.Clamp(
                                    transform.localPosition.z,
                                    -RealMaxDistance,
                                    RealMaxDistance
                                );
                        }
                        else
                        {
                            ConstrainedPosition.z =
                            Mathf.Clamp(
                                transform.localPosition.z,
                                Mathf.Min(InitialLocalPosition.z, RealMaxDistance),
                                Mathf.Max(InitialLocalPosition.z, RealMaxDistance)
                            );
                        }
                    }

                    break;

                default:
                    break;
            }

            transform.localPosition = ConstrainedPosition;
            transform.localRotation = ConstrainedRotation;
        }

        // Validates if button can move on positive or negative axis
        private bool ValidateButtonMovement(float currentValue, float initialValue)
        {
            float difference = currentValue - initialValue;

            switch (ButtonLimitDirection)
            {
                case ButtonLimitDirections.None:
                    break;

                case ButtonLimitDirections.Positive:
                    return difference > 0;

                case ButtonLimitDirections.Negative:
                    return difference < 0;

                default:
                    break;
            }

            return true;
        }

        private void LateUpdate()
        {
            ConstrainPosition();
        }

        public enum ButtonDirections
        {
            x,
            y,
            z
        }

        public enum ButtonLimitDirections
        {
            None,
            Positive,
            Negative
        }
    }
}
