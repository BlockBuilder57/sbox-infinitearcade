using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using CubicKitsune;

namespace infinitearcade
{
	public partial class ArcadePlayer : CKPlayer
	{
		[Net] public ArcadeMachine UsingMachine { get; set; }
		protected BasePlayerController m_machineController;
		
		public ArcadePlayer()
		{
			Inventory = new CKInventory(this);
		}

		public ArcadePlayer(Client cl) : this()
		{
			ClothingContainer.LoadFromClient(cl);
		}

		public override void Respawn()
		{
			base.Respawn();

			if (m_machineController == null)
				m_machineController = new GravityOnlyController();
		}

		public override void InitLoadout()
		{
			CKPlayerLoadoutResource loadout = ResourceLibrary.Get<CKPlayerLoadoutResource>(InfiniteArcadeGame.ia_debug ? "assets/loadouts/debug.loadout" : "assets/loadouts/arcadeplayer.loadout");
			SetupFromLoadoutResource(loadout);
		}

		public override Transform GetSpawnpoint()
		{
			SpawnPoint spawnpoint = Entity.All.OfType<PlayerSpawnpoint>().Where(x => x.PlayerType == PlayerSpawnpoint.SpawnType.ArcadePlayer).OrderBy(x => Guid.NewGuid()).FirstOrDefault();

			if (spawnpoint == null) // fall back to the base spawnpoint
				spawnpoint = Entity.All.OfType<SpawnPoint>().Where(x => x is not PlayerSpawnpoint).OrderBy(x => Guid.NewGuid()).FirstOrDefault();

			if (spawnpoint != null)
				return spawnpoint.Transform;
			else
				return Transform.Zero;
		}

		public override PawnController GetActiveController()
		{
			if (this is not ArcadeMachinePlayer)
			{
				if (UsingMachine.IsValid())
					return m_machineController;
			}

			return base.GetActiveController();
		}

		public override void OnKilled()
		{
			base.OnKilled();
			
			ArcadeMachine machine = UsingMachine;
			List<ArcadeMachine> bubbleUp = new();
			while (machine != null && machine.BeingPlayed)
			{
				bubbleUp.Add(machine);
				machine = machine.CreatedPlayer.UsingMachine;
			}
			bubbleUp.Reverse();
			bubbleUp.ForEach(x => x.ExitMachine());
		}
	}
}
