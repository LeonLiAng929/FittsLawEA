
using UnityEngine;

public class SwitchTrackingMethod : MonoBehaviour
{
    public bool vicon = false;

    public Transform headsetFingerTipTrans;
    public Transform viconFingerTipTrans;
    public Transform anchor;
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
            anchor.parent = viconFingerTipTrans;
        else
            anchor.parent = headsetFingerTipTrans;
    }
}
