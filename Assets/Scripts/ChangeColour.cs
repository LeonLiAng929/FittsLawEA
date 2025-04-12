using UnityEngine;

public class ChangeColour : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.GetComponent<MeshRenderer>().material.color = Color.blue;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnTargetSelect()
    {
        if(TargetManager.Instance.toggle)
            this.GetComponent<MeshRenderer>().material.color = Color.blue;
        else
        {
            this.GetComponent<MeshRenderer>().material.color = Color.grey;
        }
    }
}
