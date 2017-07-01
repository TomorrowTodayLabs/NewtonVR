using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkPlayer : NVRPlayer, NVRNetworkObject
    {
        public const string renderModelCacheFolder = "NewtonCache";
        public const string renderModelExtension = "nwt";
        public abstract bool isMine();

        protected static bool cacheHashesLoaded = false;
        protected static Dictionary<string, string> cacheHashes = new Dictionary<string, string>();
        protected static Dictionary<string, byte[]> cacheData = new Dictionary<string, byte[]>();

        protected override void Awake()
        {
            if (cacheHashesLoaded == false)
            {
                LoadCacheHashes();
            }

            if (isMine())
            {
                base.Awake();
            }
        }

        protected override void Update()
        {
            if (isMine())
            {
                base.Update();
            }
        }


        protected void LoadCacheHashes()
        {
            string path = Path.Combine(Application.streamingAssetsPath, renderModelCacheFolder);
            string[] files = Directory.GetFiles(path, string.Format(".{0}", NVRNetworkPlayer.renderModelExtension));

            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                string hash = Path.GetFileNameWithoutExtension(files[fileIndex]);

                cacheHashes.Add(hash, files[fileIndex]);
            }
        }

        public void WriteCache(string hash, byte[] data)
        {
            string path = Path.Combine(Application.streamingAssetsPath, renderModelCacheFolder);
            string filename = string.Format("{0}.{1}", hash, renderModelExtension);
            string filepath = Path.Combine(path, filename);

            if (cacheHashes.ContainsKey(hash) == false)
            {
                cacheHashes.Add(hash, filepath);
            }

            if (cacheData.ContainsKey(hash) == false)
            {
                cacheData.Add(hash, data);

                File.WriteAllBytes(filepath, data);
            }
        }

        public byte[] GetCache(string hash)
        {
            if (cacheData.ContainsKey(hash) == false)
            {
                if (cacheHashes.ContainsKey(hash) == false)
                {
                    return null;
                }
                else
                {
                    byte[] bytes = File.ReadAllBytes(cacheHashes[hash]);
                    cacheData.Add(hash, bytes);
                    return bytes;
                }
            }
            else
            {
                return cacheData[hash];
            }
        }
    }
}
