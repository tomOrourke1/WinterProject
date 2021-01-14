﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastSystem : MonoBehaviour
{

    // Reference to the Main Camera
    public Camera cam;
    public float interactDistance = 5;
    public float shootDistance = 20;

    public int weaponDamage = 1;

    public LayerMask mask;

    [Header("GrappleChannel")]
    public BinaryCrossSceneReference reference;

    // Reference to Damage / Weapon System

    // Start is called before the first frame update
   
    
    // it's checking if we hit something, and then returning that data
    public RaycastHit CastRayFromScreen(float maxDistance)
    {

        Ray mainCameraRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;

        if (Physics.Raycast(mainCameraRay, out hit, maxDistance, mask))
        {
            Debug.Log(hit.transform.name);
        }

        if(hit.distance > shootDistance)
        {
            return new RaycastHit();
        }



        // look up Physics.RayCast using screen center / camera
        // if it has IInteractable, then interactable.Interact

        return hit;
    }

    private void LateUpdate()
    {
        Ray mainCameraRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        RaycastHit hit;

        if (Physics.Raycast(mainCameraRay, out hit, shootDistance, mask))
        {
            reference.InvokeMessage(true);
        }
        else
        {
            reference.InvokeMessage(false);
        }
    }

}