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
using System;
using System.Collections;
using System.Runtime.InteropServices;

/// <summary>
/// Usage: attach this script under your camera object, and point ovrOverlayObj to your overlay owner object.
/// Note: 
/// 1) your camera should use renderTexture 
/// 2) your need make sure your overlay camera was rendered before you main camera (eg. using camera depth), so the renderTexture will be available before being used.
///
/// This is a helper class to setup renderTexture to OVROverlay object
/// It did a few things
/// 1) Clear the renderTarget's border to alpha = 0 for avoiding artifacts on mobile 
/// 2) Triple buffer the render results before sending to the overlay, which is a requirement for time warping render target
/// </summary>
public class OVRRTOverlayConnector : MonoBehaviour
{
	/// <summary>
	/// OVROverlay texture required alpha = 0 border for avoiding artifacts
	/// </summary>
	public int alphaBorderSizePixels = 3;

	/// <summary>
	/// Triple buffer the render target
	/// </summary>
	const int overlayRTChainSize = 3;
	private int overlayRTIndex = 0;
	private IntPtr[] overlayTexturePtrs = new IntPtr[overlayRTChainSize]; 
	private RenderTexture[] overlayRTChain = new RenderTexture[overlayRTChainSize];

	/// <summary>
	/// Destination OVROverlay target object
	/// </summary>
	public GameObject ovrOverlayObj;
	private RenderTexture srcRT;
	private Camera ownerCamera;

	/// <summary>
	///  Reconstruct render texture chain if ownerCamera's targetTexture was changed
	/// </summary>
	public void RefreshRenderTextureChain()
	{
		srcRT = ownerCamera.targetTexture;
		Debug.Assert(srcRT);
		ConstructRenderTextureChain();
	}

/// <summary>
/// Triple buffer the textures applying to overlay
/// </summary>
	void ConstructRenderTextureChain()
	{
		for (int i = 0; i < overlayRTChainSize; i++)
		{
			overlayRTChain[i] = new RenderTexture(srcRT.width, srcRT.height, 1, srcRT.format, RenderTextureReadWrite.sRGB);
			overlayRTChain[i].antiAliasing = 1;
			overlayRTChain[i].depth = 0;
			overlayRTChain[i].wrapMode = TextureWrapMode.Clamp;
			overlayRTChain[i].hideFlags = HideFlags.HideAndDontSave;
			overlayRTChain[i].Create();
			overlayTexturePtrs[i] = overlayRTChain[i].GetNativeTexturePtr();
		}
	}

	void Start ()
	{
		ownerCamera = GetComponent<Camera>();
		Debug.Assert(ownerCamera);
		srcRT = ownerCamera.targetTexture;
		Debug.Assert(srcRT);
		ConstructRenderTextureChain();
	}

#if UNITY_ANDROID
	/// <summary>
	/// Alpha border cleaning
	/// </summary>
	private bool borderCleaned = false;
	void OnPreRender()
	{
		Debug.Assert(srcRT);
		if (!borderCleaned)
		{
			GL.Clear(false, true, new Color(0, 0, 0, 0));
			GetComponent<Camera>().pixelRect = new Rect(alphaBorderSizePixels, alphaBorderSizePixels, srcRT.width - 2 * alphaBorderSizePixels, srcRT.height - 2 * alphaBorderSizePixels);
			borderCleaned = true;
		}
	}
#endif

	/// <summary>
	/// Copy camera's render target to triple buffered texture array and send it to OVROverlay object
	/// </summary>
	void OnPostRender()
	{
		if (srcRT)
		{
			Graphics.Blit(srcRT, overlayRTChain[overlayRTIndex]);
			OVROverlay ovrOverlay = ovrOverlayObj.GetComponent<OVROverlay>();
			Debug.Assert(ovrOverlay);
			ovrOverlay.OverrideOverlayTextureInfo(overlayRTChain[overlayRTIndex], overlayTexturePtrs[overlayRTIndex], UnityEngine.VR.VRNode.LeftEye);
			overlayRTIndex++;
			overlayRTIndex = overlayRTIndex % overlayRTChainSize;
		}
	}
}
