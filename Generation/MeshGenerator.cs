using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public static MeshGenerator Instance;

    [Header("Components")]
    [SerializeField] private MeshFilter trackMeshFilter;
    [SerializeField] private MeshFilter terrainMeshFilter;

    [SerializeField] private MeshRenderer trackMeshRenderer;

    [SerializeField] private MeshCollider trackMeshCollider;
    [SerializeField] private MeshCollider terrainMeshCollider;

    [Header("Noise Settings")]
    [SerializeField] private float heightScale;
    [SerializeField] private float noiseDetail;

    public float HeightScale { set { heightScale = value; } }
    public float NoiseDetail { set { noiseDetail = value; } }

    [SerializeField] private int seedChange;
    private int seed;

    [Header("Track Settings")]
    [SerializeField] private Mesh2D crossSectionMesh;

    public Mesh2D CrossSectionMesh { set { crossSectionMesh = value; } }

    [SerializeField] private int minloopsPerVisualSegment;
    [SerializeField] private int minloopsPerColliderSegment;
    [SerializeField] private int loopsPerSegmentLength;

    [SerializeField] private float loopsPerVisualCurvature;
    [SerializeField] private float loopsPerColliderCurvature;


    [Header("Terrain Settings")]
    [SerializeField] private float terrainSize;
    [SerializeField] private int visualTerrainSegments;
    [SerializeField] private int colliderTerrainSegments;

    [SerializeField] private float terrainUVScale;

    private Mesh trackVisualMesh;
    private Mesh trackColliderMesh;
    private Mesh terrainVisualMesh;
    private Mesh terrainColliderMesh;

    private float uSpan;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        // Initialise meshes and set them
        trackVisualMesh = new();
        trackVisualMesh.name = "Track Visual";
        trackMeshFilter.sharedMesh = trackVisualMesh;

        trackColliderMesh = new();
        trackColliderMesh.name = "Track Collider";
        trackMeshCollider.sharedMesh = trackColliderMesh;

        terrainVisualMesh = new();
        terrainVisualMesh.name = "Terrain Visual";
        terrainMeshFilter.sharedMesh = terrainVisualMesh;

        terrainColliderMesh = new();
        terrainColliderMesh.name = "Terrain Collider";
        terrainMeshCollider.sharedMesh = terrainColliderMesh;

        uSpan = crossSectionMesh.CalculateUSpanWorld();

        seed = Random.Range(0, seedChange);
    }

    // Generates and sets a visual mesh and collider mesh (with a lower vertex count for physics performance), returns the start point of the track
    public OrientatedPoint Generate(List<Segment> spline)
    {
        // Generate track meshes
        OrientatedPoint trackStartPoint = GenerateTrackMesh(trackVisualMesh, true, spline, minloopsPerVisualSegment, loopsPerVisualCurvature);
        trackMeshRenderer.material = crossSectionMesh.Material;

        GenerateTrackMesh(trackColliderMesh, false, spline, minloopsPerColliderSegment, loopsPerColliderCurvature);

        // Generate terrain meshes
        GenerateTerrainMesh(terrainVisualMesh, true, visualTerrainSegments);
        GenerateTerrainMesh(terrainColliderMesh, false, colliderTerrainSegments);
        
        // Re-set the mesh colliders (otherwise physics does not update?)
        trackMeshCollider.sharedMesh = trackColliderMesh;
        terrainMeshCollider.sharedMesh = terrainColliderMesh;

        // Increment seed ready for next generation
        seed += seedChange;

        return trackStartPoint;
    }

    // Generates a container mesh of the given spline and applies it to the given mesh, returns the start orientated point of the spline
    private OrientatedPoint GenerateTrackMesh(Mesh trackMesh, bool isVisual, List<Segment> spline, int minLoopsPerSegment, float loopsPerCurvature)
    {
        MeshContainer meshContainer = new();

        OrientatedPoint trackStartPoint = null;

        int[] loopsPerSegment = new int[spline.Count];

        // Iterate through each spline segment and number of loops for each segment
        for (int segment = 0; segment < spline.Count; segment++)
        {
            float curvature = CurvatureOverSegment(spline[segment > 0 ? (segment - 1) : (spline.Count - 1)], spline[segment], spline[(segment + 1) % spline.Count]);
            loopsPerSegment[segment] = Mathf.Clamp((int) (loopsPerCurvature * curvature), minLoopsPerSegment, 1000);

            float segmentLength = isVisual ? ApproxSegmentLength(spline[segment], loopsPerSegmentLength) : 0;

            for (int loop = 0; loop < loopsPerSegment[segment]; loop++)
            {
                // Calculate distance along segment for this loop, find its orientation and respective height on nosie map
                float t = loop / (loopsPerSegment[segment] - 1f);

                OrientatedPoint orientatedPoint = new OrientatedPoint(spline[segment > 0 ? (segment - 1) : (spline.Count - 1)], spline[segment], spline[(segment + 1) % spline.Count], t);

                Vector3 offset = new Vector3(0, CalculateLoopHeight(orientatedPoint), 0);

                // Iterate through each vertex on the cross section and add it to our mesh container, transforming the local space to the world space
                // with the orientation of this spline point
                for (int vert = 0; vert < crossSectionMesh.VertexCount; vert++)
                {
                    Vertex vertex = crossSectionMesh.Vertices[vert];

                    meshContainer.Vertices.Add(orientatedPoint.LocalToWorldPosition(vertex.Position) + offset);

                    if (isVisual)
                    {
                        meshContainer.Normals.Add(orientatedPoint.LocalToWorldVector(vertex.Normal));

                        // Adjust the v coordinates with the length of this segment
                        meshContainer.Uvs.Add(new Vector2(vertex.U, t * segmentLength / uSpan));
                    }
                }

                if (segment == 0 && loop == 0)
                {
                    Vector3 position = orientatedPoint.Position + offset;
                    trackStartPoint = new OrientatedPoint(position, orientatedPoint.Rotation);
                }
            }
        }

        int totalLoops = 0;

        // Repeat iteration
        for (int segment = 0; segment < spline.Count; segment++)
        {
            // Find the starting index of this spline segment
            int segmentRootindex = totalLoops * crossSectionMesh.VertexCount;

            for (int loop = 0; loop < loopsPerSegment[segment] - 1; loop++)
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

            totalLoops += loopsPerSegment[segment];
        }

        // Clear any data from the mesh and then set all values
        trackMesh.Clear();

        trackMesh.SetVertices(meshContainer.Vertices);
        trackMesh.SetTriangles(meshContainer.Triangles, 0);

        if (isVisual)
        {
            trackMesh.SetNormals(meshContainer.Normals);
            trackMesh.SetUVs(0, meshContainer.Uvs);
        }

        return trackStartPoint;
    }

    private float CurvatureOverSegment(Segment previousSegment, Segment currentSegment, Segment nextSegment)
    {
        OrientatedPoint start = new OrientatedPoint(previousSegment, currentSegment, nextSegment, 0);
        OrientatedPoint end = new OrientatedPoint(previousSegment, currentSegment, nextSegment, 1);

        float angle = Quaternion.Angle(start.Rotation, end.Rotation);
        float height = Mathf.Abs(CalculatePointHeight(start.Position) - CalculatePointHeight(end.Position));

        return (angle / 180) + (2 * height / heightScale);
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

    // Generates a container mesh of a grid of points, elevates each point based on perlin noise map and applies this to the given mesh
    private void GenerateTerrainMesh(Mesh terrainMesh, bool isVisual, int terrainSegments)
    {
        MeshContainer meshContainer = new();

        float xSpacing = terrainSize / terrainSegments;
        float zSpacing = terrainSize / terrainSegments;

        // Iterate through each point of the grid, calculating coordinate in each axis
        for(int x = 0; x < terrainSegments + 1; x++)
        {
            float xPos = (x * xSpacing) - (terrainSize / 2);

            for(int z = 0; z < terrainSegments + 1; z++)
            {
                float zPos = (z * zSpacing) - (terrainSize / 2);
                float yPos = CalculatePointHeight(new Vector3(xPos, 0, zPos));

                meshContainer.Vertices.Add(new Vector3(xPos, yPos, zPos));

                if (isVisual)
                {
                    meshContainer.Uvs.Add(new Vector2((float) x / terrainSegments, (float) z / terrainSegments) * terrainUVScale);
                }
            }
        }

        // Iterate again through each point of the grid
        for(int x = 0; x < terrainSegments; x++)
        {
            // Find the root index of this row and the next row of teh grid
            int rootIndex = x * (terrainSegments + 1);
            int nextRootIndex = (x + 1) * (terrainSegments + 1);

            for(int z = 0; z < terrainSegments; z++)
            {
                // Find the indices of the line segment in this row and the next row
                int start1 = rootIndex + z;
                int end1 = rootIndex + z + 1;

                int start2 = nextRootIndex + z;
                int end2 = nextRootIndex + z + 1;

                // Complete quad between teh four points with two triangles
                meshContainer.Triangles.Add(start1);
                meshContainer.Triangles.Add(end2);
                meshContainer.Triangles.Add(start2);

                meshContainer.Triangles.Add(start1);
                meshContainer.Triangles.Add(end1);
                meshContainer.Triangles.Add(end2);
            }
        }

        // Clear any data from the mesh and set the new data from mesh container
        terrainMesh.Clear();

        terrainMesh.SetVertices(meshContainer.Vertices);
        terrainMesh.SetTriangles(meshContainer.Triangles, 0);       

        if (isVisual)
        {
            terrainMesh.SetUVs(0, meshContainer.Uvs);
            terrainMesh.RecalculateNormals();
        }
    }

    // Calculates the maximum relative height of the cross section mesh vertices
    private float CalculateLoopHeight(OrientatedPoint orientatedPoint)
    {
        float maxHeight = -0.5f * heightScale;

        for (int vert = 0; vert < crossSectionMesh.VertexCount; vert++)
        {
            float height = CalculatePointHeight(orientatedPoint.LocalToWorldPosition(crossSectionMesh.Vertices[vert].Position));

            if(height > maxHeight)
            {
                maxHeight = height;
            }
        }

        return maxHeight;
    }

    // Calculates height at the given points coordinate on perlin noise map
    private float CalculatePointHeight(Vector3 point)
    {
        float xCoord = point.x / 60 * noiseDetail + seed;
        float yCoord = point.z / 60 * noiseDetail + seed;

        float height = (Mathf.PerlinNoise(xCoord, yCoord) - 0.5f) * heightScale;

        return height;
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

    public OrientatedPoint(Vector3 position, Quaternion rotation)
    {
        Position = position;
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