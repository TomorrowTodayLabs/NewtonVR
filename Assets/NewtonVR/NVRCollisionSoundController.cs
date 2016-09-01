using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRCollisionSoundController : MonoBehaviour
    {
        public static NVRCollisionSoundController Instance;

        [Tooltip("The max number of sounds that can possibly be playing at once.")]
        public int SoundPoolSize = 100;

        [Tooltip("Turns on or off randomizing the pitch of the collision sounds")]
        public bool PitchModulationEnabled = true;

        [Range(0f, 3f)]
        public float PitchModulationRange = 0.5f;

        [Tooltip("Don't play collision sounds that will produce an impact with a volume lower than this number")]
        public float MinCollisionVolume = 0.1f;
        public float MaxCollisionVelocity = 5;

        private static NVRCollisionSoundProvider Provider;

        private void Awake()
        {
            Instance = this;

            #if NVR_FMOD
            //todo: implement fmod provider
            #else
            Provider = this.gameObject.AddComponent<NVRCollisionSoundProviderUnity>();
            #endif
        }

        public static void Play(NVRCollisionSoundMaterials material, Vector3 position, float impactVolume)
        {
            if (Provider != null)
                Provider.Play(material, position, impactVolume);
        }
    }
}