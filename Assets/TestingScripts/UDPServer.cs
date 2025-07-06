using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPSender : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Quest IP or broadcast (e.g. 255.255.255.255)")]
    public string remoteIP = "255.255.255.255";
    public int remotePort = 51002;
 
    private UdpClient udpClient;
    private IPEndPoint remoteEP;

    void Start()
    {
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        remoteEP = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);
        Debug.Log($"[PC Sender] Sending to {remoteEP}");
    }

    void LateUpdate()
    {
        // Read position (in meters) after Vicon has updated it
        Vector3 p = transform.position;
        string msg = $"{p.x},{p.y},{p.z}";
        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, remoteEP);
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
    }
}