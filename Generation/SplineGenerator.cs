using System.Collections.Generic;
using UnityEngine;

public class SplineGenerator : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float splineAlpha;
    [Range(0f, 1f)]
    [SerializeField] private float splineStep;

    // Generates and visualises the spline for the given points
    public List<Segment> Generate(List<Vector3> points, LineRenderer lineRenderer, bool visualise = true)
    {
        List<Segment> spline = CalculateSpline(points);

        if (visualise)
        {
            VisualiseSpline(spline, lineRenderer);
        }

        return spline;
    }

    // Determines the spline segments for the given points in a loop and returns a list of segment objects
    private List<Segment> CalculateSpline(List<Vector3> points)
    {
        List<Segment> splineSegments = new();

        for(int i = 0; i < points.Count; i++)
        {
            splineSegments.Add(CalculateSplineSegment(points[i == 0 ? points.Count - 1 : i - 1], points[i], points[(i + 1) % points.Count], points[(i + 2) % points.Count]));
        }

        return splineSegments;
    }

    // Determines the coefficients for the given segment and returns a Segment object containing these
    private Segment CalculateSplineSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t01 = Mathf.Pow(Vector3.Distance(p0, p1), splineAlpha);
        float t12 = Mathf.Pow(Vector3.Distance(p1, p2), splineAlpha);
        float t23 = Mathf.Pow(Vector3.Distance(p2, p3), splineAlpha);

        Vector3 m1 = (p2 - p1 + t12 * ((p1 - p0) / t01 - (p2 - p0) / (t01 + t12)));
        Vector3 m2 = (p2 - p1 + t12 * ((p3 - p2) / t23 - (p3 - p1) / (t12 + t23)));

        Vector3 a = 2.0f * (p1 - p2) + m1 + m2;
        Vector3 b = -3.0f * (p1 - p2) - m1 - m1 - m2;
        Vector3 c = m1;
        Vector3 d = p1;

        return new Segment(a, b, c, d);
    }

    // Determines and returns the point on a spline segment at value t along
    private Vector3 CalculateSplinePoint(Segment segment, float t)
    {
        Vector3 aVec = segment.A * Mathf.Pow(t, 3);
        Vector3 bVec = segment.B * Mathf.Pow(t, 2);
        Vector3 cVec = segment.C * t;
        Vector3 dVec = segment.D;

        return aVec + bVec + cVec + dVec;
    }

    private void VisualiseSpline(List<Segment> spline, LineRenderer lineRenderer)
    {
        List<Vector3> renderPoints = new();

        foreach(Segment segment in spline)
        {
            for(float t = 0; t < 1; t += splineStep)
            {
                Vector3 point = CalculateSplinePoint(segment, t);

                renderPoints.Add(new Vector3(point.x, 0, point.z));
            }
        }

        lineRenderer.positionCount = renderPoints.Count;
        lineRenderer.SetPositions(renderPoints.ToArray());
    }
}

public class Segment
{
    private Vector3 a;
    private Vector3 b;
    private Vector3 c;
    private Vector3 d;

    public Vector3 A { get { return a; } }
    public Vector3 B { get { return b; } }
    public Vector3 C { get { return c; } }
    public Vector3 D { get { return d; } }

    public Segment(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}
