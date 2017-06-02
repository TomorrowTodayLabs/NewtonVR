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
using System.Collections;

/// <summary>
/// Simple helper script that conditionally enables rendering of a controller if it is connected.
/// </summary>
public class OVRGearVrController : MonoBehaviour
{
	/// <summary>
	/// The root GameObject that should be conditionally enabled depending on controller connection status.
	/// </summary>
    public GameObject m_model;

	/// <summary>
	/// The controller that determines whether or not to enable rendering of the controller model.
	/// </summary>
    public OVRInput.Controller m_controller;

    private bool m_prevControllerConnected = false;
    private bool m_prevControllerConnectedCached = false;

    void Update()
    {
        bool controllerConnected = OVRInput.IsControllerConnected(m_controller);

        if ((controllerConnected != m_prevControllerConnected) || !m_prevControllerConnectedCached)
        {
            m_model.SetActive(controllerConnected);
            m_prevControllerConnected = controllerConnected;
            m_prevControllerConnectedCached = true;
        }

        if (!controllerConnected)
        {
            return;
        }
    }
}
