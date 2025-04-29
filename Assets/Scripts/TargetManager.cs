
using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit.SceneDecorator;
using Oculus.Interaction;
using Oculus.Platform.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TargetManager : MonoBehaviour
{
    public bool toggle=true;
    public static TargetManager Instance;
 
    /// <summary> The follow speed. </summary>
    [SerializeField, Range(0.0f, 100.0f), Tooltip("How quickly to interpolate the object towards its target position and rotation.")]
    protected float FollowSpeed;
    [SerializeField, Tooltip("If ticked, the object will follow the target and always stay in sight, else it will follow the target but may" +
        "be out of sight when head rotates")]
    /// <summary> The default distance. </summary>
    public float defaultDistance;

    /// <summary> default rotation at start. </summary>
    protected Vector2 defaultRotation = new Vector2(0f, 0f);
    /// <summary> The horizontal rotation. </summary>
    protected Quaternion HorizontalRotation;
    /// <summary> The horizontal rotation inverse. </summary>
    protected Quaternion HorizontalRotationInverse;
    /// <summary> The vertical rotation. </summary>
    protected Quaternion VerticalRotation;
    /// <summary> The vertical rotation inverse. </summary>
    protected Quaternion VerticalRotationInverse;
    /// <summary> The offset. </summary>
    [SerializeField, Tooltip("The offset from the view port center applied based on the object anchor selection.")]
    protected Vector2 Offset = new Vector2(0.1f, 0.1f);
    

    [SerializeField]
    protected OVRCameraRig cameraRig;
    
    [SerializeField]
    protected Transform targetContainer;
    [SerializeField]
    protected float defaultHeight;
    
    private Transform CenterCamera;
    
    public List<TargetBehaviour> targets = new List<TargetBehaviour>();
    public int currentTarget = 0;
    public bool forward = true; //true: currTarget + 6, false: currTarget - 5
    public bool trialStarted = false;
    public bool trialEnded = false;
    public float timer = 0;
    public GameObject touchTip;
    public GameObject FilterControl;
    
    #region ForUserStudy
    /*public List<float> speed = new List<float>();
    public List<float> distance = new List<float>();
    public List<float> size = new List<float>();
    public List<float> indexOfDifficulty = new List<float>(); //Mathf.Log((distance[0]/size[0])+1,2);*/
    public List<float> movementTime = new List<float>();
    public List<float> timestamp = new List<float>();
    public List<Vector3> targetPositions = new List<Vector3>();
    public List<Vector3> selectionPositions = new List<Vector3>();
    public List<Quaternion> selectionQuaternions = new List<Quaternion>();
    public List<bool> successfulSelection = new List<bool>();
    public List<Quaternion> rawQuaternions = new List<Quaternion>();
    public List<Vector3> rawPositions = new List<Vector3>();
    public float cumulativeTime = 0;
    public List<Vector3> currentTargetPos = new List<Vector3>();
    public List<int> currentTargetIndex = new List<int>();
    public List<float> rawTimestamp = new List<float>();
    #endregion ForUserStudy

    public GameObject finishText;
    private void Awake()
    {
        Instance = this;
    }

    /// <summary> Starts this object. </summary>
    protected void Start()
    {
        HorizontalRotation = Quaternion.AngleAxis(defaultRotation.y, Vector3.right);
        HorizontalRotationInverse = Quaternion.Inverse(HorizontalRotation);
        VerticalRotation = Quaternion.AngleAxis(defaultRotation.x, Vector3.up);
        VerticalRotationInverse = Quaternion.Inverse(VerticalRotation);
        CenterCamera = cameraRig.centerEyeAnchor;
        toggle = true;
        

        //InstantiateInCircle(this.prefabToInstantiate, targetContainer.position, 11, 0.035f, 0.2f, 0);
        //prefabToInstantiate.SetActive(false);

    }

    public void Reset()
    {
        targets = new List<TargetBehaviour>();
        currentTarget = 0;
        forward = true;
        trialStarted = false;
        trialEnded = false;
        timer = 0;
        /*speed = new List<float>();
        distance = new List<float>();
        size = new List<float>();
        indexOfDifficulty = new List<float>();*/
        movementTime = new List<float>();
        targetPositions = new List<Vector3>();
        selectionPositions = new List<Vector3>();
        successfulSelection = new List<bool>();
        rawQuaternions = new List<Quaternion>();
        rawPositions = new List<Vector3>();
        cumulativeTime = 0;
        currentTargetPos = new List<Vector3>();
        currentTargetIndex = new List<int>();
        rawTimestamp = new List<float>();
        selectionQuaternions = new List<Quaternion>();
        foreach (TargetBehaviour target in targetContainer.GetComponentsInChildren<TargetBehaviour>())
        {
            Destroy(target.gameObject);
        }
    }
    
    public void ShowFinishText()
    {
        finishText.SetActive(true);
    }

    public void InitialiseTargets()
    {
        foreach (TargetBehaviour target in targetContainer.GetComponentsInChildren<TargetBehaviour>())
        {
            target.transform.position = CalculatePosition(CenterCamera, target.Anchor);
        }
    }

    public Vector2 GetOffset()
    {
        return Offset;
    }
    

    /// <summary> Calculates the position. </summary>
    /// <param name="cameraTransform"> The camera transform.</param>
    /// <returns> The calculated position. </returns>
    protected Vector3 CalculatePosition(Transform cameraTransform, TextAnchor anchor)
    {
        Vector3 position = cameraTransform.position + (cameraTransform.forward * defaultDistance);
        Vector3 horizontalOffset = cameraTransform.right * Offset.x;
        Vector3 verticalOffset = cameraTransform.up * Offset.y;

        switch (anchor)
        {
            case TextAnchor.UpperLeft: position += verticalOffset - horizontalOffset; break;
            case TextAnchor.UpperCenter: position += verticalOffset; break;
            case TextAnchor.UpperRight: position += verticalOffset + horizontalOffset; break;
            case TextAnchor.MiddleLeft: position -= horizontalOffset; break;
            case TextAnchor.MiddleRight: position += horizontalOffset; break;
            case TextAnchor.LowerLeft: position -= verticalOffset + horizontalOffset; break;
            case TextAnchor.LowerCenter: position -= verticalOffset; break;
            case TextAnchor.LowerRight: position -= verticalOffset - horizontalOffset; break;
        }
        
        return position;
    }

    /// <summary> Calculates the rotation. </summary>
    /// <param name="cameraTransform"> The camera transform.</param>
    /// <returns> The calculated rotation. </returns>
    protected Quaternion CalculateRotation(Transform cameraTransform, TextAnchor anchor)
    {
        Quaternion rotation = cameraTransform.rotation;

        switch (anchor)
        {
            case TextAnchor.UpperLeft: rotation *= HorizontalRotationInverse * VerticalRotationInverse; break;
            case TextAnchor.UpperCenter: rotation *= HorizontalRotationInverse; break;
            case TextAnchor.UpperRight: rotation *= HorizontalRotationInverse * VerticalRotation; break;
            case TextAnchor.MiddleLeft: rotation *= VerticalRotationInverse; break;
            case TextAnchor.MiddleRight: rotation *= VerticalRotation; break;
            case TextAnchor.LowerLeft: rotation *= HorizontalRotation * VerticalRotationInverse; break;
            case TextAnchor.LowerCenter: rotation *= HorizontalRotation; break;
            case TextAnchor.LowerRight: rotation *= HorizontalRotation * VerticalRotation; break;
        }

        return rotation;
    }

    protected virtual void LateUpdate()
    {
        if (!trialEnded)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                selectionPositions.Add(touchTip.transform.position);
                successfulSelection.Add(targets[currentTarget].isSelected);
                selectionQuaternions.Add(touchTip.transform.rotation);
                timestamp.Add(cumulativeTime);
                ProceedTrial();
                if (!trialStarted)
                {
                    trialStarted = true;
                }
                else
                {
                    movementTime.Add(timer);
                    timer = 0;
                }

                if (trialEnded)
                {
                    UserStudy.instance.SaveCurrentParticipantRecord();
                    UserStudy.instance.WriteStudyResult();
                }
            }
        }
        /*if (toggle)
        {
            if (CenterCamera == null)
            {
                return;
            }

            float t = Time.deltaTime * FollowSpeed;
            targetContainer.transform.position = Vector3.Lerp(targetContainer.transform.position,
                CalculatePosition(CenterCamera, TextAnchor.MiddleCenter), t);

            targetContainer.transform.rotation = Quaternion.Slerp(targetContainer.transform.rotation,
                CalculateRotation(CenterCamera, TextAnchor.MiddleCenter), t);
            //if (!AlwaysStayInSight)
            {
                targetContainer.transform.position = new Vector3(targetContainer.transform.position.x, defaultHeight,
                    targetContainer.transform.position.z);
                //transform.position.z = defaultDistance * Mathf.Cos(Mathf.Deg2Rad*6f);
                targetContainer.transform.rotation =
                    Quaternion.Euler(new Vector3(0, targetContainer.transform.rotation.eulerAngles.y, 0));


            }
            /*Vector3 direction = (targetContainer.transform.position - CenterCamera.position).normalized;
            Vector3 targetPosition = CenterCamera.position + direction * defaultDistance;
            targetContainer.transform.position = Vector3.Lerp(targetContainer.transform.position, targetPosition, t);

            // Only update the y-axis rotation to follow the horizontal rotation of CenterCamera
            Quaternion targetRotation = Quaternion.Euler(0, CenterCamera.rotation.eulerAngles.y, 0);
            targetContainer.transform.rotation = Quaternion.Slerp(targetContainer.transform.rotation, targetRotation, t);

            // Ensure the height remains constant
            targetContainer.transform.position = new Vector3(targetContainer.transform.position.x, defaultHeight, targetContainer.transform.position.z);#1#
        }*/
    }

    public void ActivateRandomTarget()
    {
        //List<TargetBehaviour> targets = new List<TargetBehaviour>(targetContainer.GetComponentsInChildren<TargetBehaviour>());
        int randomIndex = Random.Range(0, targets.Count);
        targets[randomIndex].OnTargetSelect();
        timer = 0;
    }

    public void InitialiseTrial()
    {
        Reset();
        UserStudy.instance.LoadCurrentParticipantRecord();
        UserStudy.instance.LoadCurrentSettings();
        UserStudy.instance.PrepareStudy();
        targets[0].OnTargetSelect();
        
    }
    public void EndTrial()
    {
        trialEnded = true;
    }
    public void ProceedTrial()
    {
        targets[currentTarget].OnTargetDeselect();
        if (trialStarted && currentTarget == 0)
        {
            EndTrial();
            return;
        }
        if (forward)
        {
            currentTarget += 6;
            forward = false;
        }
        else
        {
            currentTarget -= 5;
            forward = true;
        }

        if (currentTarget == 11)
        {
            currentTarget = 0;
        }
        
        targets[currentTarget].OnTargetSelect();
        targetPositions.Add(targets[currentTarget].transform.position);
    }

    private void FixedUpdate()
    {
        if (trialStarted)
        {
            rawQuaternions.Add(touchTip.transform.rotation);
            rawPositions.Add(touchTip.transform.position);
            currentTargetPos.Add(targets[currentTarget].transform.position);
            currentTargetIndex.Add(currentTarget);
            rawTimestamp.Add(cumulativeTime);
 
        }
    }

    private void Update()
    {
        if (!trialEnded)
        {
            if (trialStarted)
            {
                timer += Time.deltaTime;
                cumulativeTime += Time.deltaTime;
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            Vector3 camForward = CenterCamera.forward;
            camForward.y = 0;
            targetContainer.transform.position = CenterCamera.position + (camForward * defaultDistance);
            FilterControl.transform.position = CenterCamera.position + (camForward * defaultDistance)*0.8f;
        }
        
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            InitialiseTrial();
        }
        
        if (OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
        {
            UserStudy.instance.UpdateStatus();
            UserStudy.instance.statusText.gameObject.SetActive(!UserStudy.instance.statusText.gameObject.activeSelf);
        }
        
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
        {
            FilterControl.SetActive(!FilterControl.activeSelf);
        }
    }
    
    

    public void ResetTargetContainerPosition()
    {
        if (toggle)
        {
            toggle = false;
            targetContainer.transform.position = CenterCamera.position + (CenterCamera.forward * defaultDistance);
            targetContainer.transform.rotation =
                Quaternion.Euler(new Vector3(0, targetContainer.transform.rotation.eulerAngles.y, 0));
        }
        else
        {
            toggle = true;
        }
    }
    
    
    
    public GameObject prefabToInstantiate;
    
    /// <summary>
    ///     Instantiates prefabs around center splited equality. 
    ///     The number of times indicated in <see cref="howMany" /> var is
    ///     the number of parts will be the circle cuted, with taking as a center the location,
    ///     and adding radius from it
    /// </summary>
    /// <param name="prefab">The object it will be intantiated</param>
    /// <param name="location">The center point of the circle</param>
    /// <param name="howMany">The number of parts the circle will be cut</param>
    /// <param name="amplitude">
    ///     The margin from center, if your center is at (1,1,1) and your radius is 3 
    ///     your final position can be (4,1,1) for example
    /// </param>
    /// <param name="yPosition">The yPostion for the instantiated prefabs</param>
    private void InstantiateInCircle(GameObject prefab, Vector3 location, int howMany, float size, float amplitude, float yPosition)
    {
        float angleSection = -Mathf.PI * 2f / howMany;
        for (int i = 0; i < howMany; i++)
        {
            float angle = i * angleSection;
            Vector3 newPos = location + new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle)) * amplitude;
            newPos.y += yPosition;
            prefab.transform.localScale = new Vector3(size, size, size);
            GameObject target = Instantiate(prefab, newPos, prefab.transform.rotation, targetContainer);
            target.GetComponent<TargetBehaviour>().targetID = i;
            targets.Add(target.GetComponent<TargetBehaviour>());
            TransformerUtils.PositionConstraints constraint= new TransformerUtils.PositionConstraints()
            {
                XAxis = new TransformerUtils.ConstrainedAxis(){ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange(){Min = newPos.x, Max = newPos.x}},
                YAxis = new TransformerUtils.ConstrainedAxis(){ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange(){Min = newPos.y, Max = newPos.y}},
                ZAxis = new TransformerUtils.ConstrainedAxis(){ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange(){Min = newPos.z, Max = newPos.z}}
            };
            target.GetComponent<GrabFreeTransformer>().InjectOptionalPositionConstraints(constraint);
        }
        targetContainer.transform.Rotate(new Vector3(1,0,0), -90);
        targetContainer.transform.Rotate(new Vector3(0,0,1), 90);
    }

    public void InstantiateInCircle(int howMany, float size, float amplitude)
    {
        prefabToInstantiate.SetActive(true);
        InstantiateInCircle(prefabToInstantiate, targetContainer.position, howMany, size,amplitude, 0);
        prefabToInstantiate.SetActive(false);
    }
}
