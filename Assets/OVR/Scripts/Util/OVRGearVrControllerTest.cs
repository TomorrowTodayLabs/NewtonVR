/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.3 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculus.com/licenses/LICENSE-3.3

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class OVRGearVrControllerTest : MonoBehaviour
{
	public Text uiText;

	public class BoolMonitor
	{
		public delegate bool BoolGenerator();

		private string m_name = "";
		private BoolGenerator m_generator;
		private bool m_currentValue = false;
		private float m_displayTimeout = 0.0f;
		private float m_displayTimer = 0.0f;

		public BoolMonitor(string name, BoolGenerator generator, float displayTimeout = 0.5f)
		{
			m_name = name;
			m_generator = generator;
			m_displayTimeout = displayTimeout;
		}

		public void Update()
		{
			m_currentValue = m_generator();

			if (m_currentValue)
				m_displayTimer = m_displayTimeout;

			m_displayTimer -= Time.deltaTime;

			if (m_displayTimer < 0.0f)
				m_displayTimer = 0.0f;
		}

		public override string ToString()
		{
			return "<b>" + m_name + ": </b>"
				+ ((m_displayTimer > 0.0f) ? "<color=red>" : "<color=black>")
				+ m_currentValue + "</color>";
		}
	}

	private List<BoolMonitor> monitors;

	void Start()
	{
		if (uiText != null)
		{
			uiText.supportRichText = true;
		}

		monitors = new List<BoolMonitor>()
		{
			// virtual
			new BoolMonitor("One",                      () => OVRInput.Get(OVRInput.Button.One)),
			new BoolMonitor("OneDown",                  () => OVRInput.GetDown(OVRInput.Button.One)),
			new BoolMonitor("OneUp",                    () => OVRInput.GetUp(OVRInput.Button.One)),
			new BoolMonitor("Two",                      () => OVRInput.Get(OVRInput.Button.Two)),
			new BoolMonitor("TwoDown",                  () => OVRInput.GetDown(OVRInput.Button.Two)),
			new BoolMonitor("TwoUp",                    () => OVRInput.GetUp(OVRInput.Button.Two)),
			new BoolMonitor("PrimaryIndexTrigger",      () => OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)),
			new BoolMonitor("PrimaryIndexTriggerDown",  () => OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)),
			new BoolMonitor("PrimaryIndexTriggerUp",    () => OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)),
			new BoolMonitor("Up",                       () => OVRInput.Get(OVRInput.Button.Up)),
			new BoolMonitor("Down",                     () => OVRInput.Get(OVRInput.Button.Down)),
			new BoolMonitor("Left",                     () => OVRInput.Get(OVRInput.Button.Left)),
			new BoolMonitor("Right",                    () => OVRInput.Get(OVRInput.Button.Right)),
			new BoolMonitor("Touchpad (Touch)",         () => OVRInput.Get(OVRInput.Touch.PrimaryTouchpad)),
			new BoolMonitor("TouchpadDown (Touch)",     () => OVRInput.GetDown(OVRInput.Touch.PrimaryTouchpad)),
			new BoolMonitor("TouchpadUp (Touch)",       () => OVRInput.GetUp(OVRInput.Touch.PrimaryTouchpad)),
			new BoolMonitor("Touchpad (Click)",         () => OVRInput.Get(OVRInput.Button.PrimaryTouchpad)),
			new BoolMonitor("TouchpadDown (Click)",     () => OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad)),
			new BoolMonitor("TouchpadUp (Click)",       () => OVRInput.GetUp(OVRInput.Button.PrimaryTouchpad)),
			// raw
			new BoolMonitor("Start",                    () => OVRInput.Get(OVRInput.RawButton.Start)),
			new BoolMonitor("StartDown",                () => OVRInput.GetDown(OVRInput.RawButton.Start)),
			new BoolMonitor("StartUp",                  () => OVRInput.GetUp(OVRInput.RawButton.Start)),
			new BoolMonitor("Back",                     () => OVRInput.Get(OVRInput.RawButton.Back)),
			new BoolMonitor("BackDown",                 () => OVRInput.GetDown(OVRInput.RawButton.Back)),
			new BoolMonitor("BackUp",                   () => OVRInput.GetUp(OVRInput.RawButton.Back)),
			new BoolMonitor("A",                        () => OVRInput.Get(OVRInput.RawButton.A)),
			new BoolMonitor("ADown",                    () => OVRInput.GetDown(OVRInput.RawButton.A)),
			new BoolMonitor("AUp",                      () => OVRInput.GetUp(OVRInput.RawButton.A)),
		};
	}
	
	void Update()
	{
        string status = ""
            + "<b>Active: </b>" + OVRInput.GetActiveController() + "\n"
            + "<b>Connected: </b>" + OVRInput.GetConnectedControllers() + "\n";

		status += "Orientation: " + OVRInput.GetLocalControllerRotation(OVRInput.GetActiveController()) + "\n";
		status += "AngVel: " + OVRInput.GetLocalControllerAngularVelocity(OVRInput.GetActiveController()) + "\n";
		status += "AngAcc: " + OVRInput.GetLocalControllerAngularAcceleration(OVRInput.GetActiveController()) + "\n";
		status += "Position: " + OVRInput.GetLocalControllerPosition(OVRInput.GetActiveController()) + "\n";
		status += "Vel: " + OVRInput.GetLocalControllerVelocity(OVRInput.GetActiveController()) + "\n";
		status += "Acc: " + OVRInput.GetLocalControllerAcceleration(OVRInput.GetActiveController()) + "\n";
		status += "PrimaryTouchPad: " + OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad) + "\n";
		status += "SecondaryTouchPad: " + OVRInput.Get(OVRInput.Axis2D.SecondaryTouchpad) + "\n";

		for (int i = 0; i < monitors.Count; i++)
		{
			monitors[i].Update();
			status += monitors[i].ToString() + "\n";
		}

		if (uiText != null)
		{
			uiText.text = status;
		}
	}
}
