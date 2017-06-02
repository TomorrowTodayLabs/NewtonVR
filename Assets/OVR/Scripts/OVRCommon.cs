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
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// Miscellaneous extension methods that any script can use.
/// </summary>
public static class OVRExtensions
{
	/// <summary>
	/// Converts the given world-space transform to an OVRPose in tracking space.
	/// </summary>
	public static OVRPose ToTrackingSpacePose(this Transform transform)
	{
		OVRPose headPose;
		headPose.position = UnityEngine.VR.InputTracking.GetLocalPosition(UnityEngine.VR.VRNode.Head);
		headPose.orientation = UnityEngine.VR.InputTracking.GetLocalRotation(UnityEngine.VR.VRNode.Head);

		var ret = headPose * transform.ToHeadSpacePose();

		return ret;
	}

	/// <summary>
	/// Converts the given world-space transform to an OVRPose in head space.
	/// </summary>
	public static OVRPose ToHeadSpacePose(this Transform transform)
	{
		return Camera.current.transform.ToOVRPose().Inverse() * transform.ToOVRPose();
	}

	internal static OVRPose ToOVRPose(this Transform t, bool isLocal = false)
	{
		OVRPose pose;
		pose.orientation = (isLocal) ? t.localRotation : t.rotation;
		pose.position = (isLocal) ? t.localPosition : t.position;
		return pose;
	}
	
	internal static void FromOVRPose(this Transform t, OVRPose pose, bool isLocal = false)
	{
		if (isLocal)
		{
			t.localRotation = pose.orientation;
			t.localPosition = pose.position;
		}
		else
		{
			t.rotation = pose.orientation;
			t.position = pose.position;
		}
	}

	internal static OVRPose ToOVRPose(this OVRPlugin.Posef p)
	{
		return new OVRPose()
		{
			position = new Vector3(p.Position.x, p.Position.y, -p.Position.z),
			orientation = new Quaternion(-p.Orientation.x, -p.Orientation.y, p.Orientation.z, p.Orientation.w)
		};
	}
	
	internal static OVRTracker.Frustum ToFrustum(this OVRPlugin.Frustumf f)
	{
		return new OVRTracker.Frustum()
		{
			nearZ = f.zNear,
			farZ = f.zFar,
			
			fov = new Vector2()
			{
				x = Mathf.Rad2Deg * f.fovX,
				y = Mathf.Rad2Deg * f.fovY
			}
		};
	}

	internal static Color FromColorf(this OVRPlugin.Colorf c)
	{
		return new Color() { r = c.r, g = c.g, b = c.b, a = c.a };
	}

	internal static OVRPlugin.Colorf ToColorf(this Color c)
	{
		return new OVRPlugin.Colorf() { r = c.r, g = c.g, b = c.b, a = c.a };
	}

	internal static Vector3 FromVector3f(this OVRPlugin.Vector3f v)
	{
		return new Vector3() { x = v.x, y = v.y, z = v.z };
	}

	internal static Vector3 FromFlippedZVector3f(this OVRPlugin.Vector3f v)
	{
		return new Vector3() { x = v.x, y = v.y, z = -v.z };
	}

	internal static OVRPlugin.Vector3f ToVector3f(this Vector3 v)
	{
		return new OVRPlugin.Vector3f() { x = v.x, y = v.y, z = v.z };
	}

	internal static OVRPlugin.Vector3f ToFlippedZVector3f(this Vector3 v)
	{
		return new OVRPlugin.Vector3f() { x = v.x, y = v.y, z = -v.z };
	}

	internal static Quaternion FromQuatf(this OVRPlugin.Quatf q)
	{
		return new Quaternion() { x = q.x, y = q.y, z = q.z, w = q.w };
	}

	internal static Quaternion FromFlippedZQuatf(this OVRPlugin.Quatf q)
	{
		return new Quaternion() { x = -q.x, y = -q.y, z = q.z, w = q.w };
	}

	internal static OVRPlugin.Quatf ToQuatf(this Quaternion q)
	{
		return new OVRPlugin.Quatf() { x = q.x, y = q.y, z = q.z, w = q.w };
	}

	internal static OVRPlugin.Quatf ToFlippedZQuatf(this Quaternion q)
	{
		return new OVRPlugin.Quatf() { x = -q.x, y = -q.y, z = q.z, w = q.w };
	}
}

/// <summary>
/// An affine transformation built from a Unity position and orientation.
/// </summary>
[System.Serializable]
public struct OVRPose
{
	/// <summary>
	/// A pose with no translation or rotation.
	/// </summary>
	public static OVRPose identity
	{
		get {
			return new OVRPose()
			{
				position = Vector3.zero,
				orientation = Quaternion.identity
			};
		}
	}

	public override bool Equals(System.Object obj) 
	{
		return obj is OVRPose && this == (OVRPose)obj;
	}

	public override int GetHashCode() 
	{
		return position.GetHashCode() ^ orientation.GetHashCode();
	}

	public static bool operator ==(OVRPose x, OVRPose y) 
	{
		return x.position == y.position && x.orientation == y.orientation;
	}

	public static bool operator !=(OVRPose x, OVRPose y) 
	{
		return !(x == y);
	}

	/// <summary>
	/// The position.
	/// </summary>
	public Vector3 position;

	/// <summary>
	/// The orientation.
	/// </summary>
	public Quaternion orientation;

	/// <summary>
	/// Multiplies two poses.
	/// </summary>
	public static OVRPose operator*(OVRPose lhs, OVRPose rhs)
	{
		var ret = new OVRPose();
		ret.position = lhs.position + lhs.orientation * rhs.position;
		ret.orientation = lhs.orientation * rhs.orientation;
		return ret;
	}

	/// <summary>
	/// Computes the inverse of the given pose.
	/// </summary>
	public OVRPose Inverse()
	{
		OVRPose ret;
		ret.orientation = Quaternion.Inverse(orientation);
		ret.position = ret.orientation * -position;
		return ret;
	}

	/// <summary>
	/// Converts the pose from left- to right-handed or vice-versa.
	/// </summary>
	internal OVRPose flipZ()
	{
		var ret = this;
		ret.position.z = -ret.position.z;
		ret.orientation.z = -ret.orientation.z;
		ret.orientation.w = -ret.orientation.w;
		return ret;
	}

	internal OVRPlugin.Posef ToPosef()
	{
		return new OVRPlugin.Posef()
		{
			Position = position.ToVector3f(),
			Orientation = orientation.ToQuatf()
		};
	}
}

/// <summary>
/// Encapsulates an 8-byte-aligned of unmanaged memory.
/// </summary>
public class OVRNativeBuffer : IDisposable
{
	private bool disposed = false;
	private int m_numBytes = 0;
	private IntPtr m_ptr = IntPtr.Zero;

	/// <summary>
	/// Creates a buffer of the specified size.
	/// </summary>
	public OVRNativeBuffer(int numBytes)
	{
		Reallocate(numBytes);
	}

	/// <summary>
	/// Releases unmanaged resources and performs other cleanup operations before the <see cref="OVRNativeBuffer"/> is
	/// reclaimed by garbage collection.
	/// </summary>
	~OVRNativeBuffer()
	{
		Dispose(false);
	}

	/// <summary>
	/// Reallocates the buffer with the specified new size.
	/// </summary>
	public void Reset(int numBytes)
	{
		Reallocate(numBytes);
	}

	/// <summary>
	/// The current number of bytes in the buffer.
	/// </summary>
	public int GetCapacity()
	{
		return m_numBytes;
	}

	/// <summary>
	/// A pointer to the unmanaged memory in the buffer, starting at the given offset in bytes.
	/// </summary>
	public IntPtr GetPointer(int byteOffset = 0)
	{
		if (byteOffset < 0 || byteOffset >= m_numBytes)
			return IntPtr.Zero;
		return (byteOffset == 0) ? m_ptr : new IntPtr(m_ptr.ToInt64() + byteOffset);
	}

	/// <summary>
	/// Releases all resource used by the <see cref="OVRNativeBuffer"/> object.
	/// </summary>
	/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="OVRNativeBuffer"/>. The <see cref="Dispose"/>
	/// method leaves the <see cref="OVRNativeBuffer"/> in an unusable state. After calling <see cref="Dispose"/>, you must
	/// release all references to the <see cref="OVRNativeBuffer"/> so the garbage collector can reclaim the memory that
	/// the <see cref="OVRNativeBuffer"/> was occupying.</remarks>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (disposed)
			return;

		if (disposing)
		{
			// dispose managed resources
		}

		// dispose unmanaged resources
		Release();

		disposed = true;
	}

	private void Reallocate(int numBytes)
	{
		Release();

		if (numBytes > 0)
		{
			m_ptr = Marshal.AllocHGlobal(numBytes);
			m_numBytes = numBytes;
		}
		else
		{
			m_ptr = IntPtr.Zero;
			m_numBytes = 0;
		}
	}

	private void Release()
	{
		if (m_ptr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(m_ptr);
			m_ptr = IntPtr.Zero;
			m_numBytes = 0;
		}
	}
}
