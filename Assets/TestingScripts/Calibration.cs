using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> calibrationPoints;

    public GameObject caliContainer;
    public static Calibration Instance;
    
    public bool toggle = false;
    private GameObject currPoint;
    private int index = 0;
    public Vector3 midPoint = Vector3.zero;
    public TMPro.TMP_Text midPointText;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currPoint = calibrationPoints[0];
        currPoint.transform.GetComponent<MeshRenderer>().material.color = Color.green;
    }

    void UpdateMidPoint()
    {
        if (calibrationPoints.Count == 0) return;
        
        
        midPoint = new Vector3((calibrationPoints[0].transform.position.x + calibrationPoints[1].transform.position.x)*.5f,
            (calibrationPoints[0].transform.position.y + calibrationPoints[1].transform.position.y)*.5f, 
            (calibrationPoints[0].transform.position.z + calibrationPoints[1].transform.position.z)*.5f);
        midPointText.text = midPoint.ToString("F2");
        midPointText.transform.parent.position = midPoint;
    }
    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            toggle = !toggle;
            caliContainer.SetActive(toggle);
        }

        if (toggle)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                SwitchCalibrationPoints();
            }
            Vector2 thumbstickR = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            if (thumbstickR != Vector2.zero)
            {
                Vector3 translation = new Vector3();
                if (math.abs(thumbstickR.x) > math.abs(thumbstickR.y))
                {
                    if (thumbstickR.x > 0)
                    {
                        translation = new Vector3(1, 0, 0) * Time.deltaTime;
                    }
                    else
                    {
                        translation = new Vector3(-1, 0, 0) * Time.deltaTime;
                    }
                }
                else{
                    if (thumbstickR.y > 0)
                    {
                        translation = new Vector3(0, 0, 1) * Time.deltaTime;
                    }
                    else
                    {
                        translation = new Vector3(0, 0, -1) * Time.deltaTime;
                    }
                }
                currPoint.transform.position += translation*.1f;
                //round up to 2 decimal places
                currPoint.transform.position = new Vector3(
                    Mathf.Round(currPoint.transform.position.x * 100f) / 100f,
                    Mathf.Round(currPoint.transform.position.y * 100f) / 100f,
                    Mathf.Round(currPoint.transform.position.z * 100f) / 100f);
            }
            
            Vector2 thumbstickL = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (thumbstickL != Vector2.zero)
            {
                Vector3 translation = new Vector3();
                if (thumbstickL.y > 0)
                {
                    translation = new Vector3(0, 1, 0) * Time.deltaTime;
                }
                else
                {
                    translation = new Vector3(0, -1, 0) * Time.deltaTime;
                }
                currPoint.transform.position += translation*.1f;
                //round up to 2 decimal places
                currPoint.transform.position = new Vector3(
                    Mathf.Round(currPoint.transform.position.x * 100f) / 100f,
                    Mathf.Round(currPoint.transform.position.y * 100f) / 100f,
                    Mathf.Round(currPoint.transform.position.z * 100f) / 100f);
            }
            UpdateMidPoint();
        }
    }
    
    void SwitchCalibrationPoints()
    {
        currPoint.GetComponent<MeshRenderer>().material.color = Color.white;
        if(index < calibrationPoints.Count - 1)
        {
            index++;
        }
        else
        {
            index = 0;
        }
        currPoint = calibrationPoints[index];
        currPoint.GetComponent<MeshRenderer>().material.color = Color.green;
    }
}
