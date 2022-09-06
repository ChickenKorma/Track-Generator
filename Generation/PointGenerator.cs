using System.Collections.Generic;
using UnityEngine;

public class PointGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    [SerializeField] private Vector2 maxSize;
    private Vector2 pointGenerationSize;

    [SerializeField] private float maxElevationChange;

    [SerializeField] private int minPoints;
    [SerializeField] private int maxPoints;


    [Header("Point Limits")]
    [SerializeField] private int spacingIterations;
    [SerializeField] private int anglingIterations;

    [SerializeField] private float minPointSpacing;
    private float minPointSpacingSqr;
    [SerializeField] private float maxTurnAngle;


    [Header("Midpoints")]
    [SerializeField] private float maxDisplacement;
    [SerializeField] private float displacementScale;

    private enum Orientation
    {
        Collinear,
        Clockwise,
        Anticlockwise
    }

    private void Start()
    {
        minPointSpacingSqr = Mathf.Pow(minPointSpacing, 2);

        pointGenerationSize = maxSize * 5 / 6;
    }

    // Generates points of a polygon without intersections
    public List<Vector3> Generate()
    {
        List<Vector3> trackPoints;

        do
        {
            trackPoints = GeneratePoints();
        }
        while (PointsIntersect(trackPoints) || !WithinBounds(trackPoints));

        return trackPoints;
    }

    // Generates points of a polygon with spacing and angle limitations and returns array of points
    private List<Vector3> GeneratePoints()
    {
        int totalPoints = Random.Range(minPoints, maxPoints + 1);

        Vector3[] randomPoints = new Vector3[totalPoints];

        // Generate an array of randomly placed points within the boundary size
        for(int i = 0; i < totalPoints; i++)
        {
            float x = Random.Range(0, pointGenerationSize.x) - (pointGenerationSize.x / 2);
            float y = Random.Range(0, maxElevationChange) - (maxElevationChange / 2);
            //float y = 0;
            float z = Random.Range(0, pointGenerationSize.y) - (pointGenerationSize.y / 2);

            randomPoints[i] = new Vector3(x, y, z);
        }

        // Calculate the convex hull and apply spacing limitations
        List<Vector3> trackPoints = CalculateConvexHull(randomPoints);
        
        trackPoints = SpacePoints(trackPoints, spacingIterations);
        
        // Generate new points inbetween the current points and apply angle limitations
        trackPoints = DisplaceMidpoints(trackPoints);
        
        trackPoints = SpacePoints(trackPoints, spacingIterations);      
        trackPoints = AnglePoints(trackPoints);
        trackPoints = SpacePoints(trackPoints, spacingIterations);

        return trackPoints;
    }

    // Determines the convex hull of the given points using Jarvis's Algorithm and returns list of hull points ordered anticlockwise
    private List<Vector3> CalculateConvexHull(Vector3[] points)
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

        List<Vector3> hullPoints = new();

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
    private Orientation CalculateOrientation(Vector3 p, Vector3 q, Vector3 r)
    {
        float gradientDifference = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);

        if (gradientDifference == 0)
        {
            return Orientation.Collinear;
        }

        return gradientDifference > 0 ? Orientation.Clockwise : Orientation.Anticlockwise;
    }

    // Pushes each point away from others to obtain the minimum point spacing and returns the updated points list
    private List<Vector3> SpacePoints(List<Vector3> points, int iterations)
    {
        for(int iteration = 0; iteration < iterations; iteration++)
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    if (Vector3.SqrMagnitude(points[i] - points[j]) < minPointSpacingSqr)
                    {
                        Vector3 direction = points[j] - points[i];

                        float difference = minPointSpacing - direction.magnitude;

                        Vector3 adjustment = direction.normalized * difference;

                        points[i] -= adjustment;
                        points[j] += adjustment;
                    }
                }
            }
        }

        return points;
    }

    // Generate and displace midpoints for all line segments of the given points and returns the updated points list 
    private List<Vector3> DisplaceMidpoints(List<Vector3> points)
    {
        List<Vector3> extendedPoints = new();

        for(int i = 0; i < points.Count; i++)
        {
            Vector3 midpoint = points[i] + ((points[(i + 1) % points.Count] - points[i]) / 2);

            float displacementAmount = Mathf.Pow(Random.Range(0f, 1f), displacementScale) * maxDisplacement;
            Vector3 displacement = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * displacementAmount;

            extendedPoints.Add(points[i]);
            extendedPoints.Add(midpoint + displacement);
        }

        return extendedPoints;
    }

    // Angles each point relative to the previous two points to obtain minimum angle requirement and returns the updated points list
    private List<Vector3> AnglePoints(List<Vector3> points)
    {
        for(int iteration = 0; iteration < anglingIterations; iteration++)
        {
            for(int i = 0; i < points.Count; i++)
            {
                int nextIdx = (i + 1) % points.Count;

                Vector3 previousPoint = points[i == 0 ? points.Count - 1 : i - 1];
                Vector3 currentPoint = points[i];
                Vector3 nextPoint = points[nextIdx];

                Vector3 previousDirection = currentPoint - previousPoint;
                Vector3 nextDirection = nextPoint - currentPoint;

                float angle = Vector3.SignedAngle(previousDirection.normalized, nextDirection.normalized, Vector3.up);

                if (Mathf.Abs(angle) > maxTurnAngle)
                {
                    float angleDifference = (maxTurnAngle * Mathf.Sign(angle)) - angle;

                    float cos = Mathf.Cos(angleDifference * Mathf.Deg2Rad);
                    float sin = Mathf.Sin(angleDifference * Mathf.Deg2Rad);

                    Vector3 normalDirection = nextDirection.normalized;
                    float newX = (normalDirection.x * cos) - (normalDirection.z * sin);
                    float newZ = (normalDirection.x * sin) + (normalDirection.z * cos);

                    points[nextIdx] = currentPoint + (new Vector3(newX, 0, newZ) * nextDirection.magnitude);
                }
            }

            points = SpacePoints(points, 1);
        }

        return points;
    }

    // Returns whether all points are within the boundary area
    private bool WithinBounds(List<Vector3> points)
    {
        foreach(Vector3 point in points)
        {
            if(Mathf.Abs(point.y) > maxSize.y / 2 || Mathf.Abs(point.x) > maxSize.x / 2)
            {
                return false;
            }
        }

        return true;
    }

    // Returns whether any line segments of the given points intersect
    private bool PointsIntersect(List<Vector3> points)
    {
        for(int i = 0; i < points.Count; i++)
        {
            // Find line segment of current and next point
            Vector3 start1 = points[i];
            Vector3 end1 = points[(i + 1) % points.Count];

            // Loop through every other non-connecting point to check other line segments
            for(int j = i + 2; j < i + 2 + points.Count - 3; j++)
            {
                Vector3 start2 = points[j % points.Count];
                Vector3 end2 = points[(j + 1) % points.Count];

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
    private bool OnLineSegment(Vector3 p, Vector3 q, Vector3 r)
    {
        if(q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && q.z <= Mathf.Max(p.z, r.z) && q.z >= Mathf.Min(p.z, r.z))
        {
            return true;
        }

        return false;
    }
}
