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
        private SerializedProperty useTunnellingProp;

        private bool limitToHorizontal;
        private bool useTunnelling;

		//Masks
		private SerializedProperty teleportSurfaceMask;
		private SerializedProperty teleportBlockMask;

        //Tunnelling
        private SerializedProperty tunnelOverTimeProp;
        private SerializedProperty vignettePowerProp;
        private SerializedProperty vignetteEaseInTimeProp;
        private SerializedProperty vignetteEaseOutTimeProp;

        //Arc Properties
        private SerializedProperty arcStrengthProp;
        private SerializedProperty arcLengthProp;
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

            useTunnellingProp = serializedObject.FindProperty("TunnelTeleport");
            useTunnelling = useTunnellingProp.boolValue;
            tunnelOverTimeProp = serializedObject.FindProperty("TunnelOverTime");
            vignettePowerProp = serializedObject.FindProperty("VignettePower");
            vignetteEaseInTimeProp = serializedObject.FindProperty("VignetteEaseInTime");
            vignetteEaseOutTimeProp = serializedObject.FindProperty("VignetteEaseOutTime");

            teleportSurfaceMask = serializedObject.FindProperty("TeleportSurfaceMask");
			teleportBlockMask = serializedObject.FindProperty("TeleportBlockMask");

			arcRendererTemplateProp = serializedObject.FindProperty("ArcRendererDisplay");
			playSpaceRendererTemplateProp = serializedObject.FindProperty("PlaySpaceDisplay");
			invalidRendererTemplateProp = serializedObject.FindProperty("InvalidPointDisplay");
			teleportTargetTemplateProp = serializedObject.FindProperty("TargetDisplay");

            arcStrengthProp = serializedObject.FindProperty("ArcStrength");
            arcLengthProp = serializedObject.FindProperty("ArcMaxLength");
            sampleFrequencyProp = serializedObject.FindProperty("SampleFrequency");

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
            GUIContent useTunnelTooltip = new GUIContent(" Use tunnel teleporting", "Vignette the user's view and move over a short time while teleporting");
            useTunnelling = GUILayout.Toggle(useTunnelling, useTunnelTooltip);
            if (useTunnelling != useTunnellingProp.boolValue)
            {
                useTunnellingProp.boolValue = useTunnelling;
            }

            if (useTunnelling)
            {
                EditorGUI.indentLevel = 2;

                GUIContent tunnelOverTimeTooltip = new GUIContent("Tunnelling time", "How long should it take to teleport from one place to another");
                EditorGUILayout.PropertyField(tunnelOverTimeProp, tunnelOverTimeTooltip);

                GUIContent vignettePowerTooltip = new GUIContent("Vignette power", "How much of a vignette should we display while tunnelling");
                EditorGUILayout.PropertyField(vignettePowerProp, vignettePowerTooltip);

                GUIContent vignetteEaseInTimeTooltip = new GUIContent("Vignette ease-in time", "How long should it take to fade into the vignette before tunnelling");
                EditorGUILayout.PropertyField(vignetteEaseInTimeProp, vignetteEaseInTimeTooltip);

                GUIContent vignetteEaseOutTimeTooltip = new GUIContent("Vignette ease-out time", "How long should it take to fade out of the vignette after tunnelling");
                EditorGUILayout.PropertyField(vignetteEaseOutTimeProp, vignetteEaseOutTimeTooltip);

                EditorGUI.indentLevel = 0;
            }

            GUILayout.Space(6);
			GUIContent limitToHorizTooltip = new GUIContent(" Limit surfaces to horizontal", "This property will limit your teleports to only flat surfaces");
			limitToHorizontal = GUILayout.Toggle(limitToHorizontal, limitToHorizTooltip);
			if (limitToHorizontal != limitToHorizontalProp.boolValue)
			{
				limitToHorizontalProp.boolValue = limitToHorizontal;
			}

			if (limitToHorizontal)
			{
				EditorGUI.indentLevel = 2;
				GUIContent limitSensitivityTooltip = new GUIContent("Tolerance", "How much of an angle we're allowed to teleport onto in degree difference from up.\nEx. A floor would be 0. A perpendicular wall would be 90.");
				EditorGUILayout.PropertyField(limitSensitivityProp, limitSensitivityTooltip);
				EditorGUI.indentLevel = 0;
			}


			GUILayout.Space(10);

			//Arc Stuff
			GUILayout.Label("Arc Editor", EditorStyles.boldLabel);

			GUIContent arcStrengthTooltip = new GUIContent("Arc strength", "How much the line extends forward before arching down");
			EditorGUILayout.PropertyField(arcStrengthProp, arcStrengthTooltip);

			GUIContent arcLengthTooltip = new GUIContent("Maximum arc length", "The maximum length the theleporter arc will go before cutting out");
            EditorGUILayout.PropertyField(arcLengthProp, arcLengthTooltip);

            GUIContent sampleFrequencyTooltip = new GUIContent("Sample frequency", "How many points the teleport arc has (smaller is more smooth)");
			EditorGUILayout.PropertyField(sampleFrequencyProp, sampleFrequencyTooltip);

			GUILayout.Space(10);

			//Layer Masks
			GUILayout.Label("Layer Masks", EditorStyles.boldLabel);

			GUIContent surfacaeMaskTooltip = new GUIContent("Teleport surface mask", "Layers that we can teleport onto");
			EditorGUILayout.PropertyField(teleportSurfaceMask, surfacaeMaskTooltip);

			GUIContent blockMaskTooltip = new GUIContent("Teleport block mask", "Layers that should block teleportation");
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
				GUIContent arcRendererTooltip = new GUIContent("Arc renderer display", "Line Renderer that represents the Teleport Arc");
				EditorGUILayout.PropertyField(arcRendererTemplateProp, arcRendererTooltip);

				GUIContent playSpaceRendererTooltip = new GUIContent("Play space display", 
					"GameObject that represents the Play Space for a valid teleport. Scales with play space area, default should have a scale of 1,1,1.");
				EditorGUILayout.PropertyField(playSpaceRendererTemplateProp, playSpaceRendererTooltip);

				GUIContent invalidRendererTooltip = new GUIContent("Invalid point display", 
					"GameObject that represents an invalid teleport. Rotates with invalid surface, default should face forward along z");
				EditorGUILayout.PropertyField(invalidRendererTemplateProp, invalidRendererTooltip);

				GUIContent teleportTargetTooltip = new GUIContent("Teleport target display", 
					"GameObject that represents player's position within the playspace of a valid teleport. Appears at the end of the teleport arc.");
				EditorGUILayout.PropertyField(teleportTargetTemplateProp, teleportTargetTooltip);
			}



            serializedObject.ApplyModifiedProperties();
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
