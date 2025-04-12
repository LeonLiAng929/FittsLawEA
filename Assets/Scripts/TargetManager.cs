
using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit.SceneDecorator;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TargetManager : MonoBehaviour
{
    public bool toggle=true;
    public static TargetManager Instance;
    public float timer = 0;
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
    public bool hasTargetActivated = false;

    /// <summary> Starts this object. </summary>
    protected void Start()
    {
        HorizontalRotation = Quaternion.AngleAxis(defaultRotation.y, Vector3.right);
        HorizontalRotationInverse = Quaternion.Inverse(HorizontalRotation);
        VerticalRotation = Quaternion.AngleAxis(defaultRotation.x, Vector3.up);
        VerticalRotationInverse = Quaternion.Inverse(VerticalRotation);
        CenterCamera = cameraRig.centerEyeAnchor;
        InitialiseTargets();
        Instance = this;
        toggle=true;
        this.InstantiateInCircle(this.prefabToInstantiate, new Vector3(0, 0, 0), 11, 0.2f, 2.1f);
        prefabToInstantiate.SetActive(false);
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
        List<TargetBehaviour> targets = new List<TargetBehaviour>(targetContainer.GetComponentsInChildren<TargetBehaviour>());
        int randomIndex = Random.Range(0, targets.Count);
        targets[randomIndex].intendedTarget = true;
        targets[randomIndex].OnTargetSelect();
        hasTargetActivated = true;
        timer = 0;
    }

    private void FixedUpdate()
    {
        if (!hasTargetActivated)
        {
            
            ActivateRandomTarget();
                
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
    /// <param name="radius">
    ///     The margin from center, if your center is at (1,1,1) and your radius is 3 
    ///     your final position can be (4,1,1) for example
    /// </param>
    /// <param name="yPosition">The yPostion for the instantiated prefabs</param>
    public void InstantiateInCircle(GameObject prefab, Vector3 location, int howMany, float radius, float yPosition)
    {
        float angleSection = Mathf.PI * 2f / howMany;
        for (int i = 0; i < howMany; i++)
        {
            float angle = i * angleSection;
            Vector3 newPos = location + new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            newPos.y += yPosition;
            GameObject target = Instantiate(prefab, newPos, prefab.transform.rotation, targetContainer);
            TransformerUtils.PositionConstraints constraint= new TransformerUtils.PositionConstraints()
            {
                XAxis = new TransformerUtils.ConstrainedAxis(){ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange(){Min = newPos.x, Max = newPos.x}},
                YAxis = new TransformerUtils.ConstrainedAxis(){ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange(){Min = newPos.y, Max = newPos.y}},
                ZAxis = new TransformerUtils.ConstrainedAxis(){ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange(){Min = newPos.z, Max = newPos.z}}
            };
            target.GetComponent<GrabFreeTransformer>().InjectOptionalPositionConstraints(constraint);
        }
    }
    public void InstantiateInCircle(GameObject prefab, Vector3 location, int howMany, float radius)
    {
        this.InstantiateInCircle(prefab, location, howMany, radius, location.y);
    }
    public void InstantiateInCircle(GameObject prefab, int howMany, float radius)
    {
        this.InstantiateInCircle(prefab, this.transform.position, howMany, radius);
    }
}
