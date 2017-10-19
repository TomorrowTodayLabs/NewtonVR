using System.Collections.Generic;
using UnityEngine;

namespace NewtonVR {

	public class NVRSnappable : MonoBehaviour {

		/// <summary>
		/// Shared list of add snappable objects
		/// </summary>
		public static List<NVRSnappable> snappables;

		public Collider[] colliders;
		public bool snapping = false;

		/// <summary>
		/// Clears snappables
		/// </summary>
		public static void ClearSnappables () {
			snappables.Clear ();
		}

		public void StartSnapping () {
			snapping = true;
		}

		public void StopSnapping () {
			snapping = false;
		}

		public NVRAlignment SnapToNearest (Vector3 position, float maxSnapDistance) {
			NVRAlignment startingAlignment = new NVRAlignment (position, transform.rotation, transform.localScale);
			NVRAlignment newAlignment = startingAlignment;

			float bestAlignmentDistance = float.MaxValue;

			Bounds ourBounds = GetBounds ();

			foreach (NVRSnappable snappable in snappables) {
				if (snappable != this) {
					Bounds snapToBounds = snappable.GetBounds ();
					NVRAlignment alignment = SnapToBounds (position, snapToBounds);

					Vector3 bottomOfSnappable = new Vector3 (position.x, ourBounds.min.y, position.z);

					float alignmentDistance = Mathf.Max (Vector3.Distance (bottomOfSnappable, snapToBounds.ClosestPoint (bottomOfSnappable)), startingAlignment.Distance (alignment));

					if (alignmentDistance < bestAlignmentDistance && alignmentDistance <= maxSnapDistance) {
						newAlignment = alignment;
						bestAlignmentDistance = alignmentDistance;
					}
				}
			}

			return newAlignment;
		}

		public NVRAlignment SnapToBounds (Vector3 position, Bounds snappingBounds) {
			NVRAlignment alignment = new NVRAlignment (position, transform.rotation, transform.localScale);
			Bounds mybounds = GetBounds ();

			Vector3 boundsOffset = transform.position - mybounds.center;
			alignment.position.y = snappingBounds.center.y + snappingBounds.extents.y + mybounds.extents.y + boundsOffset.y;
			return alignment;
		}

		public Bounds GetBounds () { //this could be cached for objects that aren't moving
			Bounds bounds = colliders[0].bounds;
			for (int i = 1; i < colliders.Length; i++) {
				bounds.Encapsulate (colliders[1].bounds);
			}
			return bounds;
		}

		private void Start () {
			colliders = GetComponentsInChildren<Collider> ();
		}

		private void OnEnable () {
			TrackSelf ();
		}

		private void OnDestroy () {
			ClearSelf ();
		}

		private void OnDisable () {
			ClearSelf ();
		}

		private void TrackSelf () {
			if (snappables == null) {
				snappables = new List<NVRSnappable> ();
			}

			snappables.Add (this);
		}

		private void ClearSelf () {
			snappables.Remove (this);
			StopSnapping ();
		}
	}
}