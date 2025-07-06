using TMPro;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class QuestUDPReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    public int listenPort = 51002;

    [Header("Target Transform")]
    [Tooltip("Drag the object to be driven by Vicon here")]
    public Transform targetTransform;

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running;
    private string latestMsg;
    private bool msgReady;
    private object lockObj = new object();

    void Start()
    {
        udpClient = new UdpClient(listenPort);
        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
        Debug.Log($"[Quest Receiver] Listening on port {listenPort}");
    }

    void ReceiveLoop()
    {
        var anyIP = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyIP);
                string msg = Encoding.UTF8.GetString(data);
                lock (lockObj)
                {
                    latestMsg = msg;
                    msgReady = true;
                }
            }
            catch (SocketException)
            {
                // ignore timeouts or shutdown
            }
        }
    }

    void Update()
    {
        if (msgReady)
        {
            string msg;
            lock (lockObj)
            {
                msg = latestMsg;
                msgReady = false;
            }

            var parts = msg.Split(',');
            if (parts.Length >= 3 
                && float.TryParse(parts[0], out float x) 
                && float.TryParse(parts[1], out float y) 
                && float.TryParse(parts[2], out float z))
            {
                Vector3 pos = new Vector3(x, y, z);
                if (targetTransform != null)
                    targetTransform.position = pos;
                else
                    transform.position = pos;
                Debug.Log(pos);
            }
            else
            {
                Debug.LogWarning($"[Quest Receiver] Bad UDP msg: {msg}");
            }
        }
    }

    void OnDisable()
    {
        running = false;
        udpClient.Close();
    }
}