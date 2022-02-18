using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Joints;
using Sandbox.UI;

namespace infinitearcade
{
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		[ServerCmd("spawn")]
		public static void SpawnCommand(string modelname)
		{
			var owner = ConsoleSystem.Caller?.Pawn;

			if (ConsoleSystem.Caller == null)
				return;

			var tr = Trace.Ray(owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 500)
				.UseHitboxes()
				.Ignore(owner)
				.Size(2)
				.Run();

			var ent = new Prop
			{
				Position = tr.EndPos,
				Rotation = Rotation.From(new Angles(0, owner.EyeRotation.Angles().yaw, 0)) * Rotation.FromAxis(Vector3.Up, 180)
			};
			ent.SetModel(modelname);

			// Drop to floor
			if (ent.PhysicsBody != null && ent.PhysicsGroup.BodyCount == 1)
			{
				var p = ent.PhysicsBody.FindClosestPoint(tr.EndPos);

				var delta = p - tr.EndPos;
				ent.PhysicsBody.Position -= delta;
				//DebugOverlay.Line( p, tr.EndPos, 10, false );
			}
		}

		[ServerCmd("spawncarriable")]
		public static void SpawnWeaponCommand(string path)
		{
			if (ConsoleSystem.Caller == null)
				return;

			var owner = ConsoleSystem.Caller.Pawn;

			if (!path.StartsWith("carriables/"))
				path = "carriables/" + path;

			IACarriable carriable = IACarriableDefinition.GetEntity(path);
			if (carriable == null)
				return;

			var tr = Trace.Ray(owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 200)
				.UseHitboxes()
				.Ignore(owner)
				.Size(2)
				.Run();

			carriable.Position = tr.EndPos + Vector3.Up * 8;
			carriable.Rotation = Rotation.From(new Angles(0, owner.EyeRotation.Angles().yaw, 0));
		}

		[ServerCmd("hurtme")]
		public static void HurtMeCommand(float amount)
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			cl?.Pawn?.TakeDamage(DamageInfo.Generic(amount));
		}

		[ServerCmd("sethealth")]
		public static void SetHealthCommand(float amount)
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			if (cl.Pawn is ArcadePlayer player)
				player.Health = amount;
		}

		[ServerCmd("setarmor")]
		public static void SetArmorCommand(float amount, float multiplier = 1.0f)
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			if (cl.Pawn is ArcadePlayer player)
			{
				player.Armor = amount;
				player.ArmorMultiplier = multiplier;
			}
		}

		[ServerCmd("setscale")]
		public static void SetScaleCommand(float amount)
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			Player player = (cl?.Pawn) as Player;
			if (!player.IsValid())
				return;

			if (amount <= float.Epsilon)
				return;

			player.LocalScale = amount;

			if (player.Controller is QPhysController qPhys)
				player.EyeLocalPosition = Vector3.Up * qPhys.EyeHeight * amount;
		}

		[ServerCmd("vr_reset_seated_pos")]
		public static void VRResetSeatedCommand()
		{
			Client cl = ConsoleSystem.Caller;

			if (cl?.Pawn is ArcadePlayer player)
			{
				player.ResetSeatedPos();
			}
		}

		[ServerCmd("respawnpawn")]
		public static void RespawnPawnCommand()
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.HasPermission("debug"))
				return;

			Game.Current.ClientDisconnect(cl, NetworkDisconnectionReason.UNUSUAL);
			Game.Current.ClientJoined(cl);
		}

		[ServerCmd("create_ragdoll")]
		public static void CreateRagdollCommand()
		{
			Client cl = ConsoleSystem.Caller;

			if (cl?.Pawn is ArcadePlayer player)
			{
				ModelEntity ent = player.CreateDeathRagdoll();

				foreach (PhysicsJoint joint in ent.PhysicsGroup.Joints)
				{
					//Log.Info($"{joint.Body1} -> {joint.Body2} | {joint.JointFrame1.Angles()} -> {joint.JointFrame2.Angles()}");
					// I don't think we can access specific joint properties yet so this vaguely works for now
					joint.LocalJointFrame1 = Rotation.Identity;
					joint.LocalJointFrame2 = Rotation.Identity;
				}

				ent.SetRagdollVelocityFrom(player);
				ent.PhysicsGroup.Velocity = player.Velocity;
			}
		}
	}
}
