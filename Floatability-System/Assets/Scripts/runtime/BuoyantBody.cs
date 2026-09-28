using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(Rigidbody))]
public class BuoyantBody : MonoBehaviour
{
    // Private properties
    private Rigidbody _rigidbody;
    private MeshFilter _meshFilter;

    // Public properties
    public Rigidbody Rigidbody => _rigidbody;
    public Mesh Mesh => _meshFilter.sharedMesh; // SharedMesh instead of mesh because
                                                // we will not modify the mesh itself
                                                // we just need to read it
    
    internal Vector3[] Vertices;
    internal int[] Triangles;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _meshFilter = GetComponent<MeshFilter>();
        
        Vertices =  _meshFilter.sharedMesh.vertices;
        Triangles = _meshFilter.sharedMesh.triangles;
    }
}
