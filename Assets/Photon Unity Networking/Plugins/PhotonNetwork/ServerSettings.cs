#pragma warning disable 1587
/// \file
/// <summary>ScriptableObject defining a server setup. An instance is created as <b>PhotonServerSettings</b>.</summary>
#pragma warning restore 1587

using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;


public class Region
{
    public CloudRegionCode Code;
    public string HostAndPort;
    public int Ping;

    public static CloudRegionCode Parse(string codeAsString)
    {
        codeAsString = codeAsString.ToLower();

        CloudRegionCode code = CloudRegionCode.none;
        if (Enum.IsDefined(typeof(CloudRegionCode), codeAsString))
        {
            code = (CloudRegionCode)Enum.Parse(typeof(CloudRegionCode), codeAsString);
        }

        return code;
    }

    internal static CloudRegionFlag ParseFlag(string codeAsString)
    {
        codeAsString = codeAsString.ToLower();

        CloudRegionFlag code = 0;
        if (Enum.IsDefined(typeof(CloudRegionFlag), codeAsString))
        {
            code = (CloudRegionFlag)Enum.Parse(typeof(CloudRegionFlag), codeAsString);
        }

        return code;
    }

    public override string ToString()
    {
        return string.Format("'{0}' \t{1}ms \t{2}", this.Code, this.Ping, this.HostAndPort);
    }
}


/// <summary>
/// Collection of connection-relevant settings, used internally by PhotonNetwork.ConnectUsingSettings.
/// </summary>
[Serializable]
public class ServerSettings : ScriptableObject
{
    public enum HostingOption { NotSet = 0, PhotonCloud = 1, SelfHosted = 2, OfflineMode = 3, BestRegion = 4 }

    public string AppID = "";
    public string VoiceAppID = "";
    public string ChatAppID = "";

    public HostingOption HostType = HostingOption.NotSet;

    public CloudRegionCode PreferredRegion;
    public CloudRegionFlag EnabledRegions = (CloudRegionFlag)(-1);

    public ConnectionProtocol Protocol = ConnectionProtocol.Udp;
    public string ServerAddress = "";
    public int ServerPort = 5055;
    public int VoiceServerPort = 5055;  // Voice only uses UDP


    public bool JoinLobby;
    public bool EnableLobbyStatistics;
    public PhotonLogLevel PunLogging = PhotonLogLevel.ErrorsOnly;
    public DebugLevel NetworkLogging = DebugLevel.ERROR;

	public bool RunInBackground = true;

    public List<string> RpcList = new List<string>();   // set by scripts and or via Inspector

    [HideInInspector]
    public bool DisableAutoOpenWizard;



    public void UseCloudBestRegion(string cloudAppid)
    {
        this.HostType = HostingOption.BestRegion;
        this.AppID = cloudAppid;
    }

    public void UseCloud(string cloudAppid)
    {
        this.HostType = HostingOption.PhotonCloud;
        this.AppID = cloudAppid;
    }

    public void UseCloud(string cloudAppid, CloudRegionCode code)
    {
        this.HostType = HostingOption.PhotonCloud;
        this.AppID = cloudAppid;
        this.PreferredRegion = code;
    }

    public void UseMyServer(string serverAddress, int serverPort, string application)
    {
        this.HostType = HostingOption.SelfHosted;
        this.AppID = (application != null) ? application : "master";

        this.ServerAddress = serverAddress;
        this.ServerPort = serverPort;
    }

    /// <summary>Checks if a string is a Guid by attempting to create one.</summary>
    /// <param name="val">The potential guid to check.</param>
    /// <returns>True if new Guid(val) did not fail.</returns>
    public static bool IsAppId(string val)
    {
        try
        {

            new Guid(val);
        }
        catch
        {
            return false;
        }
        return true;
    }

	/// <summary>
	/// Gets the best region code in preferences.
	/// This composes the PhotonHandler, since its Internal and can not be accessed by the custom inspector
	/// </summary>
	/// <value>The best region code in preferences.</value>
	public static CloudRegionCode BestRegionCodeInPreferences{
		get{
			return PhotonHandler.BestRegionCodeInPreferences;
		}
	}

	/// <summary>
	/// Gets the best region code currently.
	/// This composes the PhotonHandler, since its Internal and can not be accessed by the custom inspector
	/// </summary>
	/// <value>The best region code currently.</value>
	public static CloudRegionCode BestRegionCodeCurrently{
		get{
			return PhotonHandler.BestRegionCodeCurrently;
		}
	}

	public static void ResetBestRegionCodeInPreferences()
	{
		PhotonHandler.BestRegionCodeInPreferences = CloudRegionCode.none;
	}

    public override string ToString()
    {
        return "ServerSettings: " + HostType + " " + ServerAddress;
    }
}
