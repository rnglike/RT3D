using UnityEngine;

public class GrassRenderer : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;
    public Texture2D densityMap;
    public int instanceCount = 4096; // Adjust density

    void Start()
    {
        grassMaterial.SetTexture("_MainTex", densityMap);
    }

    void Update()
    {
        Shader.SetGlobalMatrix("_CameraMatrix", Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);
        Graphics.DrawMeshInstancedProcedural(grassMesh, 0, grassMaterial, new Bounds(Vector3.zero, Vector3.one * 50), instanceCount);
    }
}
