using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;
using NewtonVR;

public class NVRViveInputProxy : NVRHandInputProxy {

    private int DeviceIndex = -1;
    private bool RenderModelInitialized = false;
    private Collider[] DeviceColliders;
    private GameObject modelObj = null;

    private int _initCalled = 0;

    public bool CustomModel = false; // set to True if you are providing your own mesh

    public delegate void NewPosesEvent();
    public NewPosesEvent OnNewPoses;

    public NVRViveInputProxy()
    {
        SteamVR_Utils.Event.Listen("render_model_loaded", RenderModelLoaded);
        SteamVR_Utils.Event.Listen("new_poses_applied", OnNewPosesApplied);
    }

    private void OnDestroy()
    {
        SteamVR_Utils.Event.Remove("render_model_loaded", RenderModelLoaded);
        SteamVR_Utils.Event.Remove("new_poses_applied", OnNewPosesApplied);
    }

    private Dictionary<NVRButtonID, Valve.VR.EVRButtonId> InputMap = new Dictionary<NVRButtonID, EVRButtonId>()
    {
        { NVRButtonID.HoldButton, EVRButtonId.k_EButton_Grip },
        { NVRButtonID.UseButton,  EVRButtonId.k_EButton_SteamVR_Trigger }
    };

    private SteamVR_Controller.Device Controller;

    public override bool isReady()
    {
        return Controller != null &&
               (RenderModelInitialized || CustomModel);
    }

    public override NVRButtonInputs getButtonState(NVRButtonID button)
    {
        NVRButtonInputs buttonState = new NVRButtonInputs();

        buttonState.Axis       = Controller.GetAxis(InputMap[button]);
        buttonState.SingleAxis = buttonState.Axis.x;
        buttonState.PressDown  = Controller.GetPressDown(InputMap[button]);
        buttonState.PressUp    = Controller.GetPressUp(InputMap[button]);
        buttonState.IsPressed  = Controller.GetPress(InputMap[button]);
        buttonState.TouchDown  = Controller.GetTouchDown(InputMap[button]);
        buttonState.TouchUp    = Controller.GetTouchUp(InputMap[button]);
        buttonState.IsTouched  = Controller.GetTouch(InputMap[button]);

        return buttonState;
    }

    void OnAwake()
    {
        Debug.Log(this.gameObject.name + " ViveController awake");
        
    }

    protected virtual void OnEnable()
    {
        if (this.gameObject.activeInHierarchy)
            StartCoroutine(DoInitialize());
    }

    private void SetDeviceIndex(int index)
    {
        Debug.Log("<NVRViveInputProxy> :: SetDeviceIndex(" + index + ")");
        DeviceIndex = index;
        Controller = SteamVR_Controller.Input(index);
     //   StartCoroutine(DoInitialize());
    }

    private void RenderModelLoaded(params object[] args)
    {
        SteamVR_RenderModel renderModel = (SteamVR_RenderModel)args[0];
        bool success = (bool)args[1];

        if ((int)renderModel.index == DeviceIndex)
            RenderModelInitialized = true;

        Debug.Log(this.gameObject.name + "RenderModelLoaded: " + renderModel.index + " - " + DeviceIndex);
    }

    private IEnumerator DoInitialize()
    {

        Debug.Log(this.gameObject.name + "DoInitialize " + _initCalled++);
        if (!CustomModel && !RenderModelInitialized && modelObj == null)
        {
            Debug.Log(this.gameObject.name + " ViveController adding its thing");
            modelObj = new GameObject(this.gameObject.name + "_RenderModel", typeof(SteamVR_RenderModel));
            modelObj.transform.parent = this.transform;
        }

        do
        {
            yield return null; //wait for render model to be initialized
        } while (RenderModelInitialized == false && !CustomModel);

        Debug.Log(this.gameObject.name + "OMG Initializing");
        if (!CustomModel)
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

                    DeviceColliders = new Collider[] { dk1TrackhatCollider };
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
                    break;

                default:
                    Debug.LogError("Error. Unsupported device type: " + controllerModel);
                    break;
            }
        }
    }

    private void OnNewPosesApplied(params object[] args)
    {
        if (Controller == null)
            return;

        OnNewPoses();
    }

    public string GetDeviceName()
    {
        if (CustomModel)
        {
            return "Custom";
        }
        else
        {
            return this.GetComponentInChildren<SteamVR_RenderModel>().renderModelName;
        }
    }

    public Collider[] GetDeviceColliders()
    {
        return DeviceColliders;
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
                Debug.LogWarning("You're trying to pulse for over 3000 microseconds, you probably don't want to do that. If you do, use LongHapticPulse(float seconds)");
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
}
