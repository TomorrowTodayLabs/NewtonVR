using UnityEngine;

namespace NewtonVR {

	public class NVRInteractableItemSnappable : NVRInteractableItem {
		public float snapToMeters = 0.05f;
		public float snapToAngle = 15f;

		private bool snap = false;
		private bool set = false;
		private Vector3 lastSnappedPosition;
		private Vector3 lastSnappedRotation;

		[SerializeField]
		private positionalSnapping snappingType = positionalSnapping.objects;

		private enum positionalSnapping {
			grid,
			objects
		}

		private NVRSnappable snappable;

		protected override void Start () {
			base.Start ();

			if (!(GetComponent<NVRSnappable> ())) {
				snappable = gameObject.AddComponent<NVRSnappable> ();
				OnBeginInteraction.AddListener (snappable.StartSnapping);
				OnEndInteraction.AddListener (snappable.StopSnapping);
			}
		}

		private Vector3 SnapPosition (Vector3 pos) {
			if (set) {
				var weighted = snapToMeters / 5f;
				pos.x = (pos.x >= lastSnappedPosition.x) ? pos.x -= weighted : pos.x += weighted;
				pos.y = (pos.y >= lastSnappedPosition.y) ? pos.y -= weighted : pos.y += weighted;
				pos.z = (pos.z >= lastSnappedPosition.z) ? pos.z -= weighted : pos.z += weighted;
			}

			pos.x = Mathf.Round (pos.x / snapToMeters) * snapToMeters;
			pos.y = Mathf.Round (pos.y / snapToMeters) * snapToMeters;
			pos.z = Mathf.Round (pos.z / snapToMeters) * snapToMeters;

			lastSnappedPosition = pos;
			set = true;
			return pos;
		}

		private Quaternion SnapRotation (Quaternion rot) {
			var rounded = rot.eulerAngles;

			if (set) {
				var weighted = snapToAngle / 10f;
				rounded.x = (rounded.x >= lastSnappedRotation.x) ? rounded.x -= weighted : rounded.x += weighted;
				rounded.y = (rounded.y >= lastSnappedRotation.y) ? rounded.y -= weighted : rounded.y += weighted;
				rounded.z = (rounded.z >= lastSnappedRotation.z) ? rounded.z -= weighted : rounded.z += weighted;
			}

			rounded.x = Mathf.Round (rounded.x / snapToAngle) * snapToAngle;
			rounded.y = Mathf.Round (rounded.y / snapToAngle) * snapToAngle;
			rounded.z = Mathf.Round (rounded.z / snapToAngle) * snapToAngle;
			rot.eulerAngles = rounded;

			lastSnappedRotation = rounded;
			//set = true;
			return rot;
		}

		protected override void UpdateVelocities () {
			foreach (var hand in AttachedHands) {
				if (hand.UseButtonDown) {
					snap = !snap;
					break;
				}
			}

			Vector3 targetItemPosition;
			Quaternion targetItemRotation;

			Vector3 targetHandPosition;
			Quaternion targetHandRotation;

			GetTargetValues (out targetHandPosition, out targetHandRotation, out targetItemPosition, out targetItemRotation);

			float velocityMagic = VelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);
			float angularVelocityMagic = AngularVelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);

			// Snap position
			if (snappingType == positionalSnapping.objects) {
				NVRAlignment targetAlignment = snappable.SnapToNearest (targetHandPosition, .1f);
				if (targetAlignment == null) {
					return;
				}

				targetHandPosition = targetAlignment.position;

				Vector3 positionDelta = (targetHandPosition - targetItemPosition);
				Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.deltaTime;
				if (float.IsNaN (velocityTarget.x) == false) {
					this.Rigidbody.velocity = Vector3.MoveTowards (this.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
				}
			}

			if (snappingType == positionalSnapping.grid && snap) {
				targetHandPosition = SnapPosition (targetHandPosition);

				Vector3 positionDelta = (targetHandPosition - targetItemPosition);
				Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.deltaTime;
				if (float.IsNaN (velocityTarget.x) == false) {
					this.Rigidbody.velocity = Vector3.MoveTowards (this.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
				}
			}

			// Snap to nearest degrees
			if (snap) {
				targetHandRotation = SnapRotation (targetHandRotation);
			}
			Quaternion rotationDelta = targetHandRotation * Quaternion.Inverse (targetItemRotation);

			float angle;
			Vector3 axis;
			rotationDelta.ToAngleAxis (out angle, out axis);

			if (angle > 180)
				angle -= 360;

			if (angle != 0) {
				Vector3 angularTarget = angle * axis;
				if (float.IsNaN (angularTarget.x) == false) {
					angularTarget = (angularTarget * angularVelocityMagic) * Time.deltaTime;
					this.Rigidbody.angularVelocity = Vector3.MoveTowards (this.Rigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
				}
			}

			if (VelocityHistory != null) {
				CurrentVelocityHistoryStep++;
				if (CurrentVelocityHistoryStep >= VelocityHistory.Length) {
					CurrentVelocityHistoryStep = 0;
				}

				VelocityHistory[CurrentVelocityHistoryStep] = this.Rigidbody.velocity;
				AngularVelocityHistory[CurrentVelocityHistoryStep] = this.Rigidbody.angularVelocity;
			}
		}

		public override void BeginInteraction (NVRHand hand) {
			snap = false;
			base.BeginInteraction (hand);
		}
	}
}