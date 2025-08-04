
using UnityEngine;

public class SwitchTrackingMethod : MonoBehaviour
{
    public bool vicon = false;

    public Transform headsetFingerTipTrans;
    public Transform viconFingerTipTrans;
    public Transform anchor;
    public Transform fingertipAnchor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchTracking()
    {
        vicon= !vicon;
        if (vicon)
        {
            anchor.parent = viconFingerTipTrans;
            // // The raw Vicon rotation mapped into Unity frame
            // QuestUDPReceiver dataReceiver = QuestUDPReceiver.Instance;
            // Quaternion vMappedRot = dataReceiver.alignmentMatrix.rotation * dataReceiver.lastViconFingerRot;
            //
            // // Two axes in that frame:
            // Vector3 vForward = vMappedRot * Vector3.forward;
            // Vector3 vUp = vMappedRot * Vector3.up;
            //
            // // The mesh’s actual world axes:
            // Vector3 mForward = fingertipAnchor.forward;
            // Vector3 mUp = fingertipAnchor.up;
            // // Step 1: align forward vectors
            // Quaternion q1 = Quaternion.FromToRotation(vForward, mForward);
            //
            // // Rotate vUp by q1 so it lies in the mesh’s forward plane
            // Vector3 vUp2 = q1 * vUp;
            //
            //  // Step 2: align that up to the mesh’s up
            // Quaternion q2 = Quaternion.FromToRotation(vUp2, mUp);
            //
            //
            // dataReceiver.fingerRotationOffset = q2 * q1;
        }
        else
            anchor.parent = headsetFingerTipTrans;
    }
}
