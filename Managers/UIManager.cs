using Cinemachine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject exitUI;

    [SerializeField] private float playerYOffset;

    [SerializeField] private CinemachineFreeLook carChaseVCam;
    [SerializeField] private CinemachineVirtualCamera trackViewVCam;

    [SerializeField] private TMP_Text handbrakeText;
    [SerializeField] private TMP_Text timeText;

    private OrientatedPoint carSpawnPoint;

    [SerializeField] private Mesh2D normalRoad;
    [SerializeField] private Mesh2D curbRoad;
    [SerializeField] private Mesh2D wallRoad;

    [SerializeField] private Texture2D pointerTexture;
    [SerializeField] private Texture2D handTexture;

    [SerializeField] private Vector2 cursorHotspot;

    private void Start()
    {
        settingsUI.SetActive(false);
        exitUI.SetActive(false);

        MakeTrack();
    }

    private void OnEnable()
    {
        InputManager.exitEvent += ToggleExit;
    }

    private void OnDisable()
    {
        InputManager.exitEvent -= ToggleExit;
    }

    private void Update()
    {
        handbrakeText.text = "Handbrake " + (DrivingController.Instance.Handbrake ? "On" : "Off");
    }

    // Deselects the currently selected UI element in the event system, fixes visuals of buttons
    public void DeselectUIObject()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    // Sets the cursor to a hand
    public void OnCursorEnter()
    {
        Cursor.SetCursor(handTexture, cursorHotspot, CursorMode.Auto);
    }

    // Sets the cursor to a pointer
    public void OnCursorExit()
    {
        Cursor.SetCursor(pointerTexture, cursorHotspot, CursorMode.Auto);
    }

    //--------------------------------------------- GAME UI ------------------------------------------------------

    // Generates and visualises a new track
    public void MakeTrack()
    {
        float startTime = Time.realtimeSinceStartup;
        
        List<Vector3> trackPoints = PointGenerator.Instance.Generate();
        
        List<Segment> spline = SplineGenerator.Instance.Generate(trackPoints);
        
        carSpawnPoint = MeshGenerator.Instance.Generate(spline);

        float generationTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        timeText.text = "Generated in " + generationTime.ToString("F2") + " milliseconds";

        SetCarAndCamera();

        DeselectUIObject();
    }

    // Switches to the track view cam
    public void ViewTrack()
    {
        SetCameraRotation();

        carChaseVCam.Priority = 0;
        trackViewVCam.Priority = 1;

        DrivingController.Instance.Handbrake = true;

        DeselectUIObject();
    }

    // Switches to the car chase cam
    public void Drive()
    {
        carChaseVCam.Priority = 1;
        trackViewVCam.Priority = 0;

        DeselectUIObject();
    }

    // Sets the car transform and camera rotation 
    public void SetCarAndCamera()
    {
        SetCarTransform();
        SetCameraRotation();
    }

    private void SetCarTransform()
    {
        DrivingController.Instance.UpdateTransform(carSpawnPoint.Position + new Vector3(0, playerYOffset, 0), carSpawnPoint.Rotation);
    }

    private void SetCameraRotation()
    {
        float viewCamZRot = -carSpawnPoint.Rotation.eulerAngles.y;
        trackViewVCam.transform.rotation = Quaternion.Euler(new Vector3(90, 0, viewCamZRot));
    }

    //--------------------------------------------- EXIT SCREEN UI ------------------------------------------------------

    // Toggle the visibility of the exit screen and apply handbrake
    public void ToggleExit()
    {
        exitUI.SetActive(!exitUI.activeSelf);

        DrivingController.Instance.Handbrake = true;
    }

    // Quit the application
    public void ExitGame()
    {
        Application.Quit();
    }

    //--------------------------------------------- SETTINGS UI ------------------------------------------------------

    // Toggle the visibility of the settings screen
    public void ToggleSettings()
    {
        settingsUI.SetActive(!settingsUI.activeSelf);

        DrivingController.Instance.Handbrake = true;
    }

    // Set generator values by sliders
    public void InitialPointsChange(float value) => PointGenerator.Instance.InitialPoints = (int)value;    

    public void MidpointDisplacementChange(float value) => PointGenerator.Instance.MaxDisplacement = value;    

    public void HeightScaleChange(float value) => MeshGenerator.Instance.HeightScale = value;

    public void NoiseDetailChange(float value) => MeshGenerator.Instance.NoiseDetail = value;


    // Mesh settings
    public void NormalRoadSelect(bool selected)
    {
        if (selected)
        {
            MeshGenerator.Instance.CrossSectionMesh = normalRoad;
        }
    }

    public void CurbRoadSelect(bool selected)
    {
        if (selected)
        {
            MeshGenerator.Instance.CrossSectionMesh = curbRoad;
        }
    }

    public void WallRoadSelect(bool selected)
    {
        if (selected)
        {
            MeshGenerator.Instance.CrossSectionMesh = wallRoad;
        }
    }
}
