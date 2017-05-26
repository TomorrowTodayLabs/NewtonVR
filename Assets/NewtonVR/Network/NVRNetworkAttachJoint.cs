using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkAttachJoint : NVRAttachJoint, NVRNetworkObject
    {
        public abstract bool IsMine();

        protected override void OnTriggerStay(Collider col)
        {
            if (IsAttached == false)
            {
                NVRAttachPoint point = AttachPointMapper.GetAttachPoint(col);
                if (point != null && point.IsAttached == false)
                {
                    bool isItemMine = NVRNetworkOwnership.Instance.IsMine(point.Item);

                    if (isItemMine)
                    {
                        float distance = Vector3.Distance(point.transform.position, this.transform.position);

                        if (distance < AttachRange)
                        {
                            Attach(point);
                        }
                        else
                        {
                            point.PullTowards(this);
                        }
                    }
                }
            }
        }

        protected override void FixedUpdate()
        {
            if (IsAttached == true)
            {
                bool isItemMine = NVRNetworkOwnership.Instance.IsMine(AttachedPoint.Item);

                if (isItemMine)
                {
                    FixedUpdateAttached();
                }
            }
        }
    }
}