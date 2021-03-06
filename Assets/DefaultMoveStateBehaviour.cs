using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DefaultMoveState : MovementState
{
    public AbilityPool jumpPool;
    public AbilityPool dashPool;

    [Header("Temp Buffer System")]
    public float dampenedAirAccel;
    public float dampTime = 3f;
    private float dampT;

    [Header("Stable Movement")]
    public float MaxStableMoveSpeed = 10f;
    public float StableMovementSharpness = 15f;
    public float OrientationSharpness = 10f;

    [Header("Sprinting")]
    public float SprintSpeed = 15f;

    [Header("Air Movement")]
    public float MaxAirMoveSpeed = 15f;
    public float AirAccelerationSpeed = 15f;
    public float Drag = 0.1f;

    [Header("Jumping")]
    public bool AllowJumpingWhenSliding = false;
    public float JumpUpSpeed = 10f;
    public float JumpScalableForwardSpeed = 10f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;


    [Header("DoubleJump")]
    public bool doubleJumpEnabled = false;
    public int doubleJumpCount = 1;
    public int maxDoubleJumpCount = 1;

    [Header("Misc")]

    public List<Collider> passingThroughIgnoredColliders = new List<Collider>();
    public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
    public float BonusOrientationSharpness = 10f;
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public Transform MeshRoot;

    private Collider[] _probedColliders = new Collider[8];
    private RaycastHit[] _probedHits = new RaycastHit[8];

    private Vector3 _internalVelocityAdd = Vector3.zero;
    private bool _shouldBeCrouching = false;
    private bool _isCrouching = false;

    private bool __isPrinting = false;

    public void DampenAirAccel()
    {
        dampT = dampTime;
    }

    // here we have a state that wants to manipulate the motor itself
    public override void InformStatePropulsionForce(Vector3 newMomentum)
    {
        controller.ForceUnground();

        Vector3 horizontalMomentum = Motor.BaseVelocity;

        horizontalMomentum.y = 0;

        if (newMomentum.normalized == Vector3.up && horizontalMomentum.magnitude > MaxAirMoveSpeed + 1 && horizontalMomentum.magnitude > MaxStableMoveSpeed + 1)
        {
            // must set equal to Motor.BaseVelocity with y Reset + new Momentum

            var neutralizedY = Motor.BaseVelocity;

            neutralizedY.y = 0;

            Motor.BaseVelocity = neutralizedY + newMomentum;
        }
        else
        {
            Motor.BaseVelocity = newMomentum;
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {

        if (controller.LookInput.sqrMagnitude > 0f && OrientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, controller.LookInput, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
        }

        Vector3 currentUp = (currentRotation * Vector3.up);
        if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
        {
            // Rotate from current up to invert gravity
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }

        else
        {
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }

    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {

        if (dampT > 0)
        {
            dampT -= deltaTime;

            if (dampT <= 0)
            {
                dampT = 0;
            }
        }

        // Ground movement
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            float currentVelocityMagnitude = currentVelocity.magnitude;


            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
            if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
            {
                // Take the normal from where we're coming from
                Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
                if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
                {
                    effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
                }
                else
                {
                    effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
                }
            }

            // Reorient velocity on slope
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(controller.MoveInput, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * controller.MoveInput.magnitude;

            Vector3 targetMovementVelocity;// = reorientedInput * MaxStableMoveSpeed;

            //added boolean for quick hacky sprint speed
            if (__isPrinting)
            {
                targetMovementVelocity = reorientedInput * SprintSpeed;
            }
            else
            {
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;
            }


            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
        }
        // Air movement
        else
        {
            // Add move input
            if (controller.MoveInput.sqrMagnitude > 0f)
            {

                float binaryAirAccel = dampT > 0 ? dampenedAirAccel : AirAccelerationSpeed;

                Vector3 addedVelocity = controller.MoveInput * binaryAirAccel * deltaTime;

                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                // Limit air velocity from inputs
                if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                {
                    // clamp addedVel to make total vel not exceed max vel on inputs plane
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else
                {
                    // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                    {
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                    }
                }

                // Prevent air-climbing sloped walls
                if (Motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                    }
                }

                // Apply added velocity
                currentVelocity += addedVelocity;

                // Clam velocity if velocity <= threshold              


            }

            // Gravity
            currentVelocity += Gravity * deltaTime;

            // Drag
            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        }

        if (controller.Jump.Buffered)
        {

            //Debug.Log("Jump Requested: doubleJumpCount: " + doubleJumpCount + "!FoundAnyGround: " + !Motor.GroundingStatus.FoundAnyGround);

            if ((doubleJumpEnabled && jumpPool.IsChargesLeft() && !Motor.GroundingStatus.FoundAnyGround))
            {

                //Debug.Log("Double Jump Code Running");

                Motor.ForceUnground();

                if (currentVelocity.y < JumpUpSpeed)
                {
                    currentVelocity.y = JumpUpSpeed;
                }
                else
                {
                    // better jump algorithm then the below, if double jumping while moving upwards

                    //currentVelocity.y += JumpUpSpeed;
                }

                controller.DoubleJumpEvent();

                Debug.LogWarning("Double Jump Consumed: Not Intended On Wall Jump");
                jumpPool.currentCharges--;

                controller.Jump.EatInput();

                return;
            }

            // See if we actually are allowed to jump
            if (((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround))) //  || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime
            {


                // Calculate jump direction before ungrounding
                Vector3 jumpDirection = Motor.CharacterUp;
                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                {
                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                }

                // Makes the character skip ground probing/snapping on its next update. 
                // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                Motor.ForceUnground();

                // Add to the return velocity and reset jump state
                currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                currentVelocity += (controller.MoveInput * JumpScalableForwardSpeed);

                controller.Jump.EatInput();

            }
        }

        // Take into account additive velocity


        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }

    }

  




    // should be simplified

    /// <summary>
    /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
    /// </summary>


    private Quaternion _tmpTransientRot;


    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the character begins its movement update
    /// </summary>
    public override void BeforeCharacterUpdate(float deltaTime)
    {

    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>


    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>


    public void ResetAbilities()
    {
        dashPool.ResetCharges();
        jumpPool.ResetCharges();
    }


    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public override void AfterCharacterUpdate(float deltaTime)
    {
        controller.manager.CheckForConditions();

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            ResetAbilities();
        }

    }

    public override void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    public override bool IsColliderValidForCollisions(Collider coll)
    {
        if (passingThroughIgnoredColliders.Count == 0)
        {
            return true;
        }

        if (passingThroughIgnoredColliders.Contains(coll))
        {
            return false;
        }

        return true;
    }

    public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void AddVelocity(Vector3 velocity)
    {

        // could make this method state dependent

        _internalVelocityAdd += velocity;
    }

    public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnLanded()
    {

        controller.LandedEvent();

        dashPool.ResetCharges();
        jumpPool.ResetCharges();

        //doubleJumpCount = maxDoubleJumpCount;
    }

    protected void OnLeaveStableGround()
    {
    }

    public override void OnDiscreteCollisionDetected(Collider hitCollider)
    {

    }

}

[RequireComponent(typeof(DashStateBehaviour))]
public class DefaultMoveStateBehaviour : MonoBehaviour
{

    [SerializeField]
    public DefaultMoveState defaultMoveState;

    public MTCharacterController controller;

    private DashState dashStateReference;

    private void Awake()
    {
        dashStateReference = GetComponent<DashStateBehaviour>().dashState;

        var transition = new MovementStateTransition(defaultMoveState, CheckDash, dashStateReference);
        transition.destinationHandled = true;

        controller.manager.AddTransition(transition);

        


    }

    public bool CheckDash()
    {
        bool validTransition = controller.Dash.Buffered && dashStateReference.defaultMoveState.defaultMoveState.dashPool.currentCharges > 0;

        if (validTransition)
        {
            controller.Dash.EatInput();

            Debug.Log("Dash Started");
            dashStateReference.StartDash(dashStateReference.controller.transform.forward);
            dashStateReference.defaultMoveState.defaultMoveState.dashPool.currentCharges--;
        }

        return validTransition;
    }

    private void FixedUpdate()
    {

        for (int i = 0; i < defaultMoveState.passingThroughIgnoredColliders.Count; i++)
        {
            // needs a dynamic algorithm that responds to colliders size
            if (Mathf.Abs((defaultMoveState.passingThroughIgnoredColliders[i].transform.position - controller.transform.position).magnitude) >= 3f)
            {

                Debug.Log("Collider Removed:");

                defaultMoveState.passingThroughIgnoredColliders.RemoveAt(i);
            }
        }

    }



    public void IncrementTempJumps()
    {
        defaultMoveState.jumpPool.currentCharges = defaultMoveState.jumpPool.maxCharges + 1;
    }

    public void ResetAbilities() => defaultMoveState.ResetAbilities();


    public void DampenAirAccel() => defaultMoveState.DampenAirAccel();

    public KinematicCharacterMotor Motor => defaultMoveState.Motor;


}
