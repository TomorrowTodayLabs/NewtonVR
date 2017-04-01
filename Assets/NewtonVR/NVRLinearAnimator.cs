using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    // Adapted from SteamVR InteractionSystem
    //
    // NOTE: SteamVR version enables/disables animator when mapping value remains
    //       unchanged. I removed this because it sends the animated object back
    //       to its origin transform when enabled=false.
    //
    //======= Copyright (c) Valve Corporation, All rights reserved. ===============
    //
    // Purpose: Animator whose speed is set based on a linear mapping
    //
    //=============================================================================
	public class NVRLinearAnimator : MonoBehaviour
	{
		public NVRLinearMapping linearMapping;
		public Animator animator;

		private float currentLinearMapping = float.NaN;

		//-------------------------------------------------
		void Awake()
		{
			if ( animator == null )
			{
				animator = GetComponent<Animator>();
			}

			animator.speed = 0.0f;

			if ( linearMapping == null )
			{
				linearMapping = GetComponent<NVRLinearMapping>();
			}
		}


		//-------------------------------------------------
		void Update()
		{
			if ( currentLinearMapping != linearMapping.value )
			{
				currentLinearMapping = linearMapping.value;
				animator.Play( 0, 0, currentLinearMapping );
			}
        }
    }
}
