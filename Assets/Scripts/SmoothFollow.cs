using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        
    }
    
/// <summary> The anchor. </summary>
    [Header("Window Settings")]
    [SerializeField, Tooltip("What part of the view port to anchor the object to.")]
    protected TextAnchor Anchor = TextAnchor.LowerCenter;
    /// <summary> The follow speed. </summary>
    [SerializeField, Range(0.0f, 100.0f), Tooltip("How quickly to interpolate the object towards its target position and rotation.")]
    protected float FollowSpeed;
    [SerializeField, Tooltip("If ticked, the object will follow the target and always stay in sight, else it will follow the target but may" +
        "be out of sight when head rotates")]
    public bool AlwaysStayInSight = true;
    /// <summary> The default distance. </summary>
    public float defaultDistance;

    public float defaultHeight;

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

    protected bool init = true;

    [SerializeField]
    protected OVRCameraRig cameraRig;
    
    private Transform CenterCamera;

    /// <summary> Starts this object. </summary>
    protected void Start()
    {
        HorizontalRotation = Quaternion.AngleAxis(defaultRotation.y, Vector3.right);
        HorizontalRotationInverse = Quaternion.Inverse(HorizontalRotation);
        VerticalRotation = Quaternion.AngleAxis(defaultRotation.x, Vector3.up);
        VerticalRotationInverse = Quaternion.Inverse(VerticalRotation);
        CenterCamera = cameraRig.centerEyeAnchor;
        //defaultHeight = CalculatePosition(CenterCamera).y;

        //defaultDistance = Vector3.Distance(transform.position, CenterCamera.position);
    }

    public Vector2 GetOffset()
    {
        return Offset;
    }

    /// <summary> Late update. </summary>
    protected virtual void LateUpdate()
    {
        if (CenterCamera == null)
        {
            return;
        }
        float t = Time.deltaTime * FollowSpeed;
        transform.position = Vector3.Lerp(transform.position, CalculatePosition(CenterCamera), t);

        transform.rotation = Quaternion.Slerp(transform.rotation, CalculateRotation(CenterCamera), t);
        if (!AlwaysStayInSight)
        {
            transform.position = new Vector3(transform.position.x, defaultHeight, transform.position.z);
            //transform.position.z = defaultDistance * Mathf.Cos(Mathf.Deg2Rad*6f);
            transform.rotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0));
        }
    }

    /// <summary> Calculates the position. </summary>
    /// <param name="cameraTransform"> The camera transform.</param>
    /// <returns> The calculated position. </returns>
    protected Vector3 CalculatePosition(Transform cameraTransform)
    {
        Vector3 position = cameraTransform.position + (cameraTransform.forward * defaultDistance);
        Vector3 horizontalOffset = cameraTransform.right * Offset.x;
        Vector3 verticalOffset = cameraTransform.up * Offset.y;

        switch (Anchor)
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

        if (AlwaysStayInSight)
        {
            //position = cameraTransform.position + (cameraTransform.forward * (defaultDistance * Mathf.Cos(Mathf.Deg2Rad * 20f)));
            //position.y += Mathf.Sin(Mathf.Deg2Rad * 10f) * defaultDistance;
        }
        return position;
    }

    /// <summary> Calculates the rotation. </summary>
    /// <param name="cameraTransform"> The camera transform.</param>
    /// <returns> The calculated rotation. </returns>
    protected Quaternion CalculateRotation(Transform cameraTransform)
    {
        Quaternion rotation = cameraTransform.rotation;

        switch (Anchor)
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
}
