using UnityEngine;
using UnityEngine.VR;
#if UNITY_WSA && NVR_WindowsMR
using HoloToolkit.Unity;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA;
#else
using UnityEngine.VR.WSA;
#endif
#endif

namespace NewtonVR
{
    public class NVRWindowsMRIntegration : NVRIntegration
    {
        [Tooltip("The near clipping plane distance for an opaque display.")]
        public float NearClipPlane_OpaqueDisplay = 0.1f;

        [Tooltip("Values for Camera.clearFlags, determining what to clear when rendering a Camera for an opaque display.")]
        public CameraClearFlags CameraClearFlags_OpaqueDisplay = CameraClearFlags.Skybox;

        [Tooltip("Background color for a transparent display.")]
        public Color BackgroundColor_OpaqueDisplay = Color.black;

        [Tooltip("Set the desired quality for your application for opaque display.")]
        public int OpaqueQualityLevel;

        [Tooltip("The near clipping plane distance for a transparent display.")]
        public float NearClipPlane_TransparentDisplay = 0.85f;

        [Tooltip("Values for Camera.clearFlags, determining what to clear when rendering a Camera for an opaque display.")]
        public CameraClearFlags CameraClearFlags_TransparentDisplay = CameraClearFlags.SolidColor;

        [Tooltip("Background color for a transparent display.")]
        public Color BackgroundColor_TransparentDisplay = Color.clear;

        [Tooltip("Set the desired quality for your application for HoloLens.")]
        public int HoloLensQualityLevel;
        public enum DisplayType
        {
            Opaque = 0,
            Transparent
        };

        public override void Initialize(NVRPlayer player)
        {
            Player = player;
            Player.gameObject.SetActive(false);

            if (!Application.isEditor)
            {
#if UNITY_WSA
#if UNITY_2017_2_OR_NEWER
                if (!UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
#endif
                {
                    CurrentDisplayType = DisplayType.Transparent;
                    ApplySettingsForTransparentDisplay(Player.Head.GetComponent<Camera>());
                    if (OnDisplayDetected != null)
                    {
                        OnDisplayDetected(DisplayType.Transparent);
                    }
                    return;
                }
#endif
            }

            CurrentDisplayType = DisplayType.Opaque;
            ApplySettingsForOpaqueDisplay(Player.Head.GetComponent<Camera>());
            if (OnDisplayDetected != null)
            {
                OnDisplayDetected(DisplayType.Opaque);
            }
            Player.gameObject.SetActive(true);
        }

        private Vector3 PlayspaceBounds = Vector3.zero;
        public override Vector3 GetPlayspaceBounds()
        {
#if UNITY_2017_2_OR_NEWER
            if (UnityEngine.Experimental.XR.Boundary.configured)
            {
                UnityEngine.Experimental.XR.Boundary.TryGetDimensions(out PlayspaceBounds);
            }
#else
            if (UnityEngine.Experimental.VR.Boundary.configured)
            {
                UnityEngine.Experimental.VR.Boundary.TryGetDimensions(out PlayspaceBounds);
            }
#endif

            return PlayspaceBounds;
        }

        public override bool IsHmdPresent()
        {
#if UNITY_2017_2_OR_NEWER
            if (Application.isPlaying == false) //try and enable vr if we're in the editor so we can get hmd present
            {
                if (UnityEngine.XR.XRSettings.enabled == false)
                {
                    UnityEngine.XR.XRSettings.enabled = true;
                }
            }

            return UnityEngine.XR.XRDevice.isPresent;
#else
            if (Application.isPlaying == false) //try and enable vr if we're in the editor so we can get hmd present
            {
                if (UnityEngine.VR.VRSettings.enabled == false)
                {
                    UnityEngine.VR.VRSettings.enabled = true;
                }
            }

            return UnityEngine.VR.VRDevice.isPresent;
#endif
        }

        public DisplayType CurrentDisplayType { get; private set; }

        public delegate void DisplayEventHandler(DisplayType displayType);
        /// <summary>
        /// Event is fired when a display is detected.
        /// DisplayType enum value tells you if display is Opaque Vs Transparent.
        /// </summary>
        public event DisplayEventHandler OnDisplayDetected;

        public void ApplySettingsForOpaqueDisplay(Camera cam)
        {
            Debug.Log("Display is Opaque");
            cam.clearFlags = CameraClearFlags_OpaqueDisplay;
            cam.nearClipPlane = NearClipPlane_OpaqueDisplay;
            cam.backgroundColor = BackgroundColor_OpaqueDisplay;
            SetQuality(OpaqueQualityLevel);
        }

        public void ApplySettingsForTransparentDisplay(Camera cam)
        {
            Debug.Log("Display is Transparent");
            cam.clearFlags = CameraClearFlags_TransparentDisplay;
            cam.backgroundColor = BackgroundColor_TransparentDisplay;
            cam.nearClipPlane = NearClipPlane_TransparentDisplay;
            SetQuality(HoloLensQualityLevel);
        }

        private static void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, false);
        }
    }
}
