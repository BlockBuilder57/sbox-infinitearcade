﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[Library]
	public partial class QPhysController : BasePlayerController
	{
		[Net] public float DefaultSpeed { get; set; } = 190.0f;
		[Net] public float WalkSpeed { get; set; } = 150.0f;
		[Net] public float SprintSpeed { get; set; } = 320.0f;
		[Net] public float Acceleration { get; set; } = 10.0f;
		[Net] public float StopSpeed { get; set; } = 100.0f;
		[Net] public float GroundFriction { get; set; } = 4.0f;
		[Net] public float GroundStandableAngle { get; set; } = 46.0f;
		[Net] public float StepSize { get; set; } = 18.0f;
		[Net] public float MaxNonJumpVelocity { get; set; } = 140.0f;
		[Net] public float AirControl { get; set; } = 30.0f;
		[Net] public float AirAcceleration { get; set; } = 100.0f;
		[Net] public bool Swimming { get; set; } = false;
		[Net] public Vector3 Gravity { get; set; } = new Vector3(0, 0, 800);
		[Net] public float DistEpsilon { get; set; } = 0.03125f;

		public Vector3 DeltaVelocity => Velocity - m_lastVelocity;
		private Vector3 m_lastVelocity;

		public enum FallDamageModes { None, Fixed, Progressive, Scaled };
		[Net] public FallDamageModes FallDamageMode { get; set; } = FallDamageModes.None;
		[Net] public float FallDamageFixedAmount { get; set; } = 10;
		[Net] public float FallDamageMaxSafeHeight { get; set; } = 240;
		[Net] public float FallDamageFatalHeight { get; set; } = 720;
		private float m_fallVelocity;

		[Net] public float HullGirth { get; set; } = 32.0f;
		[Net] public float HullHeight { get; set; } = 72.0f;
		[Net] public float DuckHullHeight { get; set; } = 36.0f;
		[Net] public float EyeHeight { get; set; } = 64.0f;
		[Net] public float DuckEyeHeight { get; set; } = 28.0f;
		[Net] public bool CanUnDuckInAir { get; set; } = false;
		private BBox m_hullNormal;
		private BBox m_hullDucked;

		[Net] public bool Ducking { get; set; } = false;
		[Net] public bool DuckJumping { get; set; } = false;

		public Angles m_inputRotationLast;
		public Angles m_inputRotationDelta;

		public Unstuck Unstuck;
		public readonly string[] CollideTags = new[] { "solid", "playerclip", "passbullets" };
		
		public Angles BaseAngularVelocity { get; set; }

		private CKPlayer m_player;

		private string m_debugHopName;
		private HopType m_debugHopType;
		private enum HopType
		{
			None,
			bhop,
			ahop
		};

		public QPhysController()
		{
			Unstuck = new Unstuck(this);

			var halfGirth = HullGirth / 2f;
			var mins = new Vector3(-halfGirth, -halfGirth, 0);
			var maxs = new Vector3(+halfGirth, +halfGirth, HullHeight);
			var maxsDucked = new Vector3(+halfGirth, +halfGirth, DuckHullHeight);

			m_hullNormal = new BBox(mins, maxs);
			m_hullDucked = new BBox(mins, maxsDucked);
		}

		public BBox GetHull(bool ducked)
		{
			return (ducked ? m_hullDucked : m_hullNormal) * Pawn.Scale;
		}

		public override BBox GetHull()
		{
			return GetHull(Ducking);
		}

		public virtual void StartGravity()
		{
			float ent_gravity = 1.0f;

			if (m_player.PhysicsBody != null && m_player.PhysicsBody.GravityScale != ent_gravity)
				ent_gravity = (float)m_player.PhysicsBody?.GravityScale;

			Velocity -= ent_gravity * Gravity * 0.5f * (float)Math.Sqrt(Pawn.Scale) * Time.Delta;
			Velocity += new Vector3(0, 0, BaseVelocity.z) * Time.Delta;

			BaseVelocity = BaseVelocity.WithZ(0);
		}

		public virtual void FinishGravity()
		{
			float ent_gravity = 1.0f;

			if (m_player.PhysicsBody != null && m_player.PhysicsBody.GravityScale != ent_gravity)
				ent_gravity = (float)m_player.PhysicsBody?.GravityScale;

			Velocity -= ent_gravity * Gravity * 0.5f * (float)Math.Sqrt(Pawn.Scale) * Time.Delta;
		}

		public void CalculateEyeRot()
		{
			//if (Host.IsClient)
			//	return;

			/*m_inputRotationDelta = Input.Rotation.Angles() - m_inputRotationLast;
			m_inputRotationLast = Input.Rotation.Angles();

			Angles fullCalc = (EyeRotation.Angles() + m_inputRotationDelta).WithRoll(0) + (BaseAngularVelocity * Time.Delta);

			CKDebugging.ScreenText(Input.Rotation.Angles().ToString());
			CKDebugging.ScreenText(m_inputRotationLast.ToString());
			CKDebugging.ScreenText(m_inputRotationDelta.ToString());

			EyeRotation = Rotation.From(fullCalc.WithRoll(0));*/

			EyeRotation = Input.Rotation;

			if (VR.Enabled)
			{
				EyeRotation = Rotation.From(Input.VR.Head.Rotation.Pitch(), EyeRotation.Yaw(), EyeRotation.Roll());
			}
		}

		public override void FrameSimulate()
		{
			Host.AssertClient();

			CalculateEyeRot();
		}

		public override void Simulate()
		{
			// can't do this in the ctor for some reason
			if (!m_player.IsValid())
				m_player = Pawn as CKPlayer;
			if (!m_player.IsValid())
				return;

			m_lastVelocity = Velocity;

			//Gravity = -m_player.PhysicsBody.World.Gravity;

			EyeLocalPosition = Vector3.Up * (Ducking ? DuckEyeHeight : EyeHeight) * Pawn.Scale;
			EyeLocalPosition += TraceOffset;
			CalculateEyeRot();

			// Check Stuck
			// Unstuck - or return if stuck
			if (Unstuck.TestAndFix())
				return;

			if (Velocity.z > 250.0f)
				ClearGroundEntity();

			// if not on ground, store fall velocity
			if (!GroundEntity.IsValid())
				m_fallVelocity = -Velocity.z;


			bool attemptDuck = Input.Down(InputButton.Duck);

			if (attemptDuck != Ducking)
			{
				if (attemptDuck) TryDuck();
				else TryUnDuck();
			}

			if (Ducking)
				SetTag("ducked");

			// block: here on out is CGameMovement::FulLWalkMove

			// RunLadderMode

			CheckLadder();
			Swimming = Pawn.WaterLevel > 0.6f;

			//
			// Start Gravity
			//
			if (!Swimming && !m_isTouchingLadder)
				StartGravity();

			if (Input.Down(InputButton.Jump))
				CheckJumpButton();

			// Friction is handled before we add in any base velocity. That way, if we are on a conveyor,
			// we don't slow when standing still, relative to the conveyor.
			bool bStartOnGround = GroundEntity != null;
			//bool bDropSound = false;
			if (bStartOnGround)
			{
				Velocity = Velocity.WithZ(0);

				Friction();

				Rotation toRotate = Rotation.From(BaseAngularVelocity) * Time.Delta;
				Position = Position.RotateAroundPoint(GroundEntity.Position, toRotate);
			}

			// Work out wish velocity.. just take input, rotate it to view, clamp to -1, 1
			WishVelocity = new Vector3(Input.Forward, Input.Left, 0);
			var inSpeed = WishVelocity.Length.Clamp(0, 1);
			WishVelocity *= EyeRotation.Angles().WithPitch(0).ToRotation();

			// need to be swimming or touching a ladder
			if (!Swimming && !m_isTouchingLadder)
				WishVelocity = WishVelocity.WithZ(0);

			WishVelocity = WishVelocity.Normal * inSpeed;
			WishVelocity *= GetWishSpeed();

			if (Swimming)
			{
				Friction();
				WaterMove();
			}
			else if (m_isTouchingLadder)
			{
				LadderMove();
			}
			else if (GroundEntity != null)
			{
				WalkMove();
			}
			else
			{
				AirMove();
			}

			CategorizePosition();

			//
			// Finish Gravity
			//
			if (!Swimming && !m_isTouchingLadder)
				FinishGravity();

			CheckFalling();

			if (GroundEntity.IsValid())
				Velocity = Velocity.WithZ(0);

			// Land Sound
			// Swim Sounds

			if (CKDebugging.LocalClient != null)
			{
				var _debug_client = CKDebugging.LocalClient.GetClientData<int>(nameof(CKDebugging.debug_client), 0);
				var _debug_client_pawncontroller = CKDebugging.LocalClient.GetClientData<bool>(nameof(CKDebugging.debug_client_playercontroller), false);
				if (Host.IsServer && Client.NetworkIdent == _debug_client && _debug_client_pawncontroller)
				{
					const int pad = 19;

					DebugOverlay.Box(Position + TraceOffset, GetHull().Mins, GetHull().Maxs, Color.Red);
					/*if (Ducking)
						DebugOverlay.Box(Position, m_hullNormal.Mins, m_hullNormal.Maxs, Color.Blue);

					if (m_player.CameraMode is not FirstPersonCamera)
					{
						//DebugOverlay.Line(m_player.EyePos, m_player.EyePos + m_player.EyeRotation.Forward * 8, Color.White);
					}*/

					string debugText = "";

					debugText += $"{"Position",pad}: {Position:F2}";
					//debugText += $"{"Velocity",pad}: {Velocity:F2}";
					debugText += $"\n{"Velocity (hu/s)",pad}: {Velocity.Length:F2}";
					debugText += $"\n{"BaseVelocity",pad}: {BaseVelocity}";
					debugText += $"\n{"BaseAngularVelocity",pad}: {BaseAngularVelocity}";
					debugText += $"\n{"GroundEntity",pad}: {(GroundEntity != null ? $"{GroundEntity} [vel {GroundEntity?.Velocity}]" : "null")}";
					if (GroundEntity != null)
						debugText += $"\n{"GroundNormal",pad}: {GroundNormal} (angle {GroundNormal.Angle(Vector3.Up)})";
					//debugText += $"\n{"SurfaceFriction",pad}: {SurfaceFriction}";
					//debugText += $"\n{"WishVelocity",pad}: {WishVelocity}";

					debugText += $"\n{"Ducked (FL_DUCKING)",pad}: {Ducking}";
					debugText += $"\n{"Duckjumping",pad}: {DuckJumping}";

					if (GroundEntity == null && m_debugHopType != HopType.None)
						debugText += $"\n{"Hopping Type",pad}: {m_debugHopName}";

					CKDebugging.ScreenText(CKDebugging.ToLocal, debugText);
				}
			}
		}

		public virtual float GetWishSpeed()
		{
			float ws = -1;

			if (Ducking) ws = DefaultSpeed * (1 / 3f);
			else if (Input.Down(InputButton.Walk)) ws = WalkSpeed;
			else if (Input.Down(InputButton.Run)) ws = SprintSpeed;
			else ws = DefaultSpeed;

			return ws * Pawn.Scale;
		}

		public virtual void WalkMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			WishVelocity = WishVelocity.WithZ(0);
			WishVelocity = WishVelocity.Normal * wishspeed;

			Velocity = Velocity.WithZ(0);
			Accelerate(wishdir, wishspeed, 0, Acceleration);
			Velocity = Velocity.WithZ(0);

			// Add in any base velocity to the current velocity.
			Velocity += BaseVelocity;

			try
			{
				if (Velocity.Length < 1.0f)
				{
					Velocity = Vector3.Zero;
					return;
				}

				// first try just moving to the destination
				var dest = (Position + Velocity * Time.Delta).WithZ(Position.z);

				// first try moving directly to the next spot
				var pm = TraceBBox(Position, dest);

				if (pm.Fraction == 1)
				{
					Position = pm.EndPosition;
					StayOnGround();
					return;
				}

				StepMove();
			}
			finally
			{
				// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
				Velocity -= BaseVelocity;
			}

			StayOnGround();
		}

		public virtual void StepMove()
		{
			MoveHelper mover = new(Position, Velocity, CollideTags);
			mover.Trace = mover.Trace.Size(GetHull().Mins, GetHull().Maxs).Ignore(Pawn);
			mover.MaxStandableAngle = GroundStandableAngle;

			// block: this is essentially a minified version of CGameMovement::TryPlayerMove
			// the only thing that seems to be missing is the ability to slam into a wall at the end
			mover.TryMoveWithStep(Time.Delta, (StepSize * Pawn.Scale) + DistEpsilon);

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual void Move()
		{
			MoveHelper mover = new(Position, Velocity, CollideTags);
			mover.Trace = mover.Trace.Size(GetHull().Mins, GetHull().Maxs).Ignore(Pawn);
			mover.MaxStandableAngle = GroundStandableAngle;

			mover.TryMove(Time.Delta);

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual void TryDuck()
		{
			Ducking = true;
			TryDuckJump();
		}

		public virtual void TryUnDuck()
		{
			var pm = TraceBBox(Position, Position, GetHull(false).Mins, GetHull(false).Maxs);
			if (pm.StartedSolid) return;

			if (!GroundEntity.IsValid() && !CanUnDuckInAir)
				return;

			if (!TryUnDuckJump())
				return;

			Ducking = false;
		}

		public virtual void TryDuckJump()
		{
			if (!GroundEntity.IsValid())
			{
				DuckJumping = true;

				// pull "legs" up
				Position += new Vector3(0, 0, GetHull(true).Size.z);
			}
		}

		public virtual bool TryUnDuckJump()
		{
			if (!DuckJumping || !Ducking)
				return true; // we're not even duck jumping

			if (GroundEntity.IsValid())
				DuckJumping = false; // only unduckjump on ground

			Vector3 newPosition = Position - new Vector3(0, 0, GetHull(true).Size.z);

			var pm = TraceBBox(newPosition, newPosition, GetHull(false).Mins, GetHull(false).Maxs);
			if (pm.StartedSolid) return false;
			// snap to ground if fail?

			Position = newPosition;

			return true;
		}

		/// <summary>
		/// Add our wish direction and speed onto our velocity
		/// </summary>
		public virtual void Accelerate(Vector3 wishdir, float wishspeed, float speedLimit, float acceleration)
		{
			if (speedLimit > 0 && wishspeed > speedLimit)
				wishspeed = speedLimit;

			// See if we are changing direction a bit
			var currentspeed = Velocity.Dot(wishdir);

			// Reduce wishspeed by the amount of veer.
			var addspeed = wishspeed - currentspeed;

			// If not going to add any speed, done.
			if (addspeed <= 0)
				return;

			// Determine amount of acceleration.
			var accelspeed = acceleration * Time.Delta * wishspeed * SurfaceFriction;

			// account for pawn size
			accelspeed *= 1f / Pawn.Scale;

			// Cap at addspeed
			if (accelspeed > addspeed)
				accelspeed = addspeed;

			Velocity += wishdir * accelspeed;
		}

		/// <summary>
		/// Remove ground friction from velocity
		/// </summary>
		public virtual void Friction()
		{
			// Calculate speed
			float speed = Velocity.Length;
			//if (speed < 0.1f) return;

			float drop = 0;

			if (GroundEntity.IsValid())
			{
				float friction = GroundFriction * SurfaceFriction;

				// Bleed off some speed, but if we have less than the bleed
				//  threshold, bleed the threshold amount.
				float control = (speed < StopSpeed) ? StopSpeed : speed;

				// Add the amount to the drop amount.
				drop = control * friction * Time.Delta;
			}

			// scale the velocity
			float newspeed = speed - drop;
			if (newspeed < 0) newspeed = 0;

			if (newspeed != speed)
				Velocity *= newspeed / speed;
		}

		public void CheckFalling()
		{
			// do fall damage and landing effects here
			if (!GroundEntity.IsValid() || m_fallVelocity <= 0 || FallDamageMode == FallDamageModes.None)
				return;

			float maxSafeSpeed = MathF.Sqrt(2 * Gravity.Length * FallDamageMaxSafeHeight);

			if (m_fallVelocity >= maxSafeSpeed)
			{
				if (GroundEntity.Velocity.z != 0)
				{
					// we've landed on a moving object, add its velocity on
					// this will make our landing softer on objects moving down and harder on objects moving up
					m_fallVelocity += GroundEntity.Velocity.z;
					m_fallVelocity = MathF.Max(0, m_fallVelocity);
				}

				if (m_fallVelocity > maxSafeSpeed)
				{
					float fatalSpeed = MathF.Sqrt(2 * Gravity.Length * FallDamageFatalHeight);
					float damageAmt = 0;

					switch (FallDamageMode)
					{
						case FallDamageModes.Fixed:
							damageAmt = FallDamageFixedAmount;
							break;
						case FallDamageModes.Progressive:
							// remove the speed we're already going
							m_fallVelocity -= maxSafeSpeed;
							damageAmt = m_fallVelocity * (100f / (fatalSpeed - maxSafeSpeed));
							break;
						case FallDamageModes.Scaled:
							damageAmt = 5 * (m_fallVelocity / 300f);
							// scale by max health, with 100hp as a base
							damageAmt *= (m_player.MaxHealth / 100f);
							break;
					}

					if (damageAmt > 0)
					{
						DamageInfo damage = new DamageInfo()
						{
							Damage = damageAmt,
							Flags = DamageFlags.Fall
						};

						m_player.TakeDamage(damage);
					}
				}
			}

			// clear this so we don't run again until we land
			m_fallVelocity = 0;
		}

		[ConVar.Replicated] public static bool player_movement_pogo_jumps { get; set; } = true;
		[ConVar.Replicated] public static bool player_movement_air_jumps { get; set; } = false;
		[ConVar.Replicated] public static bool player_movement_jump_while_crouched { get; set; } = true;
		[ConVar.Replicated] public static bool player_movement_ahop { get; set; } = true;
		[ConVar.Replicated] public static bool player_movement_bhop { get; set; } = true;

		public virtual void CheckJumpButton()
		{
			if (Swimming)
			{
				// swimming, not jumping
				ClearGroundEntity();
				Velocity = Velocity.WithZ(100);
				// play swimming sound

				return;
			}

			if (!player_movement_air_jumps && GroundEntity == null)
				return;

			if (!player_movement_pogo_jumps && !Input.Pressed(InputButton.Jump))
				return;

			if (!player_movement_jump_while_crouched && Ducking)
				return;

			m_player.OnAnimEventFootstep(Position, 0, 2f, true);

			ClearGroundEntity();

			float flGroundFactor = 1.0f;
			// set based on currently touching surface (ie goop should slow us)

			float flMul;

			switch (Gravity.Length)
			{
				case 600f:
					flMul = 160.0f; // approx. 21 units.
					break;
				case 800f:
					flMul = 268.3281572999747f; // approx. 45 units.
					break;
				default:
					flMul = (float)Math.Sqrt(2 * Math.Abs(Gravity.z) * 21.0f) * Math.Sign(Gravity.z);
					break;
			}

			if (Pawn.Scale != 1)
				flMul *= MathF.Sqrt(Pawn.Scale);

			float startz = Velocity.z;

			if (Ducking)
				Velocity = Velocity.WithZ(flGroundFactor * flMul);
			else
				Velocity = Velocity.WithZ(startz + (flGroundFactor * flMul));

			// ahopping and bhopping
			{
				Vector3 vecForward = EyeRotation.Forward.WithZ(0).Normal;

				float flSpeedBoostPerc = (!Input.Down(InputButton.Run) /*&& !Duck.IsActive*/) ? 0.5f : 0.1f;
				float flSpeedAddition = Math.Abs(Input.Forward * flSpeedBoostPerc);
				float flMaxSpeed = GetWishSpeed() + (GetWishSpeed() * flSpeedBoostPerc);
				float flNewSpeed = (flSpeedAddition + Velocity.WithZ(0).Length);

				// If we're over the maximum, we want to only boost as much as will get us to the goal speed
				// this causes ahop in general
				if (flNewSpeed > flMaxSpeed)
					flSpeedAddition -= flNewSpeed - flMaxSpeed;

				// this causes AFH/ASH
				if (Input.Forward < 0.0f)
					flSpeedAddition *= -1.0f;

				// if DotProduct(vecForward, vecVelCurrent) < 0.0f or mv->m_flForwardMove < 0.0f we're in the position to ahop from the negative vector
				// otherwise, we're in a position to have our velocity "clamped" to a normal jump velocity
				// we can just disable that for bhop! :)

				// Add it on
				if (Vector3.Dot(vecForward, Velocity.WithZ(0).Normal) < 0.0f || Input.Forward < 0.0f)
				{
					if (player_movement_ahop)
					{
						Velocity += (vecForward * flSpeedAddition);

						if (Debug && Host.IsServer)
						{
							if (Input.Forward < 0.0f)
							{
								if (Input.Left != 0.0f)
									m_debugHopName = "Accelerated Side Hop (ASH)";
								else
									m_debugHopName = "Accelerated Forward Hop (AFH)";
							}
							else
								m_debugHopName = "Accelerated Back Hop (ABH)";

							m_debugHopType = HopType.ahop;
						}
					}
				}
				else if (!player_movement_bhop)
				{
					Velocity += (vecForward * flSpeedAddition);
				}
				else if (Debug)
				{
					m_debugHopName = "Bunny Hop";
					m_debugHopType = HopType.bhop;
				}

				// if we're not gaining any speed, just say we aren't hopping
				if (flNewSpeed < flMaxSpeed)
					m_debugHopType = HopType.None;
			}

			FinishGravity();

			AddEvent("jump");
		}

		public virtual void AirMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			Accelerate(wishdir, wishspeed, AirControl, AirAcceleration);

			Velocity += BaseVelocity;

			Move();

			Velocity -= BaseVelocity;
		}

		public virtual void WaterMove()
		{
			var wishdir = WishVelocity.Normal;
			var wishspeed = WishVelocity.Length;

			wishspeed *= 0.8f;

			Accelerate(wishdir, wishspeed, 100, Acceleration);

			Velocity += BaseVelocity;

			Move();

			Velocity -= BaseVelocity;
		}

		bool m_isTouchingLadder = false;
		Vector3 m_ladderNormal;
		public virtual void CheckLadder()
		{
			var wishvel = new Vector3(Input.Forward, Input.Left, 0);
			wishvel *= Input.Rotation.Angles().WithPitch(0).ToRotation();
			wishvel = wishvel.Normal;

			if (m_isTouchingLadder)
			{
				if (Input.Pressed(InputButton.Jump))
				{
					Velocity = m_ladderNormal * 100.0f;
					m_isTouchingLadder = false;

					return;

				}
				else if (GroundEntity != null && m_ladderNormal.Dot(wishvel) > 0)
				{
					m_isTouchingLadder = false;

					return;
				}
			}

			const float ladderDistance = 1.0f;
			var start = Position;
			Vector3 end = start + (m_isTouchingLadder ? (m_ladderNormal * -1.0f) : wishvel) * ladderDistance;

			var pm = Trace.Ray(start, end)
						.Size(GetHull().Mins, GetHull().Maxs)
						.WithTag("ladder")
						.Ignore(Pawn)
						.Run();

			m_isTouchingLadder = false;

			if (pm.Hit)
			{
				m_isTouchingLadder = true;
				m_ladderNormal = pm.Normal;
			}
		}

		public virtual void LadderMove()
		{
			var velocity = WishVelocity;
			float normalDot = velocity.Dot(m_ladderNormal);
			var cross = m_ladderNormal * normalDot;
			Velocity = (velocity - cross) + (-normalDot * m_ladderNormal.Cross(Vector3.Up.Cross(m_ladderNormal).Normal));

			Move();
		}

		protected float SurfaceFriction;

		public virtual void CategorizePosition()
		{
			SurfaceFriction = 1.0f;

			var point = Position.WithZ(Position.z - (2.0f * Pawn.Scale));
			var bumpOrigin = Position;

			bool bMovingUp = Velocity.z > 0;
			bool bMovingUpRapidly = Velocity.z > MaxNonJumpVelocity * Pawn.Scale;

			if (bMovingUpRapidly || Swimming) // or ladder and moving up
			{
				ClearGroundEntity();
				return;
			}

			var pm = TraceBBox(bumpOrigin, point);

			if (pm.Entity == null || Vector3.GetAngle(Vector3.Up, pm.Normal) > GroundStandableAngle)
			{
				ClearGroundEntity();

				if (Velocity.z > 0)
					SurfaceFriction = 0.25f;
			}
			else
			{
				UpdateGroundEntity(pm);
			}

			if (!pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f)
			{
				Position = pm.EndPosition;
			}

		}

		/// <summary>
		/// We have a new ground entity
		/// </summary>
		public virtual void UpdateGroundEntity(TraceResult tr)
		{
			GroundNormal = tr.Normal;

			//if ( tr.Entity == GroundEntity ) return;

			GroundEntity = tr.Entity;

			if (GroundEntity != null)
			{
				BaseVelocity = GroundEntity.Velocity;
				BaseAngularVelocity = GroundEntity.AngularVelocity;

				// we've landed now, so unduckjump
				TryUnDuckJump();

				/*Entity looper = GroundEntity;
				Rotation totalAngVel = Rotation.From(looper.AngularVelocity);
				while (looper.Parent != null)
				{
					totalAngVel *= Rotation.From(looper.Parent.AngularVelocity);
					looper = looper.Parent;
				}

				BaseAngularVelocity = totalAngVel.Angles();*/

				// VALVE HACKHACK: Scale this to fudge the relationship between vphysics friction values and player friction values.
				// A value of 0.8f feels pretty normal for vphysics, whereas 1.0f is normal for players.
				// This scaling trivially makes them equivalent.  REVISIT if this affects low friction surfaces too much.
				SurfaceFriction = tr.Surface.Friction * 1.25f;
				if (SurfaceFriction > 1) SurfaceFriction = 1;
			}
		}

		/// <summary>
		/// We're no longer on the ground, remove it
		/// </summary>
		public virtual void ClearGroundEntity()
		{
			if (GroundEntity == null) return;

			GroundEntity = null;
			GroundNormal = Vector3.Up;
			SurfaceFriction = 1.0f;
		}
		
		/// <summary>
		/// Traces the bbox and returns the trace result.
		/// LiftFeet will move the start position up by this amount, while keeping the top of the bbox at the same 
		/// position. This is good when tracing down because you won't be tracing through the ceiling above.
		/// </summary>
		public override TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
		{
			if ( liftFeet > 0 )
			{
				start += Vector3.Up * liftFeet;
				maxs = maxs.WithZ( maxs.z - liftFeet );
			}

			var tr = Trace.Ray( start + TraceOffset, end + TraceOffset )
				.Size( mins, maxs )
				.WithAnyTags( CollideTags )
				.Ignore( Pawn )
				.Run();

			tr.EndPosition -= TraceOffset;
			return tr;
		}

		/// <summary>
		/// Traces the current bbox and returns the result.
		/// liftFeet will move the start position up by this amount, while keeping the top of the bbox at the same
		/// position. This is good when tracing down because you won't be tracing through the ceiling above.
		/// </summary>
		public override TraceResult TraceBBox(Vector3 start, Vector3 end, float liftFeet = 0.0f)
		{
			/*if (Debug && Host.IsServer)
			{
				Vector3 center = start - end;
				DebugOverlay.Box(start, GetHull().Mins, GetHull().Maxs, Color.Cyan);
				DebugOverlay.Box(end, GetHull().Mins, GetHull().Maxs, Color.Cyan);
			}*/

			return TraceBBox(start, end, GetHull().Mins, GetHull().Maxs, liftFeet);
		}

		/// <summary>
		/// Try to keep a walking player on the ground when running down slopes etc
		/// </summary>
		public virtual void StayOnGround()
		{
			var start = Position + (Vector3.Up * 2 * Pawn.Scale);
			var end = Position + (Vector3.Down * StepSize * Pawn.Scale);

			// See how far up we can go without getting stuck
			var trace = TraceBBox(Position, start);
			start = trace.EndPosition;

			// Now trace down from a known safe position
			trace = TraceBBox(start, end);

			if (trace.Fraction <= 0) return; // must go somewhere
			if (trace.Fraction >= 1) return; // must hit something
			if (trace.StartedSolid) return; // can't be embedded in a solid
			if (Vector3.GetAngle(Vector3.Up, trace.Normal) > GroundStandableAngle) return; // can't hit a steep slope that we can't stand on anyway

			Position = trace.EndPosition;
		}
	}
}
