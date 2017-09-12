using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewtonVR {

	public class NVRSnappingInteractableItem : NVRInteractableItem {
		public float snapToMeters = 0.05f;
		public float snapToDegrees = 45f;

		private bool snap = false;
		private bool set = false;
		private Vector3 lastSnappedPosition;
		private Vector3 lastSnappedRotation;

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
				var weighted = snapToDegrees / 10f;
				rounded.x = (rounded.x >= lastSnappedRotation.x) ? rounded.x -= weighted : rounded.x += weighted;
				rounded.y = (rounded.y >= lastSnappedRotation.y) ? rounded.y -= weighted : rounded.y += weighted;
				rounded.z = (rounded.z >= lastSnappedRotation.z) ? rounded.z -= weighted : rounded.z += weighted;
			}

			rounded.x = Mathf.Round (rounded.x / snapToDegrees) * snapToDegrees;
			rounded.y = Mathf.Round (rounded.y / snapToDegrees) * snapToDegrees;
			rounded.z = Mathf.Round (rounded.z / snapToDegrees) * snapToDegrees;
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

			if (!snap) {
				base.UpdateVelocities ();
				return;
			}

			Vector3 targetItemPosition;
			Quaternion targetItemRotation;

			Vector3 targetHandPosition;
			Quaternion targetHandRotation;

			GetTargetValues (out targetHandPosition, out targetHandRotation, out targetItemPosition, out targetItemRotation);

			float velocityMagic = VelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);
			float angularVelocityMagic = AngularVelocityMagic / (Time.deltaTime / NVRPlayer.NewtonVRExpectedDeltaTime);

			Vector3 positionDelta;
			Quaternion rotationDelta;

			float angle;
			Vector3 axis;

			// Snap to nearest meters
			targetHandPosition = SnapPosition (targetHandPosition);

			positionDelta = (targetHandPosition - targetItemPosition);

			// Snap to nearest degrees
			targetHandRotation = SnapRotation (targetHandRotation);

			rotationDelta = targetHandRotation * Quaternion.Inverse (targetItemRotation);

			Vector3 velocityTarget = (positionDelta * velocityMagic) * Time.deltaTime;
			if (float.IsNaN (velocityTarget.x) == false) {
				this.Rigidbody.velocity = Vector3.MoveTowards (this.Rigidbody.velocity, velocityTarget, MaxVelocityChange);
			}

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