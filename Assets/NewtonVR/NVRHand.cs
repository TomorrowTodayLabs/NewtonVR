using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NewtonVR
{
    public class NVRHand : MonoBehaviour
    {
        private Valve.VR.EVRButtonId HoldButton = Valve.VR.EVRButtonId.k_EButton_Grip;
        public bool HoldButtonDown = false;
        public bool HoldButtonUp = false;
        public bool HoldButtonPressed = false;

        private Valve.VR.EVRButtonId UseButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
        public bool UseButtonDown = false;
        public bool UseButtonUp = false;
        public bool UseButtonPressed = false;

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


        private NVRInteractable CurrentlyInteracting;

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


        private void Awake()
        {
            CurrentlyHoveringOver = new Dictionary<NVRInteractable, Dictionary<Collider, float>>();

            LastPositions = new Vector3[EstimationSamples];
            LastRotations = new Quaternion[EstimationSamples];
            LastDeltas = new float[EstimationSamples];
            EstimationSampleIndex = 0;

            VisibilityLocked = false;

            SteamVR_Utils.Event.Listen("render_model_loaded", RenderModelLoaded);
        }

        private void Update()
        {
            if (Controller == null || CurrentHandState == HandState.Uninitialized)
                return;

            HoldButtonPressed = Controller.GetPress(HoldButton);
            HoldButtonDown = Controller.GetPressDown(HoldButton);
            HoldButtonUp = Controller.GetPressUp(HoldButton);

            UseButtonPressed = Controller.GetPress(UseButton);
            UseButtonDown = Controller.GetPressDown(UseButton);
            UseButtonUp = Controller.GetPressUp(UseButton);

            if (HoldButtonUp)
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

            if (IsInteracting == true)
            {
                CurrentlyInteracting.InteractingUpdate(this);
            }

            if (NVRPlayer.Instance.PhysicalHands == true)
            {
                UpdateVisibilityAndColliders();
            }
        }

        private void UpdateVisibilityAndColliders()
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
                    SetVisibility(VisibilityLevel.Ghost);
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

        private void FixedUpdate()
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

        private void BeginInteraction(NVRInteractable interactable)
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

        public void EndInteraction(NVRInteractable item)
        {
            if (item != null && CurrentlyHoveringOver.ContainsKey(item) == true)
                CurrentlyHoveringOver.Remove(item);

            if (CurrentlyInteracting != null)
            {
                CurrentlyInteracting.EndInteraction();
                CurrentlyInteracting = null;
            }
        }

        private void PickupClosest()
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
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            NVRInteractable interactable = NVRInteractables.GetInteractable(collider);
            if (interactable == null || interactable.enabled == false)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == false)
                CurrentlyHoveringOver[interactable] = new Dictionary<Collider, float>();

            if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == false)
                CurrentlyHoveringOver[interactable][collider] = Time.time;
        }

        private void OnTriggerStay(Collider collider)
        {
            NVRInteractable interactable = NVRInteractables.GetInteractable(collider);
            if (interactable == null || interactable.enabled == false)
                return;

            if (CurrentlyHoveringOver.ContainsKey(interactable) == false)
                CurrentlyHoveringOver[interactable] = new Dictionary<Collider, float>();

            if (CurrentlyHoveringOver[interactable].ContainsKey(collider) == false)
                CurrentlyHoveringOver[interactable][collider] = Time.time;
        }

        private void OnTriggerExit(Collider collider)
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

        private void OnEnable()
        {
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

            CurrentlyHoveringOver.Remove(interactable);
        }

        private void SetVisibility(VisibilityLevel visibility)
        {
            if (CurrentVisibility != visibility)
            {
                if (visibility == VisibilityLevel.Ghost)
                {
                    PhysicalController.Off();

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
                    PhysicalController.On();

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

            Collider[] Colliders = null;

            if (CustomModel == null)
            {
                string controllerModel = GetDeviceName();
                switch (controllerModel)
                {
                    case "vr_controller_05_wireless_b":
                        Transform dk1Trackhat = this.transform.FindChild("trackhat");
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
                        Transform dk2Trackhat = this.transform.FindChild("trackhat");
                        if (dk2Trackhat == null)
                        {
                            dk2Trackhat = new GameObject("trackhat").transform;
                            dk2Trackhat.parent = this.transform;
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
            else
            {
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
                if (PhysicalController != null)
                {
                    PhysicalController.Kill();
                }

                PhysicalController = this.gameObject.AddComponent<NVRPhysicalController>();
                PhysicalController.Initialize(this, false);

                Color transparentcolor = Color.white;
                transparentcolor.a = (float)VisibilityLevel.Ghost / 100f;

                GhostRenderers = this.GetComponentsInChildren<Renderer>();
                for (int rendererIndex = 0; rendererIndex < GhostRenderers.Length; rendererIndex++)
                {
                    NVRHelpers.SetTransparent(GhostRenderers[rendererIndex].material, transparentcolor);
                }

                GhostColliders = Colliders;
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
    }
}