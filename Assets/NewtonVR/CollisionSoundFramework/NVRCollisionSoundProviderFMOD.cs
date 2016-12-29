using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if NVR_FMOD
using FMOD.Studio;
using FMODUnity;

namespace NewtonVR
{
    public class NVRCollisionSoundProviderFMOD : NVRCollisionSoundProvider
    {
        private static Dictionary<NVRCollisionSoundMaterials, string> eventStrings;
        public static Dictionary<NVRCollisionSoundMaterials, string> EventStrings
        {
            get
            {
                if (eventStrings == null)
                {
                    eventStrings = new Dictionary<NVRCollisionSoundMaterials, string>(new EnumEqualityComparer<NVRCollisionSoundMaterials>());

                    foreach (NVRCollisionSoundMaterials mat in NVRCollisionSoundMaterialsList.List)
                    {
                        if (mat == NVRCollisionSoundMaterials.EndNewtonVRMaterials)
                        {
                            continue;
                        }

                        eventStrings.Add(mat, string.Format("event:/Collisions/{0}", mat.ToString()));
                    }
                }
                return eventStrings;
            }
        }

        private static Dictionary<NVRCollisionSoundMaterials, System.Guid> eventGuids;
        public static Dictionary<NVRCollisionSoundMaterials, System.Guid> EventGuids
        {
            get
            {
                if (eventGuids == null)
                {
                    eventGuids = new Dictionary<NVRCollisionSoundMaterials, System.Guid>(new EnumEqualityComparer<NVRCollisionSoundMaterials>());

                    foreach (var mat in EventStrings)
                    {
                        if (mat.Key == NVRCollisionSoundMaterials.EndNewtonVRMaterials)
                        {
                            continue;
                        }

                        eventGuids.Add(mat.Key, FMODUnity.RuntimeManager.PathToGUID(mat.Value));
                    }
                }
                return eventGuids;
            }
        }

        public override void Awake()
        {

        }

        public override void Play(NVRCollisionSoundMaterials material, Vector3 position, float impactVolume)
        {
            if (material == NVRCollisionSoundMaterials.none)
                return;

            System.Guid playGuid = EventGuids[material];
            
            EventInstance collidingInstance = RuntimeManager.CreateInstance(playGuid);
            collidingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            collidingInstance.setVolume(impactVolume);
            collidingInstance.start();
            collidingInstance.release();
        }
    }
}
#else

namespace NewtonVR
{
    public class NVRCollisionSoundProviderFMOD : NVRCollisionSoundProvider
    {
        public override void Awake()
        {
        }

        public override void Play(NVRCollisionSoundMaterials material, Vector3 position, float impactVolume)
        {
            return;
        }
    }
}
#endif