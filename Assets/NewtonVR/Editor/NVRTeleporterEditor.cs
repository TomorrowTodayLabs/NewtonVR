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
		private SerializedProperty limitToHorizontalProp;
		private SerializedProperty limitSensitivityProp;
		private bool limitToHorizontal;

		//Masks
		private SerializedProperty teleportSurfaceMask;
		private SerializedProperty teleportBlockMask;

		//Arc Properties
		private SerializedProperty arcDistanceProp;
		private SerializedProperty sampleFrequencyProp;

		//Line Renderers
		private bool _showRenderers = false;
		private SerializedProperty arcRendererTemplateProp;
		private SerializedProperty playSpaceRendererTemplateProp;
		private SerializedProperty invalidRendererTemplateProp;

		//Hands
		private enum Hand { Left, Right };
		private GameObject[] _hands;

		//Styles
		private GUIStyle _boldFoldout;

		private float arcLength;

		public void OnEnable()
		{
			NVRTeleporter teleporter = (NVRTeleporter)target;

			limitToHorizontalProp = serializedObject.FindProperty("LimitToHorizontal");
			limitSensitivityProp = serializedObject.FindProperty("LimitSensitivity");
			limitToHorizontal = limitToHorizontalProp.boolValue;

			teleportSurfaceMask = serializedObject.FindProperty("TeleportSurfaceMask");
			teleportBlockMask = serializedObject.FindProperty("TeleportBlockMask");

			arcRendererTemplateProp = serializedObject.FindProperty("ArcRendererTemplate");
			playSpaceRendererTemplateProp = serializedObject.FindProperty("PlaySpaceRendererTemplate");
			invalidRendererTemplateProp = serializedObject.FindProperty("InvalidRendererTemplate");

			arcDistanceProp = serializedObject.FindProperty("ArcDistance");
			sampleFrequencyProp = serializedObject.FindProperty("SampleFrequency");

			arcLength = (float)teleporter.SamplePoints * teleporter.SampleFrequency;

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
			limitToHorizontal = GUILayout.Toggle(limitToHorizontal, " Limit to Vertical");
			if (limitToHorizontal != limitToHorizontalProp.boolValue)
			{
				limitToHorizontalProp.boolValue = limitToHorizontal;
			}
			EditorGUILayout.LabelField("This property will limit your teleports to only flat surfaces", EditorStyles.miniLabel);

			if (limitToHorizontal)
			{
				EditorGUILayout.PropertyField(limitSensitivityProp);
			}


			GUILayout.Space(10);

			//Arc Stuff
			GUILayout.Label("Arc Editor", EditorStyles.boldLabel);
			GUILayout.Label("To edit the appearance of the different line renderers,\n the templates are all children of the NVRTeleporter GameObject.\n"
				+ "Find the LineRenderer Component and update accordingly", EditorStyles.miniLabel);
			GUILayout.Space(4);

			EditorGUILayout.PropertyField(arcDistanceProp);
			arcLength = EditorGUILayout.FloatField("Arc Length", arcLength);
			arcLength = Mathf.Max(0.01f, arcLength);
			EditorGUILayout.PropertyField(sampleFrequencyProp);

			GUILayout.Space(10);

			//Layer Masks
			GUILayout.Label("Layer Masks", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(teleportSurfaceMask);
			EditorGUILayout.PropertyField(teleportBlockMask);

			GUILayout.Space(10);

			//Line Renderers
			GUILayout.Label("Overrides", EditorStyles.boldLabel);
			_showRenderers = EditorGUILayout.Foldout(_showRenderers, "Line Renderers");
			if (_showRenderers)
			{
				EditorGUILayout.PropertyField(arcRendererTemplateProp);
				EditorGUILayout.PropertyField(playSpaceRendererTemplateProp);
				EditorGUILayout.PropertyField(invalidRendererTemplateProp);
			}

			serializedObject.ApplyModifiedProperties();

			//Adjust arc length based on density and desired length
			if ((float)teleporter.SamplePoints * teleporter.SampleFrequency != teleporter.SamplePoints)
			{
				//Distance might be off by a litttle, because it moves in increments of sample distance
				//Adjust user input to nearest point
				teleporter.SamplePoints = Mathf.RoundToInt(arcLength / teleporter.SampleFrequency);
				arcLength = (float)teleporter.SamplePoints * teleporter.SampleFrequency;
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
