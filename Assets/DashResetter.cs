using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashResetter : MonoBehaviour
{

    public float ResetTime = 3f;
    public float t;

    public bool active = true;

    public Transform visual;

    private void Awake()
    {
        t = ResetTime;
    }

    public void Reset(MTCharacterController controller)
    {
        if (active)
        {
            controller.dashPool.ResetCharges();
            Deactivate();
        }
    }

    private void FixedUpdate()
    {

        if (t < 3f)
        {
            t += Time.deltaTime;

            if (t >= 3f)
            {
                t = 3f;

                Activate();
            }
        }
    }

    public void Activate()
    {
        visual.gameObject.SetActive(true);
        active = true;
    }

    public void Deactivate()
    {
        visual.gameObject.SetActive(false);
        active = false;
        t = 0;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Reset(other.GetComponent<MTCharacterController>());
        }
    }
}
