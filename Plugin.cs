using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;

[BepInPlugin("com.imcrab.gtroomnotifier.full", "GT Self Tracker + UI", "2.0.0")]
public class GTRoomNotifierFull : BaseUnityPlugin, IInRoomCallbacks
{
    string webhookUrl = "";
    string roleId = "";

    string webhookFile;
    string roleFile;

    GameObject canvasObj;
    TextMeshProUGUI notifText;

    void Awake()
    {
        webhookFile = Path.Combine(Paths.PluginPath, "webhook.txt");
        roleFile = Path.Combine(Paths.PluginPath, "role.txt");

        if (!File.Exists(webhookFile)) File.WriteAllText(webhookFile, "PUT WEBHOOK HERE");
        if (!File.Exists(roleFile)) File.WriteAllText(roleFile, "PUT ROLE ID HERE");

        try
        {
            webhookUrl = File.ReadAllText(webhookFile).Trim();
            roleId = File.ReadAllText(roleFile).Trim();
        }
        catch { }

        if (string.IsNullOrWhiteSpace(webhookUrl) || !webhookUrl.StartsWith("https://discord.com/api/webhooks/"))
            webhookUrl = "";

        MakeUI();

        PhotonNetwork.AddCallbackTarget(this);
        _ = SendStartupMsg();
    }

    async Task SendStartupMsg()
    {
        await Task.Delay(2000);
        var name = PhotonNetwork.LocalPlayer?.NickName ?? "Unknown";
        var time = DateTime.Now.ToString("hh:mm tt");

        if (webhookUrl != "")
            await SendMsg($"🚀 **{name}** launched the game at **{time}**");
    }

    public void OnJoinedRoom()
    {
        var room = PhotonNetwork.CurrentRoom?.Name ?? "???";
        var name = PhotonNetwork.LocalPlayer?.NickName ?? "Unknown";
        var time = DateTime.Now.ToString("hh:mm tt");

        _ = SendMsg($"📡 **{name}** joined room **{room}** | {time}");
        UpdateUI($"{time} | Lobby: {room} | Player: {name}");
    }

    public void OnLeftRoom()
    {
        UpdateUI("Not in a lobby");
    }

    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed) { }
    public void OnPlayerPropertiesUpdate(Player p, ExitGames.Client.Photon.Hashtable changed) { }
    public void OnMasterClientSwitched(Player newMaster) { }

    async Task SendMsg(string msg)
    {
        try
        {
            using (var client = new HttpClient())
            {
                var ping = string.IsNullOrEmpty(roleId) ? "" : $"<@&{roleId}>\n";
                var json = $"{{\"content\":\"{ping}{msg}\"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            }
        }
        catch { }
    }

    void MakeUI()
    {
        canvasObj = new GameObject("GTNotifierCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        var textObj = new GameObject("GTNotifierText");
        textObj.transform.SetParent(canvasObj.transform);

        notifText = textObj.AddComponent<TextMeshProUGUI>();
        notifText.fontSize = 26;
        notifText.color = Color.cyan;
        notifText.alignment = TextAlignmentOptions.TopLeft;

        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(800, 60);

        UpdateUI("Not in a lobby");
    }

    void UpdateUI(string msg)
    {
        if (notifText != null) notifText.text = msg;
    }

    void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
