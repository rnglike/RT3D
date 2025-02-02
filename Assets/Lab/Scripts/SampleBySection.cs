using System.Collections;
using System.IO;
using UnityEngine;
using Unity.Profiling;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;
using UnityEngine.Profiling;
using System.Text;
using UnityEngine.SceneManagement;

public class SampleBySection : MonoBehaviour
{
    // Public variables
    public GameObject xrOrigin;
    public float fraction = 4.0f;
    public float coldBootTime = 1.0f;
    public float waitTime = 1.0f;
    public Transform[] sections;
    public int currentSection = 0;
    public string experimentName = "Experiment #1";

    // Private variables
    private bool isRotating = false;
    private bool isMoving = false;
    private Transform mainCamera;
    private Transform xrRig;
    private Transform xrControllerLeft;
    private Transform xrControllerRight;
    private StreamWriter csvWriter;
    private string folderPath;

    // Profiling recorders
    private Recorder mainThreadTimeRecorder;
    private Recorder triangleRecorder;
    private Recorder drawCallsRecorder;
    private Recorder verticesRecorder;

    public bool turning;
    public bool hold;
    public int rotated;
    private float angles;

// Method to calculate the average value from a ProfilerRecorder
private static double GetRecorderAverage(ProfilerRecorder recorder)
    {
        int samplesCount = recorder.Capacity;
        if (samplesCount == 0)
            return 0;

        double totalValue = 0;
        unsafe
        {
            var samples = stackalloc ProfilerRecorderSample[samplesCount];
            recorder.CopyTo(samples, samplesCount);
            for (int i = 0; i < samplesCount; ++i)
                totalValue += samples[i].Value;
            totalValue /= samplesCount;
        }

        return totalValue;
    }

    // Unity lifecycle methods
    private void Start()
    {
        InitializeComponents();
        InitializeSections();
        CreateResearchFolder();
        StartCoroutine(WaitForBegin());
    }

    private void Update()
    {
        if (isRotating)
        {
            RotateXrRig();
            LogRotationData();
        }
        if (isMoving)
        {
            MoveToNextSection();
        }
    }

    // Initialization methods
    private void InitializeComponents()
    {
        // References
        mainCamera = Camera.main.transform;
        xrRig = mainCamera.parent;
        xrControllerLeft = xrRig.Find("LeftHand Controller");
        xrControllerRight = xrRig.Find("RightHand Controller");

        // Disable the XR controllers to avoid tracking issues
        mainCamera.GetComponent<TrackedPoseDriver>().enabled = false;
        xrOrigin.GetComponent<XROrigin>().enabled = false;

        // Disable controls
        xrControllerLeft.gameObject.SetActive(false);
        xrControllerRight.gameObject.SetActive(false);

        // Recorders
        mainThreadTimeRecorder = Recorder.Get("Main Thread");
        mainThreadTimeRecorder.enabled = true;
        triangleRecorder = Recorder.Get("Triangles Count");
        triangleRecorder.enabled = true;
        drawCallsRecorder = Recorder.Get("Draw Calls Count");
        drawCallsRecorder.enabled = true;
        verticesRecorder = Recorder.Get("Vertices Count");
        verticesRecorder.enabled = true;
    }

    private void InitializeSections()
    {
        // Get all the sections in the scene
        sections = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            sections[i] = transform.GetChild(i);
        }
        angles = 360.0f / fraction;
    }

    private void CreateResearchFolder()
    {
        // Create a folder to store the research data
        folderPath = Application.isEditor ? Path.Combine(Application.dataPath, "Research/Logs") : Application.persistentDataPath;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    // Logging methods
    private void LogRotationData()
    {
        double framerate = 0.0;
        double cpu = 0.0;
        double vertices = 0.0;
        double tris = 0.0;
        double drawCalls = 0.0;

        if (!drawCallsRecorder.isValid)
        {
            Debug.Log("NOT VALID");
            return;
        }
        // frame rate
        framerate = 1.0f / Time.deltaTime; // s

        // main thread recorder
        cpu = mainThreadTimeRecorder.elapsedNanoseconds * 1e-6; // ms

        // vertices amount
        vertices = verticesRecorder.elapsedNanoseconds / 1000; // k

        // triangles amount
        tris = triangleRecorder.elapsedNanoseconds / 1000; // k

        // draw calls
        drawCalls = drawCallsRecorder.elapsedNanoseconds;

        // Debug.Log($"{framerate} ; {cpu} ; {vertices} ; {tris} ; {drawCalls}");

        string log = FormatLogRecord(
            xrRig.position.x,
            xrRig.position.y,
            xrRig.rotation.eulerAngles.y,
            cpu,
            vertices,
            tris,
            drawCalls,
            framerate
        );

        csvWriter.WriteLine(log);
    }

    private string FormatLogRecord(float x, float y, float rotation, double cpu, double vertices, double tris, double drawCalls, double fps)
    {
        return $"{x.ToString("F6").Replace(',', '.')},{y.ToString("F6").Replace(',', '.')},{rotation.ToString("F6").Replace(',', '.')},{cpu.ToString("F6").Replace(',', '.')},{vertices.ToString("F6").Replace(',', '.')},{tris.ToString("F6").Replace(',', '.')},{drawCalls.ToString("F6").Replace(',', '.')},{fps.ToString("F6").Replace(',', '.')}";
    }

    // Rotation and movement methods
    private void RotateXrRig()
    {
        // Rotate the XR rig
        xrRig.Rotate(0.0f, angles, 0.0f, Space.Self);

        // Check if the rotation is complete
        if (Mathf.Abs(xrRig.rotation.eulerAngles.y - 0.0f) <= angles)
        {
            turning = true;
        }
        else
        {
            turning = false;
        }

        if (turning != hold)
        {
            hold = turning;
            rotated += 1;
        }

        if (rotated == 2)
        {
            rotated = 0;
            xrRig.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            isRotating = false;
            isMoving = true;
        }
    }

    private void MoveToNextSection()
    {
        if (currentSection > sections.Length-1)
        {
            EndExperiment();
            isMoving = false;
            isRotating = false;
            currentSection = 0;
            return;
        }
        // Move the XR rig to the next section
        xrRig.position = Vector3.MoveTowards(xrRig.position, sections[currentSection].position, 0.1f);

        // Check if the movement is complete
        if (Vector3.Distance(xrRig.position, sections[currentSection].position) < 0.1f)
        {
            isMoving = false;
            StartCoroutine(WaitForNextSection());
        }
    }

    // Coroutine methods
    private IEnumerator WaitForBegin()
    {
        yield return new WaitForSeconds(coldBootTime);
        StartExperiment();
    }

    private IEnumerator WaitForNextSection()
    {
        yield return new WaitForSeconds(waitTime);
        currentSection++;
    
        OpenNewCsvWriter();
        isRotating = true;
        isMoving = false;
    }

    // Experiment control methods
    public void StartExperiment()
    {
        isMoving = true;
    }

    public void StopRotation()
    {
        isRotating = false;
        isMoving = true;
    }

    public void EndExperiment()
    {
        mainCamera.GetComponent<TrackedPoseDriver>().enabled = true;
        xrOrigin.GetComponent<XROrigin>().enabled = true;
        xrControllerLeft.gameObject.SetActive(true);
        xrControllerRight.gameObject.SetActive(true);
        csvWriter.Close();
        LoadNextScene();
    }

    private void OpenNewCsvWriter()
    {
        csvWriter?.Close();
        csvWriter = new StreamWriter(Path.Combine(folderPath, $"rotation_{experimentName}_{currentSection}.csv"));
        csvWriter.WriteLine("X,Y,Rotation,Main Thread,Vertices,Triangles,Draw Calls,FPS");
    }

    private void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int totalScenesInBuild = SceneManager.sceneCountInBuildSettings;
        int nextSceneIndex = currentSceneIndex+1;
        if (nextSceneIndex < totalScenesInBuild)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}
