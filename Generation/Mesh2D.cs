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
