using CubicKitsune;
using infinitearcade.UI;
using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		public InfiniteArcadeGame()
		{
			// dunno why this isn't default, but
			Global.TickRate = 66;

			if (IsServer)
			{
				// serverside gubbins
			}

			if (IsClient)
			{
				// clientside gubbins
				Local.Hud = new InfiniteArcadeHud();
			}
		}

		public override void Simulate(Client cl)
		{
			Entity pawn = cl.Pawn;

			if (pawn is ArcadePlayer player)
				player.Simulate(cl);

			if (!cl.IsBot)
				CKDebugging.Simulate(cl);
		}

		public override void FrameSimulate(Client cl)
		{
			Host.AssertClient();

			Entity pawn = cl.Pawn;

			if (pawn is ArcadePlayer player)
				player.FrameSimulate(cl);

			if (!cl.IsBot)
				CKDebugging.FrameSimulate(cl);
		}

		public override void MoveToSpawnpoint(Entity pawn)
		{
			if (pawn is ArcadePlayer player)
			{
				Transform spawnTransform = player.GetSpawnpoint();

				if (spawnTransform == Transform.Zero)
				{
					Log.Warning($"Couldn't find spawnpoint for {player}!");
					return;
				}

				player.Transform = spawnTransform;
			}
		}

		public override void ClientJoined(Client cl)
		{
			base.ClientJoined(cl);

			var player = new ArcadePlayer(cl);
			cl.Pawn = player;

			player.Respawn();

			if (cl == Local.Client && IsClient)
				InfiniteArcadeHud.Current?.OnNewPawn();
		}

		public override void ClientDisconnect(Client cl, NetworkDisconnectionReason reason)
		{
			foreach (ArcadeMachine machine in Entity.All.OfType<ArcadeMachine>().Where(x => x.CurrentClient == cl))
			{
				if (machine.CreatorPlayer.IsValid() && machine.CreatorPlayer is not ArcadeMachinePlayer)
					machine.CreatorPlayer.Delete();

				machine.CurrentClient = null;
			}

			base.ClientDisconnect(cl, reason);
		}

		public override void DoPlayerSuicide(Client cl)
		{
			if (cl.Pawn is Player player && player.LifeState == LifeState.Alive)
			{
				float damage = player.Health;
				if (player is ArcadePlayer arcadeplayer)
				{
					if (arcadeplayer.GodMode != ArcadePlayer.GodModes.Mortal)
					{
						player.OnKilled();
						return;
					}

					damage += arcadeplayer.Armor * arcadeplayer.ArmorMultiplier;
				}
				player.TakeDamage(DamageInfo.Generic(damage * 100f));
			}
		}

		public override void DoPlayerNoclip(Client player)
		{
			if (!player.HasPermission("noclip"))
				return;

			if (player.Pawn is Player basePlayer)
			{
				using (Prediction.Off())
				{
					if (basePlayer.DevController is NoclipController)
					{
						Log.Info("DevController off");
						basePlayer.DevController = null;
						basePlayer.EnableSolidCollisions = true;
					}
					else
					{
						Log.Info("DevController on");
						basePlayer.DevController = new NoclipController();
						basePlayer.EnableSolidCollisions = false;
					}
				}
			}
		}

		public override void DoPlayerDevCam(Client cl)
		{
			Host.AssertServer();
			if (!cl.HasPermission("devcam"))
				return;

			var camera = cl.Components.Get<CubicKitsune.DevCamera>(true);

			if (camera == null)
			{
				camera = new CubicKitsune.DevCamera();
				cl.Components.Add(camera);
				return;
			}

			camera.Enabled = !camera.Enabled;
		}
	}
}
