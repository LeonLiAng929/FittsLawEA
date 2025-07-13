using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class QuestUDPReceiver3 : MonoBehaviour
{
    [Header("UDP Settings")]
    [Tooltip("Port PC is sending on")]
    public int listenPort = 51002;

    [Header("Vicon‐driven Objects")]
    public Transform ViconHeadset;
    public Transform ViconController;
    public Transform ViconFingerTip;

    public Transform UnityFingerTip;
    public KabschCalibration calibrator;
    public SwitchTrackingMethod tracking;
    
    UdpClient  udpClient;
    Thread     receiveThread;
    bool       running;
    string     latestMsg;
    bool       msgReady;
    object     lockObj = new object();

    void Start()
    {
        udpClient     = new UdpClient(listenPort);
        running       = true;
        receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiveThread.Start();
        Debug.Log($"[Quest Receiver3] Listening on port {listenPort}");
    }

    void ReceiveLoop()
    {
        var anyEP = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref anyEP);
                string msg  = Encoding.UTF8.GetString(data);
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
        if (!msgReady) return;
        string msg;
        lock (lockObj)
        {
            msg      = latestMsg;
            msgReady = false;
        }

        var parts = msg.Split(',');
        if (parts.Length >= 9
            && float.TryParse(parts[0], out float x0)
            && float.TryParse(parts[1], out float y0)
            && float.TryParse(parts[2], out float z0)
            && float.TryParse(parts[3], out float x1)
            && float.TryParse(parts[4], out float y1)
            && float.TryParse(parts[5], out float z1)
            && float.TryParse(parts[6], out float x2)
            && float.TryParse(parts[7], out float y2)
            && float.TryParse(parts[8], out float z2))
        {
            ViconHeadset.position    = new Vector3(x0, y0, z0);
            ViconController.position = new Vector3(x1, y1, z1);
            ViconFingerTip.position  = new Vector3(x2, y2, z2);
        }
        else
        {
            Debug.LogWarning($"[Quest Receiver3] Bad msg: {msg}");
        }
    }

    private void LateUpdate()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
            calibrator.Calibrate(ViconHeadset, ViconController, ViconFingerTip);

        if (tracking.vicon)
        {
            // Now apply alignment to the *just-received* positions
            calibrator.ApplyAlignment(UnityFingerTip);
        }
    }

    void OnDisable()
    {
        running = false;
        udpClient.Close();
    }
}