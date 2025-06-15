using UnityEngine;

public class LogPos : MonoBehaviour
{
    public Transform cube;

    public TMPro.TMP_Text text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
 
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Position: " + cube.position.ToString("F2") + "\n";
    }
}
