using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class PCUDPListener : MonoBehaviour
{
    [Header("UDP Settings")]
    public int listenPort = 51003;  // Port to listen on

    private UdpClient udpClient;
    private Thread listenThread;
    private bool listening = false;

    void Start()
    {
        // Open UDP socket
        udpClient = new UdpClient(listenPort);
        listening = true;
        listenThread = new Thread(ListenLoop) { IsBackground = true };
        listenThread.Start();
        Debug.Log($"[PC Listener] Listening on port {listenPort}");
    }

    void ListenLoop()
    {
        var remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (listening)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);
                Debug.Log($"[PC Listener] Received from {remoteEP.Address}: {msg}");
            }
            catch (SocketException)
            {
                // ignore timeouts or closure
            }
        }
    }

    void OnApplicationQuit()
    {
        listening = false;
        udpClient.Close();
    }
}