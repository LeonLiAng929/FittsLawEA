using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class PCUDPSender3 : MonoBehaviour
{
    [Header("Vicon Transforms")]
    public Transform ViconHeadset;
    public Transform ViconController;
    public Transform ViconFingerTip;

    [Header("UDP Settings")]
    [Tooltip("Quest’s IP on your LAN")]
    public string remoteIP   = "192.168.137.144";
    [Tooltip("Must match Quest listener’s port")]
    public int    remotePort = 51002;

    UdpClient udpClient;
    IPEndPoint remoteEP;
    

    void Start()
    {
        udpClient = new UdpClient();
        remoteEP  = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
        Debug.Log($"[PC Sender3] Streaming 3 points → {remoteEP}");
    }

    void LateUpdate()
    {
        // Read the three Vicon positions
        Vector3 p0 = ViconHeadset.position;
        Vector3 p1 = ViconController.position;
        Vector3 p2 = ViconFingerTip.position;

        // CSV: x0,y0,z0, x1,y1,z1, x2,y2,z2
        string msg = string.Join(",",
            p0.x.ToString("F4"), p0.y.ToString("F4"), p0.z.ToString("F4"),
            p1.x.ToString("F4"), p1.y.ToString("F4"), p1.z.ToString("F4"),
            p2.x.ToString("F4"), p2.y.ToString("F4"), p2.z.ToString("F4")
        );

        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, remoteEP);
        
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
    }
}
