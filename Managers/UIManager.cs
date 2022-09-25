using Cinemachine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject exitUI;

    [SerializeField] private float playerYOffset;

    [SerializeField] private CinemachineFreeLook carChaseVCam;
    [SerializeField] private CinemachineVirtualCamera trackViewVCam;

    [SerializeField] private TMP_Text handbrakeText;

    private OrientatedPoint carSpawnPoint;

    [SerializeField] private Mesh2D basicRoad;
    [SerializeField] private Mesh2D curbRoad;
    [SerializeField] private Mesh2D wallRoad;

    private int initialPointsStart;
    private float maxDisplacementStart;
    private float heightScaleStart;
    private float noiseDetailStart;


    private void Start()
    {
        gameUI.SetActive(true);
        settingsUI.SetActive(false);
        exitUI.SetActive(false);

        initialPointsStart = PointGenerator.Instance.InitialPoints;
        maxDisplacementStart = PointGenerator.Instance.MaxDisplacement;
        heightScaleStart = MeshGenerator.Instance.HeightScale;
        noiseDetailStart = MeshGenerator.Instance.NoiseDetail;

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

    //--------------------------------------------- GAME UI ------------------------------------------------------

    // Generates and visualises a new track
    public void MakeTrack()
    {
        float startTime = Time.realtimeSinceStartup;
        
        List<Vector3> trackPoints = PointGenerator.Instance.Generate();
        
        List<Segment> spline = SplineGenerator.Instance.Generate(trackPoints);
        
        carSpawnPoint = MeshGenerator.Instance.Generate(spline);

        Debug.Log("Generation Time: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + " milliseconds");

        SetCarAndCamera();
    }

    // Switches to the track view cam
    public void ViewTrack()
    {
        SetCameraRotation();

        carChaseVCam.Priority = 0;
        trackViewVCam.Priority = 1;

        DrivingController.Instance.Handbrake = true;
    }

    // Switches to the car chase cam
    public void Drive()
    {
        carChaseVCam.Priority = 1;
        trackViewVCam.Priority = 0;
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
    public void BasicRoadSelect(bool selected)
    {
        if (selected)
        {
            MeshGenerator.Instance.CrossSectionMesh = basicRoad;
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
