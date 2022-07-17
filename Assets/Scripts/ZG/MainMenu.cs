using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace ZG
{
public class MainMenu : MonoBehaviourPunCallbacks
{
    public string levelToLoad = "Game";
    public GameObject loginPanel;
    public InputField playerNameInput;
    public GameObject selectionPanel;
    public GameObject createRoomPanel;
    public InputField roomNameInputField;
    public InputField maxPlayersInputField;
    public GameObject joinRandomRoomPanel;
    public GameObject roomListPanel;
    public GameObject roomListContent;
    public GameObject roomListEntryPrefab;
    public GameObject insideRoomPanel;
    public Button startGameButton;
    public GameObject playerListEntryPrefab;
    
    private Dictionary<string, RoomInfo> _cachedRoomList;
    private Dictionary<string, GameObject> _roomListEntries;
    private Dictionary<int, GameObject> _playerListEntries;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        _cachedRoomList = new Dictionary<string, RoomInfo>();
        _roomListEntries = new Dictionary<string, GameObject>();

        playerNameInput.text = "Player " + Random.Range(1000, 10000);
    }

    public override void OnConnectedToMaster()
    {
        SetActivePanel(selectionPanel.name);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
    }

    public override void OnJoinedLobby()
    {
        _cachedRoomList.Clear(); // clear rooms list when joining a new lobby
        ClearRoomListView();
    }

    // Note: when a client joins / creates a room, OnLeftLobby does not get called, even if the client was in a lobby before
    public override void OnLeftLobby()
    {
        _cachedRoomList.Clear();
        ClearRoomListView();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetActivePanel(selectionPanel.name);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetActivePanel(selectionPanel.name);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string roomName = "Room " + Random.Range(1000, 10000);

        RoomOptions options = new RoomOptions {MaxPlayers = 8};

        PhotonNetwork.CreateRoom(roomName, options, null);
    }

    public override void OnJoinedRoom()
    {
        // joining (or entering) a room invalidates any cached lobby room list (even if LeaveLobby was not called due to just joining a room)
        _cachedRoomList.Clear();

        SetActivePanel(insideRoomPanel.name);

        if (_playerListEntries == null) { _playerListEntries = new Dictionary<int, GameObject>(); }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject entry = Instantiate(playerListEntryPrefab);
            entry.transform.SetParent(insideRoomPanel.transform);
            entry.transform.localScale = Vector3.one;
            entry.GetComponent<PlayerListEntry>().Initialize(p.ActorNumber, p.NickName);

            object isPlayerReady;

            if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
            {
                entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool) isPlayerReady);
            }

            _playerListEntries.Add(p.ActorNumber, entry);
        }

        startGameButton.gameObject.SetActive(CheckPlayersReady());
        //
        // Hashtable props = new Hashtable
        // {
        //     {AsteroidsGame.PLAYER_LOADED_LEVEL, false}
        // };
        // PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnLeftRoom()
    {
        SetActivePanel(selectionPanel.name);

        foreach (GameObject entry in _playerListEntries.Values) { Destroy(entry.gameObject); }

        _playerListEntries.Clear();
        _playerListEntries = null;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        GameObject entry = Instantiate(playerListEntryPrefab);
        entry.transform.SetParent(insideRoomPanel.transform);
        entry.transform.localScale = Vector3.one;
        entry.GetComponent<PlayerListEntry>().Initialize(newPlayer.ActorNumber, newPlayer.NickName);

        _playerListEntries.Add(newPlayer.ActorNumber, entry);

        startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Destroy(_playerListEntries[otherPlayer.ActorNumber].gameObject);
        _playerListEntries.Remove(otherPlayer.ActorNumber);

        startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            startGameButton.gameObject.SetActive(CheckPlayersReady());
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (_playerListEntries == null) { _playerListEntries = new Dictionary<int, GameObject>(); }

        GameObject entry;

        if (_playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
        {
            object isPlayerReady;

            if (changedProps.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
            {
                entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool) isPlayerReady);
            }
        }

        startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnBackButtonClicked()
    {
        if (PhotonNetwork.InLobby) { PhotonNetwork.LeaveLobby(); }

        SetActivePanel(selectionPanel.name);
    }

    public void OnCreateRoomButtonClicked()
    {
        string roomName = roomNameInputField.text;
        roomName = (roomName.Equals(string.Empty)) ? "Room " + Random.Range(1000, 10000) : roomName;

        byte maxPlayers;
        byte.TryParse(maxPlayersInputField.text, out maxPlayers);
        maxPlayers = (byte) Mathf.Clamp(maxPlayers, 2, 8);

        RoomOptions options = new RoomOptions {MaxPlayers = maxPlayers, PlayerTtl = 10000};

        PhotonNetwork.CreateRoom(roomName, options, null);
    }

    public void OnJoinRandomRoomButtonClicked()
    {
        SetActivePanel(joinRandomRoomPanel.name);

        PhotonNetwork.JoinRandomRoom();
    }

    public void OnLeaveGameButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnLoginButtonClicked()
    {
        string playerName = playerNameInput.text;

        if (!playerName.Equals(""))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
        }
        else { Debug.LogError("Player Name is invalid."); }
    }

    public void OnRoomListButtonClicked()
    {
        if (!PhotonNetwork.InLobby) { PhotonNetwork.JoinLobby(); }

        SetActivePanel(roomListPanel.name);
    }

    public void OnStartGameButtonClicked()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel(levelToLoad);
    }

    private bool CheckPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) { return false; }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isPlayerReady;

            if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
            {
                if (!(bool) isPlayerReady) { return false; }
            }
            else { return false; }
        }

        return true;
    }

    private void ClearRoomListView()
    {
        foreach (GameObject entry in _roomListEntries.Values) { Destroy(entry.gameObject); }

        _roomListEntries.Clear();
    }

    public void LocalPlayerPropertiesUpdated()
    {
        startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    private void SetActivePanel(string activePanel)
    {
        loginPanel.SetActive(activePanel.Equals(loginPanel.name));
        selectionPanel.SetActive(activePanel.Equals(selectionPanel.name));
        createRoomPanel.SetActive(activePanel.Equals(createRoomPanel.name));
        joinRandomRoomPanel.SetActive(activePanel.Equals(joinRandomRoomPanel.name));
        roomListPanel.SetActive(
                activePanel.Equals(roomListPanel.name)); // UI should call OnRoomListButtonClicked() to activate this
        insideRoomPanel.SetActive(activePanel.Equals(insideRoomPanel.name));
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cached room list if it got closed, became invisible or was marked as removed
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (_cachedRoomList.ContainsKey(info.Name)) { _cachedRoomList.Remove(info.Name); }

                continue;
            }

            // Update cached room info
            if (_cachedRoomList.ContainsKey(info.Name)) { _cachedRoomList[info.Name] = info; }
            // Add new room info to cache
            else { _cachedRoomList.Add(info.Name, info); }
        }
    }

    private void UpdateRoomListView()
    {
        foreach (RoomInfo info in _cachedRoomList.Values)
        {
            GameObject entry = Instantiate(roomListEntryPrefab);
            entry.transform.SetParent(roomListContent.transform);
            entry.transform.localScale = Vector3.one;
            entry.GetComponent<RoomListEntry>().Initialize(info.Name, (byte) info.PlayerCount, info.MaxPlayers);

            _roomListEntries.Add(info.Name, entry);
        }
    }
}
}
