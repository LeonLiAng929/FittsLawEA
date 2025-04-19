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

    private bool WithinBoundary(Vector3 touchTip, Vector3 targetCentrioid, float targetSize)
    {
        float distance = Vector3.Distance(touchTip, targetCentrioid);
        if (distance <= targetSize) 
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "touchtip")
        {
            if (this.gameObject.name == "missSelectionArea")
            {
                if (!TargetManager.Instance.trialEnded)
                {
                    TargetManager.Instance.selectionPositions.Add(TargetManager.Instance.touchTip.transform.position);

                    if (WithinBoundary(other.transform.position, TargetManager.Instance
                            .targets[TargetManager.Instance.currentTarget].transform.position, TargetManager.Instance
                            .targets[TargetManager.Instance.currentTarget].transform.localScale.x))
                    {
                        TargetManager.Instance.successfulSelection.Add(true);
                    }
                    else
                    {
                        TargetManager.Instance.successfulSelection.Add(false);
                    }

                    TargetManager.Instance.selectionQuaternions.Add(TargetManager.Instance.touchTip.transform.rotation);
                    TargetManager.Instance.timestamp.Add(TargetManager.Instance.cumulativeTime);
                    TargetManager.Instance.ProceedTrial();
                    if (!TargetManager.Instance.trialStarted)
                    {
                        TargetManager.Instance.trialStarted = true;
                    }
                    else
                    {
                        TargetManager.Instance.movementTime.Add(TargetManager.Instance.timer);
                        TargetManager.Instance.timer = 0;
                    }

                    if (TargetManager.Instance.trialEnded)
                    {
                        UserStudy.instance.SaveCurrentParticipantRecord();
                        UserStudy.instance.WriteStudyResult();
                    }

                }
            }
        }
    }

    /*private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "touchtip")
        {
            isSelected = false;

        }
    }*/

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

