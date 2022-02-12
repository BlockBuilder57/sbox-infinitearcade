﻿using System;
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

			var tr = Trace.Ray(owner.EyePos, owner.EyePos + owner.EyeRot.Forward * 500)
				.UseHitboxes()
				.Ignore(owner)
				.Size(2)
				.Run();

			var ent = new Prop();
			ent.Position = tr.EndPos;
			ent.Rotation = Rotation.From(new Angles(0, owner.EyeRot.Angles().yaw, 0)) * Rotation.FromAxis(Vector3.Up, 180);
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

		[ServerCmd("hurtme")]
		public static void HurtMeCommand(float amount)
		{
			Client client = ConsoleSystem.Caller;
			if (!client.HasPermission("debug"))
				return;

			client?.Pawn?.TakeDamage(DamageInfo.Generic(amount));
		}

		[ServerCmd("sethealth")]
		public static void SetHealthCommand(float amount)
		{
			Client client = ConsoleSystem.Caller;
			if (!client.HasPermission("debug"))
				return;

			(client?.Pawn as ArcadePlayer).Health = amount;
		}

		[ServerCmd("setarmor")]
		public static void SetArmorCommand(float amount, float multiplier = 1.0f)
		{
			Client client = ConsoleSystem.Caller;
			if (!client.HasPermission("debug"))
				return;

			(client?.Pawn as ArcadePlayer).Armor = amount;
			(client?.Pawn as ArcadePlayer).ArmorMultiplier = multiplier;
		}

		[ServerCmd("setscale")]
		public static void SetScaleCommand(float amount)
		{
			Client client = ConsoleSystem.Caller;
			if (!client.HasPermission("debug"))
				return;

			Player player = (client?.Pawn) as Player;
			if (!player.IsValid())
				return;

			if (amount <= 0)
				return;

			player.LocalScale = amount;

			if (player.Controller is QPhysController qPhys)
				player.EyePosLocal = Vector3.Up * qPhys.EyeHeight * amount;
		}

		[ServerCmd("vr_reset_seated_pos")]
		public static void VRResetSeatedCommand()
		{
			Client client = ConsoleSystem.Caller;

			if (client?.Pawn is ArcadePlayer player)
			{
				player.ResetSeatedPos();
			}
		}

		[ServerCmd("respawnpawn")]
		public static void RespawnPawnCommand()
		{
			Client client = ConsoleSystem.Caller;
			if (!client.HasPermission("debug"))
				return;

			Game.Current.ClientDisconnect(client, NetworkDisconnectionReason.UNUSUAL);
			Game.Current.ClientJoined(client);
		}

		[ServerCmd("create_ragdoll")]
		public static void CreateRagdollCommand()
		{
			Client client = ConsoleSystem.Caller;

			if (client?.Pawn is ArcadePlayer player)
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
