using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class QuestUDPSender : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Set to your PC’s LAN IP")]
    public string pcIPAddress = "192.168.137.1";
    public int pcPort = 51003;

    private UdpClient udpClient;
    private IPEndPoint pcEndPoint;

    void Start()
    {
        udpClient = new UdpClient();
        pcEndPoint = new IPEndPoint(IPAddress.Parse(pcIPAddress), pcPort);
        Debug.Log($"[Quest Sender] Will send to {pcEndPoint}");
    }

    void Update()
    {
        // Example: send once per second
        if (Time.frameCount % 60 == 0)
        {
            SendTestMessage();
        }
    }

    void SendTestMessage()
    {
        string msg = $"Hello from Quest at {Time.time:F2}s";
        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, pcEndPoint);
        Debug.Log($"[Quest Sender] Sent: {msg}");
    }

    void OnDisable()
    {
        udpClient.Close();
    }
}