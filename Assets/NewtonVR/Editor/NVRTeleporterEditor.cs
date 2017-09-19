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
		private SerializedProperty teleportTargetTemplateProp;

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

			arcRendererTemplateProp = serializedObject.FindProperty("ArcRendererDisplay");
			playSpaceRendererTemplateProp = serializedObject.FindProperty("PlaySpaceDisplay");
			invalidRendererTemplateProp = serializedObject.FindProperty("InvalidPointDisplay");
			teleportTargetTemplateProp = serializedObject.FindProperty("TargetDisplay");

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
			GUIContent limitToHorizTooltip = new GUIContent(" Limit Surfaces to Horizontal", "This property will limit your teleports to only flat surfaces");
			limitToHorizontal = GUILayout.Toggle(limitToHorizontal, limitToHorizTooltip);
			if (limitToHorizontal != limitToHorizontalProp.boolValue)
			{
				limitToHorizontalProp.boolValue = limitToHorizontal;
			}

			if (limitToHorizontal)
			{
				EditorGUI.indentLevel = 2;
				GUIContent limitSensitivityTooltip = new GUIContent("Tolerance", "Tolerance for horizontal surface limit. 0 limits to only perfectly flat surfaces.");
				EditorGUILayout.PropertyField(limitSensitivityProp, limitSensitivityTooltip);
				EditorGUI.indentLevel = 0;
			}


			GUILayout.Space(10);

			//Arc Stuff
			GUILayout.Label("Arc Editor", EditorStyles.boldLabel);

			GUIContent arcDistanceTooltip = new GUIContent("Arc Distance", "How far the teleport arc reaches");
			EditorGUILayout.PropertyField(arcDistanceProp, arcDistanceTooltip);

			GUIContent arcLengthTooltip = new GUIContent("Arc Length", "The length of the teleport arc line");
			arcLength = EditorGUILayout.FloatField(arcLengthTooltip, arcLength);
			arcLength = Mathf.Max(0.01f, arcLength);

			GUIContent sampleFrequencyTooltip = new GUIContent("Sample Frequency", "How many points the teleport arc has");
			EditorGUILayout.PropertyField(sampleFrequencyProp, sampleFrequencyTooltip);

			GUILayout.Space(10);

			//Layer Masks
			GUILayout.Label("Layer Masks", EditorStyles.boldLabel);

			GUIContent surfacaeMaskTooltip = new GUIContent("Teleport Surface Mask", "Layer Mask of Valid Teleport Surfaces");
			EditorGUILayout.PropertyField(teleportSurfaceMask, surfacaeMaskTooltip);

			GUIContent blockMaskTooltip = new GUIContent("Teleport Block Mask", "Layer Mask of Invalid Teleport Surfaces");
			EditorGUILayout.PropertyField(teleportBlockMask, blockMaskTooltip);

			GUILayout.Space(10);

			//Line Renderers
			GUILayout.Label("Overrides", EditorStyles.boldLabel);
			GUILayout.Label("The default displays are all children of the NVRTeleporter GameObject and can be edited. They can also be replaced and overidden below."
				+ "The arc must be a line renderer, but the other displays can be any GameObject."
				, EditorStyles.wordWrappedMiniLabel);
			GUILayout.Space(4);

			_showRenderers = EditorGUILayout.Foldout(_showRenderers, "Displays");
			if (_showRenderers)
			{
				GUIContent arcRendererTooltip = new GUIContent("Arc Renderer Display", "Line Renderer that represents the Teleport Arc");
				EditorGUILayout.PropertyField(arcRendererTemplateProp, arcRendererTooltip);

				GUIContent playSpaceRendererTooltip = new GUIContent("Play Space Display", 
					"GameObject that represents the Play Space for a valid teleport. Scales with play space area, default should have a scale of 1,1,1.");
				EditorGUILayout.PropertyField(playSpaceRendererTemplateProp, playSpaceRendererTooltip);

				GUIContent invalidRendererTooltip = new GUIContent("Invalid Point Display", 
					"GameObject that represents an invalid teleport. Rotates with invalid surface, default should face forward along z");
				EditorGUILayout.PropertyField(invalidRendererTemplateProp, invalidRendererTooltip);

				GUIContent teleportTargetTooltip = new GUIContent("Teleport Target Display", 
					"GameObject that represents player's position within the playspace of a valid teleport. Appears at the end of the teleport arc.");
				EditorGUILayout.PropertyField(teleportTargetTemplateProp, teleportTargetTooltip);
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
