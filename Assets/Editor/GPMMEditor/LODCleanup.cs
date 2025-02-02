using UnityEditor;
using UnityEngine;

public class LODCleanup : EditorWindow
{
    [MenuItem("Tools/LOD Cleanup")]
    public static void ShowWindow()
    {
        GetWindow<LODCleanup>("LOD Cleanup");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Delete LOD1, LOD2, LOD3 GameObjects and Set LOD0"))
        {
            DeleteLODsAndSetLOD0();
        }
    }

    private static void DeleteLODsAndSetLOD0()
    {
        // Find all GameObjects in the active scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Check if the object is part of a prefab instance
            if (PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                // Unpack the prefab instance if needed
                GameObject rootPrefabInstance = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (rootPrefabInstance != null && PrefabUtility.IsAnyPrefabInstanceRoot(rootPrefabInstance))
                {
                    PrefabUtility.UnpackPrefabInstance(rootPrefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }

            // Handle LODGroup components
            LODGroup lodGroup = obj.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods != null && lods.Length > 0)
                {
                    Transform lod0 = lods[0].renderers != null && lods[0].renderers.Length > 0 ? lods[0].renderers[0].transform : null;

                    // Delete LOD1, LOD2, LOD3 renderers
                    for (int i = 1; i < lods.Length; i++)
                    {
                        foreach (Renderer renderer in lods[i].renderers)
                        {
                            if (renderer != null)
                            {
                                DestroyImmediate(renderer.gameObject, true);
                            }
                        }
                    }

                    // Set LOD0 to the first LOD's renderer if not null
                    if (lod0 != null)
                    {
                        lodGroup.SetLODs(new LOD[] { new LOD(1.0f, new Renderer[] { lod0.GetComponent<Renderer>() }) });
                    }
                }
            }
            else
            {
                // If no LODGroup but has MeshRenderer, make it LOD0
                MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    LODGroup newLodGroup = obj.AddComponent<LODGroup>();
                    newLodGroup.SetLODs(new LOD[] { new LOD(1.0f, new Renderer[] { meshRenderer }) });
                }
            }

            // Delete GameObjects with names containing LOD1, LOD2, or LOD3
            if (obj.name.Contains("LOD1") || obj.name.Contains("LOD2") || obj.name.Contains("LOD3"))
            {
                DestroyImmediate(obj, true);
            }
        }

        // Refresh the hierarchy
        EditorApplication.RepaintHierarchyWindow();
    }
}
