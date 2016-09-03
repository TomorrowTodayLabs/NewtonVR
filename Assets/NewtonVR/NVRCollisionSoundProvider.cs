using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public abstract class NVRCollisionSoundProvider : MonoBehaviour
    {
        public abstract void Awake();
        public abstract void Play(NVRCollisionSoundMaterials material, Vector3 position, float impactVolume);
    }
}