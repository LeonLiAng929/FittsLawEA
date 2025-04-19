using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class 
    UserStudy : MonoBehaviour
{
    public static UserStudy instance;
    public int currentID;
    public float[] currentSetting;
    public Dictionary<int, List<int>> userStudySettings;
    [FormerlySerializedAs("currentSetting")] public int currentSettingIndex;
    public bool trainingStart = false;
    public int currentConditionIndex; // 0 - 27, if reaches 27, increment currentID and reset currentConditionIndex to 0
    public TMP_Text statusText;
    public List<float[]> testingCombinations; // for each float[], float[0] is the size, float[1] is the distance, float[2] is the speed
    //public bool training = true;
    public Mode currMode = Mode.Regular;
    public enum Mode
    {
        Regular,
        Training
    }
    //[SerializeField]
    //public List<UserStudyButtons> userStudyButtons;
    private void Awake()
    {
        instance = this;
    }
    
    
    // Start is called before the first frame update
    void Start()
    {
        //currentID = 1;
        LoadTestingCombinations();
        LoadStudySettings();
        LoadCurrentParticipantRecord();
        LoadCurrentSettings();
        UpdateStatus();
        //currentSettingIndex = userStudySettings[currentID][currentConditionIndex];
        
        //Invoke(nameof(SetTargetSpeedPerHourToNine),3f);
        //SetTargetSpeedPerHourToNine();
        //Invoke(nameof(BeginStudy), 3f);
        //Invoke(nameof(BeginUserStudy), 3f);
    }

    // Update is called once per frame
    void Update()
    {
    }


    public void LoadCurrentParticipantRecord()
    {
        string fname = "ParticipantRecord.csv";
        string path = Path.Combine(Application.persistentDataPath, fname);

        using (var reader = new System.IO.StreamReader(path))
        {
            reader.ReadLine();
            string[] line = new string[]{};
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Split(',');
            }
            currentID = int.Parse(line[0]);
            currentConditionIndex = int.Parse(line[1]);
            Mode.TryParse(line[2], out currMode);
        }
    }
    
    
    public void SaveCurrentParticipantRecord()
    {
        string fname = "ParticipantRecord.csv";
        string path = Path.Combine(Application.persistentDataPath, fname);

        //using (var writer = new StreamWriter(path, false))
        using (var writer = new StreamWriter(path, true))
        {
            //writer.WriteLine("currID,currConditionIndex");
            if (currentConditionIndex == 26)
            {
                currentConditionIndex = 0;
                writer.WriteLine($"{currentID+1},{currentConditionIndex},{currMode}");
            }
            else
            {
                writer.WriteLine($"{currentID},{currentConditionIndex+1},{currMode}");
            }
        }
    }
    /// <summary>
    /// Load and parse the study settings from a csv file. The first line of the csv file is the header. Its format is:
    /// ID, Condition1, Condition2, Condition3, Condition4. The contents should be parsed into the dictionary userStudySettings.
    /// The key of the dictionary is the ID, and the value is a list comprising of the conditions.
    /// </summary>
    public void LoadStudySettings()
    {
        userStudySettings = new Dictionary<int, List<int>>();
        string fname = "UserStudySchedule.csv";
        string path = Path.Combine(Application.persistentDataPath, fname);
        
        using (var reader = new System.IO.StreamReader(path))
        {
            reader.ReadLine(); // skip the header line
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                int id = int.Parse(values[0]);
                var conditions = new List<int>();
                for (int i = 1; i < values.Length; i++)
                {
                    conditions.Add(int.Parse(values[i]));
                }
                userStudySettings.Add(id, conditions);
                
            }
        } 
    }

    public void LoadTestingCombinations()
    {
        string fname = "Combinations.csv";
        string path = Path.Combine(Application.persistentDataPath, fname);
        testingCombinations = new List<float[]>();
        using (var reader = new System.IO.StreamReader(path))
        {
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                // parse strings into floats in values
                var settings = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    settings[i] = float.Parse(values[i]);
                }
                testingCombinations.Add(settings);
                
            }
        }
    }
    
    public void UpdateStatus()
    {
        var currentSetting = testingCombinations[currentSettingIndex];
        statusText.text = string.Format("Current ID: {0}, Size: {1}, Distance: {2}, Speed: {3}, Mode: {4}", currentID, currentSetting[0], currentSetting[1], currentSetting[2],currMode.ToString());
    }
    
    public void PrepareStudy()
    {
        currentSetting = testingCombinations[currentSettingIndex];
        TargetManager.Instance.InstantiateInCircle(11, currentSetting[0], currentSetting[1]/2);
    }
    

    private void TrainingStart()
    {
        trainingStart = true;
    }
    public void LoadCurrentSettings()
    {
        /*List<AnchorType> conditions = userStudySettings[currentID];
        for (int i = 0; i < userStudyButtons.Count;i++)
        {
            userStudyButtons[i].anchorType = conditions[i];
        }*/
        currentSettingIndex = userStudySettings[currentID][currentConditionIndex]-1;
        UpdateStatus();
    }

    /// <summary>
    ///  Write the study result found in RawDataManager.instance.historicalSpeedPerHour to a csv file. The first line of the csv file is the header. Its format is:
    ///  KPH. 
    ///  The name of the csv file should be the ID of the current user + the current setting.
    /// </summary>
    public void WriteStudyResult()
    {
        if (currMode != Mode.Training)
        {
            float size = currentSetting[0];
            float distance = currentSetting[1];
            float speed = currentSetting[2];
            float indexOfDifficulty = Mathf.Log((distance / size) + 1, 2);
            List<float> movementTime = TargetManager.Instance.movementTime;
            List<float> timestamp = TargetManager.Instance.timestamp;
            List<Vector3> targetPositions = TargetManager.Instance.targetPositions;
            List<Vector3> selectionPositions = TargetManager.Instance.selectionPositions;
            List<Quaternion> selectionQuaternions = TargetManager.Instance.selectionQuaternions;
            List<bool> successfulSelection = TargetManager.Instance.successfulSelection;

            // Create a new CSV file with the name as the ID of the current user + the current setting
            string fileName = currentID.ToString() + ".csv";

            string path = Path.Combine(Application.persistentDataPath, fileName);
            
            FileInfo fileInfo = new FileInfo(path);
            using (var writer = new StreamWriter(path, true))
            {
                if (fileInfo.Length == 0)
                {
                    writer.WriteLine(
                        "UID,Size,Distance,Speed,IndexOfDifficulty,Timestamp,MovementTime,TargetPositionX,TargetPositionY,TargetPositionZ," +
                        "SelectionPositionX,SelectionPositionY,SelectionPositionZ," +
                        "SelectionQuaternionX,SelectionQuaternionY,SelectionQuaternionZ," +
                        "SelectionQuaternionW,SuccessfulSelection");
                }
                //writer.WriteLine("UID,Size,Distance, TargetSpeed,ActualSpeed,Distance,#ofGaze,GazeDwellingTime,RawX,RawY,RawZ,RawRotX,RawRotY,RawRotZ,RawRotW");
                //Debug.Log(movementTime.Count);
                for (int i = 0; i < movementTime.Count; i++)
                {
                    
                    try
                    {
                        writer.WriteLine(
                            $"{currentID.ToString()},{size},{distance},{speed},{indexOfDifficulty},{timestamp[i]},{movementTime[i]},{targetPositions[i].x},{targetPositions[i].y},{targetPositions[i].z}," +
                            $"{selectionPositions[i].x},{selectionPositions[i].y},{selectionPositions[i].z}," +
                            $"{selectionQuaternions[i].x},{selectionQuaternions[i].y},{selectionQuaternions[i].z}," +
                            $"{selectionQuaternions[i].w},{successfulSelection[i]}");
                    }
                    catch (ArgumentOutOfRangeException)
                    {

                    }

                }
            }
        }
    }

}