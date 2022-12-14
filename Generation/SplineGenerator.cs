using System.Collections.Generic;
using UnityEngine;

public class SplineGenerator : MonoBehaviour
{
    public static SplineGenerator Instance;

    [Range(0f, 1f)]
    [SerializeField] private float splineAlpha;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Determines the spline segments for the given points in a loop and returns a list of segment objects
    public List<Segment> Generate(List<Vector3> points)
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
}

public class Segment
{
    private Vector3 a;
    private Vector3 b;
    private Vector3 c;
    private Vector3 d;

    public Segment(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    // Calculates and returns the point on this spline segment at value t along
    public Vector3 CalculatePoint(float t)
    {
        Vector3 aVec = a * Mathf.Pow(t, 3);
        Vector3 bVec = b * Mathf.Pow(t, 2);
        Vector3 cVec = c * t;
        Vector3 dVec = d;

        return aVec + bVec + cVec + dVec;
    }
}
