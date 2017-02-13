using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if NVR_WWISE

namespace NewtonVR
{
    public class NVRCollisionSoundProviderWwise : NVRCollisionSoundProvider
    {
        private const string kSoundEventPrefix = "Play_sfx_collision_";
        private const string kImpactVolumeControlName = "impactVolume";
        private static string AudioSourcePrefabPath = "WwiseCollisionSoundPrefab";
        private GameObject AudioSourcePrefab;

        private AkGameObj[] AudioPool;
        private int CurrentPoolIndex;
        private uint mImpactVolumeControlId;

        public bool showCollisions;

        private bool mWwiseAvailable;

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

                        string event_name = string.Format(kSoundEventPrefix + "{0}", mat.ToString());
                        eventStrings.Add(mat, event_name);
                        //Debug.Log("Generated event name for \"" + mat.ToString() + "\": \"" + event_name + "\"");
                    }
                }
                return eventStrings;
            }
        }

#if false
        private static Dictionary<NVRCollisionSoundMaterials, uint> eventIds;
        public static Dictionary<NVRCollisionSoundMaterials, uint> EventIds
        {
            get
            {
                if (eventIds == null)
                {
                    eventIds = new Dictionary<NVRCollisionSoundMaterials, uint>(new EnumEqualityComparer<NVRCollisionSoundMaterials>());

                    foreach (var mat in EventStrings)
                    {
                        if (mat.Key == NVRCollisionSoundMaterials.EndNewtonVRMaterials)
                        {
                            continue;
                        }

                        Debug.Log("Would be adding event id for \""+ mat.Key + "\"");
                        uint event_id = AkSoundEngine.GetIDFromString(mat.Value);
                        // FIXME: everything is using Play_sfx_brick_scrape for testing.
                        //uint event_id = AkSoundEngine.GetIDFromString("Play_sfx_brick_scrape");
                        eventIds.Add(mat.Key, event_id);

                        Debug.Log("Would be adding event id for \"" + mat.Value + "\" " + event_id);
                    }
                }
                return eventIds;
            }
        }
#endif

        public override void Awake()
        {
            if (!AkSoundEngine.IsInitialized())
            {
                //
                // Check to see if the AkInitializer is in the scene
                //
                AkInitializer initializer = GameObject.FindObjectOfType<AkInitializer>();

                if (initializer == null)
                {
                    mWwiseAvailable = false;
                    Debug.LogError("Trying to use Wwise NewtonVR collision sounds but no AkInitializer in scene");
                    return;
                }

                Debug.LogWarning("Wwise NewtonVR collision sounds framework awake but Wwise not yet initialised");
            }
            mWwiseAvailable = true;
            AudioPool = new AkGameObj[NVRCollisionSoundController.Instance.SoundPoolSize];

            AudioSourcePrefab = Resources.Load<GameObject>(AudioSourcePrefabPath);

            for (int index = 0; index < AudioPool.Length; index++)
            {
                AudioPool[index] = GameObject.Instantiate<GameObject>(AudioSourcePrefab).GetComponent<AkGameObj>();
                AudioPool[index].transform.parent = this.transform;

                //
                // Disable the AkGameObjs until they are actually in use
                //
                AudioPool[index].gameObject.SetActive(false);
            }

            mImpactVolumeControlId = AkSoundEngine.GetIDFromString(kImpactVolumeControlName);
        }

        public override void Play(NVRCollisionSoundMaterials material, Vector3 position, float impactVolume)
        {
            if (!mWwiseAvailable)
            {
                return;
            }
            if (material == NVRCollisionSoundMaterials.none)
                return;

#if true
            string event_name = EventStrings[material];

            AkGameObj game_obj = AudioPool[CurrentPoolIndex];
            CurrentPoolIndex++;
            if (CurrentPoolIndex >= AudioPool.Length)
            {
                CurrentPoolIndex = 0;
            }

            game_obj.gameObject.SetActive(true);
            Collider collider = game_obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }

            if (showCollisions)
            {
                MeshRenderer renderer = game_obj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            //
            // Position the object and post the event.
            //
            game_obj.transform.position = position;

            //
            // use the impactVolume to control the sound
            //
            float impact_value = impactVolume * 100.0f;
            //Debug.Log("impactVolume = " + impactVolume);
            AkSoundEngine.SetRTPCValue(mImpactVolumeControlId, impact_value, AudioPool[CurrentPoolIndex].gameObject);

            //Debug.Log("Position in Unity: " + AudioPool[CurrentPoolIndex].transform.position +
            //    " position in Wise: " + AudioPool[CurrentPoolIndex].GetPosition());
            AkSoundEngine.PostEvent(event_name, AudioPool[CurrentPoolIndex].gameObject,
                (uint)AkCallbackType.AK_EndOfEvent, DisableGameObjectCallback, game_obj);

#else
            uint event_id = EventIds[material];
            if (event_id != AkSoundEngine.AK_INVALID_UNIQUE_ID)
            {
                //
                // Position the object and post the event.
                //
                AudioPool[CurrentPoolIndex].transform.position = position;

                //
                // FIXME: need some sort of control for the impactVolume
                //
                //Debug.Log("Position in Unity: " + AudioPool[CurrentPoolIndex].transform.position +
                //    " position in Wise: " + AudioPool[CurrentPoolIndex].GetPosition());
                AkSoundEngine.PostEvent(event_id, AudioPool[CurrentPoolIndex].gameObject);

                CurrentPoolIndex++;

                if (CurrentPoolIndex >= AudioPool.Length)
                {
                    CurrentPoolIndex = 0;
                }
            }
#endif
        }

        void DisableGameObjectCallback(object in_cookie, AkCallbackType in_type, object in_info)
        {
            //Debug.Log("Hello from the callback");
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                //Debug.Log("Event has ended");
                AkGameObj game_obj = in_cookie as AkGameObj;
                if (game_obj != null)
                {
                    //
                    // Disable the collider and GameObject to save processing the object
                    // whilst it is not doing anything.
                    //
                    Collider collider = game_obj.GetComponent<Collider>();
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }

                    MeshRenderer renderer = game_obj.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                    game_obj.gameObject.SetActive(false);
                }
            }
        }
    }
}
#else

    namespace NewtonVR
{
    public class NVRCollisionSoundProviderWwise : NVRCollisionSoundProvider
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