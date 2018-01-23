using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

#if UNITY_WSA && UNITY_2017_2_OR_NEWER && NVR_WindowsMR
using GLTF;
using UnityEngine.XR.WSA.Input;
using HoloToolkit.Unity;

#if !UNITY_EDITOR
using Windows.Foundation;
using Windows.Storage.Streams;
#endif

#endif



#if NVR_WindowsMR && UNITY_WSA
namespace NewtonVR
{
    public class NVRWindowsMRInput : NVRInputDevice
    {
        private GameObject RenderModel;
        private bool loading;
        private bool lost = false;

        private bool RenderModelInitialized = false;

        private Dictionary<NVRButtons, WindowsMRInputName> ButtonMapping = new Dictionary<NVRButtons, WindowsMRInputName>(new NVRButtonsComparer());
        private Dictionary<WindowsMRInputName, WindowsMRInput> Inputs = new Dictionary<WindowsMRInputName, WindowsMRInput>();

#if UNITY_EDITOR_WIN
        [DllImport("MotionControllerModel")]
        private static extern bool TryGetMotionControllerModel([In] uint controllerId, [Out] out uint outputSize, [Out] out IntPtr outputBuffer);
#endif

        public override void Initialize(NVRHand hand)
        {
            SetupButtonMapping();
            base.Initialize(hand);

            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceUpdated += InteractionManager_InteractionSourceUpdated;
            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;
        }

        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs obj)
        {
            if ((obj.state.source.handedness == InteractionSourceHandedness.Left) == Hand.IsLeft)
            {
                if (loading)
                    return;

                loading = true;
                StartCoroutine(LoadSourceControllerModel(obj.state.source));
            }
        }

        private void InteractionManager_InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
        {
            if (RenderModelInitialized && !lost)
            {
                return;
            }
            if ((obj.state.source.handedness == InteractionSourceHandedness.Left) == Hand.IsLeft)
            {
                if (lost)
                {
                    RenderModel.SetActive(true);
                    lost = false;
                    return;
                }
                if (loading)
                    return;

                loading = true;
                StartCoroutine(LoadSourceControllerModel(obj.state.source));
            }
        }
        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs obj)
        {
            if (!RenderModelInitialized)
            {
                return;
            }
            if ((obj.state.source.handedness == InteractionSourceHandedness.Left) == Hand.IsLeft)
            {
                RenderModel.SetActive(false);
                lost = true;
            }
        }
        private void OnDestroy()
        {
            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceUpdated -= InteractionManager_InteractionSourceUpdated;
            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;
        }

       
        private void Update()
        {
#if UNITY_WSA && UNITY_2017_2_OR_NEWER
            // NOTE: The controller's state is being updated here in order to provide a good position and rotation
            // for any child GameObjects that might want to raycast or otherwise reason about their location in the world.
            foreach (var sourceState in InteractionManager.GetCurrentReading())
            {
                if ((sourceState.source.handedness == InteractionSourceHandedness.Left) == Hand.IsLeft)
                {
                    Vector3 newPosition;
                    if (sourceState.sourcePose.TryGetPosition(out newPosition, InteractionSourceNode.Grip))
                    {
                        gameObject.transform.localPosition = newPosition;
                    }

                    Quaternion newRotation;
                    if (sourceState.sourcePose.TryGetRotation(out newRotation, InteractionSourceNode.Grip))
                    {
                        gameObject.transform.localRotation = newRotation;
                    }
                    ProcessInputs(sourceState);
                }
            }
#endif
        }
        private IEnumerator LoadSourceControllerModel(InteractionSource source)
        {
            //throw new NotImplementedException();
            byte[] fileBytes = null;
            UnityEngine.Material GLTFMaterial = new UnityEngine.Material(Shader.Find("Standard"));

#if !UNITY_EDITOR
            // This API returns the appropriate glTF file according to the motion controller you're currently using, if supported.
            IAsyncOperation<IRandomAccessStreamWithContentType> modelTask = source.TryGetRenderableModelAsync();

            if (modelTask == null)
            {
                Debug.Log("Model task is null; loading alternate.");
                LoadAlternateControllerModel(source);
                yield break;
            }

            while (modelTask.Status == AsyncStatus.Started)
            {
                yield return null;
            }

            IRandomAccessStreamWithContentType modelStream = modelTask.GetResults();

            if (modelStream == null)
            {
                Debug.Log("Model stream is null; loading alternate.");
                LoadAlternateControllerModel(source);
                yield break;
            }

            if (modelStream.Size == 0)
            {
                Debug.Log("Model stream is empty; loading alternate.");
                LoadAlternateControllerModel(source);
                yield break;
            }

            fileBytes = new byte[modelStream.Size];

            using (DataReader reader = new DataReader(modelStream))
            {
                DataReaderLoadOperation loadModelOp = reader.LoadAsync((uint)modelStream.Size);

                while (loadModelOp.Status == AsyncStatus.Started)
                {
                    yield return null;
                }

                reader.ReadBytes(fileBytes);
            }
#else
            IntPtr controllerModel = new IntPtr();
            uint outputSize = 0;

            try
            {
                if (TryGetMotionControllerModel(source.id, out outputSize, out controllerModel))
                {
                    fileBytes = new byte[Convert.ToInt32(outputSize)];

                    Marshal.Copy(controllerModel, fileBytes, 0, Convert.ToInt32(outputSize));
                }
                else
                {
                    Debug.Log("Unable to load controller models; loading alternate.");
                    LoadAlternateControllerModel(source);
                    yield break;
                }
            }
            catch (Exception e)
            {
                loading = false;
            }

#endif

            RenderModel = new GameObject { name = source.handedness + "-glTFController" };
            GLTFComponentStreamingAssets gltfScript = RenderModel.AddComponent<GLTFComponentStreamingAssets>();
            gltfScript.ColorMaterial = GLTFMaterial;
            gltfScript.NoColorMaterial = GLTFMaterial;
            gltfScript.GLTFData = fileBytes;

            yield return gltfScript.LoadModel();

            RenderModelInitialized = true;
            loading = false;

            FinishControllerSetup(RenderModel, source.handedness, source.vendorId + "/" + source.productId + "/" + source.productVersion + "/" + source.handedness);
        }

        private void LoadAlternateControllerModel(InteractionSource source)
        {
            if (Hand.IsLeft == true)
            {
                RenderModel = GameObject.Instantiate(Resources.Load<GameObject>("AcerControllers/AcerControllerLeft"));
            }
            else
            {
                RenderModel = GameObject.Instantiate(Resources.Load<GameObject>("AcerControllers/AcerControllerRight"));
            }

            RenderModel.name = "Render Model for " + Hand.gameObject.name;
            RenderModel.transform.parent = transform;
            RenderModel.transform.localPosition = Vector3.zero;
            RenderModel.transform.localRotation = Quaternion.identity;
            RenderModel.transform.Rotate(Vector3.right, -60f);
            RenderModel.transform.localScale = Vector3.one;

            RenderModelInitialized = true;
        }


        private void FinishControllerSetup(GameObject controllerModelGameObject, InteractionSourceHandedness handedness, string dictionaryKey)
        {
            controllerModelGameObject.transform.parent = transform;
            controllerModelGameObject.transform.localRotation = Quaternion.identity;
            controllerModelGameObject.transform.localPosition = Vector3.zero;
        }

        protected virtual void SetupButtonMapping()
        {
            ButtonMapping.Add(NVRButtons.Grip, WindowsMRInputName.Grasp);
            Inputs.Add(WindowsMRInputName.Grasp, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.System, WindowsMRInputName.Menu);
            Inputs.Add(WindowsMRInputName.Menu, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.Touchpad, WindowsMRInputName.TouchPad);
            Inputs.Add(WindowsMRInputName.TouchPad, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.Trigger, WindowsMRInputName.Select);
            Inputs.Add(WindowsMRInputName.Select, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.DPad_Down, WindowsMRInputName.DPad_Down);
            Inputs.Add(WindowsMRInputName.DPad_Down, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.DPad_Left, WindowsMRInputName.DPad_Left);
            Inputs.Add(WindowsMRInputName.DPad_Left, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.DPad_Right, WindowsMRInputName.DPad_Right);
            Inputs.Add(WindowsMRInputName.DPad_Right, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.DPad_Up, WindowsMRInputName.DPad_Up);
            Inputs.Add(WindowsMRInputName.DPad_Up, new WindowsMRInput());
            ButtonMapping.Add(NVRButtons.Back, WindowsMRInputName.Menu);
            ButtonMapping.Add(NVRButtons.A, WindowsMRInputName.TouchPadButton);
            ButtonMapping.Add(NVRButtons.ApplicationMenu, WindowsMRInputName.Menu);
        }

        void ProcessInputs(InteractionSourceState currentState)
        {
            foreach (KeyValuePair<WindowsMRInputName, WindowsMRInput> pair in Inputs)
            {
                if (pair.Key == WindowsMRInputName.Select)
                {
                    pair.Value.SetPressState(currentState.selectPressed);
                    pair.Value.SetTouchState(currentState.selectPressed);
                    pair.Value.Axis = currentState.selectPressedAmount;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.Menu)
                {
                    pair.Value.SetPressState(currentState.menuPressed);
                    pair.Value.SetTouchState(currentState.menuPressed);
                    pair.Value.Axis = currentState.menuPressed ? 1 : 0;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.ThumbStick)
                {
                    pair.Value.SetPressState(currentState.thumbstickPressed);
                    pair.Value.SetTouchState(currentState.thumbstickPressed);
                    pair.Value.Axis = currentState.thumbstickPosition.x;
                    pair.Value.Axis2D = currentState.thumbstickPosition;
                }
                else if (pair.Key == WindowsMRInputName.TouchPad)
                {
                    pair.Value.SetPressState(currentState.touchpadPressed);
                    pair.Value.SetTouchState(currentState.touchpadTouched);
                    pair.Value.Axis = currentState.touchpadPosition.x;
                    pair.Value.Axis2D = currentState.touchpadPosition;
                }
                else if (pair.Key == WindowsMRInputName.TouchPadButton)
                {
                    pair.Value.SetPressState(currentState.touchpadPressed);
                    pair.Value.SetTouchState(currentState.touchpadTouched);
                    pair.Value.Axis = currentState.touchpadPressed ? 1 : 0;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.Grasp)
                {
                    pair.Value.SetPressState(currentState.grasped);
                    pair.Value.SetTouchState(currentState.grasped);
                    pair.Value.Axis = currentState.grasped ? 1 : 0;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.DPad_Up)
                {
                    pair.Value.SetPressState(currentState.thumbstickPosition.y > 0.5f);
                    pair.Value.Axis = currentState.thumbstickPosition.y;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.DPad_Down)
                {
                    pair.Value.SetPressState(currentState.thumbstickPosition.y < -0.5f);
                    pair.Value.Axis = currentState.thumbstickPosition.y;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.DPad_Left)
                {
                    pair.Value.SetPressState(currentState.thumbstickPosition.x > 0.5f);
                    pair.Value.Axis = currentState.thumbstickPosition.x;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
                else if (pair.Key == WindowsMRInputName.DPad_Right)
                {
                    pair.Value.SetPressState(currentState.thumbstickPosition.x < -0.5f);
                    pair.Value.Axis = currentState.thumbstickPosition.x;
                    pair.Value.Axis2D = new Vector2(pair.Value.Axis, 0);
                }
            }
        }

        public override float GetAxis1D(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].Axis;
            }

            return 0;
        }

        public override Vector2 GetAxis2D(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].Axis2D;
            }
            return Vector2.zero;
        }

        public override bool GetPressDown(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].PressDown;
            }

            return false;
        }

        public override bool GetPressUp(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].PressReleased;
            }
            return false;
        }

        public override bool GetPress(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].IsPressed;
            }
            return false;
        }

        public override bool GetTouchDown(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].TouchDown;
            }
            return false;
        }

        public override bool GetTouchUp(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].TouchReleased;
            }
            return false;
        }

        public override bool GetTouch(NVRButtons button)
        {
            if (ButtonMapping.ContainsKey(button))
            {
                if (Inputs.ContainsKey(ButtonMapping[button]))
                    return Inputs[ButtonMapping[button]].IsTouched;
            }
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
            if (durationMicroSec < 3000)
            {
                foreach (var sourceState in InteractionManager.GetCurrentReading())
                {
                    if ((sourceState.source.handedness == InteractionSourceHandedness.Left) == Hand.IsLeft)
                    {
                        Debug.Log("Vibration");
                        sourceState.source.StartHaptics(0.4f, durationMicroSec / 1000f);
                    }
                }
            }
        }

        public override bool IsCurrentlyTracked
        {
            get
            {
                return RenderModelInitialized && lost;
            }
        }

        protected float curlAmountNeeded = 0.7f;

        public override GameObject SetupDefaultRenderModel()
        {
            return RenderModel;
        }

        public override bool ReadyToInitialize()
        {
            return (RenderModelInitialized || Hand.HasCustomModel);
        }

        public override string GetDeviceName()
        {
            if (Hand.HasCustomModel == true)
            {
                return "Custom controller";
            }
            else
            {
                return "Windows Mixed Reality Controller";
            }
        }

        public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
        {
            Collider[] colliders = null;

            string controllerModel = GetDeviceName();
            colliders = AddWindowsMRPhysicalColliders(ModelParent, controllerModel);

            return colliders;
        }

        public override Collider[] SetupDefaultColliders()
        {
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
            if (colliders.Length > 0)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].isTrigger = true;
                }
            }
            else
            {
                string controllerModel = GetDeviceName();
                colliders = AddWindowsMRTouchTriggerCollider(RenderModel, controllerModel);
            }
            return colliders;
        }

        protected Collider[] AddWindowsMRPhysicalColliders(Transform ModelParent, string controllerModel)
        {
            string name = "AcerController";
            if (Hand.IsLeft == true)
            {
                name += "Left";
            }
            else
            {
                name += "Right";
            }
            name += "_Colliders";

            Transform acerColliders = ModelParent.Find(name);
            if (acerColliders == null)
            {
                acerColliders = GameObject.Instantiate(Resources.Load<GameObject>("AcerControllers/" + name)).transform;
                acerColliders.parent = ModelParent;
                acerColliders.localPosition = Vector3.zero;
                acerColliders.localRotation = Quaternion.identity;
                acerColliders.localScale = Vector3.one;
            }

            return acerColliders.GetComponentsInChildren<Collider>();
        }
        protected Collider[] AddWindowsMRTouchTriggerCollider(GameObject renderModel, string name)
        {
            Debug.Log("Adding Sphere Collider on " + renderModel.name);
            SphereCollider sphereCollider = renderModel.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.10f;

            return new Collider[] { sphereCollider };
        }

        protected static Vector3 SteamVROculusControllerPositionAddition = new Vector3(0.001f, -0.0086f, -0.0197f);
    }

    enum WindowsMRInputName { Select, Grasp, TouchPad, Menu, ThumbStick, DPad_Up, DPad_Down, DPad_Left, DPad_Right, TouchPadButton, }
    class WindowsMRInput
    {
        public bool IsPressed = false;
        public bool PressReleased = false;
        public bool PressDown = false;
        public bool IsTouched = false;
        public bool TouchDown = false;
        public bool TouchReleased = false;
        public float Axis = 0;
        public Vector2 Axis2D = Vector2.zero;

        public void SetPressState(bool value)
        {
            if (value)
            {
                if (IsPressed)
                {
                    PressDown = false;
                }
                else
                {
                    IsPressed = true;
                    PressDown = true;
                    PressReleased = false;
                }
            }
            else
            {
                if (IsPressed)
                {
                    IsPressed = false;
                    PressDown = false;
                    PressReleased = true;
                }
                else
                {
                    PressReleased = false;
                }
            }
        }
        public void SetTouchState(bool value)
        {
            if (value)
            {
                if (IsTouched)
                {
                    TouchDown = false;
                }
                else
                {
                    IsTouched = true;
                    TouchDown = true;
                    TouchReleased = false;
                }
            }
            else
            {
                if (IsTouched)
                {
                    IsTouched = false;
                    TouchDown = false;
                    TouchReleased = true;
                }
                else
                {
                    TouchReleased = false;
                }
            }
        }
    }
}
#else
namespace NewtonVR
    {
        public class NVRWindowsMRInput : NVRInputDevice
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