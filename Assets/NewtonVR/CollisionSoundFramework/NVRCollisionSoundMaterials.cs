using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    //NOTE: These must be lowercase
    public enum NVRCollisionSoundMaterials
    {
        none,
        _default,
        carpet,
        wood,
        metal,
        glass,
        plastic,
        cardboard,
        EndNewtonVRMaterials = 50, //your custom collision sound materials go below here. That way if NewtonVR adds more we don't overwrite yours.


    }

    public class NVRCollisionSoundMaterialsList
    {
        public static System.Type typeCache;
        public static System.Type TypeCache
        {
            get
            {
                if (typeCache == null)
                {
                    typeCache = typeof(NVRCollisionSoundMaterials);
                }
                return typeCache;
            }
        }

        private static NVRCollisionSoundMaterials[] list;
        public static NVRCollisionSoundMaterials[] List
        {
            get
            {
                if (list == null)
                {
                    List<NVRCollisionSoundMaterials> temp = new List<NVRCollisionSoundMaterials>();
                    foreach (NVRCollisionSoundMaterials mat in System.Enum.GetValues(typeof(NVRCollisionSoundMaterials)))
                        temp.Add(mat);
                    list = temp.ToArray();
                }
                return list;
            }
        }

        public static NVRCollisionSoundMaterials? Parse(string materialString)
        {
            materialString = materialString.ToLower();
            bool defined = System.Enum.IsDefined(TypeCache, materialString);

            if (defined == true)
                return (NVRCollisionSoundMaterials)System.Enum.Parse(TypeCache, materialString);
            else
                return null;
        }
    }
}