using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class PCUDPSender : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Quest’s IP on your LAN (set this to the Quest IP)")]
    public string remoteIP   = "192.168.137.169";
    [Tooltip("Must match Quest listener’s listenPort")]
    public int    remotePort = 51002;

    private UdpClient udpClient;
    private IPEndPoint remoteEP;

    public Transform fingerTip;
    public Transform leftFoot;
    public Transform rightFoot;
    public Transform caliCube;
    public Transform controllerTip;
    
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
        Vector3    fingerP = fingerTip.position;
        Quaternion fingerR = fingerTip.rotation;
        
        Vector3    leftP = leftFoot.position;
        Quaternion leftR = leftFoot.rotation;
        
        Vector3    rightP = rightFoot.position;
        Quaternion rightR = rightFoot.rotation;
        
        // Vector3   caliP = caliCube.position;
        // Quaternion caliR = caliCube.rotation;
        
        Vector3  controllerP = controllerTip.position;
        Quaternion controllerR = controllerTip.rotation;

        Vector3 pred = FittsPredictor.Instance.CurrentPredictionLocal;

        // Format as CSV: x,y,z,qx,qy,qz,qw
        string msg = $"{fingerP.x:F3},{fingerP.y:F3},{fingerP.z:F3}," +
                     $"{fingerR.x:F3},{fingerR.y:F3},{fingerR.z:F3},{fingerR.w:F3},"+
                     $"{leftP.x:F3},{leftP.y:F3},{leftP.z:F3}," +
                     $"{leftR.x:F3},{leftR.y:F3},{leftR.z:F3},{leftR.w:F3},"+
                     $"{rightP.x:F3},{rightP.y:F3},{rightP.z:F3}," +
                     $"{rightR.x:F3},{rightR.y:F3},{rightR.z:F3},{rightR.w:F3}," +
                     // $"{caliP.x:F3},{caliP.y:F3},{caliP.z:F3}," +
                     // $"{caliR.x:F3},{caliR.y:F3},{caliR.z:F3},{caliR.w:F3}," +
                     $"{controllerP.x:F3},{controllerP.y:F3},{controllerP.z:F3}," +
                     $"{controllerR.x:F3},{controllerR.y:F3},{controllerR.z:F3},{controllerR.w:F3},"+
                     $"{pred.x:F4},{pred.y:F4},{pred.z:F4}"; // Higher precision F4 for prediction

        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, remoteEP);
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
    }
}
