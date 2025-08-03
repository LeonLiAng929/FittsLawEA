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
    public static QuestUDPReceiver Instance;
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
    private Vector3     lastViconCaliCubePos;
    private Quaternion  lastViconCaliCubeRot;

    public TMP_Text log;
    // procustes alignment
    public List<Vector3> sourcePoints = new List<Vector3>();
    public List<Vector3> targetPoints = new List<Vector3>();
    public int calibrationPointIndex;
    [SerializeField]
    public Matrix4x4 alignmentMatrix; 
    private bool calibrationVicon = false;
    public Transform calibrationContainer;
    public List<TargetBehaviour> calibrationPoints = new List<TargetBehaviour>();
    
    // physical object alignment --- calibration method 2
    public Transform caliCube;
    private CaliMovementControl movementControl = CaliMovementControl.Position;
    private Quaternion rotOffset;
    public Vector3 fingerTipOffset;

    private enum CaliMovementControl
    {
        Position,
        Rotation
    }

    public void SwitchMovement()
    {
        movementControl = movementControl switch
        {
            CaliMovementControl.Position => CaliMovementControl.Rotation,
            CaliMovementControl.Rotation => CaliMovementControl.Position
        };
    }
    
    //public int calibrationInstance = 27;
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
        //caliCube.gameObject.SetActive(false);
        udpClient = new UdpClient(listenPort);
        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
        Debug.Log($"[Quest Receiver] Listening on port {listenPort}");
        log.text = $"[Quest Receiver] Listening on port {listenPort}";
        Instance = this;
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
                // Procrustes alignment
                    // sourcePoints.Clear();
                    // targetPoints.Clear();
                    // calibrationPointIndex = 0;
                    alignmentMatrix = CalculateAlignmentTransform2();
                    //caliCube.gameObject.SetActive(false);
                    log.text = "Calibrated, " + $"[Quest Receiver] Listening on port {listenPort}";
            }
            else
            {
                log.text = "Calibrating...Position Calibration Mode";
                caliCube.gameObject.SetActive(true);
                // Procrustes alignment
                //SpawnCalibrationPoint();
            }
        }

        if (calibrationVicon)
        {
            // Procrustes alignment
            // if (OVRInput.GetDown(OVRInput.RawButton.RHandTrigger))
            // {
            //     log.text = "Calibrating..."+ calibrationPointIndex.ToString() + $" [Quest Receiver] Listening on port {listenPort}";
            //     AddTargetPoint();
            // }
            
            // alignemnt method 2
            if (OVRInput.GetDown(OVRInput.RawButton.RHandTrigger))
            {
                SwitchMovement();
            }
            if(movementControl == CaliMovementControl.Position)
            {
                log.text = "Calibrating...Position Calibration Mode";
                Vector2 moveCaliXZ = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                Vector2 moveCaliY = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                caliCube.position += new Vector3(moveCaliXZ.x, moveCaliY.y, moveCaliXZ.y) * (Time.deltaTime * 0.1f);
            }
            else
            {
                log.text = "Rotation Calibration Mode";
                Vector2 rotateCaliXY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                Vector2 rotateCaliZ = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                    
                caliCube.rotation *= Quaternion.Euler(rotateCaliXY.y * 10f * Time.deltaTime, rotateCaliXY.x * 10f * Time.deltaTime, rotateCaliZ.x * 10f * Time.deltaTime);
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
            if (parts.Length >= 28
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
                && float.TryParse(parts[20], out float rightqw)
                && float.TryParse(parts[21], out float caliX)
                && float.TryParse(parts[22], out float caliY)
                && float.TryParse(parts[23], out float caliZ)
                && float.TryParse(parts[24], out float caliqx)
                && float.TryParse(parts[25], out float caliqy)
                && float.TryParse(parts[26], out float caliqz)
                && float.TryParse(parts[27], out float caliqw)
                )
            {
                // Store raw Vicon pose
                lastViconFingerPos = new Vector3(fingerX, fingerY, fingerZ);
                lastViconFingerRot = new Quaternion(fingerqx, fingerqy, fingerqz, fingerqw);
                
                lastViconLeftFootPos = new Vector3(leftX, leftY, leftZ);
                lastViconLeftFootRot = new Quaternion(leftqx, leftqy, leftqz, leftqw);
                
                lastViconRightFootPos = new Vector3(rightX, rightY, rightZ);
                lastViconRightFootRot = new Quaternion(rightqx, rightqy, rightqz, rightqw);
                
                lastViconCaliCubePos = new Vector3(caliX, caliY, caliZ);
                lastViconCaliCubeRot = new Quaternion(caliqx, caliqy, caliqz, caliqw);

                if (fingertipAnchor != null)
                {
                    if (tracking.vicon)
                    {
                        // procustes alignment
                        //ApplyAlignment(alignmentMatrix);
                        ApplyAlignment2();
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
    
    // Alignment method 2
    public Matrix4x4 CalculateAlignmentTransform2()
    {
        // Vector3 sourcePos = lastViconCaliCubePos;
        // Quaternion sourceRot = lastViconCaliCubeRot;
        //
        // Vector3 targetPos = caliCube.position;
        // Quaternion targetRot = caliCube.rotation;

        Matrix4x4 viconTransform = Matrix4x4.TRS(lastViconFingerPos, lastViconFingerRot, Vector3.one);
        Matrix4x4 unityTransform = Matrix4x4.TRS(caliCube.position, caliCube.rotation, Vector3.one);
        var C = new Matrix4x4(
            new Vector4(  0, -1, 0, 0 ),
            new Vector4(  0, 0,  -1, 0 ),
            new Vector4( 1, 0,  0, 0 ),
            new Vector4(  0, 0,  0, 1 )
        );
        
        Matrix4x4 alignmentTransform = C*unityTransform * viconTransform.inverse;
        
        Matrix4x4 posOffset = Matrix4x4.TRS(caliCube.position - alignmentTransform.MultiplyPoint3x4(lastViconFingerPos), Quaternion.identity, Vector3.one);
        rotOffset = Quaternion.Inverse(alignmentTransform.rotation*lastViconFingerRot) * caliCube.rotation;
        alignmentTransform = posOffset * alignmentTransform;
        //Matrix4x4 alignmentTransform = Matrix4x4.TRS(targetPos, targetRot * Quaternion.Inverse(sourceRot), Vector3.one);
        return alignmentTransform;
    }
    
    public void ApplyAlignment2()
    {
        fingertipAnchor.position = alignmentMatrix.MultiplyPoint3x4(lastViconFingerPos);
        fingertipAnchor.rotation = alignmentMatrix.rotation * lastViconFingerRot* rotOffset;
        TargetManager.Instance.LFPos = alignmentMatrix.MultiplyPoint3x4(lastViconLeftFootPos);
        TargetManager.Instance.LFRot = alignmentMatrix.rotation * lastViconLeftFootRot* rotOffset;
        TargetManager.Instance.RFPos = alignmentMatrix.MultiplyPoint3x4(lastViconRightFootPos);
        TargetManager.Instance.RFRot = alignmentMatrix.rotation * lastViconRightFootRot* rotOffset;
        // caliCube.gameObject.SetActive(true);
        // caliCube.rotation = alignmentMatrix.rotation * lastViconCaliCubeRot* rotOffset;
        // caliCube.position = alignmentMatrix.MultiplyPoint3x4(lastViconCaliCubePos);
    }
    void OnDisable()
    {
        running = false;
        udpClient.Close();
    }
    
}