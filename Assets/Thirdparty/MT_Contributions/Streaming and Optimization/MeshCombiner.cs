using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{


    private void Start()
    {
        CombineMeshes();
    }

    public void CombineMeshes()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();

        Debug.Log(name + " is combining" + filters.Length + "meshes!");

        Mesh finalMesh = new Mesh();
        

        CombineInstance[] combiners = new CombineInstance[filters.Length];

        Material finalMaterial = renderer.material;

        for (int i = 1; i < filters.Length; i++)
        {
            combiners[i].subMeshIndex = 0;
            combiners[i].mesh = filters[i].sharedMesh;
            combiners[i].transform = filters[i].transform.localToWorldMatrix;

            Destroy(filters[i]);

        }

        finalMesh.CombineMeshes(combiners);

        

        // always use SHARED MESH in editor
        GetComponent<MeshFilter>().sharedMesh = finalMesh;
        //GetComponent<MeshRenderer>().material = finalMaterial;
    }
}
