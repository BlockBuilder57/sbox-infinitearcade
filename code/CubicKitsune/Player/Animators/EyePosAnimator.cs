using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public class EyePosAnimator : PawnAnimator
	{
		TimeSince TimeSinceFootShuffle = 60;

		float duck;
		float skidAmount = 0;

		public override void Simulate()
		{
			var idealRotation = Rotation.LookAt(Pawn.EyeRotation.Forward.WithZ(0), Vector3.Up);

			DoRotation(idealRotation);
			DoWalk();

			//
			// Let the animation graph know some shit
			//
			bool sitting = HasTag("sitting");
			bool noclip = HasTag("noclip") && !sitting;

			SetAnimParameter("b_grounded", GroundEntity != null || noclip || sitting);
			SetAnimParameter("b_noclip", noclip);
			SetAnimParameter("b_sit", sitting);
			SetAnimParameter("b_swim", Pawn.WaterLevel > 0.5f && !sitting);

			if (Host.IsClient && Client.IsValid())
			{
				SetAnimParameter("voice", Client.TimeSinceLastVoice < 0.5f ? Client.VoiceLevel : 0.0f);
			}

			Vector3 aimPos = Pawn.EyePosition + Pawn.EyeRotation.Forward * 200;
			Vector3 lookPos = aimPos;

			SetLookAt("aim_eyes", lookPos);
			SetLookAt("aim_head", lookPos);
			SetLookAt("aim_body", aimPos);

			if (HasTag("ducked"))
				duck = duck.LerpTo(1.0f, Time.Delta * 10.0f);
			else
				duck = duck.LerpTo(0.0f, Time.Delta * 5.0f);

			SetAnimParameter("duck", duck);

			if (Pawn is Player player)
			{
				if (player.ActiveChild is BaseCarriable carry)
				{
					carry.SimulateAnimator(this);
				}
				else
				{
					SetAnimParameter("holdtype", 0);
					SetAnimParameter("aim_body_weight", 0.5f);
				}

				if (player.Controller is QPhysController qPhys)
				{
					if (GroundEntity.IsValid())
					{
						// skidding!

						float curSpeed = Velocity.Length;
						float wishSpeed = WishVelocity.Length;
						float maxSpeed = qPhys.SprintSpeed * Pawn.Scale * 1.25f; // mm, strafe fudge

						float fracSpeed = curSpeed / maxSpeed;
						float fracWish = wishSpeed / maxSpeed;

						// finish existing skids, unless we're moving faster than them
						if (skidAmount > 0 && (skidAmount > fracWish + 0.1f)) // more fudge
							skidAmount = fracSpeed;

						// if we're over speed, start a skid.
						else if (curSpeed > maxSpeed)
							skidAmount = fracSpeed - 1f; // push the range so we need at least 2x maxSpeed to count

						// otherwise, reset skids
						else
							skidAmount = 0;

						skidAmount = MathF.Max(0, skidAmount);
						SetAnimParameter("skid", skidAmount);

						/*if (Host.IsServer)
						{
							if (skidAmount > 0)
								DebugOverlay.Text(Position + Vector3.Up * 70, $"SKIDDING! {skidAmount:F2}\nfracWish: {fracWish:F2}");

							DebugOverlay.Line(Position, Position + qPhys.Velocity.ClampLength(32), Color.White);
							DebugOverlay.Line(Position, Position + qPhys.WishVelocity.ClampLength(32), Color.Yellow);
							DebugOverlay.Line(Position, Position + qPhys.DeltaVelocity, Color.Red);

							DebugOverlay.Text(Position + qPhys.Velocity.ClampLength(32), qPhys.Velocity.ToString(), Color.White);
							DebugOverlay.Text(Position + qPhys.WishVelocity.ClampLength(32), qPhys.WishVelocity.ToString(), Color.Yellow);
							DebugOverlay.Text(Position + qPhys.DeltaVelocity, qPhys.DeltaVelocity.ToString(), Color.Red);
						}*/
					}
				}
			}

		}

		public virtual void DoRotation(Rotation idealRotation)
		{
			//
			// Our ideal player model rotation is the way we're facing
			//
			var allowYawDiff = Pawn is Player player && player.ActiveChild == null ? 90 : 50;

			float turnSpeed = 0.01f;
			if (HasTag("ducked"))
				turnSpeed = 0.1f;

			//
			// If we're moving, rotate to our ideal rotation
			//
			Rotation = Rotation.Slerp(Rotation, idealRotation, WishVelocity.Length * Time.Delta * turnSpeed);

			//
			// Clamp the foot rotation to within 120 degrees of the ideal rotation
			//
			Rotation = Rotation.Clamp(idealRotation, allowYawDiff, out var change);

			//
			// If we did restrict, and are standing still, add a foot shuffle
			//
			if (change > 1 && WishVelocity.Length <= 1) TimeSinceFootShuffle = 0;

			SetAnimParameter("b_shuffle", TimeSinceFootShuffle < 0.1);
		}

		void DoWalk()
		{
			// Move Speed
			{
				var dir = Velocity * 1 / Pawn.Scale;
				var forward = Rotation.Forward.Dot(dir);
				var sideward = Rotation.Right.Dot(dir);

				var angle = MathF.Atan2(sideward, forward).RadianToDegree().NormalizeDegrees();

				SetAnimParameter("move_direction", angle);
				SetAnimParameter("move_speed", dir.Length);
				SetAnimParameter("move_groundspeed", dir.WithZ(0).Length);
				SetAnimParameter("move_y", sideward);
				SetAnimParameter("move_x", forward);
			}

			// Wish Speed
			{
				var dir = WishVelocity * 1 / Pawn.Scale;
				var forward = Rotation.Forward.Dot(dir);
				var sideward = Rotation.Right.Dot(dir);

				var angle = MathF.Atan2(sideward, forward).RadianToDegree().NormalizeDegrees();

				SetAnimParameter("wish_direction", angle);
				SetAnimParameter("wish_speed", dir.Length);
				SetAnimParameter("wish_groundspeed", dir.WithZ(0).Length);
				SetAnimParameter("wish_y", sideward);
				SetAnimParameter("wish_x", forward);
			}
		}

		public override void OnEvent(string name)
		{
			//DebugOverlay.Text(Pos + Vector3.Up * 100, name, 5.0f);

			if (name == "jump")
			{
				Trigger("b_jump");
			}

			base.OnEvent(name);
		}
	}
}
