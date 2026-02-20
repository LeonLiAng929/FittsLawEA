using UnityEngine;
using Unity.Sentis; // Note: In the newest Unity 6 versions, this might be Unity.InferenceEngine
using System.Collections.Generic;

public class FittsPredictor : MonoBehaviour
{
    [Header("Model Setup")]
    public ModelAsset onnxModel;
    private Model runtimeModel;
    private Worker worker;
    public static FittsPredictor Instance { get; set; }

     void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    [Header("Tracking References")]
    public Transform targetBoard;
    public Transform indexFingertip;

    [Header("Output Visualization")]
    public Transform predictedCursor;

    // Network configuration
    private const int MAX_FRAMES = 150; 
    private const int FEATURE_COUNT = 8;
    
    private Queue<float[]> frameBuffer = new Queue<float[]>();
    private Vector3 previousLocalPos;
    private float cumulativeDistance = 0f;
    private bool isInitialized = false;
    
    
    private bool isPredicting = false;
    private int frameCounter = 0;
    private const int PREDICTION_INTERVAL = 5; // Run inference every 5 frames

    void Start()
    {
        // Load the unrolled ONNX model
        runtimeModel = ModelLoader.Load(onnxModel);
        
        worker = new Worker(runtimeModel, BackendType.GPUCompute);

        if (targetBoard != null && indexFingertip != null)
        {
            previousLocalPos = targetBoard.InverseTransformPoint(indexFingertip.position);
            isInitialized = true;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 1. Convert global fingertip position to local board space
        Vector3 currentLocalPos = targetBoard.InverseTransformPoint(indexFingertip.position);
        
        // 2. Calculate Kinematics
        Vector3 velocity = (currentLocalPos - previousLocalPos) / Time.deltaTime;
        cumulativeDistance += Vector3.Distance(currentLocalPos, previousLocalPos);
        
        // 3. Get context
        float normalizedRunningSpeed = GetNormalizedRunningSpeed(); 

        // 4. Construct the 8-feature array for this frame
        float[] currentFrame = new float[FEATURE_COUNT]
        {
            currentLocalPos.x, currentLocalPos.y, currentLocalPos.z,
            velocity.x, velocity.y, velocity.z,
            cumulativeDistance,
            normalizedRunningSpeed
        };

        // 5. Update the sliding window
        if (frameBuffer.Count >= MAX_FRAMES) 
        {
            frameBuffer.Dequeue();
        }
        frameBuffer.Enqueue(currentFrame);
        previousLocalPos = currentLocalPos;

        // 6. Run inference
        if (frameBuffer.Count > 10) 
        {
            PredictLandingCoordinate();
        }
        
        
    }

    private void PredictLandingCoordinate()
    {
        float[] flattenedData = new float[MAX_FRAMES * FEATURE_COUNT];
        int i = 0;
        
        int paddingFrames = MAX_FRAMES - frameBuffer.Count;
        for (int p = 0; p < paddingFrames * FEATURE_COUNT; p++)
        {
            flattenedData[i++] = -10.0f;
        }

        foreach (float[] frame in frameBuffer)
        {
            for (int f = 0; f < FEATURE_COUNT; f++) 
            {
                flattenedData[i++] = frame[f];
            }
        }

        TensorShape shape = new TensorShape(1, MAX_FRAMES, FEATURE_COUNT);
        
        // <-- API Update: TensorFloat is now Tensor<float>
        using Tensor<float> inputTensor = new Tensor<float>(shape, flattenedData);

        // <-- API Update: Execute is now Schedule
        worker.Schedule(inputTensor);

        // Extract the prediction
        using Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
        
        // <-- API Update: MakeReadable is removed. DownloadToArray forces a GPU-to-CPU copy
        float[] outData = outputTensor.DownloadToArray();
        
        // The network outputs in local coordinates
        Vector3 predictedLocalPos = new Vector3(outData[0], outData[1], outData[2]);

        // Convert back to Global World Space
        Vector3 predictedWorldPos = targetBoard.TransformPoint(predictedLocalPos);

        // Move the visualization sphere to the predicted spot
        if (predictedCursor != null)
        {
            predictedCursor.position = Vector3.Lerp(predictedCursor.position, predictedWorldPos, Time.deltaTime * 15f);
        }
    }

    private float GetNormalizedRunningSpeed()
    {
        return (UserStudy.instance.currentSetting[2]/9f); 
    }

    public void ResetTrial()
    {
        frameBuffer.Clear();
        cumulativeDistance = 0f;
        if (targetBoard != null && indexFingertip != null)
        {
            previousLocalPos = targetBoard.InverseTransformPoint(indexFingertip.position);
        }
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}
