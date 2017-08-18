using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NewtonVR
{
	[CustomEditor(typeof(NVRTeleporter))]
	public class NVRTeleporterEditor : Editor
	{
		//Options
		private SerializedProperty _limitToHorizontalProp;
		private SerializedProperty _limitSensitivityProp;
		private bool _limitToHorizontal;

		//Masks
		private SerializedProperty _teleportSurfaceMask;
		private SerializedProperty _teleportBlockMask;

		//Arc Properties
		private SerializedProperty _velocityProp;
		private SerializedProperty _accelerationProp;
		private SerializedProperty _sampleDistanceProp;

		//Line Renderers
		private bool _showRenderers = false;
		private SerializedProperty _arcRendererTemplateProp;
		private SerializedProperty _playSpaceRendererTemplateProp;
		private SerializedProperty _invalidRendererTemplateProp;

		//Hands
		private enum Hand { Left, Right };
		private GameObject[] _hands;

		//Styles
		private GUIStyle _boldFoldout;

		private float _arcLength;

		public void OnEnable()
		{
			NVRTeleporter teleporter = (NVRTeleporter)target;

			_limitToHorizontalProp = serializedObject.FindProperty("LimitToHorizontal");
			_limitSensitivityProp = serializedObject.FindProperty("LimitSensitivity");
			_limitToHorizontal = _limitToHorizontalProp.boolValue;

			_teleportSurfaceMask = serializedObject.FindProperty("TeleportSurfaceMask");
			_teleportBlockMask = serializedObject.FindProperty("TeleportBlockMask");

			_arcRendererTemplateProp = serializedObject.FindProperty("ArcRendererTemplate");
			_playSpaceRendererTemplateProp = serializedObject.FindProperty("PlaySpaceRendererTemplate");
			_invalidRendererTemplateProp = serializedObject.FindProperty("InvalidRendererTemplate");

			_velocityProp = serializedObject.FindProperty("Velocity");
			_accelerationProp = serializedObject.FindProperty("Acceleration");
			_sampleDistanceProp = serializedObject.FindProperty("SampleDistance");

			_arcLength = (float)teleporter.SamplePoints * teleporter.SampleDistance;

			NVRPlayer player = teleporter.gameObject.GetComponentInParent<NVRPlayer>();
			if (player != null)
			{
				_hands = new GameObject[2];
				_hands[0] = player.LeftHand.gameObject;
				_hands[1] = player.RightHand.gameObject;
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			NVRTeleporter teleporter = (NVRTeleporter)target;

			GUILayout.Label("Options", EditorStyles.boldLabel);
			HandCheck(Hand.Left);
			HandCheck(Hand.Right);

			GUILayout.Space(6);
			_limitToHorizontal = GUILayout.Toggle(_limitToHorizontal, " Limit to Vertical");
			if (_limitToHorizontal != _limitToHorizontalProp.boolValue)
			{
				_limitToHorizontalProp.boolValue = _limitToHorizontal;
			}
			EditorGUILayout.LabelField("This property will limit your teleports to only flat surfaces", EditorStyles.miniLabel);

			if (_limitToHorizontal)
			{
				EditorGUILayout.PropertyField(_limitSensitivityProp);
			}


			GUILayout.Space(10);

			//Arc Stuff
			GUILayout.Label("Arc Editor", EditorStyles.boldLabel);
			GUILayout.Label("To edit the appearance of the different line renderers,\n the templates are all children of the NVRTeleporter GameObject.\n"
				+ "Find the LineRenderer Component and update accordingly", EditorStyles.miniLabel);
			GUILayout.Space(4);

			EditorGUILayout.PropertyField(_velocityProp);
			EditorGUILayout.PropertyField(_accelerationProp);
			_arcLength = EditorGUILayout.FloatField("Arc Length", _arcLength);
			EditorGUILayout.PropertyField(_sampleDistanceProp);

			GUILayout.Space(10);

			//Layer Masks
			GUILayout.Label("Layer Masks", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(_teleportSurfaceMask);
			EditorGUILayout.PropertyField(_teleportBlockMask);

			GUILayout.Space(10);

			//Line Renderers
			GUILayout.Label("Overrides", EditorStyles.boldLabel);
			_showRenderers = EditorGUILayout.Foldout(_showRenderers, "Line Renderers");
			if (_showRenderers)
			{
				EditorGUILayout.PropertyField(_arcRendererTemplateProp);
				EditorGUILayout.PropertyField(_playSpaceRendererTemplateProp);
				EditorGUILayout.PropertyField(_invalidRendererTemplateProp);
			}

			serializedObject.ApplyModifiedProperties();

			//Adjust arc length based on density and desired length
			if ((float)teleporter.SamplePoints * teleporter.SampleDistance != teleporter.SamplePoints)
			{
				//Distance might be off by a litttle, because it moves in increments of sample distance
				//Adjust user input to nearest point
				teleporter.SamplePoints = Mathf.CeilToInt(_arcLength / teleporter.SampleDistance);
				_arcLength = (float)teleporter.SamplePoints * teleporter.SampleDistance;
			}
		}

		private void HandCheck(Hand hand)
		{
			//If right hand was found
			if (_hands != null && _hands[(int)hand] != null)
			{
				//If the right hand doesn't already have the teleport controller
				NVRTeleportController comp = _hands[(int)hand].GetComponent<NVRTeleportController>();
				if (comp == null)
				{
					if (GUILayout.Button("Attach Teleporter to " + hand.ToString() + " Hand"))
					{
						_hands[(int)hand].AddComponent<NVRTeleportController>();
					}
				}
				else
				{
					if (GUILayout.Button("Remove Teleporter from " + hand.ToString() + " Hand"))
					{
						DestroyImmediate(comp);
					}
				}
			}
		}
	}
}
