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

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

/// <summary>
///Scans the project and warns about the following conditions:
///Audio sources > 16
///Using MSAA levels other than recommended level
///GPU skinning is also probably usually ideal.
///Excessive pixel lights (>1 on Gear VR; >3 on Rift)
///Directional Lightmapping Modes (on Gear; use Non-Directional)
///Preload audio setting on individual audio clips
///Decompressing audio clips on load
///Disabling occlusion mesh
///Android target API level set to 19 or higher
///Unity skybox use (on by default, but if you can't see the skybox switching to Color is much faster on Gear)
///Lights marked as "baked" but that were not included in the last bake (and are therefore realtime).
///Lack of static batching and dynamic batching settings activated.
///Full screen image effects (Gear)
///Warn about large textures that are marked as uncompressed.
///32-bit depth buffer (use 16)
///Use of projectors (Gear; can be used carefully but slow enough to warrant a warning)
///Maybe in the future once quantified: Graphics jobs and IL2CPP on Gear.
///Real-time global illumination
///No texture compression, or non-ASTC texture compression as a global setting (Gear).
///Using deferred rendering
///Excessive texture resolution after LOD bias (>2k on Gear VR; >4k on Rift)
///Not using trilinear or aniso filtering and not generating mipmaps
///Excessive render scale (>1.2)
///Slow physics settings: Sleep Threshold < 0.005, Default Contact Offset < 0.01, Solver Iteration Count > 6
///Shadows on when approaching the geometry or draw call limits
///Non-static objects with colliders that are missing rigidbodies on themselves or in the parent chain.
///No initialization of GPU/CPU throttling settings, or init to dangerous values (-1 or > 3)  (Gear)
///Using inefficient effects: SSAO, motion blur, global fog, parallax mapping, etc.
///Too many Overlay layers
///Use of Standard shader or Standard Specular shader on Gear.  More generally, excessive use of multipass shaders (legacy specular, etc).
///Multiple cameras with clears (on Gear, potential for excessive fill cost)
///Excessive shader passes (>2)
///Material pointers that have been instanced in the editor (esp. if we could determine that the instance has no deltas from the original)
///Excessive draw calls (>150 on Gear VR; >2000 on Rift)
///Excessive tris or verts (>100k on Gear VR; >1M on Rift)
///Large textures, lots of prefabs in startup scene (for bootstrap optimization)
/// </summary>
public static class OVRLint
{
	//TODO: The following require reflection or static analysis.
	///Use of ONSP reflections (Gear)
	///Use of LoadLevelAsync / LoadLevelAdditiveAsync (on Gear, this kills frame rate so dramatically it's probably better to just go to black and load synchronously)
	///Use of Linq in non-editor assemblies (common cause of GCs).  Minor: use of foreach.
	///Use of Unity WWW (exceptionally high overhead for large file downloads, but acceptable for tiny gets).
	///Declared but empty Awake/Start/Update/OnCollisionEnter/OnCollisionExit/OnCollisionStay.  Also OnCollision* star methods that declare the Collision  argument but do not reference it (omitting it short-circuits the collision contact calculation).

	[MenuItem("Tools/Oculus/Lint")]
	static void RunCheck()
	{
		CheckStaticCommonIssues();
		#if UNITY_ANDROID
		CheckStaticAndroidIssues();
		#endif

		if (EditorApplication.isPlaying)
		{
			CheckRuntimeCommonIssues();
			#if UNITY_ANDROID
			CheckRuntimeAndroidIssues();
			#endif
		}
	}

	static void CheckStaticCommonIssues ()
	{
		if (QualitySettings.anisotropicFiltering != AnisotropicFiltering.Enable &&
			EditorUtility.DisplayDialog("Optimize Aniso?", "Anisotropic filtering is recommended for optimal quality and performance.", "Use recommended", "Skip"))
			QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

		#if UNITY_ANDROID
		int recommendedPixelLightCount = 1;
		#else
		int recommendedPixelLightCount = 3;
		#endif

		if (QualitySettings.pixelLightCount > recommendedPixelLightCount &&
			EditorUtility.DisplayDialog ("Optimize Pixel Light Count?", "For GPU performance use no more than " + recommendedPixelLightCount + " pixel light count.", "Use recommended", "Skip"))
			QualitySettings.pixelLightCount = recommendedPixelLightCount;

		if (!PlayerSettings.gpuSkinning &&
		    EditorUtility.DisplayDialog ("Optimize GPU Skinning?", "For CPU performance, please use GPU skinning.", "Use recommended", "Skip"))
			PlayerSettings.gpuSkinning = true;

#if UNITY_5_4_OR_NEWER
		if (!PlayerSettings.graphicsJobs &&
			EditorUtility.DisplayDialog ("Optimize Graphics Jobs?", "For CPU performance, please use graphics jobs.", "Use recommended", "Skip"))
			PlayerSettings.graphicsJobs = true;
#endif

		if ((!PlayerSettings.MTRendering || !PlayerSettings.mobileMTRendering) &&
		    EditorUtility.DisplayDialog ("Optimize MT Rendering?", "For CPU performance, please enable multithreaded rendering.", "Use recommended", "Skip"))
			PlayerSettings.MTRendering = PlayerSettings.mobileMTRendering = true;

		if ((PlayerSettings.renderingPath == RenderingPath.DeferredShading ||
		    PlayerSettings.renderingPath == RenderingPath.DeferredLighting ||
		    PlayerSettings.mobileRenderingPath == RenderingPath.DeferredShading ||
		    PlayerSettings.mobileRenderingPath == RenderingPath.DeferredLighting) &&
		    EditorUtility.DisplayDialog ("Optimize Rendering Path?", "For CPU performance, please do not use deferred shading.", "Use recommended", "Skip"))
			PlayerSettings.renderingPath = PlayerSettings.mobileRenderingPath = RenderingPath.Forward;

#if UNITY_5_5_OR_NEWER
		if (PlayerSettings.stereoRenderingPath == StereoRenderingPath.MultiPass &&
		    EditorUtility.DisplayDialog ("Optimize Stereo Rendering?", "For CPU performance, please enable single-pass or instanced stereo rendering.", "Use recommended", "Skip"))
			PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
#elif UNITY_5_4_OR_NEWER
		if (!PlayerSettings.singlePassStereoRendering &&
			EditorUtility.DisplayDialog ("Optimize Stereo Rendering?", "For CPU performance, please enable single-pass or instanced stereo rendering.", "Use recommended", "Skip"))
			PlayerSettings.singlePassStereoRendering = true;
#endif

		if (RenderSettings.skybox &&
			EditorUtility.DisplayDialog ("Optimize Clearing?", "For GPU performance, please don't use Unity's built-in Skybox.", "Use recommended", "Skip"))
			RenderSettings.skybox = null;

		if (LightmapSettings.lightmaps.Length > 0 && LightmapSettings.lightmapsMode != LightmapsMode.NonDirectional &&
		    EditorUtility.DisplayDialog ("Optimize Lightmap Directionality?", "For GPU performance, please don't use directional lightmaps.", "Use recommended", "Skip"))
			LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

#if UNITY_5_4_OR_NEWER
		if (Lightmapping.realtimeGI &&
			EditorUtility.DisplayDialog ("Optimize Realtime GI?", "For GPU performance, please don't use real-time global illumination.", "Use recommended", "Skip"))
			Lightmapping.realtimeGI = false;
#endif

		bool bakeLights = false;
		var lights = GameObject.FindObjectsOfType<Light> ();
		for (int i = 0; i < lights.Length; ++i) {
#if UNITY_5_4_OR_NEWER
			if (lights [i].type != LightType.Directional && !lights [i].isBaked &&
				EditorUtility.DisplayDialog ("Optimize Light Baking?", "For GPU performance, please bake light " + lights [i].name, "Use recommended", "Skip"))
				bakeLights = true;
#endif

			if (lights [i].shadows != LightShadows.None &&
			    EditorUtility.DisplayDialog ("Optimize Shadows?", "For CPU performance, please disable shadows on light " + lights [i].name, "Use recommended", "Skip"))
				lights [i].shadows = LightShadows.None;
		}

		if (bakeLights) {
			Debug.Log ("Initiating lightmap bake...");
			Lightmapping.Bake ();
		}

		var sources = GameObject.FindObjectsOfType<AudioSource> ();
		if (sources.Length > 16 &&
		    EditorUtility.DisplayDialog ("Optimize Audio Source Count?", "For CPU performance, please disable all but the top 16 AudioSources.", "Use recommended", "Skip")) {
			Array.Sort(sources, (a, b) => { return a.priority.CompareTo(b.priority); });
			for (int i = 16; i < sources.Length; ++i) {
				sources[i].enabled = false;
			}
		}

		var clips = GameObject.FindObjectsOfType<AudioClip> ();
		for (int i = 0; i < clips.Length; ++i) {
			if (clips [i].loadType == AudioClipLoadType.DecompressOnLoad)
				Debug.LogWarning("For fast loading, please don't use decompress on load for clip " + clips [i].name);

			if (clips [i].preloadAudioData)
				Debug.LogWarning("For fast loading, please don't preload audio data for clip " + clips [i].name);
		}

		if (Physics.defaultContactOffset < 0.01f &&
		    EditorUtility.DisplayDialog ("Optimize Contact Offset?", "For CPU performance, please don't use default contact offset below 0.01.", "Use recommended", "Skip"))
			Physics.defaultContactOffset = 0.01f;

		if (Physics.sleepThreshold < 0.005f &&
			EditorUtility.DisplayDialog ("Optimize Sleep Threshold?", "For CPU performance, please don't use sleep threshold below 0.005.", "Use recommended", "Skip"))
			Physics.sleepThreshold = 0.005f;

#if UNITY_5_4_OR_NEWER
		if (Physics.defaultSolverIterations > 8 &&
		    EditorUtility.DisplayDialog ("Optimize Solver Iterations?", "For CPU performance, please don't use excessive solver iteration counts.", "Use recommended", "Skip"))
			Physics.defaultSolverIterations = 8;
#endif

		var colliders = GameObject.FindObjectsOfType<Collider> ();
		for (int i = 0; i < colliders.Length; ++i) {
			if (!colliders [i].gameObject.isStatic && colliders [i].attachedRigidbody == null && colliders [i].GetComponent<Rigidbody>() == null &&
			    EditorUtility.DisplayDialog ("Optimize Nonstatic Collider?", "For CPU performance, please attach a Rigidbody to non-static collider " + colliders [i].name, "Use recommended", "Skip")) {
				var rb = colliders [i].gameObject.AddComponent<Rigidbody> ();
				rb.isKinematic = true;
			}
		}

		var materials = Resources.FindObjectsOfTypeAll<Material> ();
		for (int i = 0; i < materials.Length; ++i) {
			if (materials [i].shader.name.Contains ("Parallax") || materials [i].IsKeywordEnabled ("_PARALLAXMAP") &&
			    EditorUtility.DisplayDialog ("Optimize Shading?", "For GPU performance, please don't use parallax-mapped materials.", "Use recommended", "Skip")) {
				if (materials [i].IsKeywordEnabled ("_PARALLAXMAP"))
					materials [i].DisableKeyword ("_PARALLAXMAP");

				if (materials [i].shader.name.Contains ("Parallax")) {
					var newName = materials[i].shader.name.Replace ("-ParallaxSpec", "-BumpSpec");
					newName = newName.Replace ("-Parallax", "-Bump");
					var newShader = Shader.Find (newName);
					if (newShader)
						materials [i].shader = newShader;
					else
						Debug.LogWarning ("Unable to find a replacement for shader " + materials [i].shader.name);
				}
			}
		}

		var renderers = GameObject.FindObjectsOfType<Renderer> ();
		for (int i = 0; i < renderers.Length; ++i) {
			if (renderers [i].sharedMaterial == null)
				Debug.LogWarning ("Please avoid instanced materials on renderer " + renderers [i].name);
		}
		
		var overlays = GameObject.FindObjectsOfType<OVROverlay> ();
		if (overlays.Length > 4 &&
		    EditorUtility.DisplayDialog ("Optimize VR Layer Count?", "For GPU performance, please use 4 or fewer VR layers.", "Use recommended", "Skip")) {
			for (int i = 4; i < OVROverlay.instances.Length; ++i)
				OVROverlay.instances[i].enabled = false;
		}
	}

	static void CheckRuntimeCommonIssues()
	{
		if (!OVRPlugin.occlusionMesh &&
			EditorUtility.DisplayDialog ("Optimize Occlusion Mesh?", "For GPU performance, please use occlusion mesh.", "Use recommended", "Skip"))
			OVRPlugin.occlusionMesh = true;
		
		if (QualitySettings.antiAliasing != OVRManager.display.recommendedMSAALevel &&
			EditorUtility.DisplayDialog("Optimize MSAA?", "Multisample antialiasing level " + OVRManager.display.recommendedMSAALevel + " is recommended for optimal quality and performance.", "Use recommended", "Skip"))
			QualitySettings.antiAliasing = OVRManager.display.recommendedMSAALevel;

		if (UnityEngine.VR.VRSettings.renderScale > 1.5 &&
			EditorUtility.DisplayDialog ("Optimize Render Scale?", "For CPU performance, please don't use render scale over 1.5.", "Use recommended", "Skip"))
			UnityEngine.VR.VRSettings.renderScale = 1.5f;
	}

	static void CheckStaticAndroidIssues ()
	{
		AndroidSdkVersions recommendedAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevel19;
		if ((int)PlayerSettings.Android.minSdkVersion < (int)recommendedAndroidSdkVersion &&
			EditorUtility.DisplayDialog ("Optimize Android API Level?", "To avoid legacy work-arounds, please require at least API level " + (int)recommendedAndroidSdkVersion, "Use recommended", "Skip"))
			PlayerSettings.Android.minSdkVersion = recommendedAndroidSdkVersion;
		
		var materials = Resources.FindObjectsOfTypeAll<Material> ();
		for (int i = 0; i < materials.Length; ++i) {
			if (materials [i].IsKeywordEnabled ("_SPECGLOSSMAP") || materials [i].IsKeywordEnabled ("_METALLICGLOSSMAP") &&
			    EditorUtility.DisplayDialog ("Optimize Specular Material?", "For GPU performance, please don't use specular shader on material " + materials [i].name, "Use recommended", "Skip")) {
				materials [i].DisableKeyword ("_SPECGLOSSMAP");
				materials [i].DisableKeyword ("_METALLICGLOSSMAP");
			}

			if (materials [i].passCount > 1)
				Debug.LogWarning ("Please use 2 or fewer passes in material " + materials [i].name);
		}

#if UNITY_5_5_OR_NEWER
		ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(UnityEditor.BuildTargetGroup.Android);
		if (backend != UnityEditor.ScriptingImplementation.IL2CPP &&
			EditorUtility.DisplayDialog ("Optimize Scripting Backend?", "For CPU performance, please use IL2CPP.", "Use recommended", "Skip"))
			PlayerSettings.SetScriptingBackend(UnityEditor.BuildTargetGroup.Android, UnityEditor.ScriptingImplementation.IL2CPP);
#else
		ScriptingImplementation backend = (ScriptingImplementation)PlayerSettings.GetPropertyInt("ScriptingBackend", UnityEditor.BuildTargetGroup.Android);
		if (backend != UnityEditor.ScriptingImplementation.IL2CPP &&
			EditorUtility.DisplayDialog ("Optimize Scripting Backend?", "For CPU performance, please use IL2CPP.", "Use recommended", "Skip"))
			PlayerSettings.SetPropertyInt("ScriptingBackend", (int)UnityEditor.ScriptingImplementation.IL2CPP, UnityEditor.BuildTargetGroup.Android);
#endif

		var monoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour> ();
		System.Type effectBaseType = System.Type.GetType ("UnityStandardAssets.ImageEffects.PostEffectsBase");
		if (effectBaseType != null) {
			for (int i = 0; i < monoBehaviours.Length; ++i) {
				if (monoBehaviours [i].GetType ().IsSubclassOf (effectBaseType))
					Debug.LogWarning ("Please don't use image effects.");
			}
		}

		var textures = Resources.FindObjectsOfTypeAll<Texture2D> ();

		int maxTextureSize = 1024 * (1 << QualitySettings.masterTextureLimit);
		maxTextureSize = maxTextureSize * maxTextureSize;

		for (int i = 0; i < textures.Length; ++i) {
			if (textures [i].filterMode == FilterMode.Trilinear && textures [i].mipmapCount == 1 &&
			    EditorUtility.DisplayDialog ("Optimize Texture Filtering?", "For GPU performance, please generate mipmaps or disable trilinear filtering for texture " + textures [i].name, "Use recommended", "Skip"))
				textures [i].filterMode = FilterMode.Bilinear;
		}

		var projectors = GameObject.FindObjectsOfType<Projector> ();
		if (projectors.Length > 0 &&
		    EditorUtility.DisplayDialog ("Optimize Projectors?", "For GPU performance, please don't use projectors.", "Use recommended", "Skip")) {
			for (int i = 0; i < projectors.Length; ++i)
				projectors[i].enabled = false;
		}

		if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC &&
		    EditorUtility.DisplayDialog ("Optimize Texture Compression?", "For GPU performance, please use ASTC.", "Use recommended", "Skip"))
			EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

		var cameras = GameObject.FindObjectsOfType<Camera> ();
		int clearCount = 0;
		for (int i = 0; i < cameras.Length; ++i) {
			if (cameras [i].clearFlags != CameraClearFlags.Nothing && cameras [i].clearFlags != CameraClearFlags.Depth)
				++clearCount;
		}

		if (clearCount > 2)
			Debug.LogWarning ("Please use 2 or fewer clears.");
	}

	static void CheckRuntimeAndroidIssues()
	{
		if (UnityStats.usedTextureMemorySize + UnityStats.vboTotalBytes > 1000000)
			Debug.LogWarning ("Please use less than 1GB of vertex and texture memory.");
		
		if (OVRManager.cpuLevel < 0 || OVRManager.cpuLevel > 2 &&
		    EditorUtility.DisplayDialog ("Optimize CPU level?", "For battery life, please use a safe CPU level.", "Use recommended", "Skip"))
			OVRManager.cpuLevel = 2;

		if (OVRManager.gpuLevel < 0 || OVRManager.gpuLevel > 2 &&
			EditorUtility.DisplayDialog ("Optimize CPU level?", "For battery life, please use a safe GPU level.", "Use recommended", "Skip"))
			OVRManager.gpuLevel = 2;

		if (UnityStats.triangles > 100000 || UnityStats.vertices > 100000)
			Debug.LogWarning ("Please use less than 100000 triangles or vertices.");

		if (UnityStats.drawCalls > 100)
			Debug.LogWarning ("Please use less than 100 draw calls.");
	}
}

#endif