using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Valve.VR;

namespace NewtonVR
{
    public class NVRHand : MonoBehaviour
    {
        private Valve.VR.EVRButtonId HoldButton = EVRButtonId.k_EButton_Grip;
        public bool HoldButtonDown = false;
        public bool HoldButtonUp = false;
        public bool HoldButtonPressed = false;
        public float HoldButtonAxis = 0f;

        private Valve.VR.EVRButtonId UseButton = EVRButtonId.k_EButton_SteamVR_Trigger;
        public bool UseButtonDown = false;
        public bool UseButtonUp = false;
        public bool UseButtonPressed = false;
        public float UseButtonAxis = 0f;

        public Dictionary<EVRButtonId, NVRButtonInputs> Inputs;

        [SerializeField]
        private InterationStyle CurrentInteractionStyle = InterationStyle.GripDownToInteract;

        public Rigidbody Rigidbody;

        [Tooltip("If you want to use something other than the standard SteamVR Controller models place the Prefab here. Otherwise we use steamvr models.")]
        public GameObject CustomModel;

        [Tooltip("If you're using a custom model or if you just want custom physical colliders, stick the prefab for them here.")]
        public GameObject CustomPhysicalColliders;

        private VisibilityLevel CurrentVisibility = VisibilityLevel.Visible;
        private bool VisibilityLocked = false;

        private HandState CurrentHandState = HandState.Uninitialized;

        private Dictionary<NVRInteractable, Dictionary<Collider, float>> CurrentlyHoveringOver;

        private SteamVR_Controller.Device Controller;

        public NVRInteractable CurrentlyInteracting;

        private int EstimationSampleIndex;
        private Vector3[] LastPositions;
        private Quaternion[] LastRotations;
        private float[] LastDeltas;
        private int EstimationSamples = 5;
        private int RotationEstimationSamples = 10;

        private NVRPhysicalController PhysicalController;

        private Collider[] GhostColliders;
        private Renderer[] GhostRenderers;

        private int DeviceIndex = -1;

        private bool RenderModelInitialized = false;

        private EVRButtonId[] EVRButtonIds;

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


        protected virtual void Awake()
        {
            
            CurrentlyHoveringOver = new Dictionary<NVRInteractable, Dictionary<Collider, float>>();

            LastPositions = new Vector3[EstimationSamples];
            LastRotations = new Quaternion[EstimationSamples];
            LastDeltas = new float[EstimationSamples];
            EstimationSampleIndex = 0;

            VisibilityLocked = false;
            
            Inputs = new Dictionary<EVRButtonId, NVRButtonInputs>();
            System.Array buttonTypes = System.Enum.GetValues(typeof(EVRButtonId));
            foreach (EVRButtonId buttonType in buttonTypes)
            {
                if (Inputs.ContainsKey(buttonType) == false) //for some reason there is two EVRButtonId.2 entries
                {
                    Inputs.Add(buttonType, new NVRButtonInputs());
                }
            }

            SteamVR_Utils.Event.Listen("render_model_loaded", RenderModelLoaded);
            SteamVR_Utils.Event.Listen("new_poses_applied", OnNewPosesApplied);
        }

        private void OnNewPosesApplied(params object[] args)
        {
            if (Controller == null)
                return;

            if (CurrentlyInteracting != null)
            {
                CurrentlyInteracting.OnNewPosesApplied();
            }
        }


        protected virtual void Update()
        {
            if (Controller == null || CurrentHandState == HandState.Uninitialized)
                return;

            foreach (var button in Inputs)
            {
                button.Value.Axis = Controller.GetAxis(button.Key);
                button.Value.SingleAxis = button.Value.Axis.x;
                button.Value.PressDown = Controller.GetPressDown(button.Key);
                button.Value.PressUp = Controller.GetPressUp(button.Key);
                button.Value.IsPressed = Controller.GetPress(button.Key);
                button.Value.TouchDown = Controller.GetTouchDown(button.Key);
                button.Value.TouchUp = Controller.GetTouchUp(button.Key);
                button.Value.IsTouched = Controller.GetTouch(button.Key);
            }

            HoldButtonPressed = Inputs[HoldButton].IsPressed;
            HoldButtonDown = Inputs[HoldButton].PressDown;
            HoldButtonUp = Inputs[HoldButton].PressUp;
            HoldButtonAxis = Inputs[HoldButton].SingleAxis;

            UseButtonPressed = Inputs[UseButton].IsPressed;
            UseButtonDown = Inputs[UseButton].PressDown;
            UseButtonUp = Inputs[UseButton].PressUp;
            UseButtonAxis = Inputs[UseButton].SingleAxis;

            if (CurrentInteractionStyle == InterationStyle.GripDownToInteract)
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
            else if (CurrentInteractionStyle == InterationStyle.GripToggleToInteract)
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
                        else if (NVRPlayer.Instance.PhysicalHands == true)
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

            if (IsInteracting == true)
            {
                CurrentlyInteracting.InteractingUpdate(this);
            }
            
            UpdateVisibilityAndColliders();
        }


        public void TriggerHapticPulse(ushort durationMicroSec = 500, EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0)
        {
            if (Controller != null)
            {
                if (durationMicroSec < 3000)
                {
                    Controller.TriggerHapticPulse(durationMicroSec, buttonId);
                }
                else
                {
                    Debug.LogWarning("You're trying to pulse for over 3000 microseconds, you probably don't want to do that. If you do, use NVRHand.LongHapticPulse(float seconds)");
                }
            }
        }

        public void LongHapticPulse(float seconds, EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0)
        {
            StartCoroutine(DoLongHapticPulse(seconds, buttonId));
        }

        private IEnumerator DoLongHapticPulse(float seconds, EVRButtonId buttonId)
        {
            float startTime = Time.time;
            float endTime = startTime + seconds;
            while (Time.time < endTime)
            {
                Controller.TriggerHapticPulse(100, buttonId);
                yield return null;
            }
        }

        private void UpdateVisibilityAndColliders()
        {
            if (NVRPlayer.Instance.PhysicalHands == true)
            {
                if (CurrentInteractionStyle == InterationStyle.GripDownToInteract)
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
                            if (NVRPlayer.Instance.MakeControllerInvisibleOnInteraction == true)
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
                else if (CurrentInteractionStyle == InterationStyle.GripToggleToInteract)
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
            else if (NVRPlayer.Instance.PhysicalHands == false && NVRPlayer.Instance.MakeControllerInvisibleOnInteraction == true)
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

        public Vector3 GetVelocityEstimation()
        {
            float delta = LastDeltas.Sum();
            Vector3 distance = Vector3.zero;

            for (int index = 0; index < LastPositions.Length-1; index++)
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

            rotation =  LastRotations[LastRotations.Length-1] * Quaternion.Inverse(LastRotations[LastRotations.Length-2]);

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
            LastPositions[EstimationSampleIndex] = this.transform.position;
            LastRotations[EstimationSampleIndex] = this.transform.rotation;
            LastDeltas[EstimationSampleIndex] = Time.fixedDeltaTime;
            EstimationSampleIndex++;

            if (EstimationSampleIndex >= LastPositions.Length)
                EstimationSampleIndex = 0;

            if (Controller != null && IsInteracting == false && IsHovering == true)
            {
                Controller.TriggerHapticPulse(100);
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
            }
        }

        public virtual void EndInteraction(NVRInteractable item)
        {
            if (item != null && CurrentlyHoveringOver.ContainsKey(item) == true)
                CurrentlyHoveringOver.Remove(item);

            if (CurrentlyInteracting != null)
            {
                CurrentlyInteracting.EndInteraction();
                CurrentlyInteracting = null;
            }

            if (CurrentInteractionStyle == InterationStyle.GripToggleToInteract)
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

        protected virtual void OnEnable()
        {
            VisibilityLocked = false;

            if (CustomModel != null)
            {
                this.GetComponentInChildren<SteamVR_RenderModel>().enabled = false;
            }
            if (this.gameObject.activeInHierarchy)
                StartCoroutine(DoInitialize());
        }

        private void SetDeviceIndex(int index)
        {
            DeviceIndex = index;
            Controller = SteamVR_Controller.Input(index);
            StartCoroutine(DoInitialize());
        }

        public void DeregisterInteractable(NVRInteractable interactable)
        {
            if (CurrentlyInteracting == interactable)
                CurrentlyInteracting = null;

            if (CurrentlyHoveringOver != null)
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

                    for (int index = 0; index < GhostRenderers.Length; index++)
                    {
                        GhostRenderers[index].enabled = false;
                    }

                    for (int index = 0; index < GhostColliders.Length; index++)
                    {
                        GhostColliders[index].enabled = true;
                    }
                }

                if (visibility == VisibilityLevel.Ghost)
                {
                    if (PhysicalController != null)
                    {
                        PhysicalController.Off();
                    }

                    for (int index = 0; index < GhostRenderers.Length; index++)
                    {
                        GhostRenderers[index].enabled = true;
                    }

                    for (int index = 0; index < GhostColliders.Length; index++)
                    {
                        GhostColliders[index].enabled = true;
                    }
                }

                if (visibility == VisibilityLevel.Visible)
                {
                    if (PhysicalController != null)
                    {
                        PhysicalController.On();
                    }

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

            CurrentVisibility = visibility;
        }

        private void RenderModelLoaded(params object[] args)
        {
            SteamVR_RenderModel renderModel = (SteamVR_RenderModel)args[0];
            bool success = (bool)args[1];

            if ((int)renderModel.index == DeviceIndex)
                RenderModelInitialized = true;
        }

        private IEnumerator DoInitialize()
        {
            do
            {
                yield return null; //wait for render model to be initialized
            } while (RenderModelInitialized == false && CustomModel == null);

            Rigidbody = this.GetComponent<Rigidbody>();
            if (Rigidbody == null)
                Rigidbody = this.gameObject.AddComponent<Rigidbody>();
            Rigidbody.isKinematic = true;
            Rigidbody.maxAngularVelocity = float.MaxValue;
            Rigidbody.useGravity = false;

            Collider[] Colliders = null;

            if (CustomModel == null)
            {
                string controllerModel = GetDeviceName();
                SteamVR_RenderModel renderModel = this.GetComponentInChildren<SteamVR_RenderModel>();

                switch (controllerModel)
                {
                    case "vr_controller_05_wireless_b":
                        Transform dk1Trackhat = renderModel.transform.Find("trackhat");
                        if (dk1Trackhat == null)
                        {
                            // Dk1 controller model has trackhat
                        }
                        else
                        {
                            dk1Trackhat.gameObject.SetActive(true);
                        }

                        SphereCollider dk1TrackhatCollider = dk1Trackhat.gameObject.GetComponent<SphereCollider>();
                        if (dk1TrackhatCollider == null)
                        {
                            dk1TrackhatCollider = dk1Trackhat.gameObject.AddComponent<SphereCollider>();
                            dk1TrackhatCollider.isTrigger = true;
                        }

                        Colliders = new Collider[] { dk1TrackhatCollider };
                        break;

                    case "vr_controller_vive_1_5":
                        Transform dk2Trackhat = renderModel.transform.FindChild("trackhat");
                        if (dk2Trackhat == null)
                        {
                            dk2Trackhat = new GameObject("trackhat").transform;
                            dk2Trackhat.gameObject.layer = this.gameObject.layer;
                            dk2Trackhat.parent = renderModel.transform;
                            dk2Trackhat.localPosition = new Vector3(0, -0.033f, 0.014f);
                            dk2Trackhat.localScale = Vector3.one * 0.1f;
                            dk2Trackhat.localEulerAngles = new Vector3(325, 0, 0);
                            dk2Trackhat.gameObject.SetActive(true);
                        }
                        else
                        {
                            dk2Trackhat.gameObject.SetActive(true);
                        }

                        Collider dk2TrackhatCollider = dk2Trackhat.gameObject.GetComponent<SphereCollider>();
                        if (dk2TrackhatCollider == null)
                        {
                            dk2TrackhatCollider = dk2Trackhat.gameObject.AddComponent<SphereCollider>();
                            dk2TrackhatCollider.isTrigger = true;
                        }

                        Colliders = new Collider[] { dk2TrackhatCollider };
                        break;

                    default:
                        Debug.LogError("Error. Unsupported device type: " + controllerModel);
                        break;
                }
            }
            else if (RenderModelInitialized == false)
            {

                RenderModelInitialized = true;
                GameObject CustomModelObject = GameObject.Instantiate(CustomModel);
                Colliders = CustomModelObject.GetComponentsInChildren<Collider>(); //note: these should be trigger colliders

                CustomModelObject.transform.parent = this.transform;
                CustomModelObject.transform.localScale = Vector3.one;
                CustomModelObject.transform.localPosition = Vector3.zero;
                CustomModelObject.transform.localRotation = Quaternion.identity;
            }

            NVRPlayer.Instance.RegisterHand(this);

            if (NVRPlayer.Instance.PhysicalHands == true)
            {
                bool InitialState = false;

                if (PhysicalController != null)
                {
                    if (PhysicalController.State == true)
                    {
                        InitialState = true;
                    }
                    else
                    {
                        InitialState = false;
                    }
                    PhysicalController.Kill();
                }

                PhysicalController = this.gameObject.AddComponent<NVRPhysicalController>();
                PhysicalController.Initialize(this, InitialState);

                if (InitialState == true)
                {
                    ForceGhost();
                }

                Color transparentcolor = Color.white;
                transparentcolor.a = (float)VisibilityLevel.Ghost / 100f;

                GhostRenderers = this.GetComponentsInChildren<Renderer>();
                for (int rendererIndex = 0; rendererIndex < GhostRenderers.Length; rendererIndex++)
                {
                    NVRHelpers.SetTransparent(GhostRenderers[rendererIndex].material, transparentcolor);
                }
                
                if (Colliders != null)
                {
                    GhostColliders = Colliders;
                }

                CurrentVisibility = VisibilityLevel.Ghost;
            }
            else
            {
                Color transparentcolor = Color.white;
                transparentcolor.a = (float)VisibilityLevel.Ghost / 100f;

                GhostRenderers = this.GetComponentsInChildren<Renderer>();
                for (int rendererIndex = 0; rendererIndex < GhostRenderers.Length; rendererIndex++)
                {
                    NVRHelpers.SetTransparent(GhostRenderers[rendererIndex].material, transparentcolor);
                }

                if (Colliders != null)
                {
                    GhostColliders = Colliders;
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

        public string GetDeviceName()
        {
            if (CustomModel != null)
            {
                return "Custom";
            }
            else
            {
                return this.GetComponentInChildren<SteamVR_RenderModel>().renderModelName;
            }
        }

        private void OnDestroy()
        {
            SteamVR_Utils.Event.Remove("render_model_loaded", RenderModelLoaded);
            SteamVR_Utils.Event.Remove("new_poses_applied", OnNewPosesApplied);
        }

        public void GetDeviceVelocity(out Vector3 velocity, out Vector3 angularVelocity)
        {
            velocity = Controller.velocity;
            angularVelocity = Controller.angularVelocity;
        }

        public Vector3 GetDeviceVelocity()
        {
            return Controller.velocity;
        }

        public Vector3 GetDeviceAngularVelocity()
        {
            return Controller.angularVelocity;
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
        GripDownToInteract,
        GripToggleToInteract,
    }
}