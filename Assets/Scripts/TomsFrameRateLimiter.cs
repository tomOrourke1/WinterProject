using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TomsFrameRateLimiter : MonoBehaviour
{

    public int fps = 60;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = fps;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}