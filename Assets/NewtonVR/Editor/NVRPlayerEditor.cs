using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using NewtonVR;
using System.Net;
using System.Net.Security;
using System.IO;
using System.ComponentModel;

using System.Threading;

using System.Security.Cryptography.X509Certificates;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NewtonVR
{

    [CustomEditor(typeof(NVRPlayer))]
    public class NVRPlayerEditor : Editor
    {
        private const string SteamVRDefine = "NVR_SteamVR";
        private const string OculusDefine = "NVR_Oculus";

        private static bool hasReloaded = false;
        private static bool waitingForReload = false;
        private static DateTime startedWaitingForReload;

        private static bool hasOculusSDK = false;
        private static bool hasSteamVR = false;
        private static bool hasOculusSDKDefine = false;
        private static bool hasSteamVRDefine = false;

        private static string progressBarMessage = null;

        private static string CheckForUpdatesKey = "NewtonVRCheckForUpdates";

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            hasReloaded = true;

            hasOculusSDK = DoesTypeExist("OVRInput");

            hasSteamVR = DoesTypeExist("SteamVR");

            string scriptingDefine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            string[] scriptingDefines = scriptingDefine.Split(';');
            hasOculusSDKDefine = scriptingDefines.Contains(OculusDefine);
            hasSteamVRDefine = scriptingDefines.Contains(SteamVRDefine);

            waitingForReload = false;
            ClearProgressBar();

            if (PlayerPrefs.HasKey(CheckForUpdatesKey) == false || PlayerPrefs.GetInt(CheckForUpdatesKey) == 1)
            {
                Thread thread = new Thread(new ThreadStart(CheckForUpdate));
                thread.Start();
            }
        }

        private static void CheckForUpdate() //turn this off in NVRPlayer inspector under NotifyOnVersionUpdate.
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string version = wc.DownloadString("http://www.newtonvr.com/version.php?ver=" + NVRPlayer.NewtonVRVersion);
                    string[] split = version.Split(new char[] { '=' });
                    version = split[1];//
                    decimal versionResult;
                    decimal.TryParse(version, out versionResult);
                    if (NVRPlayer.NewtonVRVersion < versionResult)
                    {
                        Debug.Log("[NewtonVR] The version of newtonvr you are using (" + NVRPlayer.NewtonVRVersion + ") is out of date. Check the github for the most recent version (" + version + ")! Disable this check in the NVRPlayer inspector under NotifyOnVersionUpdate.");
                    }
                }
            }
            catch
            {

            }
        }

        private static bool DoesTypeExist(string className)
        {
            var foundType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        from type in assembly.GetTypes()
                        where type.Name == className
                        select type).FirstOrDefault();

            return foundType != null;
        }

        private void RemoveDefine(string define)
        {
            DisplayProgressBar("Removing support for " + define);
            waitingForReload = true;
            startedWaitingForReload = DateTime.Now;

            string scriptingDefine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            string[] scriptingDefines = scriptingDefine.Split(';');
            List<string> listDefines = scriptingDefines.ToList();
            listDefines.Remove(define);

            string newDefines = string.Join(";", listDefines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);
        }

        private void AddDefine(string define)
        {
            DisplayProgressBar("Setting up support for " + define);
            waitingForReload = true;
            startedWaitingForReload = DateTime.Now;

            string scriptingDefine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            string[] scriptingDefines = scriptingDefine.Split(';');
            List<string> listDefines = scriptingDefines.ToList();
            listDefines.Add(define);

            string newDefines = string.Join(";", listDefines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);

            if (PlayerSettings.virtualRealitySupported == false)
            {
                PlayerSettings.virtualRealitySupported = true;
            }
        }

        private static void DisplayProgressBar(string newMessage = null)
        {
            if (newMessage != null)
            {
                progressBarMessage = newMessage;
            }

            EditorUtility.DisplayProgressBar("NewtonVR", progressBarMessage, UnityEngine.Random.value); // :D
        }

        private static void ClearProgressBar()
        {
            progressBarMessage = null;
            EditorUtility.ClearProgressBar();
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        private static void HasWaitedLongEnough()
        {
            TimeSpan waitedTime = DateTime.Now - startedWaitingForReload;
            if (waitedTime.TotalSeconds > 15)
            {
                DidReloadScripts();
            }
        }

        public override void OnInspectorGUI()
        {
            NVRPlayer player = (NVRPlayer)target;
            if (PlayerPrefs.HasKey(CheckForUpdatesKey) == false || PlayerPrefs.GetInt(CheckForUpdatesKey) != System.Convert.ToInt32(player.NotifyOnVersionUpdate))
            {
                PlayerPrefs.SetInt("NewtonVRCheckForUpdates", System.Convert.ToInt32(player.NotifyOnVersionUpdate));
            }

            if (hasReloaded == false)
                DidReloadScripts();

            if (waitingForReload)
                HasWaitedLongEnough();

            player.OculusSDKEnabled = hasOculusSDKDefine;
            player.SteamVREnabled = hasSteamVRDefine;

            bool installSteamVR = false;
            bool installOculusSDK = false;
            bool enableSteamVR = player.SteamVREnabled;
            bool enableOculusSDK = player.OculusSDKEnabled;
            
            EditorGUILayout.BeginHorizontal();
            if (hasSteamVR == false)
            {
                using (new EditorGUI.DisabledScope(hasSteamVR == false))
                {
                    EditorGUILayout.Toggle("Enable SteamVR", player.SteamVREnabled);
                }
                installSteamVR = GUILayout.Button("Install SteamVR");
            }
            else
            {
                enableSteamVR = EditorGUILayout.Toggle("Enable SteamVR", player.SteamVREnabled);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (hasOculusSDK == false)
            {
                using (new EditorGUI.DisabledScope(hasOculusSDK == false))
                {
                    EditorGUILayout.Toggle("Enable Oculus SDK", player.OculusSDKEnabled);
                }
                installOculusSDK = GUILayout.Button("Install Oculus SDK");
            }
            else
            {
                enableOculusSDK = EditorGUILayout.Toggle("Enable Oculus SDK", player.OculusSDKEnabled);
            }
            EditorGUILayout.EndHorizontal();


            GUILayout.Space(10);

            GUILayout.Label("Model override for all SDKs");
            bool modelOverrideAll = EditorGUILayout.Toggle("Override hand models for all SDKs", player.OverrideAll);
            EditorGUILayout.BeginFadeGroup(1);
            using (new EditorGUI.DisabledScope(modelOverrideAll == false))
            {
                player.OverrideAllLeftHand = (GameObject)EditorGUILayout.ObjectField("Left Hand", player.OverrideAllLeftHand, typeof(GameObject), false);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                player.OverrideAllLeftHandPhysicalColliders = (GameObject)EditorGUILayout.ObjectField("Left Hand Physical Colliders", player.OverrideAllLeftHandPhysicalColliders, typeof(GameObject), false);
                GUILayout.EndHorizontal();
                player.OverrideAllRightHand = (GameObject)EditorGUILayout.ObjectField("Right Hand", player.OverrideAllRightHand, typeof(GameObject), false);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                player.OverrideAllRightHandPhysicalColliders = (GameObject)EditorGUILayout.ObjectField("Right Hand Physical Colliders", player.OverrideAllRightHandPhysicalColliders, typeof(GameObject), false);
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFadeGroup();
            if (modelOverrideAll == true)
            {
                player.OverrideOculus = false;
                player.OverrideSteamVR = false;
            }
            if (player.OverrideAll != modelOverrideAll)
            {
                EditorUtility.SetDirty(target);
                player.OverrideAll = modelOverrideAll;
            }

            GUILayout.Space(10);

            if (player.OculusSDKEnabled == true)
            {
                GUILayout.Label("Model override for Oculus SDK");
                using (new EditorGUI.DisabledScope(hasOculusSDK == false))
                {
                    bool modelOverrideOculus = EditorGUILayout.Toggle("Override hand models for Oculus SDK", player.OverrideOculus);
                    EditorGUILayout.BeginFadeGroup(Convert.ToSingle(modelOverrideOculus));
                    using (new EditorGUI.DisabledScope(modelOverrideOculus == false))
                    {
                        player.OverrideOculusLeftHand = (GameObject)EditorGUILayout.ObjectField("Left Hand", player.OverrideOculusLeftHand, typeof(GameObject), false);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        player.OverrideOculusLeftHandPhysicalColliders = (GameObject)EditorGUILayout.ObjectField("Left Hand Physical Colliders", player.OverrideOculusLeftHandPhysicalColliders, typeof(GameObject), false);
                        GUILayout.EndHorizontal();
                        player.OverrideOculusRightHand = (GameObject)EditorGUILayout.ObjectField("Right Hand", player.OverrideOculusRightHand, typeof(GameObject), false);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        player.OverrideOculusRightHandPhysicalColliders = (GameObject)EditorGUILayout.ObjectField("Right Hand Physical Colliders", player.OverrideOculusRightHandPhysicalColliders, typeof(GameObject), false);
                        GUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndFadeGroup();

                    if (modelOverrideOculus == true)
                    {
                        player.OverrideAll = false;
                    }
                    if (player.OverrideOculus != modelOverrideOculus)
                    {
                        EditorUtility.SetDirty(target);
                        player.OverrideOculus = modelOverrideOculus;
                    }
                }
            }

            if (player.SteamVREnabled == true)
            {
                GUILayout.Label("Model override for SteamVR");
                using (new EditorGUI.DisabledScope(hasSteamVR == false))
                {
                    bool modelOverrideSteamVR = EditorGUILayout.Toggle("Override hand models for SteamVR", player.OverrideSteamVR);
                    EditorGUILayout.BeginFadeGroup(Convert.ToSingle(modelOverrideSteamVR));
                    using (new EditorGUI.DisabledScope(modelOverrideSteamVR == false))
                    {
                        player.OverrideSteamVRLeftHand = (GameObject)EditorGUILayout.ObjectField("Left Hand", player.OverrideSteamVRLeftHand, typeof(GameObject), false);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        player.OverrideSteamVRLeftHandPhysicalColliders = (GameObject)EditorGUILayout.ObjectField("Left Hand Physical Colliders", player.OverrideSteamVRLeftHandPhysicalColliders, typeof(GameObject), false);
                        GUILayout.EndHorizontal();
                        player.OverrideSteamVRRightHand = (GameObject)EditorGUILayout.ObjectField("Right Hand", player.OverrideSteamVRRightHand, typeof(GameObject), false);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        player.OverrideSteamVRRightHandPhysicalColliders = (GameObject)EditorGUILayout.ObjectField("Right Hand Physical Colliders", player.OverrideSteamVRRightHandPhysicalColliders, typeof(GameObject), false);
                        GUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndFadeGroup();

                    if (modelOverrideSteamVR == true)
                    {
                        player.OverrideAll = false;
                    }
                    if (player.OverrideSteamVR != modelOverrideSteamVR)
                    {
                        EditorUtility.SetDirty(target);
                        player.OverrideSteamVR = modelOverrideSteamVR;
                    }
                }

                GUILayout.Space(10);
            }



            GUILayout.Space(10);



            if (enableSteamVR == false && player.SteamVREnabled == true)
            {
                RemoveDefine(SteamVRDefine);
            }
            else if (enableSteamVR == true && player.SteamVREnabled == false)
            {
                AddDefine(SteamVRDefine);
            }

            
            if (enableOculusSDK == false && player.OculusSDKEnabled == true)
            {
                RemoveDefine(OculusDefine);
            }
            else if (enableOculusSDK == true && player.OculusSDKEnabled == false)
            {
                AddDefine(OculusDefine);
            }

            if (installOculusSDK == true)
            {
                Application.OpenURL("https://developer3.oculus.com/downloads/game-engines/1.11.0/Oculus_Utilities_for_Unity_5/");
            }

            if (installSteamVR == true)
            {
                Application.OpenURL("com.unity3d.kharma:content/32647");
            }

            DrawDefaultInspector();

            if (waitingForReload == true || string.IsNullOrEmpty(progressBarMessage) == false)
            {
                DisplayProgressBar();
            }
            if (GUI.changed)
            {
                if (Application.isPlaying == false)
                {
                    EditorUtility.SetDirty(target);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }
        }
    }
}
