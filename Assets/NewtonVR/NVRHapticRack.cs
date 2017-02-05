using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace NewtonVR
{
    // Adapted from SteamVR InteractionSystem
    //
    //======= Copyright (c) Valve Corporation, All rights reserved. ===============
    //
    // Purpose: Triggers haptic pulses based on a linear mapping
    //
    //=============================================================================
    public class NVRHapticRack : MonoBehaviour
    {
        [Tooltip(" The interactable this rack tracks. If no hand is attached no haptics will be triggered.")]
        public NVRInteractable interactable;

        [Tooltip( "The linear mapping driving the haptic rack" )]
        public NVRLinearMapping linearMapping;

        [Tooltip( "The number of haptic pulses evenly distributed along the mapping" )]
        public int teethCount = 128;

        [Tooltip( "Minimum duration of the haptic pulse" )]
        public int minimumPulseDuration = 500;

        [Tooltip( "Maximum duration of the haptic pulse" )]
        public int maximumPulseDuration = 900;

        [Tooltip( "This event is triggered every time a haptic pulse is made" )]
        public UnityEvent onPulse;

        private int previousToothIndex = -1;

        //-------------------------------------------------
        void Awake()
        {
            if ( linearMapping == null )
            {
                linearMapping = GetComponent<NVRLinearMapping>();
            }

            if (interactable == null)
            {
                interactable = GetComponent<NVRInteractable>();
            }
        }

        //-------------------------------------------------
        private void Update()
        {
            int currentToothIndex = Mathf.RoundToInt( linearMapping.value * teethCount - 0.5f );
            if ( currentToothIndex != previousToothIndex )
            {
                Pulse();
                previousToothIndex = currentToothIndex;
            }
        }


        //-------------------------------------------------
        private void Pulse()
        {
            if (interactable != null && interactable.IsAttached)
            {
                ushort duration = (ushort)Random.Range( minimumPulseDuration, maximumPulseDuration + 1 );
                interactable.AttachedHand.TriggerHapticPulse(duration);

                onPulse.Invoke();
            }
        }
    }
}

