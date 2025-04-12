using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    public bool intendedTarget = false;
    /// <summary> The anchor. </summary>
    [Header("Window Settings")]
    [SerializeField, Tooltip("What part of the view port to anchor the object to.")]
    public TextAnchor Anchor = TextAnchor.LowerCenter;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnTargetSelect()
    {
        if(intendedTarget)
            this.GetComponent<MeshRenderer>().material.color = Color.green;
        else
            this.GetComponent<MeshRenderer>().material.color = Color.red;
    }

    public void OnTargetDeselect()
    {
        LeanTween.color(this.gameObject, Color.white, 0.5f);
        if (intendedTarget)
        {
            TargetManager.Instance.hasTargetActivated = false;
            intendedTarget = false;
        }
    }
}

