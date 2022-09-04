using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    private PointGenerator pointGenerator;
    private SplineGenerator splineGenerator;
    private MeshGenerator meshGenerator;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private MeshFilter meshFilter;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            MakeTrack();
        }
    }

    // Generates and visualises a new track
    private void MakeTrack()
    {
        
        List<Vector3> points = pointGenerator.Generate();
        
        List<Segment> spline = splineGenerator.Generate(points, lineRenderer, false);

        meshGenerator.Generate(spline, points, meshFilter);
    }
}
