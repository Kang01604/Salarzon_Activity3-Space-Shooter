/*// RuntimeMeshCombiner.cs
using UnityEngine;
using System.Collections.Generic;

public class RuntimeMeshCombiner : MonoBehaviour
{
    [Tooltip("Materials to assign to the combined mesh. Ensure the order matches original submesh materials if 'Merge Submeshes' is false.")]
    public Material[] combinedMaterials;

    [Tooltip("If true, all original submeshes will be merged into a single submesh, losing individual material assignments but simplifying the mesh. If false, each original submesh keeps its material slot.")]
    public bool mergeSubmeshes = true;

    [Tooltip("If true, original child MeshRenderers will be disabled. Set to false if you're only combining their geometry onto the parent for another purpose.")]
    public bool disableChildRenderers = true;

    [Tooltip("If true, original child Colliders will be destroyed. This is usually desired before an explosion script takes over collider creation.")]
    public bool destroyChildColliders = true;

    void Awake()
    {
        CombineMeshes();
    }

    public void CombineMeshes()
    {
        // Get all MeshFilter components in children, including this GameObject's own.
        // We filter out any MeshFilter that is null, or from an inactive GameObject.
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>(true)) // true to include inactive children
        {
            // Only consider meshes from GameObjects that are intended to be part of the visual model
            // and are not the main parent's MeshFilter if it's going to be the target of the combined mesh.
            // For simplicity, we'll collect all, then handle the parent specifically.
            if (mf.sharedMesh != null && mf.GetComponent<MeshRenderer>() != null)
            {
                meshFilters.Add(mf);
            }
        }

        // We need at least one mesh to combine.
        if (meshFilters.Count == 0)
        {
            Debug.LogWarning($"No valid MeshFilters found on {gameObject.name} or its children to combine.");
            return;
        }

        List<CombineInstance> combineInstances = new List<CombineInstance>();
        List<Material> newMaterials = new List<Material>();

        MeshFilter targetMeshFilter = GetComponent<MeshFilter>();
        MeshRenderer targetMeshRenderer = GetComponent<MeshRenderer>();

        if (targetMeshFilter == null)
        {
            targetMeshFilter = gameObject.AddComponent<MeshFilter>();
        }
        if (targetMeshRenderer == null)
        {
            targetMeshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Iterate through all mesh filters to prepare them for combining
        foreach (MeshFilter filter in meshFilters)
        {
            // Get the MeshRenderer associated with this MeshFilter
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || filter.sharedMesh == null)
            {
                continue; // Skip if no renderer or no mesh
            }

            // If we're merging submeshes, we only need one CombineInstance per mesh,
            // otherwise, we need one for each submesh.
            if (mergeSubmeshes)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = filter.sharedMesh;
                // Transform matrix converts local positions to world positions relative to the combiner's transform
                ci.transform = filter.transform.localToWorldMatrix;
                combineInstances.Add(ci);

                // For merged submeshes, we only take the first material found or use the specified materials
                if (combinedMaterials == null || combinedMaterials.Length == 0) // Only add if no explicit materials are set
                {
                    if (newMaterials.Count == 0 && renderer.sharedMaterial != null)
                    {
                        newMaterials.Add(renderer.sharedMaterial); // Add just one material for the combined mesh
                    }
                }
            }
            else // Keep original submeshes and their materials
            {
                for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = filter.sharedMesh;
                    ci.subMeshIndex = i; // Specify which submesh to combine
                    ci.transform = filter.transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                }

                // Add all materials from this renderer if no explicit materials are set
                if (combinedMaterials == null || combinedMaterials.Length == 0)
                {
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        newMaterials.Add(mat);
                    }
                }
            }

            // Disable original child renderers and destroy colliders
            // IMPORTANT: Only do this for actual children, not the GameObject this script is on.
            if (filter.gameObject != gameObject)
            {
                if (disableChildRenderers && renderer != null) // Check renderer is not null before accessing .enabled
                {
                    renderer.enabled = false;
                    // We don't disable the MeshFilter directly, only its renderer.
                    // The MeshFilter still holds the data, but the renderer won't draw it.
                }

                if (destroyChildColliders)
                {
                    // Get all colliders on the child GameObject and destroy them
                    Collider[] childColliders = filter.GetComponents<Collider>();
                    foreach (Collider col in childColliders)
                    {
                        Destroy(col);
                    }
                }
            }
        }

        // Create the new combined mesh
        Mesh newCombinedMesh = new Mesh();
        // The second 'true' parameter in CombineMeshes uses matrices to correctly transform vertices
        newCombinedMesh.CombineMeshes(combineInstances.ToArray(), mergeSubmeshes, true);
        newCombinedMesh.name = gameObject.name + "_CombinedMesh";

        // Assign the new mesh to the main MeshFilter
        targetMeshFilter.mesh = newCombinedMesh;

        // Assign materials to the main MeshRenderer
        if (combinedMaterials != null && combinedMaterials.Length > 0)
        {
            targetMeshRenderer.materials = combinedMaterials; // Use the explicitly set materials
        }
        else if (newMaterials.Count > 0)
        {
            // If no explicit materials set, use collected materials
            targetMeshRenderer.materials = newMaterials.ToArray();
        }
        // If no materials were collected and none were explicitly set, the renderer might be left without materials.
        // Ensure your main prefab's MeshRenderer has a default material if you're not setting combinedMaterials.

        Debug.Log($"Successfully combined meshes for {gameObject.name}. New vertex count: {newCombinedMesh.vertexCount}");
    }
}*/