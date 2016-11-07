using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

#if NVR_SteamVR
using Valve.VR;

namespace NewtonVR
{
    public class NVRSteamVRIntegration : NVRIntegration
    {
        public override void Initialize(NVRPlayer player)
        {
            Player = player;
            Player.gameObject.SetActive(false);

            SteamVR_ControllerManager controllerManager = Player.gameObject.AddComponent<SteamVR_ControllerManager>();
            controllerManager.left = Player.LeftHand.gameObject;
            controllerManager.right = Player.RightHand.gameObject;
            //controllerManager.objects = new GameObject[2] { Player.LeftHand.gameObject, Player.RightHand.gameObject };

            Player.gameObject.AddComponent<SteamVR_PlayArea>();

            for (int index = 0; index < Player.Hands.Length; index++)
            {
                Player.Hands[index].gameObject.AddComponent<SteamVR_TrackedObject>();
            }


            SteamVR_Camera steamVrCamera = Player.Head.gameObject.AddComponent<SteamVR_Camera>();
            SteamVR_Ears steamVrEars = Player.Head.gameObject.AddComponent<SteamVR_Ears>();
            NVRHelpers.SetField(steamVrCamera, "_head", Player.Head.transform, false);
            NVRHelpers.SetField(steamVrCamera, "_ears", Player.Head.transform, false);

            Player.Head.gameObject.AddComponent<SteamVR_TrackedObject>();

            Player.gameObject.SetActive(true);
        }
    }
}
#else
namespace NewtonVR
{
    public class NVRSteamVRIntegration : NVRIntegration
    {
        public override void Initialize(NVRPlayer player)
        {
            PrintNotEnabledError();
        }

        private void PrintNotEnabledError()
        {
            Debug.LogError("Enable SteamVR in NVRPlayer to allow steamvr calls.");
        }
    }
}
#endif