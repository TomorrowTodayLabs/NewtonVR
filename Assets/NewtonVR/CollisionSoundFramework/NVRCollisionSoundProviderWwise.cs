using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if NVR_WWISE

namespace NewtonVR
{
    //
    //
    // The Wwise provider for the Collidion Sound framework.
    //
    // The Wwise events for the collision sounds are Play_sfx_collision_ with the Collision Sound material added
    // to the end, for example Play_sfx_collision_wood and Play_sfx_collision_metal.
    // The RTPC impactVolume (0 to 100) is used to convey the impact volume to the sound engine.
    //
    // The WwiseCollisionSoundPrefab prefab should be on a layer that will only collider with
    // any Wwise environments set up.
    //
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
            uint res = AkSoundEngine.PostEvent(event_name, AudioPool[CurrentPoolIndex].gameObject,
                (uint)AkCallbackType.AK_EndOfEvent, DisableGameObjectCallback, game_obj);

            if (res == AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                //
                // Failed to play the sound
                //
                DisableGameObject(game_obj);
            }
        }

        void DisableGameObjectCallback(object in_cookie, AkCallbackType in_type, object in_info)
        {
            //Debug.Log("Hello from the callback");
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                //Debug.Log("Event has ended");
                AkGameObj game_obj = in_cookie as AkGameObj;
                DisableGameObject(game_obj);
            }
        }

        void DisableGameObject(AkGameObj game_obj)
        {
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