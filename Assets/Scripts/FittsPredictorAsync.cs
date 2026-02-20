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
    public Transform predictedCursor;

    // Network configuration
    private const int MAX_FRAMES = 150; 
    private const int FEATURE_COUNT = 8;
    
    private Queue<float[]> frameBuffer = new Queue<float[]>();
    private Vector3 previousLocalPos;
    private Vector3 trialStartPosition;
    private float cumulativeDistance = 0f;
    
    private bool isInitialized = false;
    private int frameCounter = 0;
    private const int PREDICTION_INTERVAL = 5; // Run inference every 5 frames

    void Start()
    {
        runtimeModel = ModelLoader.Load(onnxModel);
        
        // CRITICAL FIX: Use the CPU backend. 
        // Bypasses Android/Vulkan GPU readback deadlocks on the headset.
        // Sentis uses Unity Burst, making this blazingly fast for small LSTMs.
        worker = new Worker(runtimeModel, BackendType.CPU);

        if (targetBoard != null && indexFingertip != null)
        {
            Vector3 currentLocal = targetBoard.InverseTransformPoint(indexFingertip.position);
            previousLocalPos = currentLocal;
            trialStartPosition = currentLocal; 
            isInitialized = true;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        Vector3 currentLocalPos = targetBoard.InverseTransformPoint(indexFingertip.position);
        
        Vector3 velocity = (currentLocalPos - previousLocalPos) / Time.deltaTime;
        cumulativeDistance += Vector3.Distance(currentLocalPos, previousLocalPos);
        
        // Offset from the trial's starting position
        Vector3 localOffset = currentLocalPos - trialStartPosition;

        float[] currentFrame = new float[FEATURE_COUNT]
        {
            localOffset.x, localOffset.y, localOffset.z, 
            velocity.x, velocity.y, velocity.z,         
            cumulativeDistance,                          
            0.5f // Placeholder for normalized speed                                         
        };

        if (frameBuffer.Count >= MAX_FRAMES) frameBuffer.Dequeue();
        frameBuffer.Enqueue(currentFrame);
        previousLocalPos = currentLocalPos;

        frameCounter++;
        if (frameBuffer.Count > 10 && frameCounter >= PREDICTION_INTERVAL) 
        {
            frameCounter = 0;
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

        // Memory Safe Execution: 'using' blocks guarantee tensors are flushed from RAM
        // immediately after inference, preventing Quest out-of-memory crashes.
        using (var inputTensor = new Tensor<float>(new TensorShape(1, MAX_FRAMES, FEATURE_COUNT), flattenedData))
        {
            worker.Schedule(inputTensor);

            using (var outputTensor = worker.PeekOutput() as Tensor<float>)
            {
                // Synchronous readback is safe and instant on the CPU backend
                float[] outData = outputTensor.DownloadToArray();
                
                Vector3 predictedOffset = new Vector3(outData[0], outData[1], outData[2]);
                Vector3 predictedLocalPos = trialStartPosition + predictedOffset; 

                if (predictedCursor != null)
                {
                    Vector3 predictedWorldPos = targetBoard.TransformPoint(predictedLocalPos);
                    predictedCursor.position = Vector3.Lerp(predictedCursor.position, predictedWorldPos, Time.deltaTime * 15f);
                }
            }
        }
    }

    public void ResetTrial()
    {
        frameBuffer.Clear();
        cumulativeDistance = 0f;
        if (targetBoard != null && indexFingertip != null)
        {
            Vector3 currentLocal = targetBoard.InverseTransformPoint(indexFingertip.position);
            previousLocalPos = currentLocal;
            trialStartPosition = currentLocal;
        }
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}