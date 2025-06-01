using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;

[BepInPlugin("com.imcrab.gtroomnotifier.full", "GT Self Tracker + UI", "1.7.0")]
public class GTRoomNotifierFull : BaseUnityPlugin
{
    private string webhookUrl = "";
    private string lastRoom = "";
    private string webhookFilePath;

    // UI
    private GameObject notificationGO;
    private Text notificationText;

    private void Start()
    {
        // Set webhook.txt path in plugins folder
        webhookFilePath = Path.Combine(Paths.PluginPath, "webhook.txt");

        try
        {
            if (!File.Exists(webhookFilePath))
            {
                File.WriteAllText(webhookFilePath, "PUT YOUR WEBHOOK HERE");
                Debug.LogWarning($"GTRoomNotifier: Created webhook.txt at {webhookFilePath}. Please edit it with your webhook URL.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("GTRoomNotifier: Error creating webhook.txt - " + ex.Message);
        }

        // Read webhook URL
        try
        {
            webhookUrl = File.ReadAllText(webhookFilePath).Trim();
        }
        catch (Exception ex)
        {
            Debug.LogError("GTRoomNotifier: Error reading webhook.txt - " + ex.Message);
            webhookUrl = "";
        }

        if (string.IsNullOrEmpty(webhookUrl) || !webhookUrl.StartsWith("https://discord.com/api/webhooks/"))
        {
            Debug.LogWarning("GTRoomNotifier: Invalid webhook URL in webhook.txt.");
            webhookUrl = "";
        }

        CreateNotificationUI();
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            string currentRoom = PhotonNetwork.CurrentRoom?.Name;

            if (!string.IsNullOrEmpty(currentRoom) && currentRoom != lastRoom)
            {
                lastRoom = currentRoom;

                string playerName = GetLocalPlayerName();
                string timestamp = DateTime.Now.ToString("hh:mm tt");
                string message = $"📡 {playerName} joined room: {currentRoom} at {timestamp}";

                if (!string.IsNullOrEmpty(webhookUrl))
                {
                    _ = SendDiscordMessage(message);
                }

                UpdateNotification($"{timestamp} | Lobby: {currentRoom} | Player: {playerName}");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(lastRoom))
            {
                lastRoom = "";
                UpdateNotification("Not in a lobby");
            }
        }
    }

    private string GetLocalPlayerName()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal)
                return player.NickName;
        }
        return "UnknownPlayer";
    }

    private async Task SendDiscordMessage(string message)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var json = $"{{\"content\":\"{message}\"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Webhook error: " + ex.Message);
        }
    }

    private void CreateNotificationUI()
    {
        GameObject canvasGO = new GameObject("GTRoomNotifierCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(canvasGO);

        notificationGO = new GameObject("RoomNotificationText");
        notificationGO.transform.SetParent(canvasGO.transform);

        notificationText = notificationGO.AddComponent<Text>();
        notificationText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        notificationText.fontSize = 24;
        notificationText.alignment = TextAnchor.UpperLeft;
        notificationText.color = Color.cyan;
        notificationText.horizontalOverflow = HorizontalWrapMode.Overflow;
        notificationText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = notificationGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(600, 50);

        UpdateNotification("Not in a lobby");
    }

    private void UpdateNotification(string message)
    {
        if (notificationText != null)
            notificationText.text = message;
    }
}
