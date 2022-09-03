using System.Collections.Generic;
using UnityEngine;

public class SplineGenerator : MonoBehaviour
{
    [SerializeField] private float splineAlpha;

    [SerializeField] private float splineStep;

    // Generates and visualises the spline for the given points
    public void GenerateSpline(List<Vector2> points, LineRenderer lineRenderer)
    {
        List<Segment> spline = CalculateSpline(points);

        VisualiseSpline(spline, lineRenderer);
    }

    // Determines the spline segments for the given points in a loop and returns a list of segment objects
    private List<Segment> CalculateSpline(List<Vector2> points)
    {
        List<Segment> splineSegments = new();

        for(int i = 0; i < points.Count; i++)
        {
            splineSegments.Add(CalculateSplineSegment(points[i == 0 ? points.Count - 1 : i - 1], points[i], points[(i + 1) % points.Count], points[(i + 2) % points.Count]));
        }

        return splineSegments;
    }

    // Determines the coefficients for the given segment and returns a Segment object containing these
    private Segment CalculateSplineSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float t01 = Mathf.Pow(Vector2.Distance(p0, p1), splineAlpha);
        float t12 = Mathf.Pow(Vector2.Distance(p1, p2), splineAlpha);
        float t23 = Mathf.Pow(Vector2.Distance(p2, p3), splineAlpha);

        Vector2 m1 = (p2 - p1 + t12 * ((p1 - p0) / t01 - (p2 - p0) / (t01 + t12)));
        Vector2 m2 = (p2 - p1 + t12 * ((p3 - p2) / t23 - (p3 - p1) / (t12 + t23)));

        Vector2 a = 2.0f * (p1 - p2) + m1 + m2;
        Vector2 b = -3.0f * (p1 - p2) - m1 - m1 - m2;
        Vector2 c = m1;
        Vector2 d = p1;

        return new Segment(a, b, c, d);
    }

    // Determines and returns the point on a spline segment at value t along
    private Vector2 CalculateSplinePoint(Segment segment, float t)
    {
        Vector2 aVec = segment.A * Mathf.Pow(t, 3);
        Vector2 bVec = segment.B * Mathf.Pow(t, 2);
        Vector2 cVec = segment.C * t;
        Vector2 dVec = segment.D;

        return aVec + bVec + cVec + dVec;
    }

    private void VisualiseSpline(List<Segment> spline, LineRenderer lineRenderer)
    {
        List<Vector3> renderPoints = new();

        foreach(Segment segment in spline)
        {
            for(float t = 0; t < 1; t += splineStep)
            {
                Vector2 point = CalculateSplinePoint(segment, t);

                renderPoints.Add(new Vector3(point.x, 0, point.y));
            }
        }

        lineRenderer.positionCount = renderPoints.Count;
        lineRenderer.SetPositions(renderPoints.ToArray());
    }
}

class Segment
{
    private Vector2 a;
    private Vector2 b;
    private Vector2 c;
    private Vector2 d;

    public Vector2 A { get { return a; } }
    public Vector2 B { get { return b; } }
    public Vector2 C { get { return c; } }
    public Vector2 D { get { return d; } }

    public Segment(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}
