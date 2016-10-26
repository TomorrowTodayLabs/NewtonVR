using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRHand : MonoBehaviour
    {
        public NVRPlayer player = null;

        [Space]

        public bool HoldButtonDown = false;
        public bool HoldButtonUp = false;
        public bool HoldButtonPressed = false;
        public float HoldButtonAxis = 0f;

        [Space]

        public bool UseButtonDown = false;
        public bool UseButtonUp = false;
        public bool UseButtonPressed = false;
        public float UseButtonAxis = 0f;

        public NVRDriver Driver; /// TODO: make this private?

        public Dictionary<NVRButtonID, NVRButtonInputs> Inputs;

        [SerializeField]
        private InteractionStyle CurrentInteractionStyle = InteractionStyle.GripDownToInteract;

        public Rigidbody Rigidbody;

        [Tooltip("If you want to use something other than the standard SteamVR Controller models place the Prefab here. Otherwise we use steamvr models.")]
        public GameObject CustomModel;

        [Tooltip("If you're using a custom model or if you just want custom physical colliders, stick the prefab for them here.")]
        public GameObject CustomPhysicalColliders;

        private VisibilityLevel CurrentVisibility = VisibilityLevel.Visible;
        private bool VisibilityLocked = false;

        private HandState CurrentHandState = HandState.Uninitialized;

        private Dictionary<NVRInteractable, Dictionary<Collider, float>> CurrentlyHoveringOver;

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
            
            Inputs = new Dictionary<NVRButtonID, NVRButtonInputs>();
            System.Array buttonTypes = System.Enum.GetValues(typeof(NVRButtonID));
            foreach (NVRButtonID buttonType in buttonTypes)
            {
                if (Inputs.ContainsKey(buttonType) == false)
                {
                    Inputs.Add(buttonType, new NVRButtonInputs());
                }
            }

            Driver.OnNewPoses += OnNewPosesApplied;
        }

        private void OnNewPosesApplied()
        {
            if (CurrentlyInteracting != null)
            {
                CurrentlyInteracting.OnNewPosesApplied();
            }
        }

        protected virtual void Update()
        {
            if (CurrentHandState == HandState.Uninitialized)
                return;

            HoldButtonPressed = Inputs[NVRButtonID.HoldButton].IsPressed;
            HoldButtonDown    = Inputs[NVRButtonID.HoldButton].PressDown;
            HoldButtonUp      = Inputs[NVRButtonID.HoldButton].PressUp;
            HoldButtonAxis    = Inputs[NVRButtonID.HoldButton].SingleAxis;

            UseButtonPressed  = Inputs[NVRButtonID.UseButton].IsPressed;
            UseButtonDown     = Inputs[NVRButtonID.UseButton].PressDown;
            UseButtonUp       = Inputs[NVRButtonID.UseButton].PressUp;
            UseButtonAxis     = Inputs[NVRButtonID.UseButton].SingleAxis;

            if (CurrentInteractionStyle == InteractionStyle.GripDownToInteract)
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
            else if (CurrentInteractionStyle == InteractionStyle.GripToggleToInteract)
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
                        else if (player.PhysicalHands == true)
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

        public void TriggerHapticPulse(ushort durationMicroSec = 500)
        {
            Driver.TriggerHapticPulse(this, durationMicroSec);
        }

        public void LongHapticPulse(float seconds)
        {
            Driver.LongHapticPulse(this, seconds);
        }

        public void SetColliders(Collider[] newColliders)
        {
            GhostColliders = new Collider[newColliders.Length];
            newColliders.CopyTo(GhostColliders, 0);

            if (PhysicalController != null)
            {
                PhysicalController.SetColliders(newColliders);
            }
        }

        private void UpdateVisibilityAndColliders()
        {
            if (player.PhysicalHands == true)
            {
                if (CurrentInteractionStyle == InteractionStyle.GripDownToInteract)
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
                            if (player.MakeControllerInvisibleOnInteraction == true)
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
                else if (CurrentInteractionStyle == InteractionStyle.GripToggleToInteract)
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
            else if (player.PhysicalHands == false && player.MakeControllerInvisibleOnInteraction == true)
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

            if (IsInteracting == false && IsHovering == true)
            {
                Driver.TriggerHapticPulse(this, 100);
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

            if (CurrentInteractionStyle == InteractionStyle.GripToggleToInteract)
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
        }

        public void DoInitialize()
        {
            Debug.Log(this.gameObject.name + "Hand DoInitialize");
            Rigidbody = this.GetComponent<Rigidbody>();
            if (Rigidbody == null)
                Rigidbody = this.gameObject.AddComponent<Rigidbody>();
            Rigidbody.isKinematic = true;
            Rigidbody.maxAngularVelocity = float.MaxValue;
            Rigidbody.useGravity = false;

            Collider[] Colliders = null;

            if (CustomModel != null)
            {
                GameObject CustomModelObject = GameObject.Instantiate(CustomModel);
                Colliders = CustomModelObject.GetComponentsInChildren<Collider>();
                foreach (var collider in Colliders)
                {
                    collider.isTrigger = true;
                }

                CustomModelObject.transform.parent = this.transform;
                CustomModelObject.transform.localScale = Vector3.one;
                CustomModelObject.transform.localPosition = Vector3.zero;
                CustomModelObject.transform.localRotation = Quaternion.identity;
            }
            else if (GhostColliders == null)
            {
                Colliders = this.GetComponentsInChildren<Collider>();
            }

            player.RegisterHand(this);

            if (player.PhysicalHands == true)
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

        public void ForceGhost()
        {
            SetVisibility(VisibilityLevel.Ghost);
            PhysicalController.Off();
        }

        public string GetDeviceName()
        {
            return Driver.GetDeviceName(this);
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

    public enum InteractionStyle
    {
        GripDownToInteract,
        GripToggleToInteract,
    }
}