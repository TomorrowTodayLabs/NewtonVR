using UnityEngine;
using System.Collections;
using NewtonVR;

public class NVRHandedInteractableItem : NVRInteractableItem
{
	[SerializeField]
	Transform LeftHandInteractionPoint;

	[SerializeField]
	Transform RightHandInteractionPoint;

	public override void BeginInteraction (NVRHand hand)
	{
		if (hand.IsLeft)
		{
			InteractionPoint = LeftHandInteractionPoint;
		}
		else
		{
			InteractionPoint = RightHandInteractionPoint;
		}

		base.BeginInteraction (hand);
	}
}