using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
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

    
    private Vector3     lastViconFingerPos;
    private Quaternion  lastViconFingerRot;
    private Vector3     lastViconLeftFootPos;
    private Quaternion  lastViconLeftFootRot;
    private Vector3     lastViconRightFootPos;
    private Quaternion  lastViconRightFootRot;

    public TMP_Text log;
    public List<Vector3> sourcePoints = new List<Vector3>();
    public List<Vector3> targetPoints = new List<Vector3>();
    public int calibrationPointIndex;
    [SerializeField]
    public Matrix4x4 alignmentMatrix; 
    private bool calibrationVicon = false;
    public Transform calibrationContainer;
    public List<TargetBehaviour> calibrationPoints = new List<TargetBehaviour>();

    public int calibrationInstance = 27;
    //For Kabsch Calibration

    public void SpawnCalibrationPoint()
    {
        targetPoints.Clear();
        calibrationPoints.Clear();
        calibrationPointIndex = 0;
        calibrationContainer.gameObject.SetActive(true);
        Vector3 midpoint = Calibration.Instance.midPoint;
        midpoint.y = TargetManager.Instance.CenterCamera.position.y;// chest level
        calibrationContainer.position = midpoint- new Vector3(0,.25f,-.15f);
        foreach (TargetBehaviour point in calibrationContainer.GetComponentsInChildren<TargetBehaviour>())
        {
            targetPoints.Add(point.transform.position);
            calibrationPoints.Add(point);
        }
        calibrationPoints[calibrationPointIndex].OnTargetSelect();
    }

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
                SpawnCalibrationPoint();
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
            if (parts.Length >= 21
                && float.TryParse(parts[0], out float fingerX)
                && float.TryParse(parts[1], out float fingerY)
                && float.TryParse(parts[2], out float fingerZ)
                && float.TryParse(parts[3], out float fingerqx)
                && float.TryParse(parts[4], out float fingerqy)
                && float.TryParse(parts[5], out float fingerqz)
                && float.TryParse(parts[6], out float fingerqw)
                && float.TryParse(parts[7], out float leftX)
                && float.TryParse(parts[8], out float leftY)
                && float.TryParse(parts[9], out float leftZ)
                && float.TryParse(parts[10], out float leftqx)
                && float.TryParse(parts[11], out float leftqy)
                && float.TryParse(parts[12], out float leftqz)
                && float.TryParse(parts[13], out float leftqw)
                && float.TryParse(parts[14], out float rightX)
                && float.TryParse(parts[15], out float rightY)
                && float.TryParse(parts[16], out float rightZ)
                && float.TryParse(parts[17], out float rightqx)
                && float.TryParse(parts[18], out float rightqy)
                && float.TryParse(parts[19], out float rightqz)
                && float.TryParse(parts[20], out float rightqw))
            {
                // Store raw Vicon pose
                lastViconFingerPos = new Vector3(fingerX, fingerY, fingerZ);
                lastViconFingerRot = new Quaternion(fingerqx, fingerqy, fingerqz, fingerqw);
                
                lastViconLeftFootPos = new Vector3(leftX, leftY, leftZ);
                lastViconLeftFootRot = new Quaternion(leftqx, leftqy, leftqz, leftqw);
                
                lastViconRightFootPos = new Vector3(rightX, rightY, rightZ);
                lastViconRightFootRot = new Quaternion(rightqx, rightqy, rightqz, rightqw);

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
        sourcePoints.Add(lastViconFingerPos);
        //targetPoints.Add(fingertipAnchor.position);
        calibrationPoints[calibrationPointIndex].OnTargetDeselect();
        calibrationPointIndex += 1;
        if (calibrationPointIndex < targetPoints.Count)
		    calibrationPoints[calibrationPointIndex].OnTargetSelect();
        else
        {
            //calibrationPointIndex = 0;
            alignmentMatrix = CalculateAlignmentTransform();
            //ApplyAlignment(alignmentTransform);
            calibrationDistanceError = CalculateCalibrationDistance();
            log.text = "Calibrated, " + $"[Quest Receiver] Listening on port {listenPort}";
            calibrationContainer.gameObject.SetActive(false);
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
        fingertipAnchor.position = alignmentTransform.MultiplyPoint3x4(lastViconFingerPos);
        fingertipAnchor.rotation = alignmentTransform.rotation * lastViconFingerRot;
        TargetManager.Instance.LFPos = alignmentTransform.MultiplyPoint3x4(lastViconLeftFootPos);
        TargetManager.Instance.LFRot = alignmentTransform.rotation * lastViconLeftFootRot;
        TargetManager.Instance.RFPos = alignmentTransform.MultiplyPoint3x4(lastViconRightFootPos);
        TargetManager.Instance.RFRot = alignmentTransform.rotation * lastViconRightFootRot;
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