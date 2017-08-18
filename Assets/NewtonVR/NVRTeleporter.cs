using System.Collections.Generic;
using UnityEngine;

namespace NewtonVR
{
	public class NVRTeleporter : MonoBehaviour
	{
		public static NVRTeleporter Instance;

		public LineRenderer ArcRendererTemplate;
		public LineRenderer PlaySpaceRendererTemplate;
		public LineRenderer InvalidRendererTemplate;

		public bool LimitToHorizontal = false;
		public float LimitSensitivity = 0f;

		public NVRButtons TeleportButton = NVRButtons.Touchpad;

		public LayerMask TeleportSurfaceMask;
		public LayerMask TeleportBlockMask;
		private LayerMask _fullMask;

		public float Velocity = 5;
		public float Acceleration = -5;
		public int SamplePoints = 150;
		public float SampleDistance = 0.2f;

		private float _curveMod = 0.25f;
		private float _arcLineDisplayOffset = 0.1f;

		private Dictionary<int, TeleportPreview> _teleportPreviews;


		private void Awake()
		{
			Instance = this;
			_fullMask = TeleportSurfaceMask | TeleportBlockMask;
			_teleportPreviews = new Dictionary<int, TeleportPreview>();
		}

		private void Start()
		{ 
			if (NVRPlayer.Instance != null)
			{
				Vector3 scale = NVRPlayer.Instance.PlayspaceSize / 2;
				scale.y = 1;

				PlaySpaceRendererTemplate.gameObject.transform.localScale = scale;
			}
			else
			{
				Debug.Log("NVR Player is Null");
			}
		}

		/// <summary>
		/// Paint and check parabolic curve for valid teleport position. Returns null if none found. 
		/// </summary>
		/// <param name="origin"></param>
		/// <returns></returns>
		public Vector3? UpdateArcTeleport(Transform origin, int controllerIndex)
		{
			//Controller is not currently paired with a line. Assign
			if (!_teleportPreviews.ContainsKey(controllerIndex))
			{
				TeleportPreview tp = new TeleportPreview();

				//Default line is not being used. Assign it.
				if (_teleportPreviews.Count == 0)
				{
					tp.ArcLine = ArcRendererTemplate;
					tp.PlaySpaceLine = PlaySpaceRendererTemplate;
					tp.InvalidLine = InvalidRendererTemplate;
				}
				//Default line is already in use. Create another one.
				else
				{
					GameObject newLine = Instantiate(ArcRendererTemplate.gameObject, transform) as GameObject;
					tp.ArcLine = newLine.GetComponent<LineRenderer>();

					GameObject newSpace = Instantiate(PlaySpaceRendererTemplate.gameObject, transform) as GameObject;
					tp.PlaySpaceLine = newSpace.GetComponent<LineRenderer>();

					GameObject newInavalid = Instantiate(InvalidRendererTemplate.gameObject, transform) as GameObject;
					tp.InvalidLine = newInavalid.GetComponent<LineRenderer>();
				}

				_teleportPreviews.Add(controllerIndex, tp);
			}
			_teleportPreviews[controllerIndex].ArcLine.enabled = true;

			//Update start position to be a little away from the controller
			Vector3 startPosition = origin.position + (origin.transform.forward * _arcLineDisplayOffset);

			List<Vector3> points;
			bool hit;
			RaycastHit hitInfo;
			bool validTeleport = CheckTeleportCurve(startPosition, origin.TransformDirection(Vector3.forward * Velocity), Vector3.up * Acceleration, out points, out hit, out hitInfo);

			//Render Line to second to last point (small gap for display)
			_teleportPreviews[controllerIndex].ArcLine.SetVertexCount(points.Count - 1);
			for (int i = 0; i < points.Count - 1; i++)
			{
				_teleportPreviews[controllerIndex].ArcLine.SetPosition(i, points[i]);
			}


			if (hit)
			{
				//Line hit a surface
				if (validTeleport)
				{
					//Render Plasypace
					_teleportPreviews[controllerIndex].PlaySpaceLine.enabled = true;
					_teleportPreviews[controllerIndex].PlaySpaceLine.gameObject.transform.position = hitInfo.point;

					//Hide Invalid
					_teleportPreviews[controllerIndex].InvalidLine.enabled = false;

					//Hit point is final point in curve
					return hitInfo.point;
				}
				else
				{
					//Show Invalid
					_teleportPreviews[controllerIndex].InvalidLine.enabled = true;
					_teleportPreviews[controllerIndex].InvalidLine.gameObject.transform.position = hitInfo.point + (hitInfo.normal * 0.05f);
					_teleportPreviews[controllerIndex].InvalidLine.gameObject.transform.rotation = Quaternion.LookRotation(hitInfo.normal);

					//Hide Playspace
					_teleportPreviews[controllerIndex].PlaySpaceLine.enabled = false;
				}
			}
			else
			{
				//Hide Playspace and Invalid Marker
				_teleportPreviews[controllerIndex].PlaySpaceLine.enabled = false;
				_teleportPreviews[controllerIndex].InvalidLine.enabled = false;
			}

			//No valid teleport data. Return null
			return null;
		}

		public void HideArcTeleport(int controllerIndex)
		{
			_teleportPreviews[controllerIndex].ArcLine.SetVertexCount(0);
			_teleportPreviews[controllerIndex].ArcLine.enabled = false;
			_teleportPreviews[controllerIndex].PlaySpaceLine.enabled = false;
			_teleportPreviews[controllerIndex].InvalidLine.enabled = false;
		}


		/// <summary>
		/// Sample points along a curve until you hit a collider, or you run out of sample points
		/// </summary>
		/// <param name="startingPoint">Starting point of your parabolic curve</param>
		/// <param name="initialVelocity">Initial velocity of your parabolic curve</param>
		/// <param name="initialAcceleration">Initial accelleration of your parabolic curve</param>
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
				t += SampleDistance / CurveDerivitive(initialVelocity, initialAcceleration, t).magnitude;
				Vector3 nextPoint = Curve(startingPoint, initialVelocity, initialAcceleration, t);

				//Check for a valid teleport collision
				if (Physics.Linecast(lastPoint, nextPoint, out hitInfo, _fullMask))
				{
					//Register the Hit
					hit = true;

					//End of curve. Add final position.
					points.Add(hitInfo.point);

					//If the hit was a valid teleport position
					if (TeleportSurfaceMask == (TeleportSurfaceMask | 1 << hitInfo.collider.gameObject.layer))
					{

						if (!LimitToHorizontal || Vector3.Angle(hitInfo.normal, Vector3.up) < LimitSensitivity)
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
			if (NVRPlayer.Instance != null)
			{
				Vector3 offset = NVRPlayer.Instance.Head.transform.position - NVRPlayer.Instance.transform.position;
				offset.y = 0;

				//Move Player
				NVRPlayer.Instance.transform.position = teleportPosition - offset;

				//Teleport any objects in left hand to new hand position
				if (NVRPlayer.Instance.LeftHand.CurrentlyInteracting != null)
				{
					NVRPlayer.Instance.LeftHand.CurrentlyInteracting.transform.position = NVRPlayer.Instance.LeftHand.transform.position;
				}

				//Teleport any objects in left hand to new hand position
				if (NVRPlayer.Instance.RightHand.CurrentlyInteracting != null)
				{
					NVRPlayer.Instance.RightHand.CurrentlyInteracting.transform.position = NVRPlayer.Instance.RightHand.transform.position;
				}
			}
		}

		private Vector3 CurveDerivitive(Vector3 v0, Vector3 a, float t)
		{
			Vector3 point = new Vector3();
			for (int x = 0; x < 3; x++)
				point[x] = CurveDerivitive(v0[x], a[x], t);
			return point;
		}

		private float CurveDerivitive(float v0, float a, float t)
		{
			return v0 + a * t;
		}

		private Vector3 Curve(Vector3 p0, Vector3 v0, Vector3 a, float t)
		{
			Vector3 point = new Vector3();
			for (int x = 0; x < 3; x++)
				point[x] = Curve(p0[x], v0[x], a[x], t);
			return point;
		}

		private float Curve(float p0, float v0, float a, float t)
		{
			return p0 + v0 * t + _curveMod * a * t * t;
		}

		public class TeleportPreview
		{
			public LineRenderer ArcLine;
			public LineRenderer PlaySpaceLine;
			public LineRenderer InvalidLine;
		}
	}
}
