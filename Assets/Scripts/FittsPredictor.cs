using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;

public class FittsPredictor : MonoBehaviour
{
    [Header("Scene References")]
    public ModelAsset modelAsset;
    public Transform targetBoard;
    public Vector3 CurrentPredictionLocal;
    // DRAG YOUR "FILTERED TRANSFORM" HERE (The output of your other script)
    public Transform fingertip;        
    
    public Transform predictionDot;

    [Header("Simulation Parameters")]
    [Range(0, 9.0f)]
    public float walkingSpeed = 0.0f;

    public static FittsPredictor Instance;
    // --- Sentis Objects ---
    private Model runtimeModel;
    private Worker worker;
    private TensorShape inputShape = new TensorShape(1, 150, 4);

    // Ring Buffer
    private LinkedList<float[]> historyBuffer = new LinkedList<float[]>();

    void Start()
    {
        if (modelAsset == null) { Debug.LogError("❌ Model Asset missing!"); return; }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);

        for (int i = 0; i < 150; i++) 
            historyBuffer.AddLast(new float[] { 0, 0, 0, walkingSpeed });
        Instance = this;
    }

    void Update()
    {
        if (targetBoard == null || fingertip == null) return;

        // --- STEP 1: READ DATA (No Smoothing) ---
        // We assume 'fingertip' is ALREADY smoothed by your FilteringOneEuro script
        Vector3 currentLocalPos = GetNormalizedPosition();
        
        float[] newFrame = new float[] { 
            currentLocalPos.x, 
            currentLocalPos.y, 
            currentLocalPos.z, 
            walkingSpeed 
        };

        historyBuffer.RemoveFirst();
        historyBuffer.AddLast(newFrame);

        // --- STEP 2: RUN INFERENCE ---
        RunInference();
    }

    void RunInference()
    {
        float[] flatInput = new float[150 * 4];
        int idx = 0;
        foreach (var frame in historyBuffer)
        {
            flatInput[idx++] = frame[0];
            flatInput[idx++] = frame[1];
            flatInput[idx++] = frame[2];
            flatInput[idx++] = frame[3];
        }

        using (Tensor<float> inputTensor = new Tensor<float>(inputShape, flatInput))
        {
            worker.Schedule(inputTensor);
            using (Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>)
            {
                float[] result = outputTensor.DownloadToArray();
            
                // STORE THE RESULT PUBLICLY
                CurrentPredictionLocal = new Vector3(result[0], result[1], result[2]);

                // Optional: Keep visualizer on PC for debugging
                if (targetBoard != null && predictionDot != null)
                {
                    predictionDot.position = targetBoard.TransformPoint(CurrentPredictionLocal);
                }
            }
        }
    }

    Vector3 GetNormalizedPosition()
    {
        return targetBoard.InverseTransformPoint(fingertip.position);
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}