using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    // Adapted from SteamVR InteractionSystem
    //
    // Enhancements:
    //     - If another class modifies the LinearMapping this class references and
    //       repositionGameObject is set to true, the position will be updated to
    //       track the changing values of the mapping.
    //
    //======= Copyright (c) Valve Corporation, All rights reserved. ===============
    //
    // Purpose: Drives a linear mapping based on position between 2 positions
    //
    //=============================================================================
    public class NVRLinearDrive : NVRInteractable
    {
        [Space]
        [Header("Linear Drive")]

        public Transform startPosition;
        public Transform endPosition;
        public Transform grabPoint;
        public NVRLinearMapping linearMapping;
        public bool repositionGameObject = true;
        public bool maintainMomentum = true;
        public float momentumDampenRate = 5.0f;
        public bool startsSnappedToMapping = false;

        private float initialMappingOffset;
        private int numMappingChangeSamples = 5;
        private float[] mappingChangeSamples;
        private float prevMapping = 0.0f;
        private float mappingChangeRate;
        private int sampleCount = 0;
        private float lastValue = float.NaN;

        //-------------------------------------------------
        protected override void Awake()
        {
            base.Awake();
            mappingChangeSamples = new float[numMappingChangeSamples];

            DisableKinematicOnAttach = false;
            EnableKinematicOnDetach = true;
            EnableGravityOnDetach = false;

            if (grabPoint == null) {
                grabPoint = transform;
            }
        }


        //-------------------------------------------------
        protected override void Start()
        {
            base.Start();

            if ( linearMapping == null )
            {
                linearMapping = GetComponent<NVRLinearMapping>();
            }

            if ( linearMapping == null )
            {
                linearMapping = gameObject.AddComponent<NVRLinearMapping>();
            }

            if ( repositionGameObject )
            {
                if (startsSnappedToMapping) 
                {
                    // Snap to where the mapping is now and not the other way around
                    transform.position = Vector3.Lerp( startPosition.position, endPosition.position, linearMapping.value );
                }
                else {
                    UpdateLinearMapping( transform );
                }
            }
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);
            
            initialMappingOffset = linearMapping.value - CalculateLinearMapping( hand.transform );
            sampleCount = 0;
            mappingChangeRate = 0.0f;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            CalculateMappingChangeRate();
        }

        public override void InteractingUpdate(NVRHand hand)
        {
            UpdateLinearMapping(hand.transform);
        }

        //-------------------------------------------------
        private void CalculateMappingChangeRate()
        {
            //Compute the mapping change rate
            mappingChangeRate = 0.0f;
            int mappingSamplesCount = Mathf.Min( sampleCount, mappingChangeSamples.Length );
            if ( mappingSamplesCount != 0 )
            {
                for ( int i = 0; i < mappingSamplesCount; ++i )
                {
                    mappingChangeRate += mappingChangeSamples[i];
                }
                mappingChangeRate /= mappingSamplesCount;
            }
        }


        //-------------------------------------------------
        private void UpdateLinearMapping( Transform tr )
        {
            prevMapping = linearMapping.value;
            linearMapping.value = Mathf.Clamp01( initialMappingOffset + CalculateLinearMapping( tr ) );

            mappingChangeSamples[sampleCount % mappingChangeSamples.Length] = ( 1.0f / Time.deltaTime ) * ( linearMapping.value - prevMapping );
            sampleCount++;

            if ( repositionGameObject )
            {
                transform.position = Vector3.Lerp( startPosition.position, endPosition.position, linearMapping.value );
            }
        }


        //-------------------------------------------------
        private float CalculateLinearMapping( Transform tr )
        {
            Vector3 direction = endPosition.position - startPosition.position;
            float length = direction.magnitude;
            direction.Normalize();

            Vector3 displacement = tr.position - startPosition.position;

            return Vector3.Dot( displacement, direction ) / length;
        }


        //-------------------------------------------------
        protected override void Update()
        {
            base.Update();

            if (!IsAttached) 
            {
                if ( maintainMomentum && mappingChangeRate != 0.0f )
                {
                    //Dampen the mapping change rate and apply it to the mapping
                    mappingChangeRate = Mathf.Lerp( mappingChangeRate, 0.0f, momentumDampenRate * Time.deltaTime );
                    linearMapping.value = Mathf.Clamp01( linearMapping.value + ( mappingChangeRate * Time.deltaTime ) );
                    
                    if (Mathf.Abs(mappingChangeRate) < 0.05f) 
                    {
                        mappingChangeRate = 0f;
                    }

                    if ( repositionGameObject )
                    {
                        transform.position = Vector3.Lerp( startPosition.position, endPosition.position, linearMapping.value );
                    }
                }
                else if (repositionGameObject && lastValue != linearMapping.value) 
                {
                    // Keep in sync with anyone else who modifies the mapping 
                    transform.position = Vector3.Lerp( startPosition.position, endPosition.position, linearMapping.value );
                    lastValue = linearMapping.value; 
                }
            }
        }
    }
}
