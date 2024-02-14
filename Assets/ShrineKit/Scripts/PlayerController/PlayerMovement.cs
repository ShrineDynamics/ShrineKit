using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public bool _debug = false;
	public Rigidbody rb { get; private set; }
	public float playerMovementSpeed = 5;
	public float flatVeloCap = 8f;
	public float currentSpeed { get; private set; }

	//rigidbody movement
	public new CapsuleCollider collider;
	public bool grounded { get; private set; }
	public LayerMask groundCheckMask;
	public float groundDrag;
	private bool canJump = true;
	public float jumpCooldown = 0.5f;
	public float jumpForce;
	private float horizontalInput;
	private float verticalInput;
	private Vector3 moveDirection;
	public float airMultiplier = 0.5f;
	private float yaw;

	//toggles
	public bool playerCanMove = true;

	public float capsuleCastRadius = 0.2f;
	public float capsuleCastYDist = 0.2f;

	private Vector3 currentGravityVec;
	private ContactPoint[] cPoints;
	public float maxGroundedAngle = 55f;
	public float maxFlatAngle = 40f;
	private Vector3 groundNormal;
	private List<bool> groundedCollisionsThisFrame = new List<bool>();
	private List<Vector3> groundNormalsThisFrame = new List<Vector3>();
	//private bool checkedGroundedThisFrame = false;

	//crouch
	public Camera mainCam;
	public bool wantsToCrouch { get; private set; } = false;
	public Transform crouchCamTransf;
	public Transform normalCamTransf;
	public float camLerpSpeed;
	public float crouchSpeedMult = 0.33f;
	public LayerMask crouchTestMask;
	public Transform crouchCapsuleCastBase;
	public Transform crouchCapsuleCastTip;
	public float crouchTestRadius;

	//new crouch
	public float crouchLerpSpeed;
	public float capsuleHeightStanding;
	public float capsuleHeightCrouching;
	public float capsuleHeightMinimum;
	private float capsuleDesiredHeight;
	private float capsuleCurrentHeight;
	private float capsuleDefaultYOffset;
	private float crouchFrac;
	public AnimationCurve crouchSpeedCurve;
	public float camStandingHeight;
	public float camMinHeight;

	//ledge climbing
	public LayerMask ledgeMask;
	public float ledgeDist;
	public float ledgeTopdownDist;
	public Transform minLedgeClimb;
	public Transform ledgeClimbTopdownChecker;
	public MeshRenderer debug_ledgeCheckVisualizer;
	private float ledgeClimbTimer;
	public float ledgeClimbDuration;
	private bool climbingLedge;
	public AnimationCurve ledgeClimbUpCurve;
	public AnimationCurve ledgeClimbFwdCurve;
	public AnimationCurve ledgeClimbCrouchCurve;
	private Vector3 climbToPoint;
	public float timeBetweenJumpAndAllowedClimb;
	private float lastJumpTime;
	public float ledgeLookCutoffDot = -0.35f;

	//dashing (move to ability script?)
	public float dashForce;
	public float dashUpForce;
	private float dashTimer;
	public float dashCooldown;

	//override
	private bool animOverrideActive;
	private Vector3 overridePosition;
	public Transform camPivot;

	//coyote time
	private float lastGroundedTime;
	//0.03f = ~2 frames at 60fps
	public float coyoteTimeBuffer = 0.03f;

	//jump buffering
	private bool requestingJump = false;
	private float lastJumpInputTime;
	public float jumpBuffer = 0.03f;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;
		currentSpeed = playerMovementSpeed;
		capsuleDefaultYOffset = capsuleHeightStanding / 2.0f;
		capsuleCurrentHeight = capsuleDesiredHeight = capsuleHeightStanding;
	}

	void Update()
	{
		if (!Settings.paused)
		{
			groundedCollisionsThisFrame.Clear();
			groundNormalsThisFrame.Clear();
			//checkedGroundedThisFrame = false;

			UpdateYaw();
			
			//ground
			if (grounded)
				rb.drag = groundDrag;
			else
				rb.drag = 0;

			//dash cd
			if(dashTimer > 0f && grounded)
			{
				dashTimer -= Time.deltaTime;
			}

			if (canMove() && !animOverrideActive)
			{
				currentSpeed = (playerMovementSpeed * crouchSpeedCurve.Evaluate(crouchFrac));

				InputManagement();
				if (!animOverrideActive) { SetCapsuleDesiredHeight(); }

				horizontalInput = Input.GetAxisRaw("Horizontal");
				verticalInput = Input.GetAxisRaw("Vertical");
			}
			else
			{
				horizontalInput = 0;
				verticalInput = 0;
			}

			if (animOverrideActive)
			{
				OverrideMoveRigidbody();
			}

			AdjustCapsuleHeight();

			if (climbingLedge && ledgeClimbTimer < ledgeClimbDuration)
			{
				if (Input.GetKeyUp(KeyCode.Space) || !LookingAtLedge()) { EndLedgeClimb(); }
				else
				{
					ledgeClimbTimer += Time.deltaTime;
					UpdateLedgeClimbAnim();
				}
			}
			else if (climbingLedge && ledgeClimbTimer >= ledgeClimbDuration)
			{
				EndLedgeClimb();
			}

			AnimateCameraPosition();
		}
	}

	private void LateUpdate()
	{
	}

	void FixedUpdate()
	{
		if (!Settings.paused)
		{
			if (!animOverrideActive)
			{
				MovePlayerRigidbody();
			}
		}
	}

	//Experimental ground check----------------
	void OnCollisionStay(Collision ourCollision)
	{
		if (!groundedCollisionsThisFrame.Contains(ourCollision.collider)) //probably REALLY SLOW OP TO RUN *MULTIPLE TIMES* PER FRAME
		{
			Vector3 norm = Vector3.zero;
			groundedCollisionsThisFrame.Add(CheckGrounded(ourCollision, out norm));
			groundNormalsThisFrame.Add(norm);

			grounded = false;

			for (int i = 0; i < groundedCollisionsThisFrame.Count; i++)
			{
				if (groundedCollisionsThisFrame[i] == true)
				{
					grounded = true; //if even a SINGLE collision is grounded, it's true
					groundNormal = groundNormalsThisFrame[i]; //set the normal to the collision that validated us as grounded
					lastGroundedTime = Time.time;
					break;
				}
			}
		}
	}

	void OnCollisionExit(Collision ourCollision)
	{
		//It's okay to not have to check whether or not
		//the Collision we're exiting is one we're grounded on,
		//because it'll be reaffirmed next time OnCollisionStay runs.
		grounded = false;
		groundNormal = new Vector3(); //Probably not necessary, but a good habit, in my opinion
	}

	bool CheckGrounded(Collision newCol, out Vector3 normal)
	{
		cPoints = new ContactPoint[newCol.contactCount];
		newCol.GetContacts(cPoints);
		foreach (ContactPoint cP in cPoints)
		{
			//If the difference in angle between the direction of gravity
			//(usually, downward) and the current surface contacted is
			//less than our chosen maximum angle, we've found an
			//acceptable place to be grounded.
			if (maxGroundedAngle > Vector3.Angle(cP.normal, -Physics.gravity.normalized))
			{
				normal = cP.normal;
				return true;
			}
		}

		normal = Vector3.zero;
		return false;
	}

	public void FreezePlayer()
	{
		rb.velocity = Vector3.zero;
	}

	private void ResetJump()
	{
		canJump = true;
	}

	public void SetCapsuleDesiredHeight()
	{
		RaycastHit hitInfo;
		float checkDist = Mathf.Abs(crouchCapsuleCastBase.position.y - crouchCapsuleCastTip.position.y);
		bool hit = Physics.SphereCast(crouchCapsuleCastBase.position, crouchTestRadius, Vector3.up, out hitInfo, checkDist, crouchTestMask);
		float desiredMax = ((wantsToCrouch) ? capsuleHeightCrouching : capsuleHeightStanding);
		capsuleDesiredHeight = (hit) ? (Mathf.Clamp(hitInfo.distance, capsuleHeightMinimum, desiredMax)) : desiredMax;
		//Debug.Log($"Check dist: {checkDist} | Hit something? {((hit) ? hitInfo.collider.name : "False")} | Hit Distance: {hitInfo.distance} | Desired Capsule Height: {capsuleDesiredHeight} | Frac: {crouchFrac}");
	}

	public void AdjustCapsuleHeight()
	{
		crouchFrac = Mathf.Clamp01(Util.Map(capsuleCurrentHeight, capsuleHeightMinimum, capsuleHeightStanding, 0, 1));
		if (grounded || animOverrideActive)
		{
			float frac = capsuleCurrentHeight / capsuleHeightStanding;
			capsuleCurrentHeight = Mathf.MoveTowards(capsuleCurrentHeight, capsuleDesiredHeight, crouchLerpSpeed * Time.deltaTime);
			collider.height = capsuleCurrentHeight;
			collider.center = new Vector3(0f, capsuleDefaultYOffset * frac, 0f);
		}
	}

	public bool CouldClimb(out Vector3 toPoint)
	{
		bool fwd = false;
		bool top = false;
		bool cap = false;
		toPoint = Vector3.zero;

		RaycastHit hitBuff;
		//fwd trace
		if (Physics.Raycast(minLedgeClimb.position, transform.forward, out hitBuff, ledgeDist, ledgeMask))
		{
			fwd = true;

			//topdown trace
			if (Physics.Raycast(ledgeClimbTopdownChecker.position, Vector3.down, out hitBuff, ledgeTopdownDist, ledgeMask))
			{
				top = true;

				//capsule check
				cap = Physics.CheckSphere(hitBuff.point + (Vector3.up * (collider.radius * 1.01f)), collider.radius);

				//Collider[] c = Physics.OverlapSphere(hitBuff.point + (hitBuff.normal * (collider.radius * 1.11f)), collider.radius * 1.1f);
				//cap = (c.Length > 0);

				if (_debug)
				{
					debug_ledgeCheckVisualizer.transform.position = hitBuff.point + (hitBuff.normal * (collider.radius * 1.11f));
					debug_ledgeCheckVisualizer.transform.localScale = new Vector3((collider.radius * 1.1f) * 2f, (collider.radius * 1.1f) * 2f, (collider.radius * 1.1f) * 2f);
				}
				toPoint = hitBuff.point + (Vector3.up * ((collider.radius * 2) * 1.01f));
			}
		}

		if (_debug)
		{
			Debug.DrawRay(minLedgeClimb.position, transform.forward * ledgeDist, (fwd) ? Color.green : Color.red);
			if (fwd) { Debug.DrawRay(ledgeClimbTopdownChecker.position, Vector3.down * ledgeTopdownDist, (top) ? Color.green : Color.red); }
			debug_ledgeCheckVisualizer.enabled = (top && fwd);
			debug_ledgeCheckVisualizer.material.color = (cap) ? Color.red : Color.green;
		}

		return (fwd && top) && !cap;
	}

	private void BeginLedgeClimb()
	{
		if (!climbingLedge)
		{
			climbingLedge = true;
			ledgeClimbTimer = 0;
			overridePosition = rb.transform.position;
			//capsuleDesiredHeight = capsuleHeightMinimum;
			BeginAnimationOverride();
		}
	}

	private void EndLedgeClimb()
	{
		if (climbingLedge)
		{
			EndAnimationOverride();
			climbingLedge = false;
			ledgeClimbTimer = 0;
		}
	}

	private void UpdateLedgeClimbAnim()
	{
		float frac = ledgeClimbTimer / ledgeClimbDuration;
		Vector3 full = Vector3.Lerp(rb.transform.position, climbToPoint, ledgeClimbFwdCurve.Evaluate(frac));
		float up = Mathf.Lerp(rb.transform.position.y, climbToPoint.y + 0.5f, ledgeClimbUpCurve.Evaluate(frac));
		capsuleDesiredHeight = Mathf.Lerp(capsuleHeightMinimum, capsuleHeightStanding, Mathf.Clamp01(ledgeClimbCrouchCurve.Evaluate(frac)));
		overridePosition = new Vector3(full.x, up, full.z);
	}

	private void StartCrouch()
	{
		wantsToCrouch = true;
	}

	private void StopCrouch()
	{
		wantsToCrouch = false;
	}

	private void AnimateCameraPosition()
	{

		camPivot.transform.localPosition = Vector3.MoveTowards(camPivot.transform.localPosition, new Vector3(0f, Mathf.Lerp(camMinHeight, camStandingHeight, crouchFrac), 0f), Time.deltaTime * camLerpSpeed);
	}

	private void Jump()
	{
		// reset y velocity
		rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

		rb.AddForce((transform.up) * jumpForce, ForceMode.Impulse);

		requestingJump = false;

		//sfxJump_Inst.start();

		//PlayFootstep(StepTypes.Jump);
	}

	public void KnockbackForce(Vector3 force)
	{
		rb.AddForce(force, ForceMode.VelocityChange);
	}

	void OverrideMoveRigidbody()
	{
		rb.transform.position = overridePosition;
	}

	private void UpdateYaw()
	{
		//yaw = (yaw + GameManager.singleton.getMouseX() % 360);
		yaw = CameraMan.singleton.toRotate.localEulerAngles.y;
	}

	private Vector3 calcMoveDir()
	{
		// calculate movement direction
		Vector3 fwd = (grounded) ? Vector3.Cross(-groundNormal, transform.right).normalized : transform.forward;
		Vector3 right = (grounded) ? Vector3.Cross(-groundNormal, -transform.forward).normalized : transform.right;
		return (fwd * verticalInput) + (right * horizontalInput);
	}

	void MovePlayerRigidbody()
	{
		//turn player yaw
		rb.rotation = Quaternion.Euler(new Vector3(0f, yaw, 0f));

		//get gravity vec
		if (grounded)
		{
			Debug.DrawRay(transform.position, (-groundNormal * Physics.gravity.magnitude), Color.magenta);
			currentGravityVec = -groundNormal * Physics.gravity.magnitude;
		}

		moveDirection = calcMoveDir();

		// on ground
		if (grounded)
		{
			rb.AddForce(moveDirection.normalized * (currentSpeed * 10f), ForceMode.Force);
			//Debug.Log($"ground, moveDir: {moveDirection}, orientation: {transform.name} orientation Fwd: {transform.forward} orientation Y: {transform.eulerAngles.y}, vIn: {verticalInput} hIn: {horizontalInput}");

			//slope sliding
			/*
			float curAngle = Vector3.Angle(groundNormal, -Physics.gravity.normalized);
			
			float slideT = 1; 
			if(curAngle > maxFlatAngle)
			{
				slideT = Util.MapClamped(curAngle, maxFlatAngle, maxGroundedAngle, 0, 1);
				Debug.Log("slideT: " + slideT);
				//add sliding sound effect here?
			}
			*/


			//Vector3 adjNormal = Vector3.Lerp(-groundNormal, Physics.gravity.normalized, slideT);
		}

		// in air
		else if (!grounded)
		{
			rb.AddForce((moveDirection.normalized * (currentSpeed * 10f)) * airMultiplier, ForceMode.Force);
			//normal gravity
			currentGravityVec = Physics.gravity;
		}

		//apply gravity
		rb.AddForce(currentGravityVec, ForceMode.Acceleration);

		// limit velocity if needed
		Vector3 flat = flatVel();
		if (flat.magnitude > flatVeloCap)
		{
			rb.velocity = limitVelo(flat);
		}
	}

	public Vector3 flatVel()
	{
		return new Vector3(rb.velocity.x, 0f, rb.velocity.z);
	}

	public Vector3 limitVelo(Vector3 flatVel)
	{
		Vector3 limitedVel = flatVel.normalized * flatVeloCap;
		return new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
	}

	public Vector3 clampedVelo()
	{
		Vector3 flat = flatVel();
		return (flat.magnitude > flatVeloCap) ? limitVelo(flat) : rb.velocity;
	}

	private void InputManagement()
	{
		horizontalInput = Input.GetAxisRaw("Horizontal");
		verticalInput = Input.GetAxisRaw("Vertical");

		if (Input.GetKeyDown(KeyCode.Space))
		{
			requestingJump = true;
			lastJumpInputTime = Time.time;
		}

		// when to jump
		//climb ledge
		if (Input.GetKey(KeyCode.Space) && CouldClimb(out climbToPoint) && (Time.time > (lastJumpTime + timeBetweenJumpAndAllowedClimb)) && !climbingLedge && LookingAtLedge())
		{
			BeginLedgeClimb();
			return;
		}
		else if ((Input.GetKeyDown(KeyCode.Space) && playerCanJump()) || (shouldJumpBuffer() && playerCanJump(true)))
		{
			canJump = false;
			Jump();
			Invoke(nameof(ResetJump), jumpCooldown);
			lastJumpTime = Time.time;
		}

		//crouch
		if (Input.GetKey(KeyCode.LeftControl) && canCrouch())
		{
			StartCrouch();
		}
		else
		{
			StopCrouch();
		}

		if (Input.GetKeyDown(KeyCode.LeftShift) && !climbingLedge && dashAvailable()) //TEMP KEY
		{
			Dash();
		}
	}

	private bool dashAvailable ()
	{
		return dashTimer <= 0;
	}

	private void Dash()
	{
		if (((Mathf.Abs(verticalInput) + Mathf.Abs(horizontalInput)) / 2) > 0.05f)
		{
			dashTimer = dashCooldown;

			Vector3 fwd = (grounded) ? Vector3.Cross(currentGravityVec, transform.right).normalized : transform.forward;
			Vector3 right = (grounded) ? Vector3.Cross(-groundNormal, -transform.forward).normalized : transform.right;
			Vector3 dir = ((fwd * verticalInput) + (right * horizontalInput) + (Vector3.up * dashUpForce)).normalized;
			rb.AddForce(dir * (dashForce), ForceMode.VelocityChange);
		}
	}

	private void FwdDash()
	{
		Vector3 dir = mainCam.transform.forward;
		rb.AddForce(dir * dashForce, ForceMode.VelocityChange);
	}

	private bool LookingAtLedge()
	{
		float dot = Vector3.Dot((climbToPoint - mainCam.transform.position).normalized, mainCam.transform.forward);
		return dot > ledgeLookCutoffDot;
	}

	public void BeginAnimationOverride()
	{
		animOverrideActive = true;
		rb.useGravity = false;
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		rb.isKinematic = true;
	}

	public void SetOverridePosition(Vector3 position)
	{
		overridePosition = position;
	}

	public void EndAnimationOverride()
	{
		animOverrideActive = false;
		rb.useGravity = true;
		rb.isKinematic = false;
		Physics.SyncTransforms();
	}

	private bool playerCanJump(bool mustGround = false)
	{
		return canJump && ((mustGround) ? grounded : (grounded || shouldCoyoteTime())) && !isCrouching();
	}

	private bool shouldCoyoteTime()
	{
		//if(!grounded && ((Time.time - lastGroundedTime) <= coyoteTimeBuffer)) { Debug.Log($"coyote time: {(Time.time - lastGroundedTime)}"); }
		return !grounded && ((Time.time - lastGroundedTime) <= coyoteTimeBuffer);
	}

	private bool shouldJumpBuffer()
	{
		return requestingJump && ((Time.time - lastJumpInputTime) <= jumpBuffer);
	}

	public Vector3 curVelocity()
	{
		return rb.velocity;
	}

	public Vector3 curInput()
	{
		return new Vector3(horizontalInput, 0, verticalInput);
	}

	public bool canMove()
	{
		return true;
	}

	private bool canCrouch()
	{
		return true;
	}

	public bool canDash()
	{
		return true;
	}

	public bool isCrouching()
	{
		return (crouchFrac < 0.95f);
	}
}
