using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library]
	public partial class QPhysController : BasePlayerController
	{
		private const float TIME_TO_DUCK = 0.4f; // 0.2f in TF2 apparently
		private const float TIME_TO_UNDUCK = 0.2f;
		private const float GAMEMOVEMENT_DUCK_TIME = 1000.0f; // ms
		private const float GAMEMOVEMENT_JUMP_TIME = 510.0f; // ms approx - based on the 21 unit height jump
		private const float GAMEMOVEMENT_JUMP_HEIGHT = 21.0f; // units
		private const float GAMEMOVEMENT_TIME_TO_UNDUCK = (TIME_TO_UNDUCK * 1000.0f); // ms
		private const float GAMEMOVEMENT_TIME_TO_UNDUCK_INV = (GAMEMOVEMENT_DUCK_TIME - GAMEMOVEMENT_TIME_TO_UNDUCK);

		public Angles BaseAngularVelocity { get; set; }

		[Net] public float DefaultSpeed { get; set; } = 190.0f;
		[Net] public float WalkSpeed { get; set; } = 150.0f;
		[Net] public float SprintSpeed { get; set; } = 320.0f;
		[Net] public float Acceleration { get; set; } = 10.0f;
		[Net] public float AirAcceleration { get; set; } = 100.0f;
		[Net] public float FallSoundZ { get; set; } = -30.0f;
		[Net] public float GroundFriction { get; set; } = 4.0f;
		[Net] public float StopSpeed { get; set; } = 100.0f;
		[Net] public float DistEpsilon { get; set; } = 0.03125f;
		[Net] public float GroundAngle { get; set; } = 46.0f;
		[Net] public float Bounce { get; set; } = 0.0f;
		[Net] public float MoveFriction { get; set; } = 1.0f;
		[Net] public float StepSize { get; set; } = 18.0f;
		[Net] public float MaxNonJumpVelocity { get; set; } = 140.0f;
		[Net] public float Gravity { get; set; } = 800.0f;
		[Net] public float AirControl { get; set; } = 30.0f;
		public bool Swimming { get; set; } = false;

		[Net] public float HullGirth { get; set; } = 32.0f;
		[Net] public float HullHeight { get; set; } = 72.0f;
		[Net] public float DuckHullHeight { get; set; } = 36.0f;
		[Net] public float EyeHeight { get; set; } = 64.0f;
		[Net] public float DuckEyeHeight { get; set; } = 28.0f;
		private BBox m_hullNormal;
		private BBox m_hullDucked;

		public bool Ducking { get; set; } = false;
		public bool Ducked { get; set; } = false;
		public bool InDuckJump { get; set; } = false;
		private float m_flDucktime = 0.0f;
		private float m_flJumpTime = 0.0f;
		private float m_flDuckJumpTime = 0.0f;

		public Angles m_inputRotationLast;
		public Angles m_inputRotationDelta;

		public Unstuck Unstuck;

		private ArcadePlayer m_player;

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
			return GetHull(Ducked);
		}

		public virtual void StartGravity()
		{
			float ent_gravity = 1.0f;

			if (m_player.PhysicsBody != null && m_player.PhysicsBody.GravityScale != ent_gravity)
				ent_gravity = (float)m_player.PhysicsBody?.GravityScale;

			Velocity -= new Vector3(0, 0, ent_gravity * Gravity * 0.5f * (float)Math.Sqrt(Pawn.Scale)) * Time.Delta;
			Velocity += new Vector3(0, 0, BaseVelocity.z) * Time.Delta;

			BaseVelocity = BaseVelocity.WithZ(0);
		}

		public virtual void FinishGravity()
		{
			float ent_gravity = 1.0f;

			if (m_player.PhysicsBody != null && m_player.PhysicsBody.GravityScale != ent_gravity)
				ent_gravity = (float)m_player.PhysicsBody?.GravityScale;

			Velocity -= new Vector3(0, 0, ent_gravity * Gravity * 0.5f * (float)Math.Sqrt(Pawn.Scale)) * Time.Delta;
		}

		public void CalculateEyeRot()
		{
			//if (Host.IsClient)
			//	return;

			/*m_inputRotationDelta = Input.Rotation.Angles() - m_inputRotationLast;
			m_inputRotationLast = Input.Rotation.Angles();

			Angles fullCalc = (EyeRot.Angles() + m_inputRotationDelta).WithRoll(0) + (BaseAngularVelocity * Time.Delta);

			IADebugging.ScreenText(Input.Rotation.Angles().ToString());
			IADebugging.ScreenText(m_inputRotationLast.ToString());
			IADebugging.ScreenText(m_inputRotationDelta.ToString());

			EyeRot = Rotation.From(fullCalc.WithRoll(0));*/

			EyeRot = Input.Rotation;

			if (VR.Enabled)
			{
				EyeRot = Rotation.From(Input.VR.Head.Rotation.Pitch(), EyeRot.Yaw(), EyeRot.Roll());
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
				m_player = Pawn as ArcadePlayer;

			Gravity = float.Parse(ConsoleSystem.GetValue("sv_gravity"));

			EyePosLocal += TraceOffset;

			if (Host.IsServer)
				CalculateEyeRot();

			ReduceTimers();

			if (Unstuck.TestAndFix())
				return;

			// Check Stuck
			// Unstuck - or return if stuck

			if (Velocity.z > 250.0f)
				ClearGroundEntity();

			// store water level to compare later

			// if not on ground, store fall velocity

			//player->UpdateStepSound( player->m_pSurfaceData, mv->GetAbsOrigin(), mv->m_vecVelocity )

			UpdateDuckJumpEyeOffset();
			Duck();

			//
			// block: here on out is CGameMovement::FulLWalkMove
			//

			// RunLadderMode

			CheckLadder();
			Swimming = Pawn.WaterLevel.Fraction > 0.6f;

			//
			// Start Gravity
			//
			if (!Swimming && !m_isTouchingLadder)
			{
				StartGravity();
			}

			/*
			 if (player->m_flWaterJumpTime)
				{
					WaterJump();
					TryPlayerMove();
					// See if we are still in water?
					CheckWater();
					return;
				}
			*/

			// if ( underwater ) do underwater movement

			if (Input.Down(InputButton.Jump))
			{
				CheckJumpButton();
			}

			// Friction is handled before we add in any base velocity. That way, if we are on a conveyor,
			//  we don't slow when standing still, relative to the conveyor.
			bool bStartOnGround = GroundEntity != null;
			//bool bDropSound = false;
			if (bStartOnGround)
			{
				//if ( Velocity.z < FallSoundZ ) bDropSound = true;

				Velocity = Velocity.WithZ(0);
				//player->m_Local.m_flFallVelocity = 0.0f;

				Friction();

				Rotation toRotate = Rotation.From(BaseAngularVelocity) * Time.Delta;
				Position = Position.RotateAroundPoint(GroundEntity.Position, toRotate);
			}

			//
			// Work out wish velocity.. just take input, rotate it to view, clamp to -1, 1
			//
			WishVelocity = new Vector3(Input.Forward, Input.Left, 0);
			var inSpeed = WishVelocity.Length.Clamp(0, 1);
			WishVelocity *= EyeRot.Angles().WithPitch(0).ToRotation();

			if (!Swimming && !m_isTouchingLadder)
			{
				WishVelocity = WishVelocity.WithZ(0);
			}

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

			if (!Swimming && !m_isTouchingLadder)
				FinishGravity();

			if (GroundEntity != null)
			{
				Velocity = Velocity.WithZ(0);
			}

			//CheckFalling(); // block: fall damage effects, calls CGameMovement::PlayerRoughLandingEffects which sets the view punch

			// Land Sound
			// Swim Sounds

			const int pad = 19;
			if (Debug && Host.IsServer)
			{
				DebugOverlay.Box(Position + TraceOffset, GetHull().Mins, GetHull().Maxs, Color.Red);
				if (Ducking || Ducked)
					DebugOverlay.Box(Position, m_hullNormal.Mins, m_hullNormal.Maxs, Color.Blue);

				if (m_player.Camera is not FirstPersonCamera)
				{
					//DebugOverlay.Line(m_player.EyePos, m_player.EyePos + m_player.EyeRot.Forward * 8, Color.White);
				}

				if (IADebugging.LineOffset > 0)
					IADebugging.LineOffset++;

				IADebugging.ScreenText($"{"Position",pad}: {Position:F2}");
				//IADebugging.ScreenText($"{"Velocity",pad}: {Velocity:F2}");
				IADebugging.ScreenText($"{"Velocity (hu/s)",pad}: {Velocity.Length:F2}");
				IADebugging.ScreenText($"{"BaseVelocity",pad}: {BaseVelocity}");
				IADebugging.ScreenText($"{"BaseAngularVelocity",pad}: {BaseAngularVelocity}");
				IADebugging.ScreenText($"{"GroundEntity",pad}: {(GroundEntity != null ? $"{GroundEntity} [vel {GroundEntity?.Velocity}]" : "null")}");
				if (GroundEntity != null)
					IADebugging.ScreenText($"{"GroundNormal",pad}: {GroundNormal} (angle {GroundNormal.Angle(Vector3.Up)})");
				//IADebugging.ScreenText($"{"SurfaceFriction",pad}: {SurfaceFriction}");
				//IADebugging.ScreenText($"{"WishVelocity",pad}: {WishVelocity}");

				//IADebugging.ScreenText($"{"Ducked (FL_DUCKING)",pad}: {Ducked}");
				//IADebugging.ScreenText($"{"Ducking",pad}: {Ducking}");
				//IADebugging.ScreenText($"{"InDuckJump",pad}: {InDuckJump}");
				//IADebugging.ScreenText($"{"m_flDucktime",pad}: {m_flDucktime}");
				//IADebugging.ScreenText($"{"m_flJumpTime",pad}: {m_flJumpTime}");
				//IADebugging.ScreenText($"{"m_flDuckJumpTime",pad}: {m_flDuckJumpTime}");

				if (GroundEntity == null && m_debugHopType != HopType.None)
					IADebugging.ScreenText($"{"Hopping Type",pad}: {m_debugHopName}");
			}

		}

		public void ReduceTimers()
		{
			float frame_msec = 1000.0f * Time.Delta;

			m_flDucktime = Math.Max(0, m_flDucktime - frame_msec);
			m_flDuckJumpTime = Math.Max(0, m_flDuckJumpTime - frame_msec);
			m_flJumpTime = Math.Max(0, m_flJumpTime - frame_msec);
		}

		public Vector3 GetPlayerViewOffset(bool ducked)
		{
			return Vector3.Up * (ducked ? DuckEyeHeight : EyeHeight);
		}

		public virtual float GetWishSpeed()
		{
			float ws = -1;
			if (ws >= 0) return ws;

			if (Input.Down(InputButton.Duck)) ws = DefaultSpeed;
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

			//   Player.SetAnimParam( "forward", Input.Forward );
			//   Player.SetAnimParam( "sideward", Input.Right );
			//   Player.SetAnimParam( "wishspeed", wishspeed );
			//    Player.SetAnimParam( "walkspeed_scale", 2.0f / 190.0f );
			//   Player.SetAnimParam( "runspeed_scale", 2.0f / 320.0f );

			//  DebugOverlay.Text( 0, Pos + Vector3.Up * 100, $"forward: {Input.Forward}\nsideward: {Input.Right}" );

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
					Position = pm.EndPos;
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
			MoveHelper mover = new(Position, Velocity);
			mover.Trace = mover.Trace.Size(GetHull().Mins, GetHull().Maxs).Ignore(Pawn);
			mover.MaxStandableAngle = GroundAngle;

			// block: this is essentially a minified version of CGameMovement::TryPlayerMove
			// a lot of the functionality is split into other parts of this class (MoveHelper)
			// it simplifies a lot of it down, but it all does look correct
			// the only thing that seems to be missing is the ability to slam into a wall at the end
			mover.TryMoveWithStep(Time.Delta, (StepSize * Pawn.Scale) + DistEpsilon);

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual void Move()
		{
			MoveHelper mover = new(Position, Velocity);
			mover.Trace = mover.Trace.Size(GetHull().Mins, GetHull().Maxs).Ignore(Pawn);
			mover.MaxStandableAngle = GroundAngle;

			mover.TryMove(Time.Delta);

			Position = mover.Position;
			Velocity = mover.Velocity;
		}

		public virtual void Duck()
		{
			bool bInAir = GroundEntity == null;
			bool bInDuck = Ducked; // FL_DUCKING is tied to Ducked's value
			bool bDuckJump = m_flJumpTime > 0.0f;
			bool bDuckJumpTime = m_flDuckJumpTime > 0.0f;

			bool bBindDown = Input.Down(InputButton.Duck);
			bool bBindReleased = Input.Released(InputButton.Duck);

			// Slow down ducked players.
			if (bInDuck)
			{
				float duckFrac = 0.33333333f;
				Input.Forward *= duckFrac;
				Input.Left *= duckFrac;
				Input.Up *= duckFrac;
			}

			// If the player is holding down the duck button, the player is in duck transition, ducking, or duck-jumping.
			if (bBindDown || Ducking || bInDuck || bDuckJump)
			{
				// DUCK
				if (bBindDown || bDuckJump)
				{
					// Have the duck button pressed, but the player currently isn't in the duck position.
					if (bBindDown && !Ducking && !bDuckJump && !bDuckJumpTime)
					{
						m_flDucktime = GAMEMOVEMENT_DUCK_TIME;
						Ducking = true;
					}

					// The player is in duck transition and not duck-jumping.
					if (Ducking && !bDuckJump && !bDuckJumpTime)
					{
						float flDuckMilliseconds = Math.Max(0.0f, GAMEMOVEMENT_DUCK_TIME - m_flDucktime);
						float flDuckSeconds = flDuckMilliseconds * 0.001f;

						// Finish in duck transition when transition time is over, in "duck", in air.
						if (flDuckSeconds > TIME_TO_DUCK || bInDuck || bInAir)
						{
							FinishDuck();
						}
						else
						{
							// Calc parametric time
							float flDuckFraction = SimpleSpline(flDuckSeconds / TIME_TO_DUCK);
							SetDuckedEyeOffset(flDuckFraction);
						}
					}

					if (bDuckJump)
					{
						// Make the bounding box small immediately.
						if (!bInDuck)
						{
							StartUnDuckJump();
						}
						else
						{
							// Check for a crouch override.
							if (!bBindDown)
							{
								TraceResult trace = default;
								if (CanUnDuckJump(ref trace))
								{
									FinishUnDuckJump(ref trace);
									m_flDuckJumpTime = (GAMEMOVEMENT_TIME_TO_UNDUCK * (1.0f - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_INV;
								}
							}
						}
					}
				}
				// UNDUCK (or attempt to...)
				else
				{
					if (InDuckJump)
					{
						if (!bBindDown)
						{
							TraceResult trace = default;
							if (CanUnDuckJump(ref trace))
							{
								FinishUnDuckJump(ref trace);

								if (trace.Fraction < 1.0f)
									m_flDuckJumpTime = (GAMEMOVEMENT_TIME_TO_UNDUCK * (1.0f - trace.Fraction)) + GAMEMOVEMENT_TIME_TO_UNDUCK_INV;
							}
						}
						else
							InDuckJump = false;
					}

					if (bDuckJumpTime)
						return;

					if (bInAir || Ducking)
					{
						if (bBindReleased)
						{
							if (bInDuck && !bDuckJump)
							{
								m_flDucktime = GAMEMOVEMENT_DUCK_TIME;
							}
							else if (Ducking && !Ducked)
							{
								// Invert time if release before fully ducked!!!
								float unduckMilliseconds = 1000.0f * TIME_TO_UNDUCK;
								float duckMilliseconds = 1000.0f * TIME_TO_DUCK;
								float elapsedMilliseconds = GAMEMOVEMENT_DUCK_TIME - m_flDucktime;

								float fracDucked = elapsedMilliseconds / duckMilliseconds;
								float remainingUnduckMilliseconds = fracDucked * unduckMilliseconds;

								m_flDucktime = GAMEMOVEMENT_DUCK_TIME - unduckMilliseconds + remainingUnduckMilliseconds;
							}
						}
					}

					if (CanUnduck())
					{
						if (Ducking || Ducked)
						{
							float flDuckMilliseconds = Math.Max(0.0f, GAMEMOVEMENT_DUCK_TIME - m_flDucktime);
							float flDuckSeconds = flDuckMilliseconds * 0.001f;

							if (flDuckSeconds > TIME_TO_UNDUCK || (bInAir && !bDuckJump))
							{
								FinishUnDuck();
							}
							else
							{
								// Calc parametric time
								float flDuckFraction = SimpleSpline(1.0f - (flDuckSeconds / TIME_TO_UNDUCK));
								SetDuckedEyeOffset(flDuckFraction);
								Ducking = true;
							}
						}
					}
					else
					{
						// Still under something where we can't unduck, so make sure we reset this timer so
						//  that we'll unduck once we exit the tunnel, etc.
						if (m_flDucktime != GAMEMOVEMENT_DUCK_TIME)
						{
							SetDuckedEyeOffset(1.0f);
							m_flDucktime = GAMEMOVEMENT_DUCK_TIME;
							Ducked = true;
							Ducking = false;
						}
					}
				}
			}

			if (Ducked || m_flDucktime != 0)
				m_player.Controller.SetTag("ducked");
		}

		public virtual void FinishDuck()
		{
			if (Ducked)
				return;

			Ducked = true;
			Ducking = false;

			EyePosLocal = GetPlayerViewOffset(true) * Pawn.Scale;

			// HACKHACK - Fudge for collision bug - no time to fix this properly
			if (GroundEntity != null)
			{
				Position -= (m_hullDucked.Mins - m_hullNormal.Mins) * Pawn.Scale;
			}
			else
			{
				Vector3 hullSizeNormal = (m_hullNormal.Maxs - m_hullNormal.Mins) * Pawn.Scale;
				Vector3 hullSizeCrouch = (m_hullDucked.Maxs - m_hullDucked.Mins) * Pawn.Scale;
				Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);
				Position += viewDelta;
			}

			// Valve do a stuck fix here

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		public virtual bool CanUnduck()
		{
			Vector3 newOrigin = Position;

			if (GroundEntity != null)
			{
				newOrigin += (m_hullDucked.Mins - m_hullNormal.Mins) * Pawn.Scale;
			}
			else
			{
				// If in air an letting go of crouch, make sure we can offset origin to make
				//  up for uncrouching
				Vector3 hullSizeNormal = (m_hullNormal.Maxs - m_hullNormal.Mins) * Pawn.Scale;
				Vector3 hullSizeCrouch = (m_hullDucked.Maxs - m_hullDucked.Mins) * Pawn.Scale;
				Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);
				newOrigin -= viewDelta;
			}

			bool wasDucked = Ducked;
			Ducked = false;
			TraceResult tr = TraceBBox(Position, newOrigin);
			Ducked = wasDucked;
			if (tr.StartedSolid || tr.Fraction != 1.0f)
				return false;

			return true;
		}

		public virtual void FinishUnDuck()
		{
			Vector3 newOrigin = Position;

			if (GroundEntity != null)
			{
				newOrigin += (m_hullDucked.Mins - m_hullNormal.Mins) * Pawn.Scale;
			}
			else
			{
				// If in air an letting go of crouch, make sure we can offset origin to make
				//  up for uncrouching
				Vector3 hullSizeNormal = (m_hullNormal.Maxs - m_hullNormal.Mins) * Pawn.Scale;
				Vector3 hullSizeCrouch = (m_hullDucked.Maxs - m_hullDucked.Mins) * Pawn.Scale;
				Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);
				newOrigin -= viewDelta;
			}

			Ducked = false;
			Ducking = false;
			InDuckJump = false;
			EyePosLocal = GetPlayerViewOffset(false) * Pawn.Scale;
			m_flDucktime = 0;

			Position = newOrigin;

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		public virtual void StartUnDuckJump()
		{
			Ducked = true;
			Ducking = false;

			EyePosLocal = GetPlayerViewOffset(true) * Pawn.Scale;

			Vector3 hullSizeNormal = (m_hullNormal.Maxs - m_hullNormal.Mins) * Pawn.Scale;
			Vector3 hullSizeCrouch = (m_hullDucked.Maxs - m_hullDucked.Mins) * Pawn.Scale;
			Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);
			Position += viewDelta;

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		public virtual bool CanUnDuckJump(ref TraceResult trace)
		{
			// block: this is the magical thing that causes snapping to surfaces
			// in at least HL2, if maxplayers = 1, all jumps are considered as crouch jumps
			// because of this, when the jump finishes, it will try to snap to the nearest surface to finish the duck jump
			// a trace is made

			Vector3 vecEnd = Position.WithZ(Position.z - DuckHullHeight);
			// Trace down to the stand position and see if we can stand.
			trace = TraceBBox(Position, vecEnd);
			if (trace.Fraction < 1.0f)
			{
				// Find the endpoint.
				vecEnd.z = Position.z + (-DuckHullHeight * trace.Fraction);

				// Test a normal hull.
				bool wasDucked = Ducked;
				Ducked = false;
				TraceResult tr = TraceBBox(vecEnd, vecEnd);
				Ducked = wasDucked;
				if (!tr.StartedSolid)
					return true;
			}

			return false;
		}

		public virtual void FinishUnDuckJump(ref TraceResult trace)
		{
			Vector3 hullSizeNormal = (m_hullNormal.Maxs - m_hullNormal.Mins) * Pawn.Scale;
			Vector3 hullSizeCrouch = (m_hullDucked.Maxs - m_hullDucked.Mins) * Pawn.Scale;
			Vector3 viewDelta = (hullSizeNormal - hullSizeCrouch);

			float flDeltaZ = viewDelta.z;
			viewDelta.z *= trace.Fraction;
			flDeltaZ -= viewDelta.z;

			Ducked = false;
			Ducking = false;
			InDuckJump = false;
			m_flDucktime = 0;
			m_flDuckJumpTime = 0;
			m_flJumpTime = 0;

			EyePosLocal = EyePosLocal.WithZ(EyePosLocal.z - flDeltaZ) * Pawn.Scale;
			Position -= viewDelta;

			// block: this is where that snapping to a surface after jumping thing comes from
			// check CanUnDuckJump for more info

			// Recategorize position since ducking can change origin
			CategorizePosition();
		}

		public void UpdateDuckJumpEyeOffset()
		{
			if (m_flDuckJumpTime != 0)
			{
				float flDuckMilliseconds = Math.Max(0.0f, GAMEMOVEMENT_DUCK_TIME - m_flDuckJumpTime);
				float flDuckSeconds = flDuckMilliseconds / GAMEMOVEMENT_DUCK_TIME;

				// Finish in duck transition when transition time is over, in "duck", in air.
				if (flDuckSeconds > TIME_TO_UNDUCK)
				{
					m_flDuckJumpTime = 0;
					SetDuckedEyeOffset(0);
				}
				else
				{
					float flDuckFraction = SimpleSpline(1.0f - (flDuckSeconds / TIME_TO_UNDUCK));
					SetDuckedEyeOffset(flDuckFraction);
				}

				return;
			}

			if (EyePosLocal == Vector3.Zero)
				EyePosLocal = Vector3.Up * EyeHeight * Pawn.Scale;
		}

		public void SetDuckedEyeOffset(float duckFraction)
		{
			Vector3 vDuckHullMin = GetHull(true).Mins;
			Vector3 vStandHullMin = GetHull(false).Mins;

			float fMore = (vDuckHullMin.z - vStandHullMin.z);

			Vector3 vecDuckViewOffset = GetPlayerViewOffset(true);
			Vector3 vecStandViewOffset = GetPlayerViewOffset(false);
			Vector3 temp = EyePosLocal;
			temp.z = ((vecDuckViewOffset.z - fMore) * duckFraction) +
				(vecStandViewOffset.z * (1 - duckFraction));
			EyePosLocal = temp * Pawn.Scale;
		}

		/// <summary>
		/// Add our wish direction and speed onto our velocity
		/// </summary>
		public virtual void Accelerate(Vector3 wishdir, float wishspeed, float speedLimit, float acceleration)
		{
			// This gets overridden because some games (CSPort) want to allow dead (observer) players
			// to be able to move around.
			// if ( !CanAccelerate() )
			//     return;

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
			// If we are in water jump cycle, don't apply friction
			//if ( player->m_flWaterJumpTime )
			//   return;

			// Not on ground - no friction


			// Calculate speed
			float speed = Velocity.Length;
			//if (speed < 0.1f) return;

			float drop = 0;

			if (GroundEntity != null)
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
			{
				newspeed /= speed;
				Velocity *= newspeed;
			}

			// mv->m_outWishVel -= (1.f-newspeed) * mv->m_vecVelocity;
		}

		[ConVar.Replicated] public static bool player_movement_pogo_jumps { get; set; } = true;
		[ConVar.Replicated] public static bool player_movement_air_jumps { get; set; } = false;
		[ConVar.Replicated] public static bool player_movement_jump_while_crouched { get; set; } = true;
		[ConVar.Replicated] public static bool player_movement_ahop { get; set; } = true;
		[ConVar.Replicated] public static bool player_movement_bhop { get; set; } = true;

		public virtual void CheckJumpButton()
		{
			//if ( !player->CanJump() )
			//    return false;


			/*
			if ( player->m_flWaterJumpTime )
			{
				player->m_flWaterJumpTime -= gpGlobals->frametime();
				if ( player->m_flWaterJumpTime < 0 )
					player->m_flWaterJumpTime = 0;

				return false;
			}*/



			// If we are in the water most of the way...
			if (Swimming)
			{
				// swimming, not jumping
				ClearGroundEntity();

				Velocity = Velocity.WithZ(100);

				// play swimming sound
				//  if ( player->m_flSwimSoundTime <= 0 )
				{
					// Don't play sound again for 1 second
					//   player->m_flSwimSoundTime = 1000;
					//   PlaySwimSound();
				}

				return;
			}

			if (!player_movement_air_jumps && GroundEntity == null)
				return;

			if (!player_movement_pogo_jumps && !Input.Pressed(InputButton.Jump))
				return;

			if (!player_movement_jump_while_crouched && Ducking && Ducked)
				return;

			// Still updating the eye position.
			if (!player_movement_jump_while_crouched && m_flDuckJumpTime > 0)
				return;

			m_player.OnAnimEventFootstep(Position, 0, 2f, true);

			ClearGroundEntity();

			// player->PlayStepSound( (Vector &)mv->GetAbsOrigin(), player->m_pSurfaceData, 1.0, true );

			// MoveHelper()->PlayerSetAnimation( PLAYER_JUMP );

			float flGroundFactor = 1.0f;
			//if ( player->m_pSurfaceData )
			{
				//   flGroundFactor = g_pPhysicsQuery->GetGameSurfaceproperties( player->m_pSurfaceData )->m_flJumpFactor;
			}

			float flMul;

			switch (Gravity)
			{
				case 600f:
					flMul = 160.0f; // approx. 21 units.
					break;
				case 800f:
					flMul = 268.3281572999747f; // approx. 45 units.
					break;
				default:
					flMul = (float)Math.Sqrt(2 * Math.Abs(Gravity) * 21.0f) * Math.Sign(Gravity);
					break;
			}

			if (Pawn.Scale != 1)
				flMul *= (float)Math.Sqrt(Pawn.Scale);

			float startz = Velocity.z;

			if (Ducked)
				Velocity = Velocity.WithZ(flGroundFactor * flMul);
			else
				Velocity = Velocity.WithZ(startz + (flGroundFactor * flMul));

			// ahopping and bhopping
			{
				Vector3 vecForward = EyeRot.Forward.WithZ(0).Normal;

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

			// mv->m_outJumpVel.z += mv->m_vecVelocity[2] - startz;
			// mv->m_outStepHeight += 0.15f;

			// HL2 does this when maxplayers = 1
			//m_flJumpTime = GAMEMOVEMENT_JUMP_TIME;
			//InDuckJump = true;

			// don't jump again until released
			//mv->m_nOldButtons |= IN_JUMP;

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
						.HitLayer(CollisionLayer.All, false)
						.HitLayer(CollisionLayer.LADDER, true)
						.Ignore(Pawn)
						.Run();

			m_isTouchingLadder = false;

			if (pm.Hit && !(pm.Entity is ModelEntity me && me.CollisionGroup == CollisionGroup.Always))
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

			// Doing this before we move may introduce a potential latency in water detection, but
			// doing it after can get us stuck on the bottom in water if the amount we move up
			// is less than the 1 pixel 'threshold' we're about to snap to.	Also, we'll call
			// this several times per frame, so we really need to avoid sticking to the bottom of
			// water on each call, and the converse case will correct itself if called twice.
			//CheckWater();

			var point = Position.WithZ(Position.z - 2.0f);
			var bumpOrigin = Position;

			bool bMovingUp = Velocity.z > 0;
			bool bMovingUpRapidly = Velocity.z > MaxNonJumpVelocity * Pawn.Scale;

			if (bMovingUpRapidly || Swimming) // or ladder and moving up
			{
				ClearGroundEntity();
				return;
			}

			var pm = TraceBBox(bumpOrigin, point);

			if (pm.Entity == null || Vector3.GetAngle(Vector3.Up, pm.Normal) > GroundAngle)
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
				Position = pm.EndPos;
			}

		}

		/// <summary>
		/// We have a new ground entity
		/// </summary>
		public virtual void UpdateGroundEntity(TraceResult tr)
		{
			GroundNormal = tr.Normal;

			//if ( tr.Entity == GroundEntity ) return;

			Vector3 oldGroundVelocity = default;
			if (GroundEntity != null) oldGroundVelocity = GroundEntity.Velocity;

			bool wasOffGround = GroundEntity == null;

			GroundEntity = tr.Entity;

			if (GroundEntity != null)
			{
				BaseVelocity = GroundEntity.Velocity;
				BaseAngularVelocity = GroundEntity.AngularVelocity;

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

			/*
				m_vecGroundUp = pm.m_vHitNormal;
				player->m_surfaceProps = pm.m_pSurfaceProperties->GetNameHash();
				player->m_pSurfaceData = pm.m_pSurfaceProperties;
				const CPhysSurfaceProperties *pProp = pm.m_pSurfaceProperties;

				const CGameSurfaceProperties *pGameProps = g_pPhysicsQuery->GetGameSurfaceproperties( pProp );
				player->m_chTextureType = (int8)pGameProps->m_nLegacyGameMaterial;
			*/
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
			start = trace.EndPos;

			// Now trace down from a known safe position
			trace = TraceBBox(start, end);

			if (trace.Fraction <= 0) return; // must go somewhere
			if (trace.Fraction >= 1) return; // must hit something
			if (trace.StartedSolid) return; // can't be embedded in a solid
			if (Vector3.GetAngle(Vector3.Up, trace.Normal) > GroundAngle) return; // can't hit a steep slope that we can't stand on anyway

			Position = trace.EndPos;
		}

		// misc Valve functions
		public float SimpleSpline(float value, float scale = 1)
		{
			value = scale * value;
			float valueSquared = value * value;

			// Nice little ease-in, ease-out spline-like curve
			return (3 * valueSquared - 2 * valueSquared * value);
		}
	}
}
