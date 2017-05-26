using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace NewtonVR.NetworkPhoton
{
    public class NVRExamplePhotonLauncher : Photon.PunBehaviour
    {
        public Text StatusText;

        private string GameVersion = "1";
        private AudioListener TemporaryAudioListener;

        private void Awake()
        {
            PhotonNetwork.autoJoinLobby = false;
            PhotonNetwork.automaticallySyncScene = true;

            TemporaryAudioListener = this.gameObject.AddComponent<AudioListener>(); //todo: this seems kind hacky
        }

        private void Start()
        {
            Connect();
        }

        private void Connect()
        {
            if (PhotonNetwork.connected == true)
            {
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                PhotonNetwork.ConnectUsingSettings(GameVersion);
            }
        }

        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();

            SetStatus("Connected to master.");

            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnDisconnectedFromPhoton()
        {
            base.OnDisconnectedFromPhoton();

            SetStatus("Disconnected");
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            SetStatus("Joined room: " + PhotonNetwork.room.Name);

            string username = GetUsername();
            PhotonNetwork.playerName = username;

            SetStatus("Set player name to: " + username);

            Destroy(TemporaryAudioListener);
            PhotonNetwork.Instantiate("NVRPhotonPlayer", Vector3.zero, Quaternion.identity, 0);
        }

        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
        {
            base.OnPhotonRandomJoinFailed(codeAndMsg);

            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = 0 }, null);

            SetStatus("Creating room...");
        }

        private void SetStatus(string text)
        {
            Debug.Log(text);
            StatusText.text = string.Format("{0}{1}\n", StatusText.text, text);
        }

        private string GetUsername()
        {
            return System.Environment.UserName;
        }
    }
}