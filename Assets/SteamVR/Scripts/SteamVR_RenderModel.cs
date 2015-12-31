//========= Copyright 2014, Valve Corporation, All rights reserved. ===========
//
// Purpose: Render model of associated tracked object
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Valve.VR;

[ExecuteInEditMode]
public class SteamVR_RenderModel : MonoBehaviour
{
	public SteamVR_TrackedObject.EIndex index = SteamVR_TrackedObject.EIndex.None;
	public string modelOverride;

	// Enable to print out when render models are loaded.
	public bool verbose = false;

	// If available, break down into separate components instead of loading as a single mesh.
	public bool createComponents = true;

	// Update transforms of components at runtime to reflect user action.
	public bool updateDynamically = true;

	// Name of the sub-object which represents the "local" coordinate space for each component.
	public const string k_localTransformName = "attach";

	// Cached name of this render model for updating component transforms at runtime.
	public string renderModelName { get; private set; }

	// If someone knows how to keep these from getting cleaned up every time
	// you exit play mode, let me know.  I've tried marking the RenderModel
	// class below as [System.Serializable] and switching to normal public
	// variables for mesh and material to get them to serialize properly,
	// as well as tried marking the mesh and material objects as
	// DontUnloadUnusedAsset, but Unity was still unloading them.
	// The hashtable is preserving its entries, but the mesh and material
	// variables are going null.

	public class RenderModel
	{
		public RenderModel(Mesh mesh, Material material)
		{
			this.mesh = mesh;
			this.material = material;
		}
		public Mesh mesh { get; private set; }
		public Material material { get; private set; }
	}

	public static Hashtable models = new Hashtable();
	public static Hashtable materials = new Hashtable();

	// Helper class to load render models interface on demand and clean up when done.
	public sealed class RenderModelInterfaceHolder : System.IDisposable
	{
		private CVRRenderModels _instance;
		public CVRRenderModels instance
		{
			get
			{
				if (_instance == null)
				{
					var error = EVRInitError.None;
					if (!SteamVR.active)
					{
						OpenVR.Init(ref error, EVRApplicationType.VRApplication_Other);
						if (error != EVRInitError.None)
							return null;
					}

					var pRenderModels = OpenVR.GetGenericInterface(OpenVR.IVRRenderModels_Version, ref error);
					if (pRenderModels == System.IntPtr.Zero || error != EVRInitError.None)
					{
						Debug.LogError("Failed to load IVRRenderModels interface version " + OpenVR.IVRRenderModels_Version);
						if (!SteamVR.active)
							OpenVR.Shutdown();
						return null;
					}

					_instance = new CVRRenderModels(pRenderModels);
				}
				return _instance;
			}
		}
		public void Dispose()
		{
			if (_instance != null)
			{
				if (!SteamVR.active)
					OpenVR.Shutdown();
			}
		}
	}

	private void OnDeviceConnected(params object[] args)
	{
		var i = (int)args[0];
		if (i != (int)index)
			return;

		var connected = (bool)args[1];
		if (connected)
		{
			UpdateModel();
		}
		else
		{
			var meshRenderer = GetComponent<MeshRenderer>();
			if (meshRenderer != null)
				Object.DestroyImmediate(meshRenderer);
			var meshFilter = GetComponent<MeshFilter>();
			if (meshFilter != null)
				Object.DestroyImmediate(meshFilter);
		}
	}

	public void UpdateModel()
	{
		var vr = SteamVR.instance;
		var error = ETrackedPropertyError.TrackedProp_Success;
		var capacity = vr.hmd.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0, ref error);
		if (capacity <= 1)
		{
			Debug.LogError("Failed to get render model name for tracked object " + index);
			return;
		}

		var buffer = new System.Text.StringBuilder((int)capacity);
		vr.hmd.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, buffer, capacity, ref error);

		SetModel(buffer.ToString());
	}

	private void SetModel(string renderModelName)
	{
		// Strip mesh filter and renderer as these will get re-added if needed.
		var meshRenderer = GetComponent<MeshRenderer>();
		if (meshRenderer != null)
			Object.DestroyImmediate(meshRenderer);
		var meshFilter = GetComponent<MeshFilter>();
		if (meshFilter != null)
			Object.DestroyImmediate(meshFilter);

		using (var holder = new RenderModelInterfaceHolder())
		{
			if (createComponents)
			{
				if (LoadComponents(holder, renderModelName))
				{
					this.renderModelName = renderModelName;
					return;
				}

				Debug.Log("Render model does not support components, falling back to single mesh.");
			}

			if (!string.IsNullOrEmpty(renderModelName))
			{
				var model = models[renderModelName] as RenderModel;
				if (model == null || model.mesh == null)
				{
					var renderModels = holder.instance;
					if (renderModels == null)
						return;

					if (verbose)
						Debug.Log("Loading render model " + renderModelName);

					model = LoadRenderModel(renderModels, renderModelName, renderModelName);
					if (model == null)
						return;

					models[renderModelName] = model;
					this.renderModelName = renderModelName;
				}

				gameObject.AddComponent<MeshFilter>().mesh = model.mesh;
				gameObject.AddComponent<MeshRenderer>().sharedMaterial = model.material;
			}
		}
	}

	static RenderModel LoadRenderModel(CVRRenderModels renderModels, string renderModelName, string baseName)
	{
        var pRenderModel = System.IntPtr.Zero;
        if (!renderModels.LoadRenderModel(renderModelName, ref pRenderModel))
		{
			Debug.LogError("Failed to load render model " + renderModelName);
			return null;
        }

        var renderModel = (RenderModel_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t));

		var vertices = new Vector3[renderModel.unVertexCount];
		var normals = new Vector3[renderModel.unVertexCount];
		var uv = new Vector2[renderModel.unVertexCount];

		var type = typeof(RenderModel_Vertex_t);
		for (int iVert = 0; iVert < renderModel.unVertexCount; iVert++)
		{
			var ptr = new System.IntPtr(renderModel.rVertexData.ToInt64() + iVert * Marshal.SizeOf(type));
			var vert = (RenderModel_Vertex_t)Marshal.PtrToStructure(ptr, type);

			vertices[iVert] = new Vector3(vert.vPosition.v[0], vert.vPosition.v[1], -vert.vPosition.v[2]);
			normals[iVert] = new Vector3(vert.vNormal.v[0], vert.vNormal.v[1], -vert.vNormal.v[2]);
			uv[iVert] = new Vector2(vert.rfTextureCoord[0], vert.rfTextureCoord[1]);
		}

		int indexCount = (int)renderModel.unTriangleCount * 3;
		var indices = new short[indexCount];
		Marshal.Copy(renderModel.rIndexData, indices, 0, indices.Length);

		var triangles = new int[indexCount];
		for (int iTri = 0; iTri < renderModel.unTriangleCount; iTri++)
		{
			triangles[iTri * 3 + 0] = (int)indices[iTri * 3 + 2];
			triangles[iTri * 3 + 1] = (int)indices[iTri * 3 + 1];
			triangles[iTri * 3 + 2] = (int)indices[iTri * 3 + 0];
		}

		var mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uv;
		mesh.triangles = triangles;

		mesh.Optimize();
		//mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;

		// Check cache before loading texture.
		var material = materials[baseName + renderModel.diffuseTextureId] as Material;
		if (material == null || material.mainTexture == null)
		{
			var pDiffuseTexture = System.IntPtr.Zero;
			if (renderModels.LoadTexture(renderModel.diffuseTextureId, ref pDiffuseTexture))
			{
				var diffuseTexture = (RenderModel_TextureMap_t)Marshal.PtrToStructure(pDiffuseTexture, typeof(RenderModel_TextureMap_t));

				var textureMapData = new byte[diffuseTexture.unWidth * diffuseTexture.unHeight * 4]; // RGBA
				Marshal.Copy(diffuseTexture.rubTextureMapData, textureMapData, 0, textureMapData.Length);

				var colors = new Color32[diffuseTexture.unWidth * diffuseTexture.unHeight];
				int iColor = 0;
				for (int iHeight = 0; iHeight < diffuseTexture.unHeight; iHeight++)
				{
					for (int iWidth = 0; iWidth < diffuseTexture.unWidth; iWidth++)
					{
						var r = textureMapData[iColor++];
						var g = textureMapData[iColor++];
						var b = textureMapData[iColor++];
						var a = textureMapData[iColor++];
						colors[iHeight * diffuseTexture.unWidth + iWidth] = new Color32(r, g, b, a);
					}
				}

				var texture = new Texture2D(diffuseTexture.unWidth, diffuseTexture.unHeight, TextureFormat.ARGB32, true);
				texture.SetPixels32(colors);
				texture.Apply();

				material = new Material(Shader.Find("Standard"));
				material.mainTexture = texture;
				//material.hideFlags = HideFlags.DontUnloadUnusedAsset;

				materials[baseName + renderModel.diffuseTextureId] = material;
			}
			else
			{
				Debug.Log("Failed to load render model texture for render model " + renderModelName);
			}
		}

		renderModels.FreeRenderModel(ref renderModel);

		return new RenderModel(mesh, material);
	}

	public Transform FindComponent(string componentName)
	{
		var t = transform;
		for (int i = 0; i < t.childCount; i++)
		{
			var child = t.GetChild(i);
			if (child.name == componentName)
				return child;
		}
		return null;
	}

	private bool LoadComponents(RenderModelInterfaceHolder holder, string renderModelName)
	{
		// Disable existing components (we will re-enable them if referenced by this new model).
		// Also strip mesh filter and renderer since these will get re-added if the new component needs them.
		var t = transform;
		for (int i = 0; i < t.childCount; i++)
		{
			var child = t.GetChild(i);
			child.gameObject.SetActive(false);
			var meshRenderer = child.GetComponent<MeshRenderer>();
			if (meshRenderer != null)
				Object.DestroyImmediate(meshRenderer);
			var meshFilter = child.GetComponent<MeshFilter>();
			if (meshFilter != null)
				Object.DestroyImmediate(meshFilter);
		}

		// If no model specified, we're done; return success.
		if (string.IsNullOrEmpty(renderModelName))
			return true;

		var renderModels = holder.instance;
		if (renderModels == null)
			return false;

		var count = renderModels.GetComponentCount(renderModelName);
		if (count == 0)
			return false;

		for (int i = 0; i < count; i++)
		{
			var capacity = renderModels.GetComponentName(renderModelName, (uint)i, null, 0);
			if (capacity == 0)
				continue;

			var componentName = new System.Text.StringBuilder((int)capacity);
			if (renderModels.GetComponentName(renderModelName, (uint)i, componentName, capacity) == 0)
				continue;

			// Create (or reuse) a child object for this component (some components are dynamic and don't have meshes).
			t = FindComponent(componentName.ToString());
			if (t != null)
			{
				t.gameObject.SetActive(true);
			}
			else
			{
				t = new GameObject(componentName.ToString()).transform;
				t.parent = transform;
				t.gameObject.layer = gameObject.layer;

				// Also create a child 'attach' object for attaching things.
				var attach = new GameObject(k_localTransformName).transform;
				attach.parent = t;
				attach.localPosition = Vector3.zero;
				attach.localRotation = Quaternion.identity;
				attach.localScale = Vector3.one;
				attach.gameObject.layer = gameObject.layer;
			}

			// Reset transform.
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;

			capacity = renderModels.GetComponentRenderModelName(renderModelName, componentName.ToString(), null, 0);
			if (capacity == 0)
				continue;

			var componentRenderModelName = new System.Text.StringBuilder((int)capacity);
			if (renderModels.GetComponentRenderModelName(renderModelName, componentName.ToString(), componentRenderModelName, capacity) == 0)
				continue;

			// Check the cache or load into memory.
			var model = models[componentRenderModelName] as RenderModel;
			if (model == null || model.mesh == null)
			{
				if (verbose)
					Debug.Log("Loading render model " + componentRenderModelName);

				model = LoadRenderModel(renderModels, componentRenderModelName.ToString(), renderModelName);
				if (model == null)
					continue;

				models[componentRenderModelName] = model;
			}

			t.gameObject.AddComponent<MeshFilter>().mesh = model.mesh;
			t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = model.material;
		}

		return true;
	}

	void OnEnable()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			return;
#endif
		if (!string.IsNullOrEmpty(modelOverride))
		{
			Debug.Log("Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.");
			enabled = false;
			return;
		}

		if (SteamVR.active)
		{
			var vr = SteamVR.instance;
			if (vr.hmd.IsTrackedDeviceConnected((uint)index))
				UpdateModel();
		}

		SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
	}

	void OnDisable()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			return;
#endif
		SteamVR_Utils.Event.Remove("device_connected", OnDeviceConnected);
	}

#if UNITY_EDITOR
	Hashtable values;
#endif
	void Update()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			// See if anything has changed since this gets called whenever anything gets touched.
			var fields = GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

			bool modified = false;

			if (values == null)
			{
				modified = true;
			}
			else
			{
				foreach (var f in fields)
				{
					if (!values.Contains(f) || !f.GetValue(this).Equals(values[f]))
					{
						modified = true;
						break;
					}
				}
			}

			if (modified)
			{
				SetModel(modelOverride);

				values = new Hashtable();
				foreach (var f in fields)
					values[f] = f.GetValue(this);
			}

			return; // Do not update transforms (below) when not playing in Editor (to avoid keeping OpenVR running all the time).
		}
#endif
		// Update component transforms dynamically.
		if (updateDynamically)
		{
			using (var holder = new RenderModelInterfaceHolder())
			{
				var controllerState = SteamVR_Controller.Input((int)index).GetState();

				var t = transform;
				var baseTransform = new SteamVR_Utils.RigidTransform(t);

				for (int i = 0; i < t.childCount; i++)
				{
					var child = t.GetChild(i);

					var renderModels = holder.instance;
					if (renderModels == null)
						break;

					var componentState = new RenderModel_ComponentState_t();
					if (!renderModels.GetComponentState(renderModelName, child.name, ref controllerState, ref componentState))
						continue;

					var componentTransform = new SteamVR_Utils.RigidTransform(componentState.mTrackingToComponentRenderModel);
					child.localPosition = componentTransform.pos;
					child.localRotation = componentTransform.rot;

					var attach = child.FindChild(k_localTransformName);
					if (attach != null)
					{
						var attachTransform = baseTransform * new SteamVR_Utils.RigidTransform(componentState.mTrackingToComponentLocal);
						attach.position = attachTransform.pos;
						attach.rotation = attachTransform.rot;
					}

					bool visible = (componentState.uProperties & (uint)EVRComponentProperty.IsVisible) != 0;
					if (visible != child.gameObject.activeSelf)
					{
						child.gameObject.SetActive(visible);
					}
				}
			}
		}
	}

	public void SetDeviceIndex(int index)
	{
		this.index = (SteamVR_TrackedObject.EIndex)index;
		modelOverride = "";

		if (enabled)
		{
			UpdateModel();
		}
	}
}

