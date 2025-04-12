using UnityEngine;

public class RayControl : MonoBehaviour
{
    public LineRenderer lineRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3[] positions = new Vector3[2];
        positions[0] = transform.position;
        positions[1] = transform.position + transform.forward * 20;
        lineRenderer.SetPositions(positions);
    }
}
