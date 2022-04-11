using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace infinitearcade
{
	public class PlayerManagementCommands
	{
		[AdminCmd("respawn_pawn")]
		public static void RespawnPawnCommand()
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid())
				return;

			Game.Current.ClientDisconnect(cl, NetworkDisconnectionReason.UNUSUAL);
			Game.Current.ClientJoined(cl);
		}

		[AdminCmd("pawn_goto")]
		public static void GotoPlayerCommand(string search)
		{
			Client caller = ConsoleSystem.Caller;
			Client target = caller?.TryGetClient(search);

			if (!target.IsValid() || !caller.IsValid())
				return;

			if (caller.Pawn.IsValid() && target.Pawn.IsValid())
				caller.Pawn.Position = target.Pawn.Position;
		}

		[AdminCmd("pawn_bring")]
		public static void BringPlayerCommand(string search)
		{
			Client caller = ConsoleSystem.Caller;
			if (!caller.IsValid() || !caller.Pawn.IsValid())
				return; // nothing to teleport to

			foreach (Client target in caller?.TryGetClients(search))
			{
				if (!target.IsValid() || !target.Pawn.IsValid())
					continue;

				target.Pawn.Position = caller.Pawn.Position;
			}
		}

		[AdminCmd("pawn_sethealth")]
		public static void SetHealthCommand(float amount, string search = "!self")
		{
			Client caller = ConsoleSystem.Caller;

			foreach (Client target in caller?.TryGetClients(search))
			{
				if (!target.IsValid() || !target.Pawn.IsValid())
					continue;

				target.Pawn.Health = amount;
			}
		}

		[AdminCmd("pawn_setarmor")]
		public static void SetArmorCommand(float amount, float multiplier = 0, string search = "!self")
		{
			Client caller = ConsoleSystem.Caller;

			foreach (Client target in caller?.TryGetClients(search))
			{
				if (!target.IsValid() || !target.Pawn.IsValid())
					continue;

				if (target.Pawn is ArcadePlayer player)
				{
					player.Armor = amount;

					if (multiplier != 0)
						player.ArmorMultiplier = multiplier;
				}
			}
		}

		[AdminCmd("pawn_setscale")]
		public static void SetScaleCommand(float amount, string search = "!self")
		{
			Client caller = ConsoleSystem.Caller;

			foreach (Client target in caller?.TryGetClients(search))
			{
				if (!target.IsValid() || !target.Pawn.IsValid())
					continue;

				if (amount <= float.Epsilon)
					return;

				target.Pawn.LocalScale = amount;

				if (target.Pawn is Player player && player.Controller is QPhysController qPhys)
					player.EyeLocalPosition = Vector3.Up * qPhys.EyeHeight * amount;
			}
		}
	}
}
