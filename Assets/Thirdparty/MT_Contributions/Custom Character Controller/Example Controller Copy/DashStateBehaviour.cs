using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DashState : MovementState
{
    [SerializeField]
    public DashState dashState;

    public float dashEndMultiplier = 0.5f;

    public float distance;
    public float timeToReach;

    public float mushroomDashEndMult = 1f;

    public float t;

    public WallRunStateBehaviour wallRunState;

    public BinaryCrossSceneReference abilityEventReference;

    public LayerMask ignoreLayers;

    private Vector3 dir;

    private Vector3 currentWallNormal;

    private bool initiallyGrounded = false;

    private Vector3 dashVelocity;

    private bool mushroomDashed;

    private bool wallReoriented;

    private bool endState;

    private Vector3 surfaceParrallel;

    private float initialDotProduct;

    public float dotProductBonkCutOff = 0.7f;


    public override void InformStatePropulsionForce(Vector3 newMomentum)
    {
        // exit this state into character default
        controller.SetDefaultMovementState();

        if (newMomentum.normalized == Vector3.up)
        {
            newMomentum += dashVelocity * mushroomDashEndMult;
            mushroomDashed = true;

            // invoke mushroom dash event
        }

        // call on default state's default Implementation
        base.InformStatePropulsionForce(newMomentum);
    }

    public override void Initialize()
    {
        t = 0;
        dashVelocity = Vector3.zero;
        mushroomDashed = false;
        wallReoriented = false;
        dashVelocity = Vector3.zero;
        initiallyGrounded = false;
        currentWallNormal = Vector3.zero;
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (wallReoriented == false)
        {
            base.UpdateRotation(ref currentRotation, deltaTime);
        } else
        {
            currentRotation = Quaternion.LookRotation(surfaceParrallel);
        }

        
    }


    public void StartDash(Vector3 direction)
    {

        t = 0;
        dir = controller.transform.forward;
        controller.SetMovementState(this);

        abilityEventReference.InvokeMessage(true);

        initiallyGrounded = Motor.GroundingStatus.IsStableOnGround;

        Debug.Log("InitiallyGrounded: " + initiallyGrounded);

        dashVelocity = distance / timeToReach * dir.normalized; // set vel initially

    }

    public void EndDash()
    {
        abilityEventReference.InvokeMessage(false);
        controller.SetDefaultMovementState();
    }

    // something like this

    public void SetHorizontalVelocity(Vector3 vel)
    {
        float y = dashVelocity.y;

        dashVelocity = vel.normalized * (distance / timeToReach);

        dashVelocity.y = y;
    }

    // Working as intended, just need to carry this over into the next state!
    public override bool IsColliderValidForCollisions(Collider coll)
    {
        bool valid = base.IsColliderValidForCollisions(coll) && !(ignoreLayers == (ignoreLayers | (1 << coll.gameObject.layer)));

        // if we detect a non valid collision while dashing, we queue it for the character controller to ignore regardless of states.
        if (!valid)
        {
            defaultMoveState.defaultMoveState.passingThroughIgnoredColliders.Add(coll);
        }

        return valid;
    }


    // Perhaps we need an inform state wall collision, so that states can do an on wall enter type deal
    // public override void InformStateWallCollision() { }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        t += deltaTime;

        // if we're wall jumping off of a wall
        if (wallReoriented && currentWallNormal != Vector3.zero && controller.Jump.Buffered)
        {

            controller.Jump.EatInput();

            Debug.Log("WALL JUMPED");

            EndDash();

            // set the controller into a crappy air accel dampened state for a few seconds
            defaultMoveState.DampenAirAccel();

            // don't reset abilities after wall jump
            //controller.ResetAbilities();

            // dash velocity = current movementum, halted less then usual
            dashVelocity *= (0.7f);

            // add to dash velocity a force from the normal of the wall
            dashVelocity += currentWallNormal.normalized * 10;

            // add a jump to the wall jump
            dashVelocity.y += defaultMoveState.defaultMoveState.JumpUpSpeed;

            Debug.Log("Current Wall Normal: " + currentWallNormal);

            // currentVelocity = dashVelocity + wall Jump
            defaultMoveState.Motor.BaseVelocity = dashVelocity;

            return;
        }

        if (t > timeToReach)
        {

            Debug.Log("WALL ORIENTED: " + wallReoriented);

            if (wallReoriented && initialDotProduct < dotProductBonkCutOff) // && still on wall
            {
                wallRunState.StartState(surfaceParrallel, currentWallNormal);

                // idiot! Messy returns, this state was being overriden because we didn't return and code kept running

                return;

            }

            // should cache pixel perfect desired end position, and snap there if possible after dash completion, although this wouldn't work if there's interference

            EndDash();

            // !!!!!!!!!!!!!!!!
            // if (jumpBuffered && doublejump is available, consume double jump to combine it with preserved dash momentum.


            // variable curDashEndMultiplier based on initially grounded, replace this with a queue system that other states can use
            // to set up desired effects

            dashVelocity = currentVelocity * dashEndMultiplier;

            currentVelocity = dashVelocity;
            return;
        }

        if (!Motor.GroundingStatus.IsStableOnGround && initiallyGrounded)
        {

            Debug.Log("TECH");

            initiallyGrounded = false;

            Debug.LogWarning("COYOTE DASH: Replace with data collection logger");

            defaultMoveState.ResetAbilities();

            // shitty implementation, as it technically should just consume the standard jump

            defaultMoveState.IncrementTempJumps();

            //defaultMoveState.jumpPool.currentCharges = defaultMoveState.jumpPool.maxCharges + 1;

        }

        // return dash velocity
        currentVelocity = dashVelocity;
    }


    public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

        Debug.Log("Hit: " + hitCollider.name + " Dot Product = " + Vector3.Dot(-hitNormal, dashVelocity.normalized));

        surfaceParrallel = dashVelocity - hitNormal * Vector3.Dot(dashVelocity, hitNormal);

        initialDotProduct = Vector3.Dot(-hitNormal, dashVelocity.normalized);

        Debug.Log("INITIAL DOT PRODUCT: " + initialDotProduct);

        Debug.LogWarning("TEMP, SAME IMPLEMENTATION FOR HEAD ON AND SIDE WALL DASHES, not starting custom head on collision yet");
        // although the branching functionalit later on is changed by the fact that initialDotProduct is variable, it's checked on state end

        if (initialDotProduct < dotProductBonkCutOff || initialDotProduct >= dotProductBonkCutOff)
        {
            float slideMultiplier = 0.75f;

            dashVelocity = surfaceParrallel.normalized * (distance / timeToReach) * slideMultiplier; // sliding against a wall sh

            Debug.LogWarning("Not checking properly for if we're on a wall: Also Need new Logging System");

            if (hitNormal.y == 0)
            {
                wallReoriented = true;
                // Idiot! NOT currentWallNormal = surfaceParrallel.normalized;
                currentWallNormal = hitNormal.normalized;
            }
        }
        else
        {

        }
  
    }
}

public class DashStateBehaviour : MonoBehaviour
{

    public DashState dashState;

    private void Awake()
    {
        // Set State Transitions here through dashState.controller Reference
    }

    public bool CheckDashStateEnded()
    {
        Debug.LogWarning("Should Use a Bool property instead that read only returns whether t > time to reach");
        bool validTransition = dashState.t >= dashState.timeToReach;

        if (validTransition)
        {
            
        }


        return validTransition;
    }

    public bool CheckDashStateValidWallJump()
    {
        bool validTransition = false;

        if (validTransition)
        {

        }

        return validTransition;
    }

    private bool dummyCheck()
    {
        // set the valid transition equal to our conditional checks
        bool validTransition = false;

        if (validTransition) // if our transition is valid, make any necessary on exit initializations
        {

        }

        // alert the Move State Manager whether we're transitioning
        return validTransition;
    }

}
