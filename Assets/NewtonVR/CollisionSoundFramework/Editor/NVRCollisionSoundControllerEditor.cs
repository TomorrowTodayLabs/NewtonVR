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

    [CustomEditor(typeof(NVRCollisionSoundController))]
    public class NVRCollisionSoundControllerEditor : Editor
    {
        private const string FMODDefine = "NVR_FMOD";

        private static bool hasReloaded = false;
        private static bool waitingForReload = false;
        private static DateTime startedWaitingForReload;

        private static bool hasFMODSDK = false;
        //private static bool hasFMODDefine = false;

        private static string progressBarMessage = null;

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            hasReloaded = true;
            hasFMODSDK = DoesTypeExist("FMODPlatform");

            //string scriptingDefine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            //string[] scriptingDefines = scriptingDefine.Split(';');
            //hasFMODDefine = scriptingDefines.Contains(FMODDefine);

            waitingForReload = false;
            ClearProgressBar();
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
            NVRCollisionSoundController controller = (NVRCollisionSoundController)target;

            if (hasReloaded == false)
                DidReloadScripts();

            if (waitingForReload)
                HasWaitedLongEnough();

            bool installFMOD = false;
            bool isFMODEnabled = controller.SoundEngine == NVRCollisionSoundProviders.FMOD;
            bool isUnityEnabled = controller.SoundEngine == NVRCollisionSoundProviders.Unity;
            bool enableFMOD = controller.SoundEngine == NVRCollisionSoundProviders.FMOD;
            bool enableUnity = controller.SoundEngine == NVRCollisionSoundProviders.Unity;


            EditorGUILayout.BeginHorizontal();
            if (hasFMODSDK == false)
            {
                using (new EditorGUI.DisabledScope(hasFMODSDK == false))
                {
                    EditorGUILayout.Toggle("Use FMOD", false);
                }
                installFMOD = GUILayout.Button("Install FMOD");
            }
            else
            {
                enableFMOD = EditorGUILayout.Toggle("Use FMOD", enableFMOD);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            enableUnity = EditorGUILayout.Toggle("Use Unity Sound", enableUnity);
            EditorGUILayout.EndHorizontal();

            
            GUILayout.Space(10);



            if (enableFMOD == false && isFMODEnabled == true)
            {
                RemoveDefine(FMODDefine);
                controller.SoundEngine = NVRCollisionSoundProviders.None;
            }
            else if (enableFMOD == true && isFMODEnabled == false)
            {
                AddDefine(FMODDefine);
                controller.SoundEngine = NVRCollisionSoundProviders.FMOD;
            }


            if (enableUnity == false && isUnityEnabled == true)
            {
                RemoveDefine(FMODDefine);
                controller.SoundEngine = NVRCollisionSoundProviders.None;
            }
            else if (enableUnity == true && isUnityEnabled == false)
            {
                RemoveDefine(FMODDefine);
                controller.SoundEngine = NVRCollisionSoundProviders.Unity;
            }
            

            if (installFMOD == true)
            {
                Application.OpenURL("http://www.fmod.org/download/");
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
