using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library]
	public class QPhysController : WalkController
	{

		[ConVar.Replicated("player_movement_pogo_jumps")]
		public static bool player_movement_pogo_jumps { get; set; } = true;
		[ConVar.Replicated("player_movement_air_jumps")]
		public static bool player_movement_air_jumps { get; set; } = false;
		[ConVar.Replicated("player_movement_ahop")]
		public static bool player_movement_ahop { get; set; } = true;
		[ConVar.Replicated("player_movement_bhop")]
		public static bool player_movement_bhop { get; set; } = true;

		private ArcadePlayer m_player;
		private int m_debugLineOffset = 0;

		public override void Simulate()
		{
			// can't do this in the ctor for some reason
			if (!m_player.IsValid())
			{
				m_player = Pawn as ArcadePlayer;
			}

			EyePosLocal = Vector3.Up * (EyeHeight * Pawn.Scale);
			UpdateBBox();

			EyePosLocal += TraceOffset;
			EyeRot = Input.Rotation;

			//RestoreGroundPos();

			//Velocity += BaseVelocity * ( 1 + Time.Delta * 0.5f );
			//BaseVelocity = Vector3.Zero;

			//Rot = Rotation.LookAt( Input.Rotation.Forward.WithZ( 0 ), Vector3.Up );

			if (Unstuck.TestAndFix())
				return;

			if (Velocity.z > 250.0f)
				ClearGroundEntity();

			// store water level to compare later

			// if not on ground, store fall velocity

			// RunLadderMode

			CheckLadder();
			Swimming = Pawn.WaterLevel.Fraction > 0.6f;

			//
			// Start Gravity
			//
			if (!Swimming && !IsTouchingLadder)
			{
				Velocity -= new Vector3(0, 0, Gravity * 0.5f) * Time.Delta;
				Velocity += new Vector3(0, 0, BaseVelocity.z) * Time.Delta;

				BaseVelocity = BaseVelocity.WithZ(0);
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

			// Fricion is handled before we add in any base velocity. That way, if we are on a conveyor,
			//  we don't slow when standing still, relative to the conveyor.
			bool bStartOnGround = GroundEntity != null;
			//bool bDropSound = false;
			if (bStartOnGround)
			{
				//if ( Velocity.z < FallSoundZ ) bDropSound = true;

				Velocity = Velocity.WithZ(0);
				//player->m_Local.m_flFallVelocity = 0.0f;

				if (GroundEntity != null)
				{
					ApplyFriction(GroundFriction * SurfaceFriction);
				}
			}

			//
			// Work out wish velocity.. just take input, rotate it to view, clamp to -1, 1
			//
			WishVelocity = new Vector3(Input.Forward, Input.Left, 0);
			var inSpeed = WishVelocity.Length.Clamp(0, 1);
			WishVelocity *= Input.Rotation;

			if (!Swimming && !IsTouchingLadder)
			{
				WishVelocity = WishVelocity.WithZ(0);
			}

			WishVelocity = WishVelocity.Normal * inSpeed;
			WishVelocity *= GetWishSpeed();

			Duck.PreTick();

			bool bStayOnGround = false;
			if (Swimming)
			{
				ApplyFriction(1);
				WaterMove();
			}
			else if (IsTouchingLadder)
			{
				LadderMove();
			}
			else if (GroundEntity != null)
			{
				bStayOnGround = true;
				WalkMove();
			}
			else
			{
				AirMove();
			}

			CategorizePosition(bStayOnGround);

			// FinishGravity
			if (!Swimming && !IsTouchingLadder)
			{
				Velocity -= new Vector3(0, 0, Gravity * 0.5f) * Time.Delta;
			}


			if (GroundEntity != null)
			{
				Velocity = Velocity.WithZ(0);
			}

			// CheckFalling(); // fall damage etc

			// Land Sound
			// Swim Sounds

			//SaveGroundPos();

			m_debugLineOffset = 0;
			if (Debug && Host.IsServer)
			{
				DebugOverlay.Box(Position + TraceOffset, mins, maxs, Color.Red);
				DebugOverlay.Box(Position, mins, maxs, Color.Blue);

				//if (Host.IsServer) m_debugLineOffset = 10;

				DebugOverlay.ScreenText(m_debugLineOffset++, $"        Position: {Position:F2}");
				//DebugOverlay.ScreenText(m_debugLineOffset++, $"        Velocity: {Velocity:F2}");
				DebugOverlay.ScreenText(m_debugLineOffset++, $" Velocity (hu/s): {Velocity.Length:F2}");
				//DebugOverlay.ScreenText(m_debugLineOffset++, $"    BaseVelocity: {BaseVelocity}");
				DebugOverlay.ScreenText(m_debugLineOffset++, $"    GroundEntity: {GroundEntity} [{GroundEntity?.Velocity}]");
				//DebugOverlay.ScreenText(m_debugLineOffset++, $" SurfaceFriction: {SurfaceFriction}");
				DebugOverlay.ScreenText(m_debugLineOffset++, $"    WishVelocity: {WishVelocity}");
			}

		}

		bool IsTouchingLadder = false;
		Vector3 LadderNormal;

		public override void CheckLadder()
		{
			if (IsTouchingLadder && Input.Pressed(InputButton.Jump))
			{
				Velocity = LadderNormal * 100.0f;
				IsTouchingLadder = false;

				return;
			}

			const float ladderDistance = 1.0f;
			var start = Position;
			Vector3 end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : WishVelocity.Normal) * ladderDistance;

			var pm = Trace.Ray(start, end)
						.Size(mins, maxs)
						.HitLayer(CollisionLayer.All, false)
						.HitLayer(CollisionLayer.LADDER, true)
						.Ignore(Pawn)
						.Run();

			IsTouchingLadder = false;

			if (pm.Hit)
			{
				IsTouchingLadder = true;
				LadderNormal = pm.Normal;
			}
		}

		public override void CheckJumpButton()
		{
			// If we are in the water most of the way...
			if (Swimming)
			{
				// swimming, not jumping
				ClearGroundEntity();

				Velocity = Velocity.WithZ(100);
				return;
			}

			if (!player_movement_air_jumps && GroundEntity == null)
				return;

			if (!player_movement_pogo_jumps && !Input.Pressed(InputButton.Jump))
				return;

			// make jump sound
			m_player.OnAnimEventFootstep(Position, 0, 2f, true);

			ClearGroundEntity();

			float flGroundFactor = 1.0f;
			//if ( player->m_pSurfaceData )
			{
				//   flGroundFactor = g_pPhysicsQuery->GetGameSurfaceproperties( player->m_pSurfaceData )->m_flJumpFactor;
			}

			float flMul = 1.0f;

			flMul = 268.3281572999747f * 1.2f;

			float startz = Velocity.z;

			if (Duck.IsActive)
				Velocity = Velocity.WithZ(flGroundFactor * flMul);
			else
				Velocity = Velocity.WithZ(startz + (flGroundFactor * flMul));

			// ahopping and bhopping
			{
				Vector3 vecForward = EyeRot.Forward.WithZ(0).Normal;

				float flSpeedBoostPerc = (!Input.Down(InputButton.Run) && !Duck.IsActive) ? 0.5f : 0.1f;
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
							string ahopType = "";

							if (Input.Forward < 0.0f)
							{
								if (Input.Left != 0.0f)
									ahopType = "Accelerated Side Hop (ASH)";
								else
									ahopType = "Accelerated Forward Hop (AFH)";
							}
							else
								ahopType = "Accelerated Back Hop (ABH)";

							DebugOverlay.ScreenText(m_debugLineOffset++, "we're ahopping I think, it's an " + ahopType, 1f);
						}
					}
				}
				else if (!player_movement_bhop)
				{
					Velocity += (vecForward * flSpeedAddition);
				}
				else
				{
					if (Debug && Host.IsServer)
						DebugOverlay.ScreenText(m_debugLineOffset++, "we're bhopping I think", 1f);
				}
			}

			// why isn't this FinishGravity anymore?
			Velocity -= new Vector3(0, 0, Gravity * 0.5f) * Time.Delta;

			AddEvent("jump");
		}
	}
}
