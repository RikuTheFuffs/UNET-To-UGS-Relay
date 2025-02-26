using UnityEngine;
using UnityEngine.Networking;

public class SetName : MonoBehaviour
{
    public UnityEngine.UI.Text label;

    public void SetPlayerName()
    {
        var player = ClientScene.localPlayers[0];
        var control = player.gameObject.GetComponent<ShipControl>();
        control.CmdSetName(label.text);
    }

    public static void StaticPlayerName() { }

    public int IntPlayerName()
    {
        return 1;
    }
}
