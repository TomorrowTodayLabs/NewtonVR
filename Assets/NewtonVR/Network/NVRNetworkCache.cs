using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;
using System.Linq;

namespace NewtonVR.Network
{
    public abstract class NVRNetworkCache : MonoBehaviour, NVRNetworkObject
    {
        public static NVRNetworkCache instance;

        public const string renderModelCacheFolder = "NewtonCache";
        public const string renderModelExtension = "nwt";
        public abstract bool isMine();

        protected static bool cacheHashesLoaded = false;
        protected static Dictionary<string, string> cacheHashes = new Dictionary<string, string>();
        protected static Dictionary<string, byte[]> cacheData = new Dictionary<string, byte[]>();
        protected static Dictionary<string, UnityEngine.Object> cacheObjects = new Dictionary<string, UnityEngine.Object>();

        protected static List<NVRCacheJob> cacheAssignmentJobs = new List<NVRCacheJob>();
        protected static List<NVRCacheRequestJob> cacheRequestJobs = new List<NVRCacheRequestJob>();
        protected static bool waitingForAnswer = false;

        protected virtual void Awake()
        {
            if (isMine())
            {
                instance = this;

                if (cacheHashesLoaded == false)
                {
                    LoadCacheHashes();
                }
            }
        }

        protected void Update()
        {
            if (isMine())
            {
                for (int jobIndex = 0; jobIndex < cacheAssignmentJobs.Count; jobIndex++)
                {
                    if (cacheAssignmentJobs[jobIndex] != null)
                    {
                        if (cacheData.ContainsKey(cacheAssignmentJobs[jobIndex].hash))
                        {
                            cacheAssignmentJobs[jobIndex].assignment();
                            cacheAssignmentJobs.RemoveAt(jobIndex);
                            jobIndex--;
                        }
                    }
                }

                if (waitingForAnswer == false)
                {
                    NVRCacheRequestJob nextJob = cacheRequestJobs.FirstOrDefault(job => job.sent == false);
                    if (nextJob != null)
                    {
                        SendRequestJob(nextJob);
                        nextJob.sent = true;
                        waitingForAnswer = true;
                    }
                }
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

        protected abstract void SendRequestJob(NVRCacheRequestJob job);

        protected virtual void RecieveCacheData(string hash, byte[] data)
        {
            waitingForAnswer = false;
            WriteCache(hash, data);
        }

        public void AddCacheJob(NVRCacheJob job, int dataOwner)
        {
            if (cacheObjects.ContainsKey(job.hash))
            {
                job.assignment();
            }
            else
            {
                cacheAssignmentJobs.Add(job);

                if (RequestJobExists(job.hash) == false)
                {
                    AddRequestJob(job.hash, dataOwner);
                }
            }
        }

        protected void AddRequestJob(string hash, int owner)
        {
            cacheRequestJobs.Add(new NVRCacheRequestJob(hash, owner));
        }

        protected bool RequestJobExists(string hash)
        {
            return cacheRequestJobs.Any(job => job.hash == hash);
        }

        public string AddAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return string.Empty;

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            if (asset is Mesh)
            {
                NVRNetworkHelpers.SerializeMesh(writer, (Mesh)asset);
            }
            else if (asset is Collider)
            {
                NVRNetworkHelpers.SerializeCollider(writer, (Collider)asset);
            }
            else if (asset is Material)
            {
                NVRNetworkHelpers.SerializeMaterial(writer, (Material)asset);
            }
            else if (asset is PhysicMaterial)
            {
                NVRNetworkHelpers.SerializePhysicMaterial(writer, (PhysicMaterial)asset);
            }

            byte[] data = stream.ToArray();
            writer.Close();

            string hash = NVRNetworkHelpers.GetHash(data);

            WriteCache(hash, data);

            return hash;
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

        public T GetCache<T>(string hash) where T : UnityEngine.Object
        {
            if (cacheObjects.ContainsKey(hash))
            {
                return (T)cacheObjects[hash];
            }

            T cachedObject = GetObjectFromData<T>(hash);

            return cachedObject;
        }

        public T GetObjectFromData<T>(string hash) where T : UnityEngine.Object
        {
            byte[] data = GetCache(hash);

            if (data != null)
            {
                UnityEngine.Object deserializedObject = null;
                if (typeof(Texture).IsAssignableFrom(typeof(T)))
                {
                    deserializedObject = NVRNetworkHelpers.DeserializeTexture(data);
                }
                cacheObjects.Add(hash, deserializedObject);
            }

            return null;
        }
    }

    public class NVRCacheJob
    {
        public string hash;
        public Action assignment;

        public NVRCacheJob()
        {

        }

        public NVRCacheJob(string jobHash)
        {
            hash = jobHash;
        }
    }

    public class NVRCacheRequestJob
    {
        public string hash;
        public bool sent;
        public int owner;

        public NVRCacheRequestJob(string jobHash, int dataOwner)
        {
            hash = jobHash;
            owner = dataOwner;
        }
    }
}
