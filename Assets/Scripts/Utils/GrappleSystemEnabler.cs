using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleSystemEnabler : MonoBehaviour
{
    public GrapplingSystem GrapplingSystem;
    public MTCharacterCamera CharacterCamera;

    public BinaryCrossSceneReference GrappleReference;

    public Vector2 DefaultOffset;
    public Vector2 GrappleOffset;



    private void Awake()
    {
        GrappleReference.BinaryMessage += ActivateHookshot;

    }

    private void Start()
    {

    }


    public void ActivateHookshot(bool value)
    {
        GrapplingSystem.grappleEnabled = value;

        if (value)
        {
            CharacterCamera.FollowPointFraming = GrappleOffset;

        }
        else
        {
            CharacterCamera.FollowPointFraming = DefaultOffset;

        }


    }




}
