using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRHand : MonoBehaviour
    {
        public NVRButtons HoldButton = NVRButtons.Grip;
        public bool HoldButtonDown { get { return Inputs[HoldButton].PressDown; } }
        public bool HoldButtonUp { get { return Inputs[HoldButton].PressUp; } }
        public bool HoldButtonPressed { get { return Inputs[HoldButton].IsPressed; } }
        public float HoldButtonAxis { get { return Inputs[HoldButton].SingleAxis; } }

        public NVRButtons UseButton = NVRButtons.Trigger;
        public bool UseButtonDown { get { return Inputs[UseButton].PressDown; } }
        public bool UseButtonUp { get { return Inputs[UseButton].PressUp; } }
        public bool UseButtonPressed { get { return Inputs[UseButton].IsPressed; } }
        public float UseButtonAxis { get { return Inputs[UseButton].SingleAxis; } }

        [HideInInspector]
        public bool IsRight;
        [HideInInspector]
        public bool IsLeft;
        [HideInInspector]
        public NVRPlayer Player;

        public Dictionary<NVRButtons, NVRButtonInputs> Inputs;

        [HideInInspector]
        public InterationStyle CurrentInteractionStyle;

        public Rigidbody Rigidbody;

        [HideInInspector]
        public GameObject CustomModel;
        [HideInInspector]
        public GameObject CustomPhysicalColliders;

        private VisibilityLevel CurrentVisibility = VisibilityLevel.Visible;
        private bool VisibilityLocked = false;

        [HideInInspector]
        public HandState CurrentHandState = HandState.Uninitialized;

        private Dictionary<NVRInteractable, Dictionary<Collider, float>> CurrentlyHoveringOver;

        public NVRInteractable CurrentlyInteracting;

        [Serializable]
        public class NVRInteractableEvent : UnityEvent<NVRInteractable> { }

        public NVRInteractableEvent OnBeginInteraction = new NVRInteractableEvent();
        public NVRInteractableEvent OnEndInteraction = new NVRInteractableEvent();

        private int EstimationSampleIndex;
        private Vector3[] LastPositions;
        private Quaternion[] LastRotations;
        private float[] LastDeltas;
        private int EstimationSamples = 5;

        [HideInInspector]
        public NVRPhysicalController PhysicalController;

        private Collider[] GhostColliders;
        private Renderer[] GhostRenderers;

        private NVRInputDevice InputDevice;

        private GameObject RenderModel;

        public bool IsHovering
        {
            get
            {
                return CurrentlyHoveringOver.Any(kvp => kvp.Value.Count > 0);
            }
        }
        public bool IsInteracting
        {
            get
            {
                return CurrentlyInteracting != null;
            }
        }
        public bool HasCustomModel
        {
            get
            {
                return CustomModel != null;
            }
        }
        public bool IsCurrentlyTracked
        {
            get
            {
                if (InputDevice != null)
                {
                    return InputDevice.IsCurrentlyTracked;
                }

                return false;
            }
        }
        public Vector3 CurrentForward
        {
            get
            {
                if (PhysicalController != null && PhysicalController.State == true)
                {
                    return PhysicalController.PhysicalController.transform.forward;
                }
                else
                {
                    return this.transform.forward;
                }
            }
        }
        public Vector3 CurrentPosition
        {
            get
            {
                if (PhysicalController != null && PhysicalController.State == true)
                {
                    return PhysicalController.PhysicalController.transform.position;
                }
                else
                {
                    return this.transform.position;
                }
            }
        }


        public virtual void PreInitialize(NVRPlayer player)
        {
            Player = player;

            IsRight = Player.RightHand == this;
            IsLeft = Player.LeftHand == this;

            CurrentInteractionStyle = Player.InteractionStyle;

            CurrentlyHoveringOver = new Dictionary<NVRInteractable, Dictionary<Collider, float>>();

            LastPositions = new Vector3[EstimationSamples];
            LastRotations = new Quaternion[EstimationSamples];
            LastDeltas = new float[EstimationSamples];
            EstimationSampleIndex = 0;

            VisibilityLocked = false;

            Inputs = new Dictionary<NVRButtons, NVRButtonInputs>(new NVRButtonsComparer());
            for (int buttonIndex = 0; buttonIndex < NVRButtonsHelper.Array.Length; buttonIndex++)
            {
                if (Inputs.ContainsKey(NVRButtonsHelper.Array[buttonIndex]) == false)
                {
                    Inputs.Add(NVRButtonsHelper.Array[buttonIndex], new NVRButtonInputs());
                }
            }

            if (Player.CurrentIntegrationType == NVRSDKIntegrations.Oculus)
            {
                InputDevice = this.gameObject.AddComponent<NVROculusInputDevice>();

                if (Player.OverrideOculus == true)
                {
                    if (IsLeft)
                    {
                        CustomModel = Player.OverrideOculusLeftHand;
                        CustomPhysicalColliders = Player.OverrideOculusLeftHandPhysicalColliders;
                    }
                    else if (IsRight)
                    {
                        CustomModel = Player.OverrideOculusRightHand;
                        CustomPhysicalColliders = Player.OverrideOculusRightHandPhysicalColliders;
                    }
                    else
                    {
                        Debug.LogError("[NewtonVR] Error: Unknown hand for oculus model override.");
                    }
                }
            }
            else if (Player.CurrentIntegrationType == NVRSDKIntegrations.SteamVR)
            {
                InputDevice = this.gameObject.AddComponent<NVRSteamVRInputDevice>();

                if (Player.OverrideSteamVR == true)
                {
                    if (IsLeft)
                    {
                        CustomModel = Player.OverrideSteamVRLeftHand;
                        CustomPhysicalColliders = Player.OverrideSteamVRLeftHandPhysicalColliders;
                    }
                    else if (IsRight)
                    {
                        CustomModel = Player.OverrideSteamVRRightHand;
                        CustomPhysicalColliders = Player.OverrideSteamVRRightHandPhysicalColliders;
                    }
                    else
                    {
                        Debug.LogError("[NewtonVR] Error: Unknown hand for SteamVR model override.");
                    }
                }
            }
            else
            {
                //Debug.LogError("[NewtonVR] Critical Error: NVRPlayer.CurrentIntegration not setup.");
                return;
            }

            if (Player.OverrideAll)
            {

                if (IsLeft)
                {
                    CustomModel = Player.OverrideAllLeftHand;
                    CustomPhysicalColliders = Player.OverrideAllLeftHandPhysicalColliders;
                }
                else if (IsRight)
                {
                    CustomModel = Player.OverrideAllRightHand;
                    CustomPhysicalColliders = Player.OverrideAllRightHandPhysicalColliders;
                }
                else
                {
                    Debug.LogError("[NewtonVR] Error: Unknown hand for SteamVR model override.");
                    return;
                }
            }


            InputDevice.Initialize(this);
            InitializeRenderModel();
        }

        protected virtual void Update()
        {
            if (CurrentHandState == HandState.Uninitialized)
            {
                if (InputDevice == null || InputDevice.ReadyToInitialize() == false)
                {
                    return;
                }
                else
                {
                    Initialize();
                    return;
                }
            }

            UpdateButtonStates();

            UpdateInteractions();

            UpdateHovering();

            UpdateVisibilityAndColliders();
        }

        protected void UpdateHovering()
        {
            if (CurrentHandState == HandState.Idle)
            {
                var hoveringEnumerator = CurrentlyHoveringOver.GetEnumerator();
                while (hoveringEnumerator.MoveNext())
                {
                    var hoveringOver = hoveringEnumerator.Current;
                    if (hoveringOver.Value.Count > 0)
                    {
                        hoveringOver.Key.HoveringUpdate(this, Time.time - hoveringOver.Value.OrderBy(colliderTime => colliderTime.Value).First().Value);
                    }
                }
            }
        }

        protected void UpdateButtonStates()
        {
            for (int index = 0; index < NVRButtonsHelper.Array.Length; index++)
            {
                NVRButtons nvrbutton = NVRButtonsHelper.Array[index];
                NVRButtonInputs button = Inputs[nvrbutton];
                button.FrameReset(InputDevice, nvrbutton);
            }
        }

        protected void UpdateInteractions()
        {
            if (CurrentInteractionStyle == InterationStyle.Hold)
            {
                if (HoldButtonUp == true)
                {
                    VisibilityLocked = false;
                }

                if (HoldButtonDown == true)
                {
                    if (CurrentlyInteracting == null)
                    {
                        PickupClosest();
                    }
                }
                else if (HoldButtonUp == true && CurrentlyInteracting != null)
                {
                    EndInteraction(null);
                }
            }
            else if (CurrentInteractionStyle == InterationStyle.Toggle)
            {
                if (HoldButtonDown == true)
                {
                    if (CurrentHandState == HandState.Idle)
                    {
                        PickupClosest();
                        if (IsInteracting)
                        {
                            CurrentHandState = HandState.GripToggleOnInteracting;
                        }
                        else if (Player.PhysicalHands == true)
                        {
                            CurrentHandState = HandState.GripToggleOnNotInteracting;
                        }
                    }
                    else if (CurrentHandState == HandState.GripToggleOnInteracting)
                    {
                        CurrentHandState = HandState.Idle;
                        VisibilityLocked = false;
                        EndInteraction(null);
                    }
                    else if (CurrentHandState == HandState.GripToggleOnNotInteracting)
                    {
                        CurrentHandState = HandState.Idle;
                        VisibilityLocked = false;
                    }
                }
            }
            else if (CurrentInteractionStyle == InterationStyle.ByScript)
            {
                //this is handled by user customized scripts.
            }

            if (IsInteracting == true)
            {
                CurrentlyInteracting.InteractingUpdate(this);
            }
        }

        private void UpdateVisibilityAndColliders()
        {
            if (Player.PhysicalHands == true)
            {
                if (CurrentInteractionStyle == InterationStyle.Hold)
                {
                    if (HoldButtonPressed == true && IsInteracting == false)
                    {
                        if (CurrentHandState != HandState.GripDownNotInteracting && VisibilityLocked == false)
                        {
                            VisibilityLocked = true;
                            SetVisibility(VisibilityLevel.Visible);
                            CurrentHandState = HandState.GripDownNotInteracting;
                        }
                    }
                    else if (HoldButtonDown == true && IsInteracting == true)
                    {
                        if (CurrentHandState != HandState.GripDownInteracting && VisibilityLocked == false)
                        {
                            VisibilityLocked = true;
                            if (Player.MakeControllerInvisibleOnInteraction == true)
                            {
                                SetVisibility(VisibilityLevel.Invisible);
                            }
                            else
                            {
                                SetVisibility(VisibilityLevel.Ghost);
                            }
                            CurrentHandState = HandState.GripDownInteracting;
                        }
                    }
                    else if (IsInteracting == false)
                    {
                        if (CurrentHandState != HandState.Idle && VisibilityLocked == false)
                        {
                            SetVisibility(VisibilityLevel.Ghost);
                            CurrentHandState = HandState.Idle;
                        }
                    }
                }
                else if (CurrentInteractionStyle == InterationStyle.Toggle)
                {
                    if (CurrentHandState == HandState.Idle)
                    {
                        if (VisibilityLocked == false && CurrentVisibility != VisibilityLevel.Ghost)
                        {
                            SetVisibility(VisibilityLevel.Ghost);
                        }
                        else
                        {
                            VisibilityLocked = false;
                        }

                    }
                    else if (CurrentHandState == HandState.GripToggleOnInteracting)
                    {
                        if (VisibilityLocked == false)
                        {
                            VisibilityLocked = true;
                            SetVisibility(VisibilityLevel.Ghost);
                        }
                    }
                    else if (CurrentHandState == HandState.GripToggleOnNotInteracting)
                    {
                        if (VisibilityLocked == false)
                        {
                            VisibilityLocked = true;
                            SetVisibility(VisibilityLevel.Visible);
                        }
                    }
                }
            }
            else if (Player.PhysicalHands == false && Player.MakeControllerInvisibleOnInteraction == true)
            {
                if (IsInteracting == true)
                {
                    SetVisibility(VisibilityLevel.Invisible);
                }
                else if (IsInteracting == false)
                {
                    SetVisibility(VisibilityLevel.Ghost);
                }
            }
        }

        public void TriggerHapticPulse(ushort durationMicroSec = 500, NVRButtons button = NVRButtons.Grip)
        {
            if (InputDevice != null)
            {
                if (durationMicroSec < 3000)
                {
                    InputDevice.TriggerHapticPulse(durationMicroSec, button);
                }
                else
                {
                    Debug.LogWarning("You're trying to pulse for over 3000 microseconds, you probably don't want to do that. If you do, use NVRHand.LongHapticPulse(float seconds)");
                }
            }
        }

        public void LongHapticPulse(float seconds, NVRButtons button = NVRButtons.Grip)
        {
            StartCoroutine(DoLongHapticPulse(seconds, button));
        }

        private IEnumerator DoLongHapticPulse(float seconds, NVRButtons button)
        {
            float startTime = Time.time;
            float endTime = startTime + seconds;
            while (Time.time < endTime)
            {
                TriggerHapticPulse(100, button);
                yield return null;
            }
        }

        public Vector3 GetVelocityEstimation()
        {
            float delta = LastDeltas.Sum();
            Vector3 distance = Vector3.zero;

            for (int index = 0; index < LastPositions.Length - 1; index++)
            {
                Vector3 diff = LastPositions[index + 1] - LastPositions[index];
                distance += diff;
            }

            return distance / delta;
        }

        public Vector3 GetAngularVelocityEstimation()
        {
            float delta = LastDeltas.Sum();
            float angleDegrees = 0.0f;
            Vector3 unitAxis = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            rotation = LastRotations[LastRotations.Length - 1] * Quaternion.Inverse(LastRotations[LastRotations.Length - 2]);

            //Error: the incorrect rotation is sometimes returned
            rotation.ToAngleAxis(out angleDegrees, out unitAxis);
            return unitAxis * ((angleDegrees * Mathf.Deg2Rad) / delta);
        }

        public Vector3 GetPositionDelta()
        {
            int last = EstimationSampleIndex - 1;
            int secondToLast = EstimationSampleIndex - 2;

            if (last < 0)
                last += EstimationSamples;
            if (secondToLast < 0)
                secondToLast += EstimationSamples;

            return LastPositions[last] - LastPositions[secondToLast];
        }

        public Quaternion GetRotationDelta()
        {
            int last = EstimationSampleIndex - 1;
            int secondToLast = EstimationSampleIndex - 2;

            if (last < 0)
                last += EstimationSamples;
            if (secondToLast < 0)
                secondToLast += EstimationSamples;

            return LastRotations[last] * Quaternion.Inverse(LastRotations[secondToLast]);
        }

        protected virtual void FixedUpdate()
        {
            if (CurrentHandState == HandState.Uninitialized)
            {
                return;
            }

            LastPositions[EstimationSampleIndex] = this.transform.position;
            LastRotations[EstimationSampleIndex] = this.transform.rotation;
            LastDeltas[EstimationSampleIndex] = Time.deltaTime;
            EstimationSampleIndex++;

            if (EstimationSampleIndex >= LastPositions.Length)
                EstimationSampleIndex = 0;

            if (InputDevice != null && IsInteracting == false && IsHovering == true)
            {
                if (Player.VibrateOnHover == true)
                {
                    InputDevice.TriggerHapticPulse(100);
                }
            }
        }

        public virtual void BeginInteraction(NVRInteractable interactable)
        {
            if (interactable.CanAttach == true)
            {
                if (interactable.AttachedHand != null)
                {
                    interactable.AttachedHand.EndInteraction(null);
                }

                CurrentlyInteracting = interactable;
                CurrentlyInteracting.BeginInteraction(this);

                if (OnBeginInteraction != null)
                {
                    OnBeginInteraction.Invoke(interactable);
                }
            }
        }

        public virtual void EndInteraction(NVRInteractable item)
        {
            if (item != null && CurrentlyHoveringOver.ContainsKey(item) == true)
                CurrentlyHoveringOver.Remove(item);

            if (CurrentlyInteracting != null)
            {
                CurrentlyInteracting.EndInteraction();

                if (OnEndInteraction != null)
                {
                    OnEndInteraction.Invoke(CurrentlyInteracting);
                }

                CurrentlyInteracting = null;
            }

            if (CurrentInteractionStyle == InterationStyle.Toggle)
            {
                if (CurrentHandState != HandState.Idle)
                {
                    CurrentHandState = HandState.Idle;
                }
            }
        }

        private bool PickupClosest()
        {
            NVRInteractable closest = null;
            float closestDistance = float.MaxValue;

            foreach (var hovering in CurrentlyHoveringOver)
            {
                if (hovering.Key == null)
                    continue;

                float distance = Vector3.Distance(this.transform.position, hovering.Key.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = hovering.Key;
                }
            }

            if (closest != null)
            {
                BeginInteraction(closest);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            NVRInteractable interactable = NVRInteractables.GetInteractable(collider);
            if (interactable == null || interactable.enabled == false)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == false)
                CurrentlyHoveringOver[interactable] = new Dictionary<Collider, float>();

            if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == false)
                CurrentlyHoveringOver[interactable][collider] = Time.time;
        }

        protected virtual void OnTriggerStay(Collider collider)
        {
            NVRInteractable interactable = NVRInteractables.GetInteractable(collider);
            if (interactable == null || interactable.enabled == false)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == false)
                CurrentlyHoveringOver[interactable] = new Dictionary<Collider, float>();

            if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == false)
                CurrentlyHoveringOver[interactable][collider] = Time.time;
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            NVRInteractable interactable = NVRInteractables.GetInteractable(collider);
            if (interactable == null)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == true)
            {
                if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == true)
                {
                    CurrentlyHoveringOver[interactable].Remove(collider);
                    if (CurrentlyHoveringOver[interactable].Count == 0)
                    {
                        CurrentlyHoveringOver.Remove(interactable);
                    }
                }
            }
        }

        public string GetDeviceName()
        {
            if (InputDevice != null)
                return InputDevice.GetDeviceName();
            else
                return null;
        }

        public Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
        {
            return InputDevice.SetupDefaultPhysicalColliders(ModelParent);
        }

        public void DeregisterInteractable(NVRInteractable interactable)
        {
            if (CurrentlyInteracting == interactable)
                CurrentlyInteracting = null;

            if (CurrentlyHoveringOver != null && CurrentlyHoveringOver.ContainsKey(interactable))
                CurrentlyHoveringOver.Remove(interactable);
        }

        private void SetVisibility(VisibilityLevel visibility)
        {
            if (CurrentVisibility != visibility)
            {
                if (visibility == VisibilityLevel.Invisible)
                {
                    if (PhysicalController != null)
                    {
                        PhysicalController.Off();
                    }

                    if (Player.AutomaticallySetControllerTransparency == true)
                    {
                        for (int index = 0; index < GhostRenderers.Length; index++)
                        {
                            GhostRenderers[index].enabled = false;
                        }

                        for (int index = 0; index < GhostColliders.Length; index++)
                        {
                            GhostColliders[index].enabled = false;
                        }
                    }
                }

                if (visibility == VisibilityLevel.Ghost)
                {
                    if (PhysicalController != null)
                    {
                        PhysicalController.Off();
                    }

                    if (Player.AutomaticallySetControllerTransparency == true)
                    {
                        for (int index = 0; index < GhostRenderers.Length; index++)
                        {
                            GhostRenderers[index].enabled = true;
                        }

                        for (int index = 0; index < GhostColliders.Length; index++)
                        {
                            GhostColliders[index].enabled = true;
                        }
                    }
                }

                if (visibility == VisibilityLevel.Visible)
                {
                    if (PhysicalController != null)
                    {
                        PhysicalController.On();
                    }

                    if (Player.AutomaticallySetControllerTransparency == true)
                    {
                        for (int index = 0; index < GhostRenderers.Length; index++)
                        {
                            GhostRenderers[index].enabled = false;
                        }

                        for (int index = 0; index < GhostColliders.Length; index++)
                        {
                            GhostColliders[index].enabled = false;
                        }
                    }
                }
            }

            CurrentVisibility = visibility;
        }

        protected void InitializeRenderModel()
        {
            if (CustomModel == null)
            {
                RenderModel = InputDevice.SetupDefaultRenderModel();
            }
            else
            {
                RenderModel = GameObject.Instantiate(CustomModel);

                RenderModel.transform.parent = this.transform;
                RenderModel.transform.localScale = RenderModel.transform.localScale;
                RenderModel.transform.localPosition = Vector3.zero;
                RenderModel.transform.localRotation = Quaternion.identity;
            }
        }

        public void Initialize()
        {
            Rigidbody = this.GetComponent<Rigidbody>();
            if (Rigidbody == null)
                Rigidbody = this.gameObject.AddComponent<Rigidbody>();
            Rigidbody.isKinematic = true;
            Rigidbody.maxAngularVelocity = float.MaxValue;
            Rigidbody.useGravity = false;

            Collider[] colliders = null;

            if (CustomModel == null)
            {
                colliders = InputDevice.SetupDefaultColliders();
            }
            else
            {
                colliders = RenderModel.GetComponentsInChildren<Collider>(); //note: these should be trigger colliders
            }

            Player.RegisterHand(this);

            if (Player.PhysicalHands == true)
            {
                if (PhysicalController != null)
                {
                    PhysicalController.Kill();
                }

                PhysicalController = this.gameObject.AddComponent<NVRPhysicalController>();
                PhysicalController.Initialize(this, false);

                if (Player.AutomaticallySetControllerTransparency == true)
                {
                    Color transparentcolor = Color.white;
                    transparentcolor.a = (float)VisibilityLevel.Ghost / 100f;

                    GhostRenderers = this.GetComponentsInChildren<Renderer>();
                    for (int rendererIndex = 0; rendererIndex < GhostRenderers.Length; rendererIndex++)
                    {
                        NVRHelpers.SetTransparent(GhostRenderers[rendererIndex].material, transparentcolor);
                    }
                }

                if (colliders != null)
                {
                    GhostColliders = colliders;
                }

                CurrentVisibility = VisibilityLevel.Ghost;
            }
            else
            {
                if (Player.AutomaticallySetControllerTransparency == true)
                {
                    Color transparentcolor = Color.white;
                    transparentcolor.a = (float)VisibilityLevel.Ghost / 100f;

                    GhostRenderers = this.GetComponentsInChildren<Renderer>();
                    for (int rendererIndex = 0; rendererIndex < GhostRenderers.Length; rendererIndex++)
                    {
                        NVRHelpers.SetTransparent(GhostRenderers[rendererIndex].material, transparentcolor);
                    }
                }

                if (colliders != null)
                {
                    GhostColliders = colliders;
                }

                CurrentVisibility = VisibilityLevel.Ghost;
            }

            CurrentHandState = HandState.Idle;
        }

        public void ForceGhost()
        {
            SetVisibility(VisibilityLevel.Ghost);
            PhysicalController.Off();
        }
    }

    public enum VisibilityLevel
    {
        Invisible = 0,
        Ghost = 70,
        Visible = 100,
    }

    public enum HandState
    {
        Uninitialized,
        Idle,
        GripDownNotInteracting,
        GripDownInteracting,
        GripToggleOnNotInteracting,
        GripToggleOnInteracting,
        GripToggleOff
    }

    public enum InterationStyle
    {
        Hold,
        Toggle,
        ByScript,
    }
}