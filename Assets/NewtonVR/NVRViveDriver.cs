using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Valve.VR;

// Player gets: SteamVR_PlayArea, SteamVR_ControllerManager
// Hands get:   SteamVR_TrackedObject, child w/ SteamVR_RenderModel (then wait for load...) OR child w/ custom model
// Head gets:   SteamVR_TrackedObject
// \- Eyes get: SteamVR_Camera
// \- Ears get: SteamVR_Ears
public class NVRViveDriver : NVRDriver
{
    public SteamVR_PlayArea.Size PlayAreaSize;

    private int DeviceIndex = -1;
    private bool RenderModelInitialized = false;

    public bool CustomModel = false; // set to True if you are providing your own mesh
    private SteamVR_Controller.Device Controller;

    private Dictionary<NVRButtonID, Valve.VR.EVRButtonId> InputMap = new Dictionary<NVRButtonID, EVRButtonId>()
    {
        { NVRButtonID.HoldButton, EVRButtonId.k_EButton_Grip },
        { NVRButtonID.UseButton,  EVRButtonId.k_EButton_SteamVR_Trigger }
    };

    public NVRViveDriver()
    {
        SteamVR_Utils.Event.Listen("render_model_loaded", RenderModelLoaded);
        SteamVR_Utils.Event.Listen("new_poses_applied", OnNewPosesApplied);
    }

    private void OnDestroy()
    {
        SteamVR_Utils.Event.Remove("render_model_loaded", RenderModelLoaded);
        SteamVR_Utils.Event.Remove("new_poses_applied", OnNewPosesApplied);
    }

    private void OnNewPosesApplied(params object[] args)
    {
        if (Controller == null)
            return;

        OnNewPoses();
    }

    // TODO: Check *which* model got loaded
    private void RenderModelLoaded(params object[] args)
    {
        SteamVR_RenderModel renderModel = (SteamVR_RenderModel)args[0];
        bool success = (bool)args[1];

        if ((int)renderModel.index == DeviceIndex)
            RenderModelInitialized = true;
    }

    public void Awake()
    {
        // SteamVR Setup & Initialization
        GameObject go = new GameObject("SteamVR_Stuff"); // dummy object to hold SteamVR components
        go.transform.parent = this.transform;
        go.SetActive(false);

        var playArea = this.GetComponentInChildren<SteamVR_PlayArea>();
        if (playArea == null)
        {
            playArea = go.AddComponent<SteamVR_PlayArea>();
            playArea.size = PlayAreaSize;
        }

        var controllerManager = this.GetComponentInChildren<SteamVR_ControllerManager>();
        if (controllerManager == null)
        {
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

        LeftHand.gameObject.AddComponent<SteamVR_TrackedObject>();
        RightHand.gameObject.AddComponent<SteamVR_TrackedObject>();
        Head.gameObject.AddComponent<SteamVR_TrackedObject>().index = SteamVR_TrackedObject.EIndex.Hmd;
        Head.gameObject.AddComponent<SteamVR_Camera>();

        if (!CustomModel && !RenderModelInitialized)
        {
            var leftmodelObj = new GameObject(LeftHand.gameObject.name + "_RenderModel", typeof(SteamVR_RenderModel));
            leftmodelObj.transform.parent = LeftHand.transform;

            var rightmodelObj = new GameObject(RightHand.gameObject.name + "_RenderModel", typeof(SteamVR_RenderModel));
            rightmodelObj.transform.parent = RightHand.transform;
        }

        go.SetActive(true);
    }


}
