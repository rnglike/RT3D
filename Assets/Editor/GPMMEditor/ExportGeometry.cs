using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ExportGeometry : EditorWindow
{
    private string filePath = "";
    private bool includeName = true;
    private bool includeMeshName = true;
    private bool includeGlobalPosition = true;
    private bool includeMaterials = false;
    private bool includePolyCount = false;
    private string filterName = "";
    private int minPolyCount = 0;

    private float progress = 0f; // Progress indicator

    [MenuItem("Research/Optimization/Export GameObject Data to CSV")]
    public static void ShowWindow()
    {
        GetWindow<ExportGeometry>("Export to CSV");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export GameObject Data", EditorStyles.boldLabel);

        // Select Columns to Export
        GUILayout.Label("Select Columns to Export", EditorStyles.boldLabel);
        includeName = EditorGUILayout.Toggle("Include Name", includeName);
        includeMeshName = EditorGUILayout.Toggle("Include Mesh Name", includeMeshName);
        includeGlobalPosition = EditorGUILayout.Toggle("Include Global Position", includeGlobalPosition);
        includeMaterials = EditorGUILayout.Toggle("Include Materials", includeMaterials);
        includePolyCount = EditorGUILayout.Toggle("Include Poly Count", includePolyCount);

        // Filters
        GUILayout.Label("Filters", EditorStyles.boldLabel);
        filterName = EditorGUILayout.TextField("Filter by Name", filterName);
        minPolyCount = EditorGUILayout.IntField("Min Poly Count", minPolyCount);

        if (GUILayout.Button("Export"))
        {
            Export();
        }

        // Progress bar
        EditorGUI.ProgressBar(new Rect(0, position.height - 20, position.width, 20), progress, "Progress");

        if (progress >= 1f)
        {
            progress = 0f;
            EditorUtility.DisplayDialog("Export Complete", "Data exported to " + filePath, "OK");
        }
    }

    private void Export()
    {
        try
        {
            // Get all GameObjects in the scene
            string sceneName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene).Replace(" ", "");
            List<GameObject> objectsToExport = new List<GameObject>(FindObjectsOfType<GameObject>());
            int totalObjects = objectsToExport.Count;
            int processedCount = 0;
            
            if (!Directory.Exists("Assets/SceneAnalysis"))
            {
                Directory.CreateDirectory("Assets/SceneAnalysis");
            }

            filePath = "Assets/SceneAnalysis/" + "ExtractGameObjects_" + sceneName + ".csv";

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                List<string> columns = new List<string>();

                // Column headers
                if (includeName) columns.Add("GameObject Name");
                if (includeMeshName) columns.Add("Mesh Name");
                if (includeGlobalPosition) columns.Add("Global Position");
                if (includeMaterials) columns.AddRange(GetMaterialPropertyHeaders());
                if (includePolyCount) columns.Add("Poly Count");

                writer.WriteLine(string.Join(",", columns));

                foreach (GameObject obj in objectsToExport)
                {
                    // Apply filters
                    if (!string.IsNullOrEmpty(filterName) && !obj.name.Contains(filterName)) continue;

                    MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                    SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                    Mesh mesh = null;

                    // Get mesh
                    if (meshFilter != null)
                    {
                        mesh = meshFilter.sharedMesh;
                    }
                    else if (skinnedMeshRenderer != null)
                    {
                        mesh = skinnedMeshRenderer.sharedMesh;
                    }

                    // Check polygon count filter
                    if (mesh != null && mesh.triangles.Length / 3 < minPolyCount) continue;

                    List<string> values = new List<string>();

                    // Populate data
                    if (includeName) values.Add(obj.name);
                    if (includeMeshName) values.Add(mesh != null ? mesh.name : "N/A");
                    if (includeGlobalPosition) values.Add(obj.transform.position.ToString());

                    if (includeMaterials)
                    {
                        Renderer renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            List<string> materialProperties = GetMaterialProperties(renderer.sharedMaterials);
                            values.AddRange(materialProperties);
                        }
                        else
                        {
                            values.AddRange(GetEmptyMaterialProperties());
                        }
                    }

                    if (includePolyCount) values.Add(mesh != null ? (mesh.triangles.Length / 3).ToString() : "N/A");

                    writer.WriteLine(string.Join(",", values));

                    // Update progress
                    processedCount++;
                    progress = (float)processedCount / totalObjects;
                }
            }

            Debug.Log("Data exported to " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to export data: " + e.Message);
        }
    }

    // Helper method to get headers for detailed material properties
    private List<string> GetMaterialPropertyHeaders()
    {
        List<string> headers = new List<string>();
        headers.Add("Material Count");
        headers.Add("Material Properties");
        return headers;
    }

    // Helper method to get detailed material properties
    private List<string> GetMaterialProperties(Material[] materials)
    {
        List<string> properties = new List<string>();
        properties.Add(materials.Length.ToString()); // Material count

        foreach (Material mat in materials)
        {
            // Example: Get specific properties like shader name
            properties.Add(mat.shader.name);
            // Add more properties as needed (e.g., texture names, color values, etc.)
        }

        return properties;
    }

    // Helper method to provide placeholders for detailed material properties
    private List<string> GetEmptyMaterialProperties()
    {
        List<string> emptyProperties = new List<string>(); 
        emptyProperties.Add("0"); // Material count is zero
        emptyProperties.Add(""); // Empty properties placeholder
        return emptyProperties;
    }
}
