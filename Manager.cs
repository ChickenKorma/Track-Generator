using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    private TrackGenerator trackGenerator;
    private SplineGenerator splineGenerator;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        trackGenerator = GetComponent<TrackGenerator>();
        splineGenerator = GetComponent<SplineGenerator>();

        lineRenderer = GetComponent<LineRenderer>();
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
        List<Vector2> points = trackGenerator.GenerateTrack();

        splineGenerator.GenerateSpline(points, lineRenderer);
    }
}
