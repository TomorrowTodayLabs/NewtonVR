using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public class NVRTeleporter : MonoBehaviour
    {
        //private NVRHand Hand;

        [HideInInspector]
        public bool IsTeleporting;

        private void Start()
        {
            //Hand = this.GetComponent<NVRHand>();
            IsTeleporting = false;
        }

        private void Update()
        {

        }

        private IEnumerator _Teleport(NVRPlayer player, Vector3 newPositionBase)
        {
            IsTeleporting = true;
            float CurrentHeight;

            while(IsTeleporting)
            {
                yield return null;
                CurrentHeight = player.transform.position.y;
                player.transform.position = (newPositionBase);
                IsTeleporting = false;
            }

        }

        public void Teleport(NVRPlayer player, Vector3 newPositionBase)
        {
            if(IsTeleporting == false)
            {
                StartCoroutine(_Teleport(player, newPositionBase));
            }
            else
            {
                // Already teleporting
            }
        }
    }
}

