using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using NewtonVR;

namespace NewtonVR
{
    // Adapted from SteamVR InteractionSystem
    //
    // NOTE: This class in SteamVR 1.2.0 was very broken. Rotation axes only worked
    //       in global space and did not respect initial starting rotation of the
    //       object this component was on. Limits were also busted and once you hit
    //       one you could almost never leave it.
    //
    // NOTE: Additional support was added to allow for:
    //       - angleForMaxValue: Allow configuration of wheel rotations -> mapping value.
    //       - Momentum on release like LinearDrive
    //       - Update rotation if the linear mapping is changed externally to this driver.
    //
    //======= Copyright (c) Valve Corporation, All rights reserved. ===============
    //
    // Purpose: Interactable that can be used to move in a circular motion
    //
    //=============================================================================
	public class NVRCircularDrive : NVRInteractable
	{
		public enum Axis_t
		{
			XAxis,
			YAxis,
			ZAxis
		};

        [Header("Circular Drive")]

		[Tooltip( "The axis around which the circular drive will rotate in local space" )]
		public Axis_t axisOfRotation = Axis_t.XAxis;

		[Tooltip( "A LinearMapping component to drive, if not specified one will be dynamically added to this GameObject" )]
		public NVRLinearMapping linearMapping;

        public bool maintainMomentum = false;
        public float momentumDampRate = 5f;
        public float momentumBounceMultiplier = 0.55f;
        
        [Tooltip("Angle required to hit the max value. For instance if you wanted two full revolutions of this object to set linearMapping.value to 1, you would use 720 as your value.")]
        public float angleForMaxValue = 360f;

		[HeaderAttribute( "Limited Rotation" )]
		[Tooltip( "If true, the rotation will be limited to [minAngle, maxAngle], if false, the rotation is unlimited" )]
		public bool limited = false;
		public Vector2 frozenDistanceMinMaxThreshold = new Vector2( 0.1f, 0.2f );
		public UnityEvent onFrozenDistanceThreshold;

		[HeaderAttribute( "Limited Rotation Min" )]
		[Tooltip( "If limited is true, the specifies the lower limit, otherwise value is unused" )]
		public float minAngle = -45.0f;
		[Tooltip( "If limited, set whether drive will freeze its angle when the min angle is reached" )]
		public bool freezeOnMin = false;
		[Tooltip( "If limited, event invoked when minAngle is reached" )]
		public UnityEvent onMinAngle;

		[HeaderAttribute( "Limited Rotation Max" )]
		[Tooltip( "If limited is true, the specifies the upper limit, otherwise value is unused" )]
		public float maxAngle = 45.0f;
		[Tooltip( "If limited, set whether drive will freeze its angle when the max angle is reached" )]
		public bool freezeOnMax = false;
		[Tooltip( "If limited, event invoked when maxAngle is reached" )]
		public UnityEvent onMaxAngle;

		[Tooltip( "If limited is true, this forces the starting angle to be startAngle, clamped to [minAngle, maxAngle]" )]
		public bool forceStart = false;
		[Tooltip( "If limited is true and forceStart is true, the starting angle will be this, clamped to [minAngle, maxAngle]" )]
		public float startAngle = 0.0f;

		[Tooltip( "If true, the transform of the GameObject this component is on will be rotated accordingly" )]
		public bool rotateGameObject = true;

        [Header("Debug")]

		[Tooltip( "If true, the path of the Hand (red) and the projected value (green) will be drawn" )]
		public bool debugPath = false;
		[Tooltip( "If debugPath is true, this is the maximum number of GameObjects to create to draw the path" )]
		public int dbgPathLimit = 50;

		[Tooltip( "The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations" )]
		public float outAngle;

        private Quaternion start;

		private Vector3 worldPlaneNormal = new Vector3( 1.0f, 0.0f, 0.0f );
		private Vector3 localPlaneNormal = new Vector3( 1.0f, 0.0f, 0.0f );

		private Vector3 lastHandProjected;

        private float outAngleMultiplier;

        private Vector3 detachPoint;

        private float lastValue = float.NaN;

        private float angleVelocity = 0f;
        private int numOutputAngleSamples = 5;
        private float[] outputAngleSamples;
        private int sampleCount;

		private Color red = new Color( 1.0f, 0.0f, 0.0f );
		private Color green = new Color( 0.0f, 1.0f, 0.0f );

		private GameObject[] dbgHandObjects;
		private GameObject[] dbgProjObjects;
		private GameObject dbgObjectsParent;
        private GameObject dbgAttachPoint;
		private int dbgObjectCount = 0;
		private int dbgObjectIndex = 0;

        private GameObject InitialAttachPoint;

		// If the drive is limited as is at min/max, angles greater than this are ignored 
		private float minMaxAngularThreshold = 5.0f;

		private bool frozen = false;
		private float frozenAngle = 0.0f;
		private Vector3 frozenHandWorldPos = new Vector3( 0.0f, 0.0f, 0.0f );
		private Vector2 frozenSqDistanceMinMaxThreshold = new Vector2( 0.0f, 0.0f );

		//-------------------------------------------------
		private void Freeze( Vector3 handPos)
		{
			frozen = true;
			frozenAngle = outAngle;
			frozenHandWorldPos = handPos; //hand.transform.position; //hand.hoverSphereTransform.position;
			frozenSqDistanceMinMaxThreshold.x = frozenDistanceMinMaxThreshold.x * frozenDistanceMinMaxThreshold.x;
			frozenSqDistanceMinMaxThreshold.y = frozenDistanceMinMaxThreshold.y * frozenDistanceMinMaxThreshold.y;
		}


		//-------------------------------------------------
		public void UnFreeze()
		{
			frozen = false;
			frozenHandWorldPos.Set( 0.0f, 0.0f, 0.0f );
		}

        protected override void Awake()
        {
            base.Awake();
            
            outputAngleSamples = new float[numOutputAngleSamples];

            outAngleMultiplier = 360.0f / angleForMaxValue;

            DisableKinematicOnAttach = false;
            EnableKinematicOnDetach = true;
            EnableGravityOnDetach = false;
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

			worldPlaneNormal = new Vector3( 0.0f, 0.0f, 0.0f );
			worldPlaneNormal[(int)axisOfRotation] = 1.0f;

            localPlaneNormal = worldPlaneNormal;

            start = Quaternion.Euler(
                axisOfRotation != Axis_t.XAxis || true ? transform.localEulerAngles[0] : 0f,
                axisOfRotation != Axis_t.YAxis || true ? transform.localEulerAngles[1] : 0f,
                axisOfRotation != Axis_t.ZAxis || true ? transform.localEulerAngles[2] : 0f
            );

			if ( transform.parent )
			{
				worldPlaneNormal = transform.TransformDirection(worldPlaneNormal);
			}
            else
            {
                worldPlaneNormal = transform.TransformDirection(worldPlaneNormal);
                localPlaneNormal = worldPlaneNormal;
            }

			if ( limited )
			{
				outAngle = 0.0f;

				if ( forceStart )
				{
					outAngle = Mathf.Clamp( startAngle, minAngle, maxAngle );
				}
			}
			else
			{
				outAngle = 0.0f;
			}

			UpdateAll();
		}

        //-------------------------------------------------
		private IEnumerator HapticPulses( NVRHand controller, float flMagnitude, int nCount )
		{
			if ( controller != null )
			{
                var wait = new WaitForSeconds(0.01f);

				int nRangeMax = (int)RemapNumberClamped( flMagnitude, 0.0f, 1.0f, 100.0f, 900.0f );
				nCount = Mathf.Clamp( nCount, 1, 10 );

				for ( ushort i = 0; i < nCount; ++i )
				{
					ushort duration = (ushort)Random.Range( 100, nRangeMax );
					controller.TriggerHapticPulse( duration );
					yield return wait;
				}
			}
		}

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", this.gameObject.name));
            InitialAttachPoint.transform.position = hand.transform.position;
            InitialAttachPoint.transform.rotation = hand.transform.rotation;
            InitialAttachPoint.transform.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.transform.parent = this.transform;

            lastHandProjected = ComputeToTransformProjected( hand.transform.position ); //.hoverSphereTransform );

            sampleCount = 0;
            angleVelocity = 0f;
            detachPoint = Vector3.zero;

            ComputeAngle( hand.transform.position, hand );
            UpdateAll();
        }

        public override void InteractingUpdate(NVRHand hand)
        {
            ComputeAngle( hand.transform.position, hand );
            UpdateAll();
        }

        public override void EndInteraction()
        {
            var hand = AttachedHand;

            base.EndInteraction();

            if (maintainMomentum) {
                CalculateOutputAngleVelocity();
                
                //Debug.LogFormat("outputAngleVelocity: {0}", angleVelocity);

                // Remember where the hand detached
                var plane = new Vector3( 0.0f, 0.0f, 0.0f );
                plane[(int)axisOfRotation] = 1.0f;

                detachPoint = Vector3.ProjectOnPlane(
                    transform.InverseTransformPoint(hand.transform.position),
                    plane);
            }

            if (InitialAttachPoint != null) {
                Destroy(InitialAttachPoint);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (IsAttached) {
                return;
            }

            if (maintainMomentum && angleVelocity != 0f && lastValue == linearMapping.value) {
                var angleDelta = angleVelocity * Time.deltaTime;

                SetOutputAngleFromSignedAngleDelta(angleDelta, transform.TransformPoint(detachPoint));

                UpdateAll();

                lastValue = linearMapping.value;

                angleVelocity = Mathf.Lerp(angleVelocity, 0.0f, momentumDampRate * Time.deltaTime);

                var absAngleVelocity = Mathf.Abs(angleVelocity);

                if (absAngleVelocity < 0.05f || frozen) {
                    angleVelocity = 0f;
                }
                else if (!frozen && 
                        limited && 
                        (outAngle <= minAngle || outAngle >= maxAngle) &&
                        absAngleVelocity > 0.2f) {
                    // elastic-ish bounceback
                    angleVelocity = -angleVelocity * momentumBounceMultiplier;
                }
            }
            else if (rotateGameObject && lastValue != linearMapping.value) {
                angleVelocity = 0f;

                if (limited) {
                    outAngle = Mathf.Lerp(minAngle, maxAngle, linearMapping.value);
                }
                else {
                    outAngle = Mathf.Lerp(0f, 360f * outAngleMultiplier, linearMapping.value);
                }

                UpdateGameObject();

                lastValue = linearMapping.value;
            }
        }

		//-------------------------------------------------
		private Vector3 ComputeToTransformProjected( Vector3 xForm )
		{
			Vector3 toTransform = ( xForm - transform.position ).normalized;
			Vector3 toTransformProjected = new Vector3( 0.0f, 0.0f, 0.0f );

			// Need a non-zero distance from the hand to the center of the CircularDrive
			if ( toTransform.sqrMagnitude > 0.0f )
			{
				toTransformProjected = Vector3.ProjectOnPlane( toTransform, worldPlaneNormal ).normalized;
			}
			else
			{
				Debug.LogFormat( "The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString() );
				Debug.Assert( false, string.Format( "The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString() ) );
			}

            #if UNITY_EDITOR
			if ( debugPath && dbgPathLimit > 0 )
			{
				DrawDebugPath( xForm, toTransformProjected );
			}
            #endif

			return toTransformProjected;
		}


		//-------------------------------------------------
		private void DrawDebugPath( Vector3 xForm, Vector3 toTransformProjected )
		{
        #if !UNITY_EDITOR
            return;
        #else
			if ( dbgObjectCount == 0 )
			{
				dbgObjectsParent = new GameObject( "Circular Drive Debug" );
				dbgHandObjects = new GameObject[dbgPathLimit];
				dbgProjObjects = new GameObject[dbgPathLimit];
				dbgObjectCount = dbgPathLimit;
				dbgObjectIndex = 0;

                dbgAttachPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				dbgAttachPoint.transform.SetParent( dbgObjectsParent.transform );
                dbgAttachPoint.transform.localScale = new Vector3( 0.008f, 0.008f, 0.008f );
                dbgAttachPoint.gameObject.GetComponent<Renderer>().material.color = Color.black; 
			}

            if (InitialAttachPoint != null) {
                dbgAttachPoint.transform.position = InitialAttachPoint.transform.position;
            }
            else {
                dbgAttachPoint.transform.position = transform.TransformPoint(detachPoint);
            }

			//Actual path
			GameObject gSphere = null;

			if ( dbgHandObjects[dbgObjectIndex] )
			{
				gSphere = dbgHandObjects[dbgObjectIndex];
			}
			else
			{
				gSphere = GameObject.CreatePrimitive( PrimitiveType.Sphere );
				gSphere.transform.SetParent( dbgObjectsParent.transform );
				dbgHandObjects[dbgObjectIndex] = gSphere;
			}

			gSphere.name = string.Format( "actual_{0}", (int)( ( 1.0f - red.r ) * 10.0f ) );
			gSphere.transform.position = xForm;
			gSphere.transform.rotation = Quaternion.Euler( 0.0f, 0.0f, 0.0f );
			gSphere.transform.localScale = new Vector3( 0.004f, 0.004f, 0.004f );
			gSphere.gameObject.GetComponent<Renderer>().material.color = red;

			if ( red.r > 0.1f )
			{
				red.r -= 0.1f;
			}
			else
			{
				red.r = 1.0f;
			}

			//Projected path
			gSphere = null;

			if ( dbgProjObjects[dbgObjectIndex] )
			{
				gSphere = dbgProjObjects[dbgObjectIndex];
			}
			else
			{
				gSphere = GameObject.CreatePrimitive( PrimitiveType.Sphere );
				gSphere.transform.SetParent( dbgObjectsParent.transform );
				dbgProjObjects[dbgObjectIndex] = gSphere;
			}

			gSphere.name = string.Format( "projed_{0}", (int)( ( 1.0f - green.g ) * 10.0f ) );
			gSphere.transform.position = transform.position + toTransformProjected * 0.25f;
			gSphere.transform.rotation = Quaternion.Euler( 0.0f, 0.0f, 0.0f );
			gSphere.transform.localScale = new Vector3( 0.004f, 0.004f, 0.004f );
			gSphere.gameObject.GetComponent<Renderer>().material.color = green;

			if ( green.g > 0.1f )
			{
				green.g -= 0.1f;
			}
			else
			{
				green.g = 1.0f;
			}

			dbgObjectIndex = ( dbgObjectIndex + 1 ) % dbgObjectCount;
        #endif
		}


		//-------------------------------------------------
		// Updates the LinearMapping value from the angle
		//-------------------------------------------------
		private void UpdateLinearMapping()
		{
			if ( limited )
			{
				// Map it to a [0, 1] value
				linearMapping.value = Mathf.Clamp01(( outAngle - minAngle ) / ( maxAngle - minAngle ));
			}
			else
			{
				// Normalize to [0, 1] based on 360 degree windings
				float flTmp = outAngle / 360.0f * outAngleMultiplier;
				linearMapping.value = Mathf.Clamp01(flTmp - Mathf.Floor( flTmp ));
			}

            #if UNITY_EDITOR
			UpdateDebugText();
            #endif
		}


		//-------------------------------------------------
		// Updates the LinearMapping value from the angle
		//-------------------------------------------------
		private void UpdateGameObject()
		{
			if ( rotateGameObject )
			{
                //transform.localRotation = start * Quaternion.AngleAxis( outAngle, localPlaneNormal );
                
                if (transform.parent == null) {
                    transform.localRotation = Quaternion.AngleAxis( outAngle, localPlaneNormal ) * start;
                }
                else {
                    transform.localRotation = start * Quaternion.AngleAxis( outAngle, localPlaneNormal );
                }
			}
		}

		//-------------------------------------------------
		// Updates the Debug TextMesh with the linear mapping value and the angle
		//-------------------------------------------------
		private void UpdateDebugText()
		{
            /*
            NOTE: Disabled this...
			if ( debugText )
			{
				debugText.text = string.Format( "Linear: {0}\nAngle:  {1}\n", linearMapping.value, outAngle );
			}
            */
		}


		//-------------------------------------------------
		// Updates the Debug TextMesh with the linear mapping value and the angle
		//-------------------------------------------------
		private void UpdateAll()
		{
			UpdateLinearMapping();
			UpdateGameObject();
            #if UNITY_EDITOR
			UpdateDebugText();
            #endif
		}


		//-------------------------------------------------
		// Computes the angle to rotate the game object based on the change in the transform
		//-------------------------------------------------
		private void ComputeAngle( Vector3 worldPosition, NVRHand hand = null)
		{
			Vector3 toHandProjected = ComputeToTransformProjected( worldPosition ); //hand.hoverSphereTransform );

			if ( !toHandProjected.Equals( lastHandProjected ) )
			{
				float absAngleDelta = Vector3.Angle( lastHandProjected, toHandProjected );

				if ( absAngleDelta > 0.0f )
				{
					if ( frozen )
					{
						//float frozenSqDist = ( hand.hoverSphereTransform.position - frozenHandWorldPos ).sqrMagnitude;
						float frozenSqDist = ( worldPosition - frozenHandWorldPos ).sqrMagnitude;
						if ( frozenSqDist > frozenSqDistanceMinMaxThreshold.x )
						{
							outAngle = frozenAngle + Random.Range( -1.0f, 1.0f );

							float magnitude = RemapNumberClamped( frozenSqDist, frozenSqDistanceMinMaxThreshold.x, frozenSqDistanceMinMaxThreshold.y, 0.0f, 1.0f );
							if ( magnitude > 0 )
							{
								StartCoroutine( HapticPulses( hand, magnitude, 10 ) );
							}
							else
							{
								StartCoroutine( HapticPulses( hand, 0.5f, 10 ) );
							}

							if ( frozenSqDist >= frozenSqDistanceMinMaxThreshold.y )
							{
								onFrozenDistanceThreshold.Invoke();
							}
						}
					}
					else
					{
						Vector3 cross = Vector3.Cross( lastHandProjected, toHandProjected ).normalized;
						float dot = Vector3.Dot( worldPlaneNormal, cross );

						float signedAngleDelta = absAngleDelta;

						if ( dot < 0.0f )
						{
							signedAngleDelta = -signedAngleDelta;
						}

                        SetOutputAngleFromSignedAngleDelta(signedAngleDelta, hand.transform.position);

                        lastHandProjected = toHandProjected;
					}
				}
			}
		}

        private void SetOutputAngleFromSignedAngleDelta(float signedAngleDelta, Vector3 handPos)
        {
            if ( limited )
            {
                outputAngleSamples[sampleCount % outputAngleSamples.Length] = signedAngleDelta / Time.deltaTime;
                sampleCount++;

                float absAngleDelta = Mathf.Abs(signedAngleDelta);
                float angleTmp = Mathf.Clamp( outAngle + signedAngleDelta, minAngle, maxAngle );

                //Debug.LogFormat("{1} limited angleTmp: {0}, absAngleDelta: {3}, signedAngleDelta: {2}", angleTmp, Time.frameCount, signedAngleDelta, absAngleDelta);

                if ( outAngle == minAngle )
                {
                    if ( angleTmp > minAngle && absAngleDelta < minMaxAngularThreshold )
                    {
                        outAngle = angleTmp;
                    }
                }
                else if ( outAngle == maxAngle )
                {
                    if ( angleTmp < maxAngle && absAngleDelta < minMaxAngularThreshold )
                    {
                        outAngle = angleTmp;
                    }
                }
                else if ( angleTmp == minAngle )
                {
                    outAngle = angleTmp;
                    onMinAngle.Invoke();
                    if ( freezeOnMin )
                    {
                        Freeze( handPos );
                    }
                }
                else if ( angleTmp == maxAngle )
                {
                    outAngle = angleTmp;
                    onMaxAngle.Invoke();
                    if ( freezeOnMax )
                    {
                        Freeze( handPos );
                    }
                }
                else
                {
                    outAngle = angleTmp;
                }
            }
            else
            {
                outAngle += signedAngleDelta;
            }
        }

        private void CalculateOutputAngleVelocity()
        {
            // No velocity if we are at limits
            if (limited && (outAngle <= minAngle || outAngle >= maxAngle)) {
                angleVelocity = 0f;
                return;
            }

            //Compute the mapping change rate
            angleVelocity = 0.0f;
            int count = Mathf.Min( sampleCount, outputAngleSamples.Length );
            if ( count != 0)
            {
                for ( int i = 0; i < count; ++i )
                {
                    angleVelocity += outputAngleSamples[i];
                }
                angleVelocity /= count;
            }

            lastValue = linearMapping.value;
        }

        public static float RemapNumber( float num, float low1, float high1, float low2, float high2 )
        {
            return low2 + ( num - low1 ) * ( high2 - low2 ) / ( high1 - low1 );
        }

        //-------------------------------------------------
        public static float RemapNumberClamped( float num, float low1, float high1, float low2, float high2 )
        {
            return Mathf.Clamp( RemapNumber( num, low1, high1, low2, high2 ), Mathf.Min( low2, high2 ), Mathf.Max( low2, high2 ) );
        }
	}
}
