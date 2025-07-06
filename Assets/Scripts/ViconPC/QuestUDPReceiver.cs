using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine.UI;

public class QuestUDPReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Port PC is sending on")]
    public int listenPort = 51002;

    [Header("Target to Drive")]
    [Tooltip("Assign the object you want moved/rotated")]
    public Transform targetTransform;

    [Header("Alignment Offsets (auto-updated)")]
    [Tooltip("Position offset to align Vicon → Unity")]
    public Vector3 positionOffset;
    [Tooltip("Rotation offset to align Vicon → Unity")]
    public Quaternion rotationOffset = Quaternion.identity;

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
    void Start()
    {
        // Start UDP listener
        udpClient = new UdpClient(listenPort);
        running = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
        Debug.Log($"[Quest Receiver] Listening on port {listenPort}");
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
        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            CalibrateOffset();
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

                // Apply current offsets
                Vector3 finalPos = lastViconPos + positionOffset;
                Quaternion finalRot = rotationOffset * lastViconRot;

                if (targetTransform != null)
                {
                    if (tracking.vicon)
                    {
                        targetTransform.position = finalPos;
                        //targetTransform.localRotation = finalRot;
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

    /// <summary>
    /// Call this at runtime (e.g. via UI button) to compute
    /// positionOffset and rotationOffset so that
    /// current 'targetTransform' pose becomes the zero-reference
    /// for the most recent Vicon pose.
    /// </summary>
    public void CalibrateOffset()
    {
        if (targetTransform == null)
        {
            Debug.LogError("[Quest Receiver] No targetTransform assigned.");
            return;
        }

        // Compute position offset: how much Unity object differs from Vicon pos
        positionOffset = targetTransform.position - lastViconPos;

        // Compute rotation offset: so rotationOffset * lastViconRot == targetRot
        // => rotationOffset = targetRot * inverse(lastViconRot)
        //rotationOffset = targetTransform.rotation * Quaternion.Inverse(lastViconRot);

        Vector3 viconForward  = lastViconRot * Vector3.forward;
        // Headset fingertip’s actual forward:
        Vector3 headsetForward = targetTransform.forward;

        // rotationOffset makes viconForward → headsetForward
        rotationOffset = Quaternion.FromToRotation(viconForward, headsetForward);

        Debug.Log($"[Quest Receiver] Calibrated.\n" +
                  $"  positionOffset = {positionOffset:F6}\n"+
                  $"  rotationOffset = {rotationOffset.eulerAngles:F6}");
        log.text = "Calibrated, " + $"[Quest Receiver] Listening on port {listenPort}";
    }

    void OnDisable()
    {
        running = false;
        udpClient.Close();
    }
}