using UnityEngine;
using UnityEditor;
using System.IO;

public class ShaderSetterTool : EditorWindow
{
    private Shader selectedShader;
    private string newMaterialFolderPath = "Assets/NewMaterials";

    [MenuItem("Research/Optimization/Standardize Shader for Current Scene")]
    public static void ShowWindow()
    {
        GetWindow<ShaderSetterTool>("Shader Setter Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Shader", EditorStyles.boldLabel);
        selectedShader = EditorGUILayout.ObjectField("Shader", selectedShader, typeof(Shader), false) as Shader;

        if (GUILayout.Button("Set Shader"))
        {
            SetShaderForAllObjects();
        }
    }

    private void SetShaderForAllObjects()
    {
        if (selectedShader == null)
        {
            Debug.LogError("No shader selected!");
            return;
        }

        if (!Directory.Exists(newMaterialFolderPath))
        {
            Directory.CreateDirectory(newMaterialFolderPath);
        }

        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();

        foreach (MeshRenderer renderer in meshRenderers)
        {
            if (renderer.sharedMaterials != null)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material mat = renderer.sharedMaterials[i];
                    if (mat == null)
                    {
                        mat = CreateNewMaterial();
                        renderer.sharedMaterials[i] = mat;
                    }
                    mat.shader = selectedShader;
                    EnableGPUInstancing(mat);
                }
            }
        }

        foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
        {
            if (renderer.sharedMaterials != null)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material mat = renderer.sharedMaterials[i];
                    if (mat == null)
                    {
                        mat = CreateNewMaterial();
                        renderer.sharedMaterials[i] = mat;
                    }
                    mat.shader = selectedShader;
                    EnableGPUInstancing(mat);
                }
            }
        }

        Debug.Log("Shader set and GPU instancing enabled for all MeshRenderer and SkinnedMeshRenderer objects.");
    }

    private Material CreateNewMaterial()
    {
        Material newMat = new Material(selectedShader);
        string path = Path.Combine(newMaterialFolderPath, "NewMaterial_" + System.Guid.NewGuid() + ".mat");
        AssetDatabase.CreateAsset(newMat, path);
        AssetDatabase.SaveAssets();
        return newMat;
    }

    private void EnableGPUInstancing(Material mat)
    {
        if (mat != null && mat.shader != null && mat.shader.isSupported)
        {
            mat.enableInstancing = true;
        }
    }
}
