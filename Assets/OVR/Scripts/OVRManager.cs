/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.3 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculus.com/licenses/LICENSE-3.3

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

#if !UNITY_5_3_OR_NEWER
#error Oculus Utilities require Unity 5.3 or higher.
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VR = UnityEngine.VR;

/// <summary>
/// Configuration data for Oculus virtual reality.
/// </summary>
public class OVRManager : MonoBehaviour
{
	public enum TrackingOrigin
	{
		EyeLevel   = OVRPlugin.TrackingOrigin.EyeLevel,
		FloorLevel = OVRPlugin.TrackingOrigin.FloorLevel,
	}

	public enum EyeTextureFormat
	{
		Default = OVRPlugin.EyeTextureFormat.Default,
		R16G16B16A16_FP = OVRPlugin.EyeTextureFormat.R16G16B16A16_FP,
		R11G11B10_FP = OVRPlugin.EyeTextureFormat.R11G11B10_FP,
	}

	/// <summary>
	/// Gets the singleton instance.
	/// </summary>
	public static OVRManager instance { get; private set; }
		
	/// <summary>
	/// Gets a reference to the active display.
	/// </summary>
	public static OVRDisplay display { get; private set; }

	/// <summary>
	/// Gets a reference to the active sensor.
	/// </summary>
	public static OVRTracker tracker { get; private set; }

	/// <summary>
	/// Gets a reference to the active boundary system.
	/// </summary>
	public static OVRBoundary boundary { get; private set; }

	private static OVRProfile _profile;
	/// <summary>
	/// Gets the current profile, which contains information about the user's settings and body dimensions.
	/// </summary>
	public static OVRProfile profile
	{
		get {
			if (_profile == null)
				_profile = new OVRProfile();

			return _profile;
		}
	}

	private IEnumerable<Camera> disabledCameras;
	float prevTimeScale;

	/// <summary>
	/// Occurs when an HMD attached.
	/// </summary>
	public static event Action HMDAcquired;

	/// <summary>
	/// Occurs when an HMD detached.
	/// </summary>
	public static event Action HMDLost;

	/// <summary>
	/// Occurs when an HMD is put on the user's head.
	/// </summary>
	public static event Action HMDMounted;

	/// <summary>
	/// Occurs when an HMD is taken off the user's head.
	/// </summary>
	public static event Action HMDUnmounted;

	/// <summary>
	/// Occurs when VR Focus is acquired.
	/// </summary>
	public static event Action VrFocusAcquired;

	/// <summary>
	/// Occurs when VR Focus is lost.
	/// </summary>
	public static event Action VrFocusLost;

	/// <summary>
	/// Occurs when the active Audio Out device has changed and a restart is needed.
	/// </summary>
	public static event Action AudioOutChanged;

	/// <summary>
	/// Occurs when the active Audio In device has changed and a restart is needed.
	/// </summary>
	public static event Action AudioInChanged;

	/// <summary>
	/// Occurs when the sensor gained tracking.
	/// </summary>
	public static event Action TrackingAcquired;

	/// <summary>
	/// Occurs when the sensor lost tracking.
	/// </summary>
	public static event Action TrackingLost;
	
	/// <summary>
	/// Occurs when Health & Safety Warning is dismissed.
	/// </summary>
	//Disable the warning about it being unused. It's deprecated.
	#pragma warning disable 0067
	[Obsolete]
	public static event Action HSWDismissed;
	#pragma warning restore

	private static bool _isHmdPresentCached = false;
	private static bool _isHmdPresent = false;
	private static bool _wasHmdPresent = false;
	/// <summary>
	/// If true, a head-mounted display is connected and present.
	/// </summary>
	public static bool isHmdPresent
	{
		get {
			if (!_isHmdPresentCached)
			{
				_isHmdPresentCached = true;
				_isHmdPresent = OVRPlugin.hmdPresent;
			}

			return _isHmdPresent;
		}

		private set {
			_isHmdPresentCached = true;
			_isHmdPresent = value;
		}
	}

	/// <summary>
	/// Gets the audio output device identifier.
	/// </summary>
	/// <description>
	/// On Windows, this is a string containing the GUID of the IMMDevice for the Windows audio endpoint to use.
	/// </description>
	public static string audioOutId
	{
		get { return OVRPlugin.audioOutId; }
	}

	/// <summary>
	/// Gets the audio input device identifier.
	/// </summary>
	/// <description>
	/// On Windows, this is a string containing the GUID of the IMMDevice for the Windows audio endpoint to use.
	/// </description>
	public static string audioInId
	{
		get { return OVRPlugin.audioInId; }
	}

	private static bool _hasVrFocusCached = false;
	private static bool _hasVrFocus = false;
	private static bool _hadVrFocus = false;
	/// <summary>
	/// If true, the app has VR Focus.
	/// </summary>
	public static bool hasVrFocus
	{
		get {
			if (!_hasVrFocusCached)
			{
				_hasVrFocusCached = true;
				_hasVrFocus = OVRPlugin.hasVrFocus;
			}

			return _hasVrFocus;
		}

		private set {
			_hasVrFocusCached = true;
			_hasVrFocus = value;
		}
	}

	/// <summary>
	/// If true, then the Oculus health and safety warning (HSW) is currently visible.
	/// </summary>
	[Obsolete]
	public static bool isHSWDisplayed { get { return false; } }
	
	/// <summary>
	/// If the HSW has been visible for the necessary amount of time, this will make it disappear.
	/// </summary>
	[Obsolete]
	public static void DismissHSWDisplay() {}

	/// <summary>
	/// If true, chromatic de-aberration will be applied, improving the image at the cost of texture bandwidth.
	/// </summary>
	public bool chromatic
	{
		get {
			if (!isHmdPresent)
				return false;

			return OVRPlugin.chromatic;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.chromatic = value;
		}
	}
	
	/// <summary>
	/// If true, both eyes will see the same image, rendered from the center eye pose, saving performance.
	/// </summary>
	public bool monoscopic
	{
		get {
			if (!isHmdPresent)
				return true;

			return OVRPlugin.monoscopic;
		}
		
		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.monoscopic = value;
		}
	}

	/// <summary>
	/// If true, distortion rendering work is submitted a quarter-frame early to avoid pipeline stalls and increase CPU-GPU parallelism.
	/// </summary>
	public bool queueAhead = true;

	/// <summary>
	/// If true, Unity will use the optimal antialiasing level for quality/performance on the current hardware.
	/// </summary>
	public bool useRecommendedMSAALevel = false;

	/// <summary>
	/// If true, dynamic resolution will be enabled
	/// </summary>
	public bool enableAdaptiveResolution = false;

    /// <summary>
    /// Max RenderScale the app can reach under adaptive resolution mode ( enableAdaptiveResolution = ture );
    /// </summary>
    [RangeAttribute(0.5f, 2.0f)]
    public float maxRenderScale = 1.0f;

    /// <summary>
    /// Min RenderScale the app can reach under adaptive resolution mode ( enableAdaptiveResolution = ture );
    /// </summary>
    [RangeAttribute(0.5f, 2.0f)]

    public float minRenderScale = 0.7f;

	/// <summary>
	/// The number of expected display frames per rendered frame.
	/// </summary>
	public int vsyncCount
	{
		get {
			if (!isHmdPresent)
				return 1;

			return OVRPlugin.vsyncCount;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.vsyncCount = value;
		}
	}
	
	/// <summary>
	/// Gets the current battery level.
	/// </summary>
	/// <returns><c>battery level in the range [0.0,1.0]</c>
	/// <param name="batteryLevel">Battery level.</param>
	public static float batteryLevel
	{
		get {
			if (!isHmdPresent)
				return 1f;

			return OVRPlugin.batteryLevel;
		}
	}
	
	/// <summary>
	/// Gets the current battery temperature.
	/// </summary>
	/// <returns><c>battery temperature in Celsius</c>
	/// <param name="batteryTemperature">Battery temperature.</param>
	public static float batteryTemperature
	{
		get {
			if (!isHmdPresent)
				return 0f;

			return OVRPlugin.batteryTemperature;
		}
	}
	
	/// <summary>
	/// Gets the current battery status.
	/// </summary>
	/// <returns><c>battery status</c>
	/// <param name="batteryStatus">Battery status.</param>
	public static int batteryStatus
	{
		get {
			if (!isHmdPresent)
				return -1;

			return (int)OVRPlugin.batteryStatus;
		}
	}

	/// <summary>
	/// Gets the current volume level.
	/// </summary>
	/// <returns><c>volume level in the range [0,1].</c>
	public static float volumeLevel
	{
		get {
			if (!isHmdPresent)
				return 0f;

			return OVRPlugin.systemVolume;
		}
	}

	/// <summary>
	/// Gets or sets the current CPU performance level (0-2). Lower performance levels save more power.
	/// </summary>
	public static int cpuLevel
	{
		get {
			if (!isHmdPresent)
				return 2;

			return OVRPlugin.cpuLevel;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.cpuLevel = value;
		}
	}

	/// <summary>
	/// Gets or sets the current GPU performance level (0-2). Lower performance levels save more power.
	/// </summary>
	public static int gpuLevel
	{
		get {
			if (!isHmdPresent)
				return 2;

			return OVRPlugin.gpuLevel;
		}

		set {
			if (!isHmdPresent)
				return;

			OVRPlugin.gpuLevel = value;
		}
	}

	/// <summary>
	/// If true, the CPU and GPU are currently throttled to save power and/or reduce the temperature.
	/// </summary>
	public static bool isPowerSavingActive
	{
		get {
			if (!isHmdPresent)
				return false;

			return OVRPlugin.powerSaving;
		}
	}

	/// <summary>
	/// Gets or sets the eye texture format.
	/// This feature is only for UNITY_5_6_OR_NEWER On PC
	/// </summary>
	public static EyeTextureFormat eyeTextureFormat
	{
		get
		{
			return (OVRManager.EyeTextureFormat)OVRPlugin.GetDesiredEyeTextureFormat();
		}

		set
		{
			OVRPlugin.SetDesiredEyeTextureFormat((OVRPlugin.EyeTextureFormat)value);
		}
	}

	[SerializeField]
	private OVRManager.TrackingOrigin _trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
	/// <summary>
	/// Defines the current tracking origin type.
	/// </summary>
	public OVRManager.TrackingOrigin trackingOriginType
	{
		get {
			if (!isHmdPresent)
				return _trackingOriginType;

			return (OVRManager.TrackingOrigin)OVRPlugin.GetTrackingOriginType();
		}
		
		set {
			if (!isHmdPresent)
				return;

			if (OVRPlugin.SetTrackingOriginType((OVRPlugin.TrackingOrigin)value))
			{
				// Keep the field exposed in the Unity Editor synchronized with any changes.
				_trackingOriginType = value;
			}
		}
	}

	/// <summary>
	/// If true, head tracking will affect the position of each OVRCameraRig's cameras.
	/// </summary>
	public bool usePositionTracking = true;

	/// <summary>
	/// If true, the distance between the user's eyes will affect the position of each OVRCameraRig's cameras.
	/// </summary>
	public bool useIPDInPositionTracking = true;

	/// <summary>
	/// If true, each scene load will cause the head pose to reset.
	/// </summary>
	public bool resetTrackerOnLoad = false;

	/// <summary>
	/// True if the current platform supports virtual reality.
	/// </summary>
    public bool isSupportedPlatform { get; private set; }

	private static bool _isUserPresentCached = false;
	private static bool _isUserPresent = false;
	private static bool _wasUserPresent = false;
	/// <summary>
	/// True if the user is currently wearing the display.
	/// </summary>
	public bool isUserPresent
	{
		get {
			if (!_isUserPresentCached)
			{
				_isUserPresentCached = true;
				_isUserPresent = OVRPlugin.userPresent;
			}

			return _isUserPresent;
		}

		private set {
			_isUserPresentCached = true;
			_isUserPresent = value;
		}
	}

	private static bool prevAudioOutIdIsCached = false;
	private static bool prevAudioInIdIsCached = false;
	private static string prevAudioOutId = string.Empty;
	private static string prevAudioInId = string.Empty;
	private static bool wasPositionTracked = false;

#region Unity Messages

	private void Awake()
	{
		// Only allow one instance at runtime.
		if (instance != null)
		{
			enabled = false;
			DestroyImmediate(this);
			return;
		}

		instance = this;

		Debug.Log("Unity v" + Application.unityVersion + ", " +
		          "Oculus Utilities v" + OVRPlugin.wrapperVersion + ", " +
		          "OVRPlugin v" + OVRPlugin.version + ", " +
		          "SDK v" + OVRPlugin.nativeSDKVersion + ".");

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		var supportedTypes =
			UnityEngine.Rendering.GraphicsDeviceType.Direct3D11.ToString() + ", " +
			UnityEngine.Rendering.GraphicsDeviceType.Direct3D12.ToString();
		
		if (!supportedTypes.Contains(SystemInfo.graphicsDeviceType.ToString()))
			Debug.LogWarning("VR rendering requires one of the following device types: (" + supportedTypes + "). Your graphics device: " + SystemInfo.graphicsDeviceType.ToString());
#endif

        // Detect whether this platform is a supported platform
        RuntimePlatform currPlatform = Application.platform;
        isSupportedPlatform |= currPlatform == RuntimePlatform.Android;
        //isSupportedPlatform |= currPlatform == RuntimePlatform.LinuxPlayer;
        isSupportedPlatform |= currPlatform == RuntimePlatform.OSXEditor;
        isSupportedPlatform |= currPlatform == RuntimePlatform.OSXPlayer;
        isSupportedPlatform |= currPlatform == RuntimePlatform.WindowsEditor;
        isSupportedPlatform |= currPlatform == RuntimePlatform.WindowsPlayer;
        if (!isSupportedPlatform)
        {
            Debug.LogWarning("This platform is unsupported");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
		// We want to set up our touchpad messaging system
		OVRTouchpad.Create();

        // Turn off chromatic aberration by default to save texture bandwidth.
        chromatic = false;
#endif

        Initialize();

		if (resetTrackerOnLoad)
			display.RecenterPose();
		
		// Disable the occlusion mesh by default until open issues with the preview window are resolved.
		OVRPlugin.occlusionMesh = false;
	}

#if UNITY_EDITOR
	private static bool _scriptsReloaded;

	[UnityEditor.Callbacks.DidReloadScripts]
	static void ScriptsReloaded()
	{
		_scriptsReloaded = true;
	}
#endif

	void Initialize()
	{
		if (display == null)
			display = new OVRDisplay();
		if (tracker == null)
			tracker = new OVRTracker();
		if (boundary == null)
			boundary = new OVRBoundary();
	}

	private void Update()
	{
#if UNITY_EDITOR
		if (_scriptsReloaded)
		{
			_scriptsReloaded = false;
			instance = this;
			Initialize();
		}
#endif

		if (OVRPlugin.shouldQuit)
			Application.Quit();

		if (OVRPlugin.shouldRecenter)
			OVRManager.display.RecenterPose();

		if (trackingOriginType != _trackingOriginType)
			trackingOriginType = _trackingOriginType;

		tracker.isEnabled = usePositionTracking;

		OVRPlugin.useIPDInPositionTracking = useIPDInPositionTracking;

		// Dispatch HMD events.

		isHmdPresent = OVRPlugin.hmdPresent;

		if (useRecommendedMSAALevel && QualitySettings.antiAliasing != display.recommendedMSAALevel)
		{
			Debug.Log("The current MSAA level is " + QualitySettings.antiAliasing +
			", but the recommended MSAA level is " + display.recommendedMSAALevel +
			". Switching to the recommended level.");

			QualitySettings.antiAliasing = display.recommendedMSAALevel;
		}

		if (_wasHmdPresent && !isHmdPresent)
		{
			try
			{
				if (HMDLost != null)
					HMDLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

        if (!_wasHmdPresent && isHmdPresent)
		{
			try
			{
				if (HMDAcquired != null)
					HMDAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_wasHmdPresent = isHmdPresent;

		// Dispatch HMD mounted events.

		isUserPresent = OVRPlugin.userPresent;

		if (_wasUserPresent && !isUserPresent)
		{
			try
			{
				if (HMDUnmounted != null)
					HMDUnmounted();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!_wasUserPresent && isUserPresent)
		{
			try
			{
				if (HMDMounted != null)
					HMDMounted();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_wasUserPresent = isUserPresent;

		// Dispatch VR Focus events.

		hasVrFocus = OVRPlugin.hasVrFocus;

		if (_hadVrFocus && !hasVrFocus)
		{
			try
			{
				if (VrFocusLost != null)
					VrFocusLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

        if (!_hadVrFocus && hasVrFocus)
		{
			try
			{
				if (VrFocusAcquired != null)
					VrFocusAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		_hadVrFocus = hasVrFocus;


		// Changing effective rendering resolution dynamically according performance
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && UNITY_5_4_OR_NEWER

		if (enableAdaptiveResolution)
		{
            if (VR.VRSettings.renderScale < maxRenderScale)
            {
                // Allocate renderScale to max to avoid re-allocation
                VR.VRSettings.renderScale = maxRenderScale;
            }
            else
            {
                // Adjusting maxRenderScale in case app started with a larger renderScale value
                maxRenderScale = Mathf.Max(maxRenderScale, VR.VRSettings.renderScale);
            }
            float minViewportScale = minRenderScale / VR.VRSettings.renderScale;
            float recommendedViewportScale = OVRPlugin.GetEyeRecommendedResolutionScale() / VR.VRSettings.renderScale;
            recommendedViewportScale = Mathf.Clamp(recommendedViewportScale, minViewportScale, 1.0f);
            VR.VRSettings.renderViewportScale = recommendedViewportScale;
        }
#endif

		// Dispatch Audio Device events.

		string audioOutId = OVRPlugin.audioOutId;
		if (!prevAudioOutIdIsCached)
		{
			prevAudioOutId = audioOutId;
			prevAudioOutIdIsCached = true;
		}
		else if (audioOutId != prevAudioOutId)
		{
			try
			{
				if (AudioOutChanged != null)
					AudioOutChanged();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}

			prevAudioOutId = audioOutId;
		}

		string audioInId = OVRPlugin.audioInId;
		if (!prevAudioInIdIsCached)
		{
			prevAudioInId = audioInId;
			prevAudioInIdIsCached = true;
		}
		else if (audioInId != prevAudioInId)
		{
			try
			{
				if (AudioInChanged != null)
					AudioInChanged();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}

			prevAudioInId = audioInId;
		}

		// Dispatch tracking events.

		if (wasPositionTracked && !tracker.isPositionTracked)
		{
			try
			{
				if (TrackingLost != null)
					TrackingLost();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		if (!wasPositionTracked && tracker.isPositionTracked)
		{
			try
			{
				if (TrackingAcquired != null)
					TrackingAcquired();
			}
			catch (Exception e)
			{
				Debug.LogError("Caught Exception: " + e);
			}
		}

		wasPositionTracked = tracker.isPositionTracked;

		display.Update();
		OVRInput.Update();
    }

	private void LateUpdate()
	{
		OVRHaptics.Process();
	}

	private void FixedUpdate()
	{
		OVRInput.FixedUpdate();
	}

	/// <summary>
	/// Leaves the application/game and returns to the launcher/dashboard
	/// </summary>
	public void ReturnToLauncher()
	{
		// show the platform UI quit prompt
		OVRManager.PlatformUIConfirmQuit();
	}

#endregion

    public static void PlatformUIConfirmQuit()
	{
		if (!isHmdPresent)
			return;

		OVRPlugin.ShowUI(OVRPlugin.PlatformUI.ConfirmQuit);
    }

    public static void PlatformUIGlobalMenu()
	{
		if (!isHmdPresent)
			return;

		OVRPlugin.ShowUI(OVRPlugin.PlatformUI.GlobalMenu);
    }
}
