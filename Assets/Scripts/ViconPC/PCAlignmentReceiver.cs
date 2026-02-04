using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class PCAlignmentReceiver : MonoBehaviour
{
    [Header("Settings")]
    public int listenPort = 51003;

    [Header("Ghost Objects (PC Digital Twin)")]
    public Transform rawViconFinger;   // The object moved by PCUDPSender
    public Transform ghostAlignedFinger; // The "Result" -> Drag this into FittsPredictor
    public Transform ghostBoard;       // Drag this into FittsPredictor

    // Internal State
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool running;
    
    // Synced Data
    private Vector3 syncAlignPos;
    private Quaternion syncAlignRot = Quaternion.identity;
    private Vector3 syncBoardPos;
    private Quaternion syncBoardRot = Quaternion.identity;
    private bool hasData = false;

    void Start()
    {
        udpClient = new UdpClient(listenPort);
        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
    }

    void ReceiveLoop()
    {
        IPEndPoint anyEP = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyEP);
                string msg = Encoding.UTF8.GetString(data);
                string[] parts = msg.Split(',');

                if (parts.Length >= 14)
                {
                    // Parse Alignment Matrix (Pos/Rot)
                    float ax = float.Parse(parts[0]); float ay = float.Parse(parts[1]); float az = float.Parse(parts[2]);
                    float aqx = float.Parse(parts[3]); float aqy = float.Parse(parts[4]); float aqz = float.Parse(parts[5]); float aqw = float.Parse(parts[6]);

                    // Parse Board (Pos/Rot)
                    float bx = float.Parse(parts[7]); float by = float.Parse(parts[8]); float bz = float.Parse(parts[9]);
                    float bqx = float.Parse(parts[10]); float bqy = float.Parse(parts[11]); float bqz = float.Parse(parts[12]); float bqw = float.Parse(parts[13]);

                    // Atomic Update (Thread safe-ish for Unity simple types)
                    syncAlignPos = new Vector3(ax, ay, az);
                    syncAlignRot = new Quaternion(aqx, aqy, aqz, aqw);
                    syncBoardPos = new Vector3(bx, by, bz);
                    syncBoardRot = new Quaternion(bqx, bqy, bqz, bqw);
                    hasData = true;
                }
            }
            catch { }
        }
    }

    void Update()
    {
        if (!hasData) return;

        // 1. Update Ghost Board to match Quest
        ghostBoard.position = syncBoardPos;
        ghostBoard.rotation = syncBoardRot;

        // 2. Replicate the Quest's Alignment Logic EXACTLY
        // Reconstruct Matrix
        Matrix4x4 mat = Matrix4x4.TRS(syncAlignPos, syncAlignRot, Vector3.one);

        // Apply Matrix to Raw Vicon Position
        Vector3 alignedPos = mat.MultiplyPoint3x4(rawViconFinger.position);
        Quaternion alignedRot = mat.rotation * rawViconFinger.rotation;

        // 3. Apply the "Hardcoded Hack" from your Quest Script
        // Your Quest script does: Rotate(Up, -90) then Rotate(Right, -90)
        // We replicate that manually here:
        ghostAlignedFinger.position = alignedPos;
        ghostAlignedFinger.rotation = alignedRot;
        ghostAlignedFinger.Rotate(Vector3.up, -90, Space.Self);
        ghostAlignedFinger.Rotate(Vector3.right, -90, Space.Self);
    }

    void OnDestroy()
    {
        running = false;
        udpClient?.Close();
    }
}