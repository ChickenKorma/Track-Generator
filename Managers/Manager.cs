using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cinemachine;

public class Manager : MonoBehaviour
{
    private PointGenerator pointGenerator;
    private SplineGenerator splineGenerator;
    private MeshGenerator meshGenerator;

    [SerializeField] private Transform player;

    [SerializeField] private float playerYOffset;

    [SerializeField] private CinemachineFreeLook carChaseVCam;
    [SerializeField] private CinemachineVirtualCamera trackViewVCam;

    [SerializeField] private TMP_Text handbrakeText;

    private OrientatedPoint carSpawnPoint;

    private void Awake()
    {
        pointGenerator = GetComponent<PointGenerator>();
        splineGenerator = GetComponent<SplineGenerator>();
        meshGenerator = GetComponent<MeshGenerator>();
    }

    private void Start()
    {
        MakeTrack();
    }

    // Generates and visualises a new track
    public void MakeTrack()
    {
        float startTime = Time.realtimeSinceStartup;

        List<Vector3> trackPoints = pointGenerator.Generate();
        
        List<Segment> spline = splineGenerator.Generate(trackPoints);

        carSpawnPoint = meshGenerator.Generate(spline);

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
        handbrakeText.text = "Handbrake On";
    }

    // Switches to the car chase cam
    public void Drive()
    {
        carChaseVCam.Priority = 1;
        trackViewVCam.Priority = 0;

        DrivingController.Instance.Handbrake = false;
        handbrakeText.text = "Handbrake Off";
    }

    // Sets the car transform and camera rotation 
    public void SetCarAndCamera()
    {
        SetCarTransform();
        SetCameraRotation();

        if(carChaseVCam.Priority == 1)
        {
            DrivingController.Instance.Handbrake = false;
        }
        else
        {
            DrivingController.Instance.Handbrake = true;
        }
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
}
