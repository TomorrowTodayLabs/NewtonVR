using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;
using NewtonVR;
using System;

// Player gets: SteamVR_PlayArea, SteamVR_ControllerManager
// Hands get:   SteamVR_TrackedObject, child w/ SteamVR_RenderModel (then wait for load...) OR child w/ custom model
// Head gets:   SteamVR_TrackedObject
// \- Eyes get: SteamVR_Camera
// \- Ears get: SteamVR_Ears
public class NVRViveDriver : NVRDriver
{
    public SteamVR_PlayArea.Size PlayAreaSize;

    private bool LeftRenderModelInitialized = false;
    private bool RightRenderModelInitialized = false;

    private SteamVR_TrackedObject LeftHandTracker;
    private SteamVR_TrackedObject RightHandTracker;

    public bool CustomModel = false; // set to True if you are providing your own mesh

    private Dictionary<NVRButtonID, Valve.VR.EVRButtonId> InputMap = new Dictionary<NVRButtonID, EVRButtonId>()
    {
        { NVRButtonID.HoldButton, EVRButtonId.k_EButton_Grip },
        { NVRButtonID.UseButton,  EVRButtonId.k_EButton_SteamVR_Trigger }
    };

    void OnEnable()
    {
        SteamVR_Utils.Event.Listen("render_model_loaded", RenderModelLoaded);
        SteamVR_Utils.Event.Listen("new_poses_applied", OnNewPosesApplied);
    }

    void OnDisable()
    {
        SteamVR_Utils.Event.Remove("render_model_loaded", RenderModelLoaded);
        SteamVR_Utils.Event.Remove("new_poses_applied", OnNewPosesApplied);
    }

    private void OnNewPosesApplied(params object[] args)
    {
        if (OnNewPoses != null)
        {
            OnNewPoses();
        }
    }

    public override NVRButtonInputs GetButtonState(NVRHand hand, NVRButtonID button)
    {
        SteamVR_Controller.Device controller;
        if (hand == LeftHand)
        {
            controller = SteamVR_Controller.Input((int)LeftHandTracker.index);
        }
        else
        {
            controller = SteamVR_Controller.Input((int)RightHandTracker.index);
        }

        NVRButtonInputs buttonState = new NVRButtonInputs();

        buttonState.Axis       = controller.GetAxis(InputMap[button]);
        buttonState.SingleAxis = buttonState.Axis.x;
        buttonState.PressDown  = controller.GetPressDown(InputMap[button]);
        buttonState.PressUp    = controller.GetPressUp(InputMap[button]);
        buttonState.IsPressed  = controller.GetPress(InputMap[button]);
        buttonState.TouchDown  = controller.GetTouchDown(InputMap[button]);
        buttonState.TouchUp    = controller.GetTouchUp(InputMap[button]);
        buttonState.IsTouched  = controller.GetTouch(InputMap[button]);

        return buttonState;
    }

    private void RenderModelLoaded(params object[] args)
    {
        SteamVR_RenderModel renderModel = (SteamVR_RenderModel)args[0];
        bool success = (bool)args[1];

        NVRHand hand = renderModel.transform.parent.GetComponent<NVRHand>();
        if (hand != null && LeftHand != null && RightHand != null)
        {
            if (hand.name == this.LeftHand.name)
            {
                Debug.Log("Loaded RenderModel for Left hand: " + success);
                LeftRenderModelInitialized = true;
            }
            else if (hand.name == this.RightHand.name)
            {
                Debug.Log("Loaded RenderModel for Right hand: " + success);
                RightRenderModelInitialized = true;
            }
            else
            {
                Debug.LogWarning("Loaded RenderModel for unexpected hand: " + hand.gameObject.name + " (" + success + ")");
            }
        }
        else
        {
            Debug.LogError("Loaded RenderModel '" + renderModel.name + "' but could not find corresponding NVRHand :(");
        }
    }

    public void Awake()
    {
        Debug.Log("NVRViveDriver initializing...");
        // SteamVR Setup & Initialization
        GameObject go = new GameObject("SteamVR_Stuff"); // dummy object to hold SteamVR components
        go.transform.parent = this.transform;
        go.SetActive(false); // SteamVR components do a lot of initialization in Awake() and OnEnable(), so we want to wait until everything is ready before letting them run

        

        var playArea = this.GetComponentInChildren<SteamVR_PlayArea>();
        if (playArea == null)
        {
            Debug.Log("Adding new SteamVR PlayArea");
            playArea = go.AddComponent<SteamVR_PlayArea>();
            playArea.size = PlayAreaSize;
        }

        var controllerManager = this.GetComponentInChildren<SteamVR_ControllerManager>();
        if (controllerManager == null)
        {
            Debug.Log("Adding new SteamVR ControllerManager");
            controllerManager = go.AddComponent<SteamVR_ControllerManager>();
        }

        if (controllerManager.left == null)
        {
            controllerManager.left = LeftHand.gameObject;
        }
        if (controllerManager.right == null)
        {
            controllerManager.right = RightHand.gameObject;
        }

        //var headTracker = Head.GetComponent<SteamVR_TrackedObject>();
        //if (headTracker == null)
        //{
        //    Head.gameObject.AddComponent<SteamVR_TrackedObject>().index = SteamVR_TrackedObject.EIndex.Hmd;
        //}
        //var headSteamCamera = Head.GetComponent<SteamVR_Camera>();
        //if (headSteamCamera == null)
        //{
        //    Head.gameObject.AddComponent<SteamVR_Camera>();
        //}

        var leftmodelObj = new GameObject(LeftHand.gameObject.name + "_RenderModel", typeof(SteamVR_RenderModel));
        leftmodelObj.transform.parent = LeftHand.transform;

        var rightmodelObj = new GameObject(RightHand.gameObject.name + "_RenderModel", typeof(SteamVR_RenderModel));
        rightmodelObj.transform.parent = RightHand.transform;

        RightHandTracker = RightHand.gameObject.AddComponent<SteamVR_TrackedObject>();
        LeftHandTracker = LeftHand.gameObject.AddComponent<SteamVR_TrackedObject>();

        // enable dummy object containing ControllerManager & PlayArea
        go.SetActive(true);

        if (!CustomModel && !(LeftRenderModelInitialized || RightRenderModelInitialized))
        {
            StartCoroutine(DoInitSteamVRModels());
        }
    }

    private IEnumerator DoInitSteamVRModels()
    {
        Debug.Log("Waiting for models to load... ");
        var leftModel = LeftHand.GetComponentInChildren<SteamVR_RenderModel>();
        var rightModel = RightHand.GetComponentInChildren<SteamVR_RenderModel>();

        // check to see if models have already been loaded for the controllers
        if (leftModel.renderModelName != null)
        {
            Debug.Log("Left already initialized");
            LeftRenderModelInitialized = true;
        }
        if (rightModel.renderModelName!= null)
        {
            Debug.Log("Right already initialized");
            RightRenderModelInitialized = true;
        }

        do
        {
            yield return null; //wait for render model to be initialized
        } while ((LeftRenderModelInitialized == false || RightRenderModelInitialized == false) && !CustomModel);

        Debug.Log("RenderModels initialized!");

        // configure colliders
        InitColliders(LeftHand);
        InitColliders(RightHand);

        LeftHand.DoInitialize();
        RightHand.DoInitialize();
    }

    private void InitColliders(NVRHand hand)
    {
        Collider[] DeviceColliders;
        if (!CustomModel)
        {
            string controllerModel = GetDeviceName(hand);
            SteamVR_RenderModel renderModel = hand.GetComponentInChildren<SteamVR_RenderModel>();

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

                    DeviceColliders = new Collider[] { dk1TrackhatCollider };
                    hand.SetColliders(DeviceColliders);
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

                    DeviceColliders = new Collider[] { dk2TrackhatCollider };
                    hand.SetColliders(DeviceColliders);
                    break;

                default:
                    Debug.LogError("Error. Unsupported device type: " + controllerModel);
                    break;
            }
        }
    }

    public void Update()
    {
        if (LeftHandTracker.isValid)
        {
            foreach (NVRButtonID button in LeftHand.Inputs.Keys.ToList())
            {
                LeftHand.Inputs[button] = GetButtonState(LeftHand, button);
            }
        }

        if (RightHandTracker.isValid)
        {
            foreach (NVRButtonID button in RightHand.Inputs.Keys.ToList())
            {
                RightHand.Inputs[button] = GetButtonState(RightHand, button);
            }
        }
    }

    public override string GetDeviceName(NVRHand hand)
    {
        var renderModel = hand.GetComponentInChildren<SteamVR_RenderModel>();
        if (renderModel != null)
        {
            return hand.GetComponentInChildren<SteamVR_RenderModel>().renderModelName;
        }
        else
        {
            return "Custom";
        }
    }

    public override string GetDeviceName(NVRHead head)
    {
        return "HTC Vive HMD";
    }

    public override void TriggerHapticPulse(NVRHand hand, ushort durationMicroSec = 500)
    {
        var controller = SteamVR_Controller.Input((int)hand.GetComponentInChildren<SteamVR_TrackedObject>().index);
        if (controller != null)
        {
            if (durationMicroSec < 3000)
            {
                controller.TriggerHapticPulse(durationMicroSec);
            }
            else
            {
                Debug.LogWarning("you're trying to pulse for over 3000 microseconds, you probably don't want to do that. if you do, use longhapticpulse(float seconds)");
            }
        }
    }

    public override void LongHapticPulse(NVRHand hand, float seconds)
    {
        StartCoroutine(DoLongHapticPulse(hand, seconds));
    }

    private IEnumerator DoLongHapticPulse(NVRHand hand, float seconds)
    {
        float startTime = Time.time;
        float endTime = startTime + seconds;
        var controller = SteamVR_Controller.Input((int)hand.GetComponentInChildren<SteamVR_TrackedObject>().index);
        while (Time.time < endTime)
        {
            controller.TriggerHapticPulse(100);
            yield return null;
        }
    }
}
