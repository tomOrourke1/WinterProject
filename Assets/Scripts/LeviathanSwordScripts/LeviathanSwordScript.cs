using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;


public class LeviathanSwordScript : MonoBehaviour
{

    public Camera cam;

    [Header("Character")]
    public MyCharacterController myCharacterController;
    public KinematicCharacterMotor kinematicCharacterMotor;

    [Header("Reference Points")]
    public Transform resting;
    public Transform slash1;
    public Transform slash2;
    public Transform curvePoint;


    [Header("bools")]
    public bool walking;
    public bool aiming = false;
    public bool hasWeapon = true;
    public bool pulling = false;
    public bool returning = false;
    public bool throwing = false;
    public bool isResting = true;
    public bool grounded = false;
    public bool zipping = false; 


    public Vector3 HitPoint = default;
    public float throwStartTime = 0f;

    public float returnTime = 0;

    public float speed = 5f;
    public float startedHookShot;

    [Header("Casting Stuff")]
    public float radius = 0.25f;
    public float maxDistance = 70f;



    private void Start()
    {

    }


    private void Update()
    {
        
        if(isResting)
        {
            transform.position = Vector3.Lerp(transform.position, resting.position, 0.3f);
            transform.rotation = Quaternion.Lerp(transform.rotation, kinematicCharacterMotor.TransientRotation * Quaternion.FromToRotation(Vector3.forward, Vector3.up), 0.5f);

        }

        if(Input.GetMouseButtonDown(1) && hasWeapon)
        {
            Aim(true);
        }
        if(Input.GetMouseButtonUp(1) && hasWeapon)
        {
            Aim(false);
        }

        if (hasWeapon)
        {
            if(aiming && Input.GetMouseButtonDown(0))
            {
                Throw();
            }

        }
        else
        {
            if(Input.GetKeyDown(KeyCode.R))
            {
                WeaponReturn();
            }
        }

        if(pulling)
        {
            if(returnTime < 1)
            {
                Vector3 pos = GetQuadraticCurvePoint(returnTime, HitPoint, curvePoint.position, resting.position);
                returnTime += Time.deltaTime * 1.5f;
            }
            else
            {
                pulling = false;
                isResting = true;
                hasWeapon = true;
            }
        }


        if(throwing)
        {
            Vector3 pos = ReLerp(transform.position, HitPoint, throwStartTime);
            transform.position = pos;

            

        }

        if(grounded)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                zipping = true;
                startedHookShot = Time.time;

            }
            if(zipping)
            {
                kinematicCharacterMotor.SetPosition(ReLerp(kinematicCharacterMotor.TransientPosition, HitPoint, startedHookShot));

                if(Vector3.Distance(kinematicCharacterMotor.TransientPosition, HitPoint) <= 0.5f)
                {
                    zipping = false;
                    WeaponReturn();
                    startedHookShot = 0f;
                }

            }

        }
        

    }

    void Aim(bool state)
    {
        aiming = state;


    }

    void Throw()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if(Physics.SphereCast(ray, radius, out hit, maxDistance))
        {
            HitPoint = hit.point;
            
            
            ThrowHitComponent throwHit = hit.collider.gameObject.GetComponent<ThrowHitComponent>();
            if (throwHit != null)
            {

            }
        }
        else
        {
            HitPoint = ray.GetPoint(maxDistance);
        }

        Vector3 dir = HitPoint - transform.position;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.forward);

        hasWeapon = false;
        throwing = true;
        throwStartTime = Time.time;
        isResting = false;
        aiming = false;

        grounded = true;
    }

    void WeaponReturn()
    {
        pulling = true;
        throwing = false;
        grounded = false;
    }



    private void LateUpdate()
    {




    }


    public Vector3 GetQuadraticCurvePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }

    public Vector3 ReLerp(Vector3 pos, Vector3 desPos, float timeStartedLerping, float lerptime = 1f)
    {
        float timeSinceStarted = Time.time - timeStartedLerping;

        float percentageComplete = timeSinceStarted / lerptime;

        return Vector3.Lerp(pos, desPos, percentageComplete);
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.SphereCast(ray, radius, out hit, maxDistance))
        {

        }

        Gizmos.DrawSphere(hit.point, radius);


    }
}
