using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VR;
using System.Linq;
using UnityEngine.Events;

namespace NewtonVR
{
    public class NVRPlayer : MonoBehaviour
    {
        public const decimal NewtonVRVersion = 0.95m;

        public static List<NVRPlayer> Instances = new List<NVRPlayer>();
        public static NVRPlayer Instance
        {
            get
            {
                return Instances.First(player => player != null && player.gameObject != null);
            }
        }

        [HideInInspector]
        public bool SteamVREnabled = false;
        [HideInInspector]
        public bool OculusSDKEnabled = false;

        public InterationStyle InteractionStyle;
        public bool PhysicalHands = true;
        public bool MakeControllerInvisibleOnInteraction = false;
        public bool AutomaticallySetControllerTransparency = true;
        public bool VibrateOnHover = true;
        public int VelocityHistorySteps = 3;

        public UnityEvent OnInitialized;

        [Space]

        [HideInInspector]
        public bool OverrideAll;
        [HideInInspector]
        public GameObject OverrideAllLeftHand;
        [HideInInspector]
        public GameObject OverrideAllLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideAllRightHand;
        [HideInInspector]
        public GameObject OverrideAllRightHandPhysicalColliders;

        [HideInInspector]
        public bool OverrideSteamVR;
        [HideInInspector]
        public GameObject OverrideSteamVRLeftHand;
        [HideInInspector]
        public GameObject OverrideSteamVRLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideSteamVRRightHand;
        [HideInInspector]
        public GameObject OverrideSteamVRRightHandPhysicalColliders;

        [HideInInspector]
        public bool OverrideOculus;
        [HideInInspector]
        public GameObject OverrideOculusLeftHand;
        [HideInInspector]
        public GameObject OverrideOculusLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideOculusRightHand;
        [HideInInspector]
        public GameObject OverrideOculusRightHandPhysicalColliders;

        [Space]

        public NVRHead Head;
        public NVRHand LeftHand;
        public NVRHand RightHand;

        [HideInInspector]
        public NVRHand[] Hands;

        [HideInInspector]
        public NVRSDKIntegrations CurrentIntegrationType = NVRSDKIntegrations.None;

        private NVRIntegration Integration;

        private Dictionary<Collider, NVRHand> ColliderToHandMapping;

        [Space]

        public bool DEBUGEnableFallback2D = false;
        public bool DEBUGDropFrames = false;
        public int DEBUGSleepPerFrame = 13;

        public bool NotifyOnVersionUpdate = true;

        private void Awake()
        {
            Instances.Add(this);

            NVRInteractables.Initialize();

            if (Head == null)
            {
                Head = this.GetComponentInChildren<NVRHead>();
            }
            Head.Initialize();

            if (LeftHand == null || RightHand == null)
            {
                Debug.LogError("[FATAL ERROR] Please set the left and right hand to a nvrhands.");
            }

            ColliderToHandMapping = new Dictionary<Collider, NVRHand>();

            DetermineCurrentIntegration();

            if (CurrentIntegrationType == NVRSDKIntegrations.Oculus)
            {
                Integration = this.gameObject.AddComponent<NVROculusIntegration>();
            }
            else if (CurrentIntegrationType == NVRSDKIntegrations.SteamVR)
            {
                Integration = this.gameObject.AddComponent<NVRSteamVRIntegration>();
            }
            else if (CurrentIntegrationType == NVRSDKIntegrations.FallbackNonVR)
            {
                Debug.LogError("[NewtonVR] Fallback non-vr not yet implemented.");
                return;
            }
            else
            {
                Debug.LogError("[NewtonVR] Critical Error: Oculus / SteamVR not setup properly or no headset found.");
                return;
            }


            if (Hands == null || Hands.Length == 0)
            {
                Hands = new NVRHand[] { LeftHand, RightHand };

                for (int index = 0; index < Hands.Length; index++)
                {
                    Hands[index].PreInitialize(this);
                }
            }

            Integration.Initialize(this);

            if (OnInitialized != null)
            {
                OnInitialized.Invoke();
            }
        }

        private void DetermineCurrentIntegration()
        {
            string resultLog = "[NewtonVR] Version : " + NewtonVRVersion + ". ";
            if (VRDevice.isPresent == true)
            {
                resultLog += "Found VRDevice: " + VRDevice.model + ". ";

                #if NVR_Oculus
                if (VRDevice.model.IndexOf("oculus", System.StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    CurrentIntegrationType = NVRSDKIntegrations.Oculus;
                    resultLog += "Using Oculus SDK";
                    return;
                }
                #endif

                #if NVR_SteamVR
                CurrentIntegrationType = NVRSDKIntegrations.SteamVR;
                resultLog += "Using SteamVR SDK";
                return;
                #endif
            }

            if (CurrentIntegrationType == NVRSDKIntegrations.None && DEBUGEnableFallback2D == true)
            {
                CurrentIntegrationType = NVRSDKIntegrations.FallbackNonVR;
                resultLog += "Did not find supported VR device. Or no integrations enabled.";
            }

            Debug.Log(resultLog);
        }

        public void RegisterHand(NVRHand hand)
        {
            Collider[] colliders = hand.GetComponentsInChildren<Collider>();

            for (int index = 0; index < colliders.Length; index++)
            {
                if (ColliderToHandMapping.ContainsKey(colliders[index]) == false)
                {
                    ColliderToHandMapping.Add(colliders[index], hand);
                }
            }
        }

        public NVRHand GetHand(Collider collider)
        {
            return ColliderToHandMapping[collider];
        }

        public static void DeregisterInteractable(NVRInteractable interactable)
        {
            for (int instanceIndex = 0; instanceIndex < Instances.Count; instanceIndex++)
            {
                if (Instances[instanceIndex] != null && Instances[instanceIndex].Hands != null)
                {
                    for (int index = 0; index < Instances[instanceIndex].Hands.Length; index++)
                    {
                        if (Instances[instanceIndex].Hands[index] != null)
                        {
                            Instances[instanceIndex].Hands[index].DeregisterInteractable(interactable);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
        }

        private void Update()
        {
            if (DEBUGDropFrames == true)
            {
                System.Threading.Thread.Sleep(DEBUGSleepPerFrame);
            }
        }
    }
}