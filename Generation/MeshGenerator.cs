using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;

    [SerializeField] private MeshRenderer meshRenderer;

    [SerializeField] private MeshCollider meshCollider;

    [SerializeField] private Mesh2D crossSectionMesh;

    [SerializeField] private int loopsPerVisualSegment;
    [SerializeField] private int loopsPerColliderSegment;

    private Mesh trackVisualMesh;
    private Mesh trackColliderMesh;

    private float uSpan;

    private void Awake()
    {
        trackVisualMesh = new();
        trackVisualMesh.name = "Track Visual";
        meshFilter.sharedMesh = trackVisualMesh;

        trackColliderMesh = new();
        trackColliderMesh.name = "Track Collider";
        meshCollider.sharedMesh = trackColliderMesh;

        uSpan = crossSectionMesh.CalculateUSpanWorld();
    }

    // Generates and sets a visual mesh and collider mesh (with a lower vertex count for physics performance), returns the start point of the track
    public OrientatedPoint Generate(List<Segment> spline)
    {
        OrientatedPoint trackStartPoint = GenerateTrackMesh(trackVisualMesh, true, spline, loopsPerVisualSegment);
        GenerateTrackMesh(trackColliderMesh, false, spline, loopsPerColliderSegment);
        
        // Needs to be set each time for physics engine to update it
        meshCollider.sharedMesh = trackColliderMesh;

        return trackStartPoint;
    }

    // Generates a container mesh of the given spline and applies it to the given mesh, returns the start orientated point of the spline
    private OrientatedPoint GenerateTrackMesh(Mesh mesh, bool isVisual, List<Segment> spline, int loopsPerSegment)
    {
        MeshContainer meshContainer = new();

        OrientatedPoint startPoint = null;

        float segmentLength = 0f;

        // Iterate through each spline segment and number of loops for each segment
        for (int segment = 0; segment < spline.Count; segment++)
        {
            if (isVisual)
            {
                segmentLength = ApproxSegmentLength(spline[segment], loopsPerSegment * 2);
            }

            for (int loop = 0; loop < loopsPerSegment; loop++)
            {
                // Calculate distance along segment for this loop and find its orientation
                float t = loop / (loopsPerSegment - 1f);

                OrientatedPoint orientatedPoint = new OrientatedPoint(spline[segment > 0 ? (segment - 1) : (spline.Count - 1)], spline[segment], spline[(segment + 1) % spline.Count], t);

                if(segment == 0 && loop == 0)
                {
                    startPoint = orientatedPoint;
                }    

                // Iterate through each vertex on the cross section and add it to our mesh container, transforming the local space to the world space
                // with the orientation of this spline point
                for (int j = 0; j < crossSectionMesh.VertexCount; j++)
                {
                    Vertex vertex = crossSectionMesh.Vertices[j];

                    meshContainer.Vertices.Add(orientatedPoint.LocalToWorldPosition(vertex.Position));

                    if (isVisual)
                    {
                        meshContainer.Normals.Add(orientatedPoint.LocalToWorldVector(vertex.Normal));

                        // Adjust the v coordinates with the length of this segment
                        meshContainer.Uvs.Add(new Vector2(vertex.U, t * segmentLength / uSpan));
                    }
                }
            }
        }

        // Repeat iteration
        for (int segment = 0; segment < spline.Count; segment++)
        {
            // Find the starting index of this spline segment
            int segmentRootindex = segment * loopsPerSegment * crossSectionMesh.VertexCount;

            for (int loop = 0; loop < loopsPerSegment - 1; loop++)
            {
                // Find the starting index of this loop and the next loop with respect to the entire spline
                int rootIndex = segmentRootindex + (loop * crossSectionMesh.VertexCount);
                int nextRootIndex = segmentRootindex + ((loop + 1) * crossSectionMesh.VertexCount);

                for(int line = 0; line < crossSectionMesh.LineCount; line += 2)
                {
                    // Find the indices of this line and the respective line on the next loop
                    int lineStart = crossSectionMesh.LineIndices[line];
                    int lineEnd = crossSectionMesh.LineIndices[line + 1];

                    int start1 = rootIndex + lineStart;
                    int end1 = rootIndex + lineEnd;
                    int start2 = nextRootIndex + lineStart;
                    int end2 = nextRootIndex + lineEnd;

                    // Complete quad between both lines by adding two triangles
                    meshContainer.Triangles.Add(start1);
                    meshContainer.Triangles.Add(start2);
                    meshContainer.Triangles.Add(end2);

                    meshContainer.Triangles.Add(start1);
                    meshContainer.Triangles.Add(end2);
                    meshContainer.Triangles.Add(end1);
                }
            }
        }

        // Clear any data from the mesh and then set all values
        mesh.Clear();

        mesh.SetVertices(meshContainer.Vertices);
        mesh.SetTriangles(meshContainer.Triangles, 0);

        if (isVisual)
        {
            mesh.SetNormals(meshContainer.Normals);
            mesh.SetUVs(0, meshContainer.Uvs);

            meshRenderer.material = crossSectionMesh.Material;
        }

        return startPoint;
    }

    // Calculates numerically the length of the given segment using steps number of iterations
    private float ApproxSegmentLength(Segment segment, int steps)
    {
        Vector3[] points = new Vector3[steps];

        for(int step = 0; step < steps; step++)
        {
            points[step] = segment.CalculatePoint(step / (steps - 1));
        }

        float length = 0;

        for(int step = 0; step < steps - 1; step++)
        {
            Vector3 currentPoint = points[step];
            Vector3 nextPoint = points[step + 1];

            length += Vector3.Distance(currentPoint, nextPoint);
        }

        return length;
    }

    // Changes the cross sectional mesh used for generated the track mesh and recalculates the uSpan
    public void SetCrossSection(Mesh2D crossSectionMesh)
    {
        this.crossSectionMesh = crossSectionMesh;

        uSpan = crossSectionMesh.CalculateUSpanWorld();
    }
}

class MeshContainer
{
    public List<Vector3> Vertices { get; set; }
    public List<Vector3> Normals { get; set; }
    public List<int> Triangles { get; set; }
    public List<Vector2> Uvs { get; set; }

    public MeshContainer()
    {
        Vertices = new();
        Normals = new();
        Triangles = new();
        Uvs = new();
    }
}

public class OrientatedPoint
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    // Calculates average forward direction between two points slightly before and after this point
    public OrientatedPoint(Segment previousSegment, Segment currentSegment, Segment nextSegment, float t)
    {
        float tStep = 0.001f;

        float previousT = t - tStep;
        float nextT = t + tStep;

        Vector3 previousPoint;
        Vector3 currentPoint = currentSegment.CalculatePoint(t);
        Vector3 nextPoint;

        if(previousT < 0.0f)
        {
            previousPoint = previousSegment.CalculatePoint(previousT + 1.0f);
        }
        else
        {
            previousPoint = currentSegment.CalculatePoint(previousT);
        }

        if(nextT > 1.0f)
        {
            nextPoint = nextSegment.CalculatePoint(nextT - 1.0f);
        }
        else
        {
            nextPoint = currentSegment.CalculatePoint(nextT);
        }

        Vector3 previousDirection = currentPoint - previousPoint;
        Vector3 nextDirection = nextPoint - currentPoint;

        Vector3 forward = (nextDirection + previousDirection) / 2;
        Quaternion rotation = Quaternion.LookRotation(forward.normalized);

        Position = currentPoint;
        Rotation = rotation;
    }

    // Transforms local position to world space
    public Vector3 LocalToWorldPosition(Vector3 localPosition)
    {
        return Position + (Rotation * localPosition);
    }

    // Transforms local direction to world space
    public Vector3 LocalToWorldVector(Vector3 localVector)
    {
        return Rotation * localVector;
    }
}