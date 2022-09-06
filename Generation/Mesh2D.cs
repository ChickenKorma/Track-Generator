using UnityEngine;

[CreateAssetMenu]
public class Mesh2D : ScriptableObject
{
    [SerializeField] private Vertex[] vertices;

    public Vertex[] Vertices { get { return vertices; } }

    [SerializeField] private int[] lineIndices;

    public int[] LineIndices { get { return lineIndices; } }

    public int VertexCount => vertices.Length;

    public int LineCount => lineIndices.Length;

    // Calculates the u-span of the 2D mesh in world space
    public float CalculateUSpanWorld()
    {
        float span = 0f;

        for(int line = 0; line < LineCount; line += 2)
        {
            Vector2 startPoint = vertices[lineIndices[line]].Position;
            Vector2 endPoint = vertices[lineIndices[line + 1]].Position;

            span += Vector2.Distance(startPoint, endPoint);
        }

        return span;
    }
}

[System.Serializable]
public class Vertex
{
    [SerializeField] private Vector2 position;
    [SerializeField] private Vector2 normal;

    public Vector2 Position { get { return position; } }
    public Vector2 Normal { get { return normal; } }

    [SerializeField] private float u;

    public float U { get { return u; } }
}
