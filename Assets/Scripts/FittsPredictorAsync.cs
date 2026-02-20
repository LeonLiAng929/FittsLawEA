using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;
using System.Threading.Tasks; // Required for async tasks

public class FittsPredictorAsync : MonoBehaviour
{
    [Header("Model Setup")]
    public ModelAsset onnxModel;
    private Model runtimeModel;
    private Worker worker;

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
    private Vector3 trialStartPosition; // <-- FIXED: Explicitly defined
    private float cumulativeDistance = 0f;
    
    private bool isInitialized = false;
    private bool isPredicting = false;
    private int frameCounter = 0;
    private const int PREDICTION_INTERVAL = 12; 

    void Start()
    {
        runtimeModel = ModelLoader.Load(onnxModel);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);

        if (targetBoard != null && indexFingertip != null)
        {
            // Establish the reference frame for the current trial
            Vector3 currentLocal = targetBoard.InverseTransformPoint(indexFingertip.position);
            previousLocalPos = currentLocal;
            trialStartPosition = currentLocal; 
            isInitialized = true;
        }
    }

    private float GetNormalizedRunningSpeed()
    {
        return (UserStudy.instance.currentSetting[2]/9f); 
    }
    void Update()
    {
        if (!isInitialized) return;

        Vector3 currentLocalPos = targetBoard.InverseTransformPoint(indexFingertip.position);
        
        // Calculate kinematics relative to the start of this specific trial
        Vector3 velocity = (currentLocalPos - previousLocalPos) / Time.deltaTime;
        cumulativeDistance += Vector3.Distance(currentLocalPos, previousLocalPos);
        
        // Normalize the position so the AI sees OFFSET from START, not absolute scene coords
        Vector3 localOffset = currentLocalPos - trialStartPosition;

        float[] currentFrame = new float[FEATURE_COUNT]
        {
            localOffset.x, localOffset.y, localOffset.z, // Feature 1-3: Relative Position
            velocity.x, velocity.y, velocity.z,         // Feature 4-6: Velocity
            cumulativeDistance,                          // Feature 7: Path distance
            0                                         // Feature 8: Normalized Speed Placeholder
        };

        if (frameBuffer.Count >= MAX_FRAMES) frameBuffer.Dequeue();
        frameBuffer.Enqueue(currentFrame);
        previousLocalPos = currentLocalPos;

        // Trigger asynchronous inference to prevent frame stuttering
        frameCounter++;
        if (frameBuffer.Count > 10 && !isPredicting && frameCounter >= PREDICTION_INTERVAL) 
        {
            frameCounter = 0;
            _ = PredictLandingCoordinateAsync(); // Fire and forget async call
        }
    }

    // FIXED: Switched from Coroutine to 'async void' to support Sentis Awaitable
    private async Task PredictLandingCoordinateAsync()
    {
        isPredicting = true;

        float[] flattenedData = new float[MAX_FRAMES * FEATURE_COUNT];
        int i = 0;
        int paddingFrames = MAX_FRAMES - frameBuffer.Count;
        for (int p = 0; p < paddingFrames * FEATURE_COUNT; p++)
        {
            for (int f = 0; f < FEATURE_COUNT; f++) flattenedData[i++] = -10.0f;
        }

        foreach (float[] frame in frameBuffer)
        {
            for (int f = 0; f < FEATURE_COUNT; f++) flattenedData[i++] = frame[f];
        }

        using var inputTensor = new Tensor<float>(new TensorShape(1, MAX_FRAMES, FEATURE_COUNT), flattenedData);
        worker.Schedule(inputTensor);

        var outputTensor = worker.PeekOutput() as Tensor<float>;

        // FIXED: Using C# 'await' directly on the Sentis Awaitable object
        // This prevents the "IsDone" and "WaitForCompletion" errors
        using var downloadedTensor = await outputTensor.ReadbackAndCloneAsync();
        float[] outData = downloadedTensor.DownloadToArray();
        
        //[cite_start]// The prediction is the offset from where the user started the movement [cite: 133, 135, 178]
        Vector3 predictedOffset = new Vector3(outData[0], outData[1], outData[2]);
        Vector3 predictedLocalPos = trialStartPosition + predictedOffset; 

        if (predictedCursor != null)
        {
            Vector3 predictedWorldPos = targetBoard.TransformPoint(predictedLocalPos);
            predictedCursor.position = Vector3.Lerp(predictedCursor.position, predictedWorldPos, Time.deltaTime * 15f);
        }

        isPredicting = false;
    }

    public void ResetTrial()
    {
        frameBuffer.Clear();
        cumulativeDistance = 0f;
        if (targetBoard != null && indexFingertip != null)
        {
            Vector3 currentLocal = targetBoard.InverseTransformPoint(indexFingertip.position);
            previousLocalPos = currentLocal;
            trialStartPosition = currentLocal; // Capture new reference point for next target
        }
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}