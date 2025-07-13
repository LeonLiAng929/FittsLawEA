using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class QuestUDPReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Port PC is sending on")]
    public int listenPort = 51002;

    [Header("Target to Drive")]
    public Transform fingertipAnchor;

    [Header("Alignment Offsets (auto-updated)")]
    [Tooltip("Position offset to align Vicon → Unity")]
    public Vector3 positionOffset;
    [Tooltip("Rotation offset to align Vicon → Unity")]
    public Quaternion rotationOffset = Quaternion.identity;
    public float calibrationDistanceError = 0;
    public SwitchTrackingMethod tracking;
    // Internals
    private UdpClient   udpClient;
    private Thread      receiveThread;
    private bool        running;
    private string      latestMsg;
    private bool        msgReady;
    private object      lockObj = new object();

    // Last raw Vicon pose
    private Vector3     lastViconPos;
    private Quaternion  lastViconRot;

    public TMP_Text log;
    public List<Vector3> sourcePoints = new List<Vector3>();
    public List<Vector3> targetPoints = new List<Vector3>();
    public int calibrationPointIndex;
    [SerializeField]
    public Matrix4x4 alignmentMatrix; 
    private bool calibrationVicon = false;
    void Start()
    {
        // Start UDP listener
        udpClient = new UdpClient(listenPort);
        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
        Debug.Log($"[Quest Receiver] Listening on port {listenPort}");
        log.text = $"[Quest Receiver] Listening on port {listenPort}";
    }

    void ReceiveLoop()
    {
        var anyEP = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                var data = udpClient.Receive(ref anyEP);
                string msg = Encoding.UTF8.GetString(data);
                lock (lockObj)
                {
                    latestMsg = msg;
                    msgReady   = true;
                }
            }
            catch (SocketException) { /* ignore */ }
        }
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            calibrationVicon = !calibrationVicon;
            if (!calibrationVicon)
            {
                    sourcePoints.Clear();
                    targetPoints.Clear();
                    calibrationPointIndex = 0;
            }
            else
            {
                log.text = "Calibration Start..." + $" [Quest Receiver] Listening on port {listenPort}";
            }
        }

        if (calibrationVicon)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RHandTrigger))
            {
                log.text = "Calibrating..."+ calibrationPointIndex.ToString() + $" [Quest Receiver] Listening on port {listenPort}";
                AddTargetPoint();
            }
        }

        if (msgReady)
        {
            string msg;
            lock (lockObj)
            {
                msg      = latestMsg;
                msgReady = false;
            }

            // Parse x,y,z,qx,qy,qz,qw
            var parts = msg.Split(',');
            if (parts.Length >= 7
                && float.TryParse(parts[0], out float x)
                && float.TryParse(parts[1], out float y)
                && float.TryParse(parts[2], out float z)
                && float.TryParse(parts[3], out float qx)
                && float.TryParse(parts[4], out float qy)
                && float.TryParse(parts[5], out float qz)
                && float.TryParse(parts[6], out float qw))
            {
                // Store raw Vicon pose
                lastViconPos = new Vector3(x, y, z);
                lastViconRot = new Quaternion(qx, qy, qz, qw);

                if (fingertipAnchor != null)
                {
                    if (tracking.vicon)
                    {
                        ApplyAlignment(alignmentMatrix);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[Quest Receiver] Bad msg: {msg}");
                log.text = $"[Quest Receiver] Bad msg: {msg}";
            }
        }
    }

    
    public void CalibrateOffset()
    {
        
        log.text = "Calibrated, " + $"[Quest Receiver] Listening on port {listenPort}";
    }

    public void AddTargetPoint()
    {
        sourcePoints.Add(lastViconPos);
        targetPoints.Add(fingertipAnchor.position);
        calibrationPointIndex += 1;
		
        if (calibrationPointIndex >= 4)
        {
            //calibrationPointIndex = 0;
            alignmentMatrix = CalculateAlignmentTransform();
            //ApplyAlignment(alignmentTransform);
            calibrationDistanceError = CalculateCalibrationDistance();
            log.text = "Calibrated, " + $"[Quest Receiver] Listening on port {listenPort}";
            //SaveOriginalPosition(); 
        }
    }
    
    private float CalculateCalibrationDistance()
    {
        float result = 0;

        for(int i = 0; i < sourcePoints.Count; i++)
        {
            result += Vector3.Distance(sourcePoints[i], targetPoints[i]);
        }

        result = result / sourcePoints.Count;
        return result;
    }

    public void ApplyAlignment(Matrix4x4 alignmentTransform)
    {
        fingertipAnchor.position = alignmentTransform.MultiplyPoint3x4(lastViconPos);
        fingertipAnchor.rotation = alignmentTransform.rotation * lastViconRot;
    }

    public Matrix4x4 CalculateAlignmentTransform()
    {
        Vector3[] sourcePointsPosition = new Vector3[sourcePoints.Count];
        Vector4[] targetPointsPosition = new Vector4[sourcePoints.Count];;

        for (int i = 0; i < sourcePoints.Count; i++)
        {
            sourcePointsPosition[i] = sourcePoints[i];
            targetPointsPosition[i] = new Vector4(targetPoints[i].x, targetPoints[i].y, targetPoints[i].z, 1);
        }

        return KabschSolver.SolveKabsch(sourcePointsPosition, targetPointsPosition);
    }
    void OnDisable()
    {
        running = false;
        udpClient.Close();
    }
}