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

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;

[InitializeOnLoad]
class OVRMoonlightLoader
{
    static OVRMoonlightLoader()
	{
		EnforceInputManagerBindings();
		EditorApplication.update += EnforceBundleId;
		EditorApplication.update += EnforceVRSupport;

		if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
			return;

		if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.LandscapeLeft)
		{
			Debug.Log("MoonlightLoader: Setting orientation to Landscape Left");
			// Default screen orientation must be set to landscape left.
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
		}

		if (!PlayerSettings.virtualRealitySupported)
		{
			// NOTE: This value should not affect the main window surface
			// when Built-in VR support is enabled.

			// NOTE: On Adreno Lollipop, it is an error to have antiAliasing set on the
			// main window surface with front buffer rendering enabled. The view will
			// render black.
			// On Adreno KitKat, some tiling control modes will cause the view to render
			// black.
			if (QualitySettings.antiAliasing != 0 && QualitySettings.antiAliasing != 1)
			{
				Debug.Log("MoonlightLoader: Disabling antiAliasing");
				QualitySettings.antiAliasing = 1;
			}
		}

		if (QualitySettings.vSyncCount != 0)
		{
			Debug.Log("MoonlightLoader: Setting vsyncCount to 0");
			// We sync in the TimeWarp, so we don't want unity syncing elsewhere.
			QualitySettings.vSyncCount = 0;
		}
	}

	static void EnforceVRSupport()
	{
		if (PlayerSettings.virtualRealitySupported)
			return;
		
		var mgrs = GameObject.FindObjectsOfType<OVRManager>();
		for (int i = 0; i < mgrs.Length; ++i)
		{
			if (mgrs [i].isActiveAndEnabled)
			{
				Debug.Log ("Enabling Unity VR support");
				PlayerSettings.virtualRealitySupported = true;
				return;
			}
		}
	}

	private static void EnforceBundleId()
	{
		if (!PlayerSettings.virtualRealitySupported)
			return;

#if UNITY_5_6_OR_NEWER
		if (PlayerSettings.applicationIdentifier == "" || PlayerSettings.applicationIdentifier == "com.Company.ProductName")
		{
			string defaultBundleId = "com.oculus.UnitySample";
			Debug.LogWarning("\"" + PlayerSettings.applicationIdentifier + "\" is not a valid bundle identifier. Defaulting to \"" + defaultBundleId + "\".");
			PlayerSettings.applicationIdentifier = defaultBundleId;
		}
#else
		if (PlayerSettings.bundleIdentifier == "" || PlayerSettings.bundleIdentifier == "com.Company.ProductName")
		{
			string defaultBundleId = "com.oculus.UnitySample";
			Debug.LogWarning("\"" + PlayerSettings.bundleIdentifier + "\" is not a valid bundle identifier. Defaulting to \"" + defaultBundleId + "\".");
			PlayerSettings.bundleIdentifier = defaultBundleId;
		}
#endif
	}

	private static void EnforceInputManagerBindings()
	{
		try
		{
			BindAxis(new Axis() { name = "Oculus_GearVR_LThumbstickX",  axis =  0,               });
			BindAxis(new Axis() { name = "Oculus_GearVR_LThumbstickY",  axis =  1, invert = true });
			BindAxis(new Axis() { name = "Oculus_GearVR_RThumbstickX",  axis =  2,               });
			BindAxis(new Axis() { name = "Oculus_GearVR_RThumbstickY",  axis =  3, invert = true });
			BindAxis(new Axis() { name = "Oculus_GearVR_DpadX",         axis =  4,               });
			BindAxis(new Axis() { name = "Oculus_GearVR_DpadY",         axis =  5, invert = true });
			BindAxis(new Axis() { name = "Oculus_GearVR_LIndexTrigger", axis = 12,               });
			BindAxis(new Axis() { name = "Oculus_GearVR_RIndexTrigger", axis = 11,               });
		}
		catch
		{
			Debug.LogError("Failed to apply Oculus GearVR input manager bindings.");
		}
	}

	private class Axis
	{
		public string name = String.Empty;
		public string descriptiveName = String.Empty;
		public string descriptiveNegativeName = String.Empty;
		public string negativeButton = String.Empty;
		public string positiveButton = String.Empty;
		public string altNegativeButton = String.Empty;
		public string altPositiveButton = String.Empty;
		public float gravity = 0.0f;
		public float dead = 0.001f;
		public float sensitivity = 1.0f;
		public bool snap = false;
		public bool invert = false;
		public int type = 2;
		public int axis = 0;
		public int joyNum = 0;
	}

	private static void BindAxis(Axis axis)
	{
		SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
		SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

		SerializedProperty axisIter = axesProperty.Copy();
		axisIter.Next(true);
		axisIter.Next(true);
		while (axisIter.Next(false))
		{
			if (axisIter.FindPropertyRelative("m_Name").stringValue == axis.name)
			{
				// Axis already exists. Don't create binding.
				return;
			}
		}

		axesProperty.arraySize++;
		serializedObject.ApplyModifiedProperties();

		SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
		axisProperty.FindPropertyRelative("m_Name").stringValue = axis.name;
		axisProperty.FindPropertyRelative("descriptiveName").stringValue = axis.descriptiveName;
		axisProperty.FindPropertyRelative("descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
		axisProperty.FindPropertyRelative("negativeButton").stringValue = axis.negativeButton;
		axisProperty.FindPropertyRelative("positiveButton").stringValue = axis.positiveButton;
		axisProperty.FindPropertyRelative("altNegativeButton").stringValue = axis.altNegativeButton;
		axisProperty.FindPropertyRelative("altPositiveButton").stringValue = axis.altPositiveButton;
		axisProperty.FindPropertyRelative("gravity").floatValue = axis.gravity;
		axisProperty.FindPropertyRelative("dead").floatValue = axis.dead;
		axisProperty.FindPropertyRelative("sensitivity").floatValue = axis.sensitivity;
		axisProperty.FindPropertyRelative("snap").boolValue = axis.snap;
		axisProperty.FindPropertyRelative("invert").boolValue = axis.invert;
		axisProperty.FindPropertyRelative("type").intValue = axis.type;
		axisProperty.FindPropertyRelative("axis").intValue = axis.axis;
		axisProperty.FindPropertyRelative("joyNum").intValue = axis.joyNum;
		serializedObject.ApplyModifiedProperties();
	}
}

