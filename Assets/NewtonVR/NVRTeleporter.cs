using System.Collections.Generic;
using UnityEngine;

namespace NewtonVR
{
	public class NVRTeleporter : MonoBehaviour
	{
		public LineRenderer ArcRendererTemplate;
		public GameObject PlaySpaceDisplayTemplate;
		public GameObject InvalidPointDisplayTemplate;
		public GameObject TeleportTargetDisplayTemplate;

		public bool LimitToHorizontal = false;
		public float LimitSensitivity = 0f;

		public NVRButtons TeleportButton = NVRButtons.Touchpad;

		public LayerMask TeleportSurfaceMask;
		public LayerMask TeleportBlockMask;
		private LayerMask fullMask;

		public float ArcDistance = 5;
		public int SamplePoints = 150;
		public float SampleFrequency = 0.2f;

		private float curveMod = 0.25f;
		private float acceleration = -5;
		private float arcLineDisplayOffset = 0.1f;

		private Dictionary<int, TeleportPreview> teleportPreviews;
		private NVRPlayer player;

		private void Awake()
		{
			fullMask = TeleportSurfaceMask | TeleportBlockMask;
			teleportPreviews = new Dictionary<int, TeleportPreview>();
		}

		private void Start()
		{
			player = GetComponentInParent<NVRPlayer>();
			if (player != null)
			{
				Vector3 scale = player.PlayspaceSize / 2;
				scale.y = 1;

				//Render Playspace
				PlaySpaceDisplayTemplate.gameObject.transform.localScale = scale;
			}
			else
			{
				Debug.Log("NVR Player is Null");
			}
		}

		private void OnValidate()
		{
			ArcDistance = Mathf.Max(0.01f, ArcDistance);
			SamplePoints = Mathf.Max(2, SamplePoints);
			SampleFrequency = Mathf.Max(0.01f, SampleFrequency);
		}

		/// <summary>
		/// Paint and check parabolic curve for valid teleport position. Returns null if none found. 
		/// </summary>
		/// <param name="origin"></param>
		/// <returns></returns>
		public Vector3? UpdateArcTeleport(Transform origin, int controllerIndex)
		{
			//Controller is not currently paired with a line. Assign
			if (!teleportPreviews.ContainsKey(controllerIndex))
			{
				TeleportPreview tp = new TeleportPreview();

				//Default line is not being used. Assign it.
				if (teleportPreviews.Count == 0)
				{
					tp.ArcLine = ArcRendererTemplate;
					tp.PlaySpaceDisplay = PlaySpaceDisplayTemplate;
					tp.InvalidPointDisplay = InvalidPointDisplayTemplate;
					tp.TeleportTargetDisplay = TeleportTargetDisplayTemplate;
				}
				//Default line is already in use. Create another one.
				else
				{
					GameObject newLine = Instantiate(ArcRendererTemplate.gameObject, transform) as GameObject;
					tp.ArcLine = newLine.GetComponent<LineRenderer>();

					tp.PlaySpaceDisplay = Instantiate(PlaySpaceDisplayTemplate, transform);
					tp.InvalidPointDisplay = Instantiate(InvalidPointDisplayTemplate, transform);
					tp.TeleportTargetDisplay = Instantiate(TeleportTargetDisplayTemplate, transform);
				}

				teleportPreviews.Add(controllerIndex, tp);
			}
			teleportPreviews[controllerIndex].ArcLine.enabled = true;

			//Update start position to be a little away from the controller
			Vector3 startPosition = origin.position + (origin.transform.forward * arcLineDisplayOffset);

			//Check for Valid Teleport. Returns Points along curve until (possible) hit
			List<Vector3> points;
			bool hit;
			RaycastHit hitInfo;
			bool validTeleport = CheckTeleportCurve(startPosition, origin.TransformDirection(Vector3.forward * ArcDistance), Vector3.up * acceleration, out points, out hit, out hitInfo);

			//Render Line to second to last point (small gap for display)
			teleportPreviews[controllerIndex].ArcLine.numPositions = points.Count - 1;
			for (int i = 0; i < points.Count - 1; i++)
			{
				teleportPreviews[controllerIndex].ArcLine.SetPosition(i, points[i]);
			}

			if (hit)
			{
				//Line hit a surface
				if (validTeleport)
				{
					//Render Plasypace and target
					Vector3 offset = player.Head.transform.localPosition;
					offset.y = 0;

					teleportPreviews[controllerIndex].PlaySpaceDisplay.SetActive(true);
					teleportPreviews[controllerIndex].PlaySpaceDisplay.transform.position = hitInfo.point - offset;

					teleportPreviews[controllerIndex].TeleportTargetDisplay.SetActive(true);
					teleportPreviews[controllerIndex].TeleportTargetDisplay.transform.position = hitInfo.point;

					//Hide Invalid
					teleportPreviews[controllerIndex].InvalidPointDisplay.SetActive(false);

					//Hit point is final point in curve
					return hitInfo.point;
				}
				else
				{
					//Show Invalid
					teleportPreviews[controllerIndex].InvalidPointDisplay.SetActive(true);
					teleportPreviews[controllerIndex].InvalidPointDisplay.gameObject.transform.position = hitInfo.point + (hitInfo.normal * 0.05f);
					teleportPreviews[controllerIndex].InvalidPointDisplay.gameObject.transform.rotation = Quaternion.LookRotation(hitInfo.normal);

					//Hide Playspace and target
					teleportPreviews[controllerIndex].PlaySpaceDisplay.SetActive(false);
					teleportPreviews[controllerIndex].TeleportTargetDisplay.SetActive(false);
				}
			}
			else
			{
				//Hide Playspace, Target, and Invalid Marker
				teleportPreviews[controllerIndex].PlaySpaceDisplay.SetActive(false);
				teleportPreviews[controllerIndex].InvalidPointDisplay.SetActive(false);
				teleportPreviews[controllerIndex].TeleportTargetDisplay.SetActive(false);
			}

			//No valid teleport data. Return null
			return null;
		}

		/// <summary>
		/// Hide the arc teleporter (button release)
		/// </summary>
		/// <param name="controllerIndex"></param>
		public void HideArcTeleport(int controllerIndex)
		{
			teleportPreviews[controllerIndex].ArcLine.numPositions = 0;
			teleportPreviews[controllerIndex].ArcLine.enabled = false;
			teleportPreviews[controllerIndex].PlaySpaceDisplay.SetActive(false);
			teleportPreviews[controllerIndex].InvalidPointDisplay.SetActive(false);
			teleportPreviews[controllerIndex].TeleportTargetDisplay.SetActive(false);
		}


		/// <summary>
		/// Sample points along a curve until you hit a collider, or you run out of sample points
		/// </summary>
		/// <param name="startingPoint">Starting point of your parabolic curve</param>
		/// <param name="initialVelocity">Initial velocity of your parabolic curve</param>
		/// <param name="initialAcceleration">Initial acceleration of your parabolic curve</param>
		/// <returns></returns>
		private bool CheckTeleportCurve(Vector3 startingPoint, Vector3 initialVelocity, Vector3 initialAcceleration,
			out List<Vector3> points, out bool hit, out RaycastHit hitInfo)
		{
			points = new List<Vector3>() { startingPoint };
			hitInfo = new RaycastHit();
			bool validTeleport = false;
			hit = false;

			Vector3 lastPoint = startingPoint;
			float t = 0;

			for (int i = 0; i < SamplePoints; i++)
			{
				t += SampleFrequency / CurveDerivitive(initialVelocity, initialAcceleration, t).magnitude;
				Vector3 nextPoint = Curve(startingPoint, initialVelocity, initialAcceleration, t);

				//Check for a valid teleport collision
				if (Physics.Linecast(lastPoint, nextPoint, out hitInfo, fullMask))
				{
					//Register the Hit
					hit = true;

					//End of curve. Add final position.
					points.Add(hitInfo.point);

					//If the hit was a valid teleport position
					if (TeleportSurfaceMask == (TeleportSurfaceMask | 1 << hitInfo.collider.gameObject.layer))
					{

						if (!LimitToHorizontal || Vector3.Angle(hitInfo.normal, Vector3.up) <= LimitSensitivity)
						{
							validTeleport = true;
						}
					}

					break;
				}
				else
				{
					points.Add(nextPoint);
				}

				lastPoint = nextPoint;
			}

			return validTeleport;
		}

		/// <summary>
		/// Teleport Player and anything in their hands to the new location
		/// </summary>
		/// <param name="teleportPosition"></param>
		public void TeleportPlayer(Vector3 teleportPosition)
		{
			if (player != null)
			{
				Vector3 offset = player.Head.transform.position - player.transform.position;
				offset.y = 0;

				//Move Player
				player.transform.position = teleportPosition - offset;

				//Teleport any objects in left hand to new hand position
				if (player.LeftHand.CurrentlyInteracting != null)
				{
					player.LeftHand.CurrentlyInteracting.transform.position = player.LeftHand.transform.position;
				}

				//Teleport any objects in left hand to new hand position
				if (player.RightHand.CurrentlyInteracting != null)
				{
					player.RightHand.CurrentlyInteracting.transform.position = player.RightHand.transform.position;
				}
			}
		}

		private Vector3 CurveDerivitive(Vector3 velocity, Vector3 acceleration, float time)
		{
			Vector3 result = new Vector3();
			result.x = CurveDerivitive(velocity.x, acceleration.x, time);
			result.y = CurveDerivitive(velocity.y, acceleration.y, time);
			result.z = CurveDerivitive(velocity.z, acceleration.z, time);
			return result;
		}

		private float CurveDerivitive(float velocity, float acceleration, float time)
		{
			return velocity + acceleration * time;
		}

		private Vector3 Curve(Vector3 point, Vector3 velocity, Vector3 acceleration, float time)
		{
			Vector3 result = new Vector3();
			result.x = Curve(point.x, velocity.x, acceleration.x, time);
			result.y = Curve(point.y, velocity.y, acceleration.y, time);
			result.z = Curve(point.z, velocity.z, acceleration.z, time);
			return result;
		}

		private float Curve(float point, float velocity, float acceleration, float time)
		{
			return point + velocity * time + curveMod * acceleration * time * time;
		}

		public class TeleportPreview
		{
			public LineRenderer ArcLine;
			public GameObject PlaySpaceDisplay;
			public GameObject InvalidPointDisplay;
			public GameObject TeleportTargetDisplay;
		}
	}
}
