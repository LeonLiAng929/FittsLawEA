using System;
using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    public int targetID;
    public bool isSelected = false;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "touchtip")
        {
            isSelected = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "touchtip")
        {
            isSelected = false;
        }
    }

    public void OnTargetSelect()
    {
        GetComponent<MeshRenderer>().material.color = Color.green;
    }

    public void OnTargetDeselect()
    {
        //LeanTween.color(this.gameObject, Color.white, 0.5f);
        GetComponent<MeshRenderer>().material.color = Color.white;
    }
}

