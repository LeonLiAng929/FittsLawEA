using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class PCUDPSender : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Quest’s IP on your LAN (set this to the Quest IP)")]
    public string remoteIP   = "192.168.137.144";
    [Tooltip("Must match Quest listener’s listenPort")]
    public int    remotePort = 51002;

    private UdpClient udpClient;
    private IPEndPoint remoteEP;

    void Start()
    {
        // Create the UDP client and endpoint
        udpClient  = new UdpClient();
        remoteEP   = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
        Debug.Log($"[PC Sender] Streaming to {remoteEP.Address}:{remoteEP.Port}");
    }

    void LateUpdate()
    {
        // Read your Vicon‐driven transform (attach this script to that GameObject)
        Vector3    p = transform.position;
        Quaternion r = transform.rotation;

        // Format as CSV: x,y,z,qx,qy,qz,qw
        string msg = $"{p.x:F6},{p.y:F6},{p.z:F6}," +
                     $"{r.x:F6},{r.y:F6},{r.z:F6},{r.w:F6}";

        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, remoteEP);
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
    }
}
