using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using System.Net;
using System.IO;
using System.Threading;

namespace NewtonVR
{
    public class NVRPlayerEditorDownloader
    {
        
        public static Thread DownloadInstance = null;

        static string DownloadFrom;
        static string DownloadPath;

        public static void DownloadSteamVR(string downloadFrom, string downloadPath, Action<int> ProgressDelegate, Action DoneDelegate)
        {
            if (DownloadInstance != null)
            {
                DownloadInstance.Abort();
                DownloadInstance = null;
            }

            DownloadFrom = downloadFrom;
            DownloadPath = downloadPath;

            //Download SDKDownload = new Download(downloadFrom, downloadPath);

            //SDKDownload.Progress = ProgressDelegate;
            //SDKDownload.Finished = DoneDelegate;

            //ThreadStart DownloadDelegate = new ThreadStart(SDKDownload.DoDownload);
            ThreadStart DownloadDelegate = new ThreadStart(DoDownload);
            Thread DownloadThread = new Thread(DownloadDelegate);
            DownloadInstance = DownloadThread;
            DownloadThread.Start();

        }

        public static void DownloadOculusSDK(string downloadFrom, string downloadPath)
        {

        }

        private static void DoDownload()
        {
            
            
            using (WebClient wc = new WebClient())
            {
                //wc.DownloadProgressChanged += DownloadProgressChanged; //todo: these events don't fire :(
                //wc.DownloadDataCompleted += DownloadDataCompleted; //todo: these events don't fire :(
                //wc.DownloadFileAsync(new System.Uri(DownloadFrom), DownloadPath);
                wc.DownloadFile(new System.Uri(DownloadFrom), DownloadPath);
            }
            
        }

    }
    

    class Download
    {
        string DownloadFrom;
        string DownloadPath;
        
        public Delegate Progress = null;
        public Delegate Finished = null;

        /*
        public Download(string DownloadFromAddress, string DownloadLocalPath)
        {
            DownloadFrom = DownloadFromAddress;
            DownloadPath = DownloadLocalPath;
        }
        */

        /*
        public static void DoDownload()
        {

            WebClient wc = new WebClient();
            wc.DownloadFile(new System.Uri(DownloadFrom), DownloadPath);
            
            using (WebClient wc = new WebClient())
            {
                //wc.DownloadProgressChanged += DownloadProgressChanged; //todo: these events don't fire :(
                //wc.DownloadDataCompleted += DownloadDataCompleted; //todo: these events don't fire :(
                //wc.DownloadFileAsync(new System.Uri(DownloadFrom), DownloadPath);
                wc.DownloadFile(new System.Uri(DownloadFrom), DownloadPath);
            }
            
        }
        */

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        { //todo: these events don't fire :(
            //EditorUtility.DisplayProgressBar("NewtonVR", "Downloading steamvr", (e.ProgressPercentage / 100));
            if (Progress != null)
            {
                Progress.DynamicInvoke(e.ProgressPercentage / 100);
            }

        }

        private void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        { //todo: these events don't fire :(
          //Debug.Log("download done");
          //EditorUtility.ClearProgressBar();
            if (Finished != null)
            {
                Finished.DynamicInvoke();
            }
        }
    }
    
}
