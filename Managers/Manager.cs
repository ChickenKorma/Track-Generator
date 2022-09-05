using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Manager : MonoBehaviour
{
    private PointGenerator pointGenerator;
    private SplineGenerator splineGenerator;
    private MeshGenerator meshGenerator;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshCollider meshCollider;

    [SerializeField] private Transform player;

    private float playerYOffset;

    [SerializeField] private CinemachineFreeLook carChaseVCam;
    [SerializeField] private CinemachineVirtualCamera trackViewVCam;

    private OrientatedPoint carSpawnPoint;

    private void Awake()
    {
        pointGenerator = GetComponent<PointGenerator>();
        splineGenerator = GetComponent<SplineGenerator>();
        meshGenerator = GetComponent<MeshGenerator>();
    }

    private void Start()
    {
        playerYOffset = player.position.y;

        MakeTrack();
    }

    // Generates and visualises a new track
    public void MakeTrack()
    {
        List<Vector3> points = pointGenerator.Generate();
        
        List<Segment> spline = splineGenerator.Generate(points, lineRenderer, false);

        carSpawnPoint = meshGenerator.Generate(spline, meshFilter, meshCollider);

        SetCarAndCamera();
    }

    // Switches to the track view cam
    public void ViewTrack()
    {
        SetCameraRotation();

        carChaseVCam.Priority = 0;
        trackViewVCam.Priority = 1;
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
}
