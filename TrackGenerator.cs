using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    [SerializeField] private Vector2 boundarySize;

    [SerializeField] private int minPoints;
    [SerializeField] private int maxPoints;

    [SerializeField] private int spacingIterations;
    [SerializeField] private int anglingIterations;

    [SerializeField] private float minPointSpacing;
    private float minPointSpacingSqr;
    [SerializeField] private float maxTurnAngle;

    [SerializeField] private float maxDisplacement;
    [SerializeField] private float displacementScale;

    [SerializeField] private Transform cubeParent;
    [SerializeField] private GameObject normalCube;
    [SerializeField] private GameObject midpointCube;
    private LineRenderer lineRenderer;

    private enum Orientation
    {
        Collinear,
        Clockwise,
        Anticlockwise
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        minPointSpacingSqr = Mathf.Pow(minPointSpacing, 2);

        GenerateTrack();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GenerateTrack();
        }
    }

    private void GenerateTrack()
    {
        List<Vector2> trackPoints;

        do
        {
            trackPoints = GeneratePoints();
        }
        while (PointsIntersect(trackPoints));

        VisualiseTrack(trackPoints);
    }

    // Generates points of a polygon with spacing and angle limitations and returns array of points
    private List<Vector2> GeneratePoints()
    {
        int totalPoints = UnityEngine.Random.Range(minPoints, maxPoints + 1);

        Vector2[] randomPoints = new Vector2[totalPoints];

        // Generate an array of randomly placed points within the boundary size
        for(int i = 0; i < totalPoints; i++)
        {
            float x = UnityEngine.Random.Range(0, boundarySize.x) - (boundarySize.x / 2);
            float y = UnityEngine.Random.Range(0, boundarySize.y) - (boundarySize.y / 2);

            randomPoints[i] = new Vector2(x, y);
        }

        // Calculate the convex hull and apply spacing limitations
        List<Vector2> trackPoints = CalculateConvexHull(randomPoints);
        
        trackPoints = SpacePoints(trackPoints, spacingIterations);
        
        // Generate new points inbetween the current points and apply angle limitations
        trackPoints = DisplaceMidpoints(trackPoints);
        
        trackPoints = SpacePoints(trackPoints, spacingIterations);
        
        trackPoints = AnglePoints(trackPoints);

        trackPoints = SpacePoints(trackPoints, spacingIterations);

        return trackPoints;
    }

    // Determines the convex hull of the given points using Jarvis's Algorithm and returns list of hull points ordered anticlockwise
    private List<Vector2> CalculateConvexHull(Vector2[] points)
    {
        int totalPoints = points.Length;

        // Check for the minimum number of points needed for the algorithm
        if(totalPoints < 3)
        {
            return null;
        }

        // Find the starting point (furthest -x point)
        int startIdx = 0;

        for(int i = 1; i < totalPoints; i++)
        {
            if(points[i].x < points[startIdx].x)
            {
                startIdx = i;
            }
        }

        List<Vector2> hullPoints = new();

        int currentPointIdx = startIdx;

        // Loop through all the points to find the outer hull until we reach the starting point 
        do
        {
            // Add current point to hull list and choose the next point from the points list
            hullPoints.Add(points[currentPointIdx]);

            int nextPointIdx = (currentPointIdx + 1) % totalPoints;

            // Loop through all the points and find the most anticlockwise
            for(int i = 0; i < totalPoints; i++)
            {
                if (CalculateOrientation(points[currentPointIdx], points[i], points[nextPointIdx]) == Orientation.Anticlockwise)
                {
                    nextPointIdx = i;
                }
            }

            // Set this next point as the current point
            currentPointIdx = nextPointIdx;
        }
        while (currentPointIdx != startIdx);

        return hullPoints;
    }

    // Determines the orientation of the three given points and returns an Orientation enum
    private Orientation CalculateOrientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float gradientDifference = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

        if (gradientDifference == 0)
        {
            return Orientation.Collinear;
        }

        return gradientDifference > 0 ? Orientation.Clockwise : Orientation.Anticlockwise;
    }

    // Pushes each point away from others to obtain the minimum point spacing and returns the updated points list
    private List<Vector2> SpacePoints(List<Vector2> points, int iterations)
    {
        for(int iteration = 0; iteration < iterations; iteration++)
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    if (Vector2.SqrMagnitude(points[i] - points[j]) < minPointSpacingSqr)
                    {
                        Vector2 direction = points[j] - points[i];

                        float difference = minPointSpacing - direction.magnitude;

                        Vector2 adjustment = direction.normalized * difference;

                        points[i] -= adjustment;
                        points[j] += adjustment;
                    }
                }
            }
        }

        return points;
    }

    // Generate and displace midpoints for all line segments of the given points and returns the updated points list 
    private List<Vector2> DisplaceMidpoints(List<Vector2> points)
    {
        List<Vector2> extendedPoints = new();

        for(int i = 0; i < points.Count; i++)
        {
            Vector2 midpoint = points[i] + ((points[(i + 1) % points.Count] - points[i]) / 2);

            float displacementAmount = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), displacementScale) * maxDisplacement;
            Vector2 displacement = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized * displacementAmount;

            extendedPoints.Add(points[i]);
            extendedPoints.Add(midpoint + displacement);
        }

        return extendedPoints;
    }

    // Angles each point relative to the previous two points to obtain minimum angle requirement and returns the updated points list
    private List<Vector2> AnglePoints(List<Vector2> points)
    {
        for(int iteration = 0; iteration < anglingIterations; iteration++)
        {
            for(int i = 0; i < points.Count; i++)
            {
                int nextIdx = (i + 1) % points.Count;

                Vector2 previousPoint = points[i == 0 ? points.Count - 1 : i - 1];
                Vector2 currentPoint = points[i];
                Vector2 nextPoint = points[nextIdx];

                Vector2 previousDirection = currentPoint - previousPoint;
                Vector2 nextDirection = nextPoint - currentPoint;

                float angle = Vector2.SignedAngle(previousDirection.normalized, nextDirection.normalized);

                

                if (Mathf.Abs(angle) > maxTurnAngle)
                {
                    float angleDifference = (maxTurnAngle * Mathf.Sign(angle)) - angle;

                    float cos = Mathf.Cos(angleDifference * Mathf.Deg2Rad);
                    float sin = Mathf.Sin(angleDifference * Mathf.Deg2Rad);

                    Vector2 normalDirection = nextDirection.normalized;
                    float newX = (normalDirection.x * cos) - (normalDirection.y * sin);
                    float newY = (normalDirection.x * sin) + (normalDirection.y * cos);

                    points[nextIdx] = currentPoint + (new Vector2(newX, newY) * nextDirection.magnitude);
                }
            }

            points = SpacePoints(points, 1);
        }

        return points;
    }

    // Returns whether any line segments of the given points intersect
    private bool PointsIntersect(List<Vector2> points)
    {
        for(int i = 0; i < points.Count; i++)
        {
            // Find line segment of current and next point
            Vector2 start1 = points[i];
            Vector2 end1 = points[(i + 1) % points.Count];

            // Loop through every other non-connecting point to check other line segments
            for(int j = i + 2; j < i + 2 + points.Count - 3; j++)
            {
                Vector2 start2 = points[j % points.Count];
                Vector2 end2 = points[(j + 1) % points.Count];

                Orientation o1 = CalculateOrientation(start1, end1, start2);
                Orientation o2 = CalculateOrientation(start1, end1, end2);
                Orientation o3 = CalculateOrientation(start2, end2, start1);
                Orientation o4 = CalculateOrientation(start2, end2, end1);

                // General case for intersection
                if(o1 != o2 && o3 != o4)
                {
                    return true;
                }

                // Special cases for intersection
                if(o1 == Orientation.Collinear && OnLineSegment(start1, start2, end1))
                {
                    return true;
                }

                if (o2 == Orientation.Collinear && OnLineSegment(start1, end2, end1))
                {
                    return true;
                }

                if (o3 == Orientation.Collinear && OnLineSegment(start2, start1, end2))
                {
                    return true;
                }

                if (o4 == Orientation.Collinear && OnLineSegment(start2, end1, end2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Returns whether the middle point lies on the line segment of the other two points
    private bool OnLineSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if(q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
        {
            return true;
        }

        return false;
    }

    // Spawns cubes and renders line to display the generated track
    private void VisualiseTrack(List<Vector2> points)
    {
        for (int i = cubeParent.childCount - 1; i >= 0; i--)
        {
            GameObject.DestroyImmediate(cubeParent.GetChild(i).gameObject);
        }

        lineRenderer.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 position = new Vector3(points[i].x, 0, points[i].y);

            Instantiate(i % 2 == 0 ? normalCube : midpointCube, position, Quaternion.identity, cubeParent);

            lineRenderer.SetPosition(i, position);
        }
    }
}
