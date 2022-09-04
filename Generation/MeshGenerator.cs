using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    [SerializeField] private Mesh crossSectionMesh;

    [SerializeField] private int loopsPerSegment;

    public void Generate(List<Segment> spline, List<Vector3> points, MeshFilter meshFilter)
    {
        Debug.Log("Cross Section Mesh Vertices: " + crossSectionMesh.vertexCount + ", Normals: " + crossSectionMesh.normals.Length);

        for(int i = 0; i < crossSectionMesh.vertices.Length; i++)
        {
            Debug.Log("Vertex " + i + ": " + crossSectionMesh.vertices[i]);
            Debug.Log("Normal " + i + ": " + crossSectionMesh.normals[i]);
        }


        Mesh mesh = new();
        mesh.name = "Generated Track";

        List<Vector3> centralVertices = new();

        for(int s = 0; s < spline.Count; s++)
        {
            float tStep = 1f / (loopsPerSegment - 1f);

            for(int i = 0; i < loopsPerSegment - 1; i ++)
            {
                float t = Mathf.Clamp(tStep * i, 0, 1);

                centralVertices.Add(CalculateSplinePoint(spline[s], t));
            }
        }

        OrientatedPoint[] orientatedPoints = GenerateOrientations(centralVertices);

        GeneratedMesh generatedMesh = new();

        for(int i = 0; i < orientatedPoints.Length; i++)
        {
            for(int j = 0; j < crossSectionMesh.vertexCount; j++)
            {
                generatedMesh.Vertices.Add(orientatedPoints[i].LocalToWorld(crossSectionMesh.vertices[j]));
            }
        }

        for (int segment = 0; segment < spline.Count; segment++)
        {
            int segmentRootindex = segment * (loopsPerSegment - 1);

            for (int loop = 0; loop < loopsPerSegment - 1; loop++)
            {
                int rootIndex = (segmentRootindex + loop) * crossSectionMesh.vertexCount;
                int nextRootIndex = ((segmentRootindex + (loop + 1)) % (spline.Count * (loopsPerSegment - 1))) * crossSectionMesh.vertexCount;

                for(int i = 0; i < crossSectionMesh.vertexCount; i++)
                {
                    int start1 = rootIndex + i;
                    int end1 = rootIndex + ((i + 1) % crossSectionMesh.vertexCount);
                    int start2 = nextRootIndex + i;
                    int end2 = nextRootIndex + ((i + 1) % crossSectionMesh.vertexCount);

                    generatedMesh.Triangles.Add(end1);
                    generatedMesh.Triangles.Add(start2);
                    generatedMesh.Triangles.Add(start1);

                    generatedMesh.Triangles.Add(end1);
                    generatedMesh.Triangles.Add(end2);
                    generatedMesh.Triangles.Add(start2);
                }
            }
        }

        mesh.vertices = generatedMesh.Vertices.ToArray();
        mesh.triangles = generatedMesh.Triangles.ToArray();
        mesh.uv = generatedMesh.Uvs.ToArray();
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
    }

    /*
    // Finds the number of loops for a segment of the spline relative to the size
    private int CalculateNumberOfLoops(Vector2 startPoint, Vector2 endPoint)
    {
        float distance = Vector2.Distance(startPoint, endPoint);

        return (int) distance / loopStepSize;
    }
    */

    // Determines and returns the point on a spline segment at value t along
    private Vector3 CalculateSplinePoint(Segment segment, float t)
    {
        Vector3 aVec = segment.A * Mathf.Pow(t, 3);
        Vector3 bVec = segment.B * Mathf.Pow(t, 2);
        Vector3 cVec = segment.C * t;
        Vector3 dVec = segment.D;

        return aVec + bVec + cVec + dVec;
    }

    // Generates and returns orientated point objects for each given point
    private OrientatedPoint[] GenerateOrientations(List<Vector3> points)
    {
        OrientatedPoint[] orientations = new OrientatedPoint[points.Count];

        for(int i = 0; i < points.Count; i++)
        {
            Vector3 previousPoint = points[i == 0 ? points.Count - 1 : i - 1];
            Vector3 currentPoint = points[i];
            Vector3 nextPoint = points[(i + 1) % points.Count];

            orientations[i] = new OrientatedPoint(currentPoint, CalulateOrientation(previousPoint, currentPoint, nextPoint));
        }

        return orientations;
    }

    // Calculates and returns the tangent vector to the spline point
    private Vector3 CalulateOrientation(Vector3 previousPoint, Vector3 currentPoint, Vector3 nextPoint)
    {
        Vector3 previousDirection = currentPoint - previousPoint;
        Vector3 nextDirection = nextPoint - currentPoint;

        Vector3 forward = ((nextDirection + previousDirection) / 2).normalized;

        return forward;
    }

    

    // Converts vector2 to vector3 lying on the x-z plane
    private Vector3 Vec2ToVec3(Vector2 vec)
    {
        return new Vector3(vec.x, 0, vec.y);
    }
}

class GeneratedMesh
{
    public List<Vector3> Vertices { get; set; }
    public List<int> Triangles { get; set; }
    public List<Vector2> Uvs { get; set; }

    public GeneratedMesh()
    {
        Vertices = new();
        Triangles = new();
        Uvs = new();
    }
}

class OrientatedPoint
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public OrientatedPoint(Vector3 position, Vector3 forward)
    {
        Position = position;
        Rotation = Quaternion.LookRotation(forward);
    }

    public Vector3 LocalToWorld(Vector3 localPosition)
    {
        return Position + (Rotation * localPosition);
    }
}