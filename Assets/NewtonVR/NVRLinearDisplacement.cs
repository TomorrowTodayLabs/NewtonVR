using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    // Adapted from SteamVR InteractionSystem
    //
    //======= Copyright (c) Valve Corporation, All rights reserved. ===============
    //
    // Purpose: Move the position of this object based on a linear mapping
    //
    //=============================================================================
    public class NVRLinearDisplacement : MonoBehaviour
    {
        public NVRLinearMapping linearMapping;
        public Vector3 displacement;
        public float speed = Mathf.Infinity;
        public float dampTime = 0.2f;

        private Vector3 initialPosition;
        private Vector3 currentVelocity;
        private bool useSpeed;

        private void Awake()
        {
            useSpeed = speed < Mathf.Infinity;
        }

        //-------------------------------------------------
        private void Start()
        {
            initialPosition = transform.localPosition;

            if ( linearMapping == null )
            {
                linearMapping = GetComponent<NVRLinearMapping>();
            }
        }


        //-------------------------------------------------
        private void Update()
        {
            if (linearMapping == null) {
                return;
            }

            if (!useSpeed) {
                transform.localPosition = initialPosition + linearMapping.value * displacement;
            }
            else {
                var targetPosition = initialPosition + linearMapping.value * displacement;

                transform.localPosition = Vector3.SmoothDamp(
                    transform.localPosition, targetPosition, 
                    ref currentVelocity, 
                    dampTime, speed,
                    Time.deltaTime);
            }
        }
    }
}
