using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace ZG
{
public class LobbyPanel : MonoBehaviour
{
    private readonly string connectionStatusInfo = "Connection Status: ";

    public Text connectionStatusText;

    public void Update()
    {
        connectionStatusText.text = connectionStatusInfo + PhotonNetwork.NetworkClientState;
    }

}
}
