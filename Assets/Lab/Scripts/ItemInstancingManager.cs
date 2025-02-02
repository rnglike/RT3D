using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Item
{
    public List<Material> materials;
    public List<Matrix4x4> matrices;

    public Item()
    {
        materials = new List<Material>();
        matrices = new List<Matrix4x4>();
    }
}

public class ItemInstancingManager : MonoBehaviour
{
    public string[] tag;
    public List<GameObject> items;
    private Dictionary<Mesh, Item> zip = new Dictionary<Mesh, Item>();

    void Awake()
    {

        for (int i = 0; i < tag.Length; i++)
        {
            items.AddRange(GameObject.FindGameObjectsWithTag(tag[i]));
        }
        
        // Disable all found items
        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetActive(false);
        }
    }

    void Start()
    {
        // Collect all mesh data and transform matrices
        for (int i = 0; i < items.Count; i++)
        {
            GameObject item = items[i];

            if (item != null)
            {
                Mesh mesh = null;
                Material[] materials = null;

                // Get MeshFilter and MeshRenderer components
                MeshFilter meshFilter = item.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = item.GetComponent<MeshRenderer>();

                if (meshFilter != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
                else
                {
                    Debug.LogError("MeshFilter not found on " + item.name);
                }

                if (meshRenderer != null)
                {
                    materials = meshRenderer.sharedMaterials;
                }
                else
                {
                    Debug.LogError("MeshRenderer not found on " + item.name);
                }

                if (mesh != null && materials != null)
                {
                    if (!zip.ContainsKey(mesh))
                    {
                        zip[mesh] = new Item();
                    }

                    // Create transformation matrix
                    Matrix4x4 matrix = Matrix4x4.TRS(
                        item.transform.position,
                        item.transform.rotation,
                        item.transform.localScale
                    );

                    zip[mesh].materials = materials.ToList();
                    zip[mesh].matrices.Add(matrix);
                }
            }
        }
    }

    void LateUpdate()
    {
        // Draw instanced meshes
        foreach (KeyValuePair<Mesh, Item> entry in zip)
        {

            Mesh mesh = entry.Key;
            Item item = entry.Value;
            Material[] materials = item.materials.ToArray();
            Matrix4x4[] matrices = item.matrices.ToArray();

            for (int i = 0; i < materials.Length; i++)
            {
                try
                {
                    Graphics.DrawMeshInstanced(mesh, i, materials[i], matrices);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Cannot draw instanced mesh " + mesh.name + ": " + e.Message);
                }
            }
        }
    }
}