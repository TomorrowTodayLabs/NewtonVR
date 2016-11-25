using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

#if NVR_SteamVR
using Valve.VR;

namespace NewtonVR
{
    public class NVRSteamVRInputDevice : NVRInputDevice
    {
        private SteamVR_Controller.Device Controller;

        private int DeviceIndex = -1;

        private bool RenderModelInitialized = false;

        private Dictionary<NVRButtons, EVRButtonId> ButtonMapping = new Dictionary<NVRButtons, EVRButtonId>(new NVRButtonsComparer());

        public override void Initialize(NVRHand hand)
        {
            SetupButtonMapping();

            base.Initialize(hand);
            
            SteamVR_Utils.Event.Listen("render_model_loaded", RenderModelLoaded);
            SteamVR_Utils.Event.Listen("new_poses_applied", OnNewPosesApplied);
        }

        private void OnDestroy()
        {
            SteamVR_Utils.Event.Remove("render_model_loaded", RenderModelLoaded);
            SteamVR_Utils.Event.Remove("new_poses_applied", OnNewPosesApplied);
        }

        protected virtual void SetupButtonMapping()
        {
            ButtonMapping.Add(NVRButtons.A, EVRButtonId.k_EButton_A);
            ButtonMapping.Add(NVRButtons.ApplicationMenu, EVRButtonId.k_EButton_ApplicationMenu);
            ButtonMapping.Add(NVRButtons.Axis0, EVRButtonId.k_EButton_Axis0);
            ButtonMapping.Add(NVRButtons.Axis1, EVRButtonId.k_EButton_Axis1);
            ButtonMapping.Add(NVRButtons.Axis2, EVRButtonId.k_EButton_Axis2);
            ButtonMapping.Add(NVRButtons.Axis3, EVRButtonId.k_EButton_Axis3);
            ButtonMapping.Add(NVRButtons.Axis4, EVRButtonId.k_EButton_Axis4);
            ButtonMapping.Add(NVRButtons.Back, EVRButtonId.k_EButton_Dashboard_Back);
            ButtonMapping.Add(NVRButtons.DPad_Down, EVRButtonId.k_EButton_DPad_Down);
            ButtonMapping.Add(NVRButtons.DPad_Left, EVRButtonId.k_EButton_DPad_Left);
            ButtonMapping.Add(NVRButtons.DPad_Right, EVRButtonId.k_EButton_DPad_Right);
            ButtonMapping.Add(NVRButtons.DPad_Up, EVRButtonId.k_EButton_DPad_Up);
            ButtonMapping.Add(NVRButtons.Grip, EVRButtonId.k_EButton_Grip);
            ButtonMapping.Add(NVRButtons.System, EVRButtonId.k_EButton_System);
            ButtonMapping.Add(NVRButtons.Touchpad, EVRButtonId.k_EButton_SteamVR_Touchpad);
            ButtonMapping.Add(NVRButtons.Trigger, EVRButtonId.k_EButton_SteamVR_Trigger);


            ButtonMapping.Add(NVRButtons.B, EVRButtonId.k_EButton_A);
            ButtonMapping.Add(NVRButtons.X, EVRButtonId.k_EButton_A);
            ButtonMapping.Add(NVRButtons.Y, EVRButtonId.k_EButton_A);
        }

        private EVRButtonId GetButton(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button) == false)
            {
                return EVRButtonId.k_EButton_System;
                //Debug.LogError("No SteamVR button configured for: " + button.ToString());
            }
            return ButtonMapping[button];
        }

        public override float GetAxis1D(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetAxis(GetButton(button)).x;

            return 0;
        }

        public override Vector2 GetAxis2D(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetAxis(GetButton(button));

            return Vector2.zero;
        }

        public override bool GetPressDown(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetPressDown(GetButton(button));

            return false;
        }

        public override bool GetPressUp(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetPressUp(GetButton(button));

            return false;
        }

        public override bool GetPress(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetPress(GetButton(button));

            return false;
        }

        public override bool GetTouchDown(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetTouchDown(GetButton(button));

            return false;
        }

        public override bool GetTouchUp(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetTouchUp(GetButton(button));

            return false;
        }

        public override bool GetTouch(NVRButtons button)
        {
            if (Controller != null)
                return Controller.GetTouch(GetButton(button));

            return false;
        }

        public override bool GetNearTouchDown(NVRButtons button)
        {
            return false;
        }

        public override bool GetNearTouchUp(NVRButtons button)
        {
            return false;
        }

        public override bool GetNearTouch(NVRButtons button)
        {
            return false;
        }

        public override void TriggerHapticPulse(ushort durationMicroSec = 500, NVRButtons button = NVRButtons.Touchpad)
        {
            if (Controller != null)
            {
                if (durationMicroSec < 3000)
                {
                    Controller.TriggerHapticPulse(durationMicroSec, ButtonMapping[button]);
                }
            }
        }

        public override bool IsCurrentlyTracked
        {
            get
            {
                return DeviceIndex != -1;
            }
        }

        private void OnNewPosesApplied(params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return;
            }

            if (Controller == null)
                return;
        }

        public override GameObject SetupDefaultRenderModel()
        {
            GameObject renderModel = new GameObject("Render Model for " + Hand.gameObject.name);
            renderModel.transform.parent = Hand.transform;
            renderModel.transform.localPosition = Vector3.zero;
            renderModel.transform.localRotation = Quaternion.identity;
            renderModel.transform.localScale = Vector3.one;
            renderModel.AddComponent<SteamVR_RenderModel>();
            renderModel.GetComponent<SteamVR_RenderModel>().shader = Shader.Find("Standard");

            return renderModel;
        }

        public override bool ReadyToInitialize()
        {
            return (RenderModelInitialized || Hand.HasCustomModel) && DeviceIndex != -1;
        }

        private void RenderModelLoaded(params object[] args)
        {
            SteamVR_RenderModel renderModel = (SteamVR_RenderModel)args[0];
            bool success = (bool)args[1];

            if ((int)renderModel.index == DeviceIndex)
                RenderModelInitialized = success;
        }

        private void SetDeviceIndex(int index)
        {
            DeviceIndex = index;
            Controller = SteamVR_Controller.Input(index);
        }

        public override string GetDeviceName()
        {
            if (Hand.HasCustomModel == true)
            {
                return "Custom";
            }
            else
            {
                return this.GetComponentInChildren<SteamVR_RenderModel>(true).renderModelName;
            }
        }

        public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
        {
            Collider[] Colliders = null;

            string controllerModel = GetDeviceName();
            switch (controllerModel)
            {
                case "vr_controller_05_wireless_b":
                    Transform dk1Trackhat = ModelParent.transform.Find("trackhat");
                    Collider dk1TrackhatCollider = dk1Trackhat.gameObject.GetComponent<BoxCollider>();
                    if (dk1TrackhatCollider == null)
                        dk1TrackhatCollider = dk1Trackhat.gameObject.AddComponent<BoxCollider>();

                    Transform dk1Body = ModelParent.transform.Find("body");
                    Collider dk1BodyCollider = dk1Body.gameObject.GetComponent<BoxCollider>();
                    if (dk1BodyCollider == null)
                        dk1BodyCollider = dk1Body.gameObject.AddComponent<BoxCollider>();

                    Colliders = new Collider[] { dk1TrackhatCollider, dk1BodyCollider };
                    break;

                case "vr_controller_vive_1_5":
                    Transform dk2TrackhatColliders = ModelParent.transform.FindChild("ViveColliders");
                    if (dk2TrackhatColliders == null)
                    {
                        dk2TrackhatColliders = GameObject.Instantiate(Resources.Load<GameObject>("ViveControllers/ViveColliders")).transform;
                        dk2TrackhatColliders.parent = ModelParent.transform;
                        dk2TrackhatColliders.localPosition = Vector3.zero;
                        dk2TrackhatColliders.localRotation = Quaternion.identity;
                        dk2TrackhatColliders.localScale = Vector3.one;
                    }

                    Colliders = dk2TrackhatColliders.GetComponentsInChildren<Collider>();
                    break;
                default:
                    Debug.LogError("[NewtonVR] NVRSteamVRInputDevice Error. Unsupported device type while trying to setup physical colliders: " + controllerModel);
                    break;
            }

            return Colliders;
        }

        public override Collider[] SetupDefaultColliders()
        {
            Collider[] Colliders = null;

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

            return Colliders;
        }
    }
}
#else
namespace NewtonVR
{
    public class NVRSteamVRInputDevice : NVRInputDevice
    {
        public override bool IsCurrentlyTracked
        {
            get
            {
                PrintNotEnabledError();
                return false;
            }
        }

        public override float GetAxis1D(NVRButtons button)
        {
            PrintNotEnabledError();
            return 0;
        }

        public override Vector2 GetAxis2D(NVRButtons button)
        {
            PrintNotEnabledError();
            return Vector2.zero;
        }

        public override string GetDeviceName()
        {
            PrintNotEnabledError();
            return "";
        }

        public override bool GetNearTouch(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetNearTouchDown(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetNearTouchUp(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetPress(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetPressDown(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetPressUp(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetTouch(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetTouchDown(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool GetTouchUp(NVRButtons button)
        {
            PrintNotEnabledError();
            return false;
        }

        public override bool ReadyToInitialize()
        {
            PrintNotEnabledError();
            return false;
        }

        public override Collider[] SetupDefaultColliders()
        {
            PrintNotEnabledError();
            return null;
        }

        public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
        {
            PrintNotEnabledError();
            return null;
        }

        public override GameObject SetupDefaultRenderModel()
        {
            PrintNotEnabledError();
            return null;
        }

        public override void TriggerHapticPulse(ushort durationMicroSec = 500, NVRButtons button = NVRButtons.Touchpad)
        {
            PrintNotEnabledError();
        }

        private void PrintNotEnabledError()
        {
            Debug.LogError("Enable SteamVR in NVRPlayer to allow steamvr calls.");
        }
    }
}
#endif