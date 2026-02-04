using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class QuestStateSender : MonoBehaviour
{
    [Header("Network Settings")]
    public string pcIP = "192.168.137.1"; 
    public int pcPort = 51003; 

    [Header("References")]
    public QuestUDPReceiver receiver;     
    public Transform targetBoard;         

    private UdpClient udpClient;
    private IPEndPoint remoteEP;

    // State Tracking (To prevent spam)
    private Matrix4x4 lastSentMatrix = Matrix4x4.identity;
    private Vector3 lastSentBoardPos = Vector3.zero;
    private Quaternion lastSentBoardRot = Quaternion.identity;
    public static QuestStateSender Instance;

    void Start()
    {
        Instance = this;
        udpClient = new UdpClient();
        remoteEP = new IPEndPoint(IPAddress.Parse(pcIP), pcPort);
    }

    public void SendUpdate() 
    {
        if (receiver == null || targetBoard == null) return;

        // 1. Get current values
        Matrix4x4 currentMatrix = receiver.alignmentMatrix;
        Vector3 currentBoardPos = targetBoard.position;
        Quaternion currentBoardRot = targetBoard.rotation;

        // 2. Validity Check: Don't send if we haven't calibrated yet
        if (currentMatrix.isIdentity) return;

        // 3. Change Detection: ONLY send if something is different
        if (currentMatrix != lastSentMatrix || 
            currentBoardPos != lastSentBoardPos || 
            currentBoardRot != lastSentBoardRot)
        {
            SendCalibrationData(currentMatrix, targetBoard);

            // Update our history so we don't send again until next change
            lastSentMatrix = currentMatrix;
            lastSentBoardPos = currentBoardPos;
            lastSentBoardRot = currentBoardRot;
            
            Debug.Log("[QuestSender] Calibration Changed -> Sent update to PC.");
        }
    }

    void SendCalibrationData(Matrix4x4 mat, Transform board)
    {
        Vector3 alignPos = mat.GetColumn(3);
        Quaternion alignRot = mat.rotation;

        Vector3 boardPos = board.position;
        Quaternion boardRot = board.rotation;

        string msg = $"{alignPos.x:F4},{alignPos.y:F4},{alignPos.z:F4}," +
                     $"{alignRot.x:F4},{alignRot.y:F4},{alignRot.z:F4},{alignRot.w:F4}," +
                     $"{boardPos.x:F4},{boardPos.y:F4},{boardPos.z:F4}," +
                     $"{boardRot.x:F4},{boardRot.y:F4},{boardRot.z:F4},{boardRot.w:F4}";

        byte[] data = Encoding.UTF8.GetBytes(msg);
        udpClient.Send(data, data.Length, remoteEP);
    }

    void OnDestroy() => udpClient?.Close();
}