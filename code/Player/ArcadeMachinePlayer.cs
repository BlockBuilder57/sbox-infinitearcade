using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	public partial class ArcadeMachinePlayer : ArcadePlayer
	{
		[Net]
		public ArcadeMachine ParentMachine { get; set; }

		public int PawnDepth { get { return PlayerChain.Count; } }

		public List<ArcadePlayer> PlayerChain
		{
			get
			{
				List<ArcadePlayer> players = new List<ArcadePlayer>();

				ArcadeMachine machine = ParentMachine;
				ArcadeMachinePlayer machinePlayer = null;

				while (machine != null && machine.CreatorPlayer is ArcadePlayer creatorPlayer)
				{
					if (creatorPlayer is ArcadeMachinePlayer)
						machine = machinePlayer.ParentMachine;
					else
						machine = machinePlayer.UsingMachine;

					players.Add(creatorPlayer);
				}

				players.Reverse();
				return players;
			}
		}

		public override void Respawn()
		{
			base.Respawn();

			
		}

		public override void InitStats()
		{
			Health = 100f;
			MaxHealth = 100f;

			Armor = 25f;
			MaxArmor = 100f;
			ArmorMultiplier = 1f;
		}

		public override void GiveWeapons()
		{
			Inventory?.Add(CKCarriableDefinition.CreateFromDefinition("assets/carriables/pistol.firearm"), true);
		}

		public override void Clothe()
		{
			Transform coneTransform = Transform.Zero;
			coneTransform.Rotation = Rotation.From(0f, 90f, 90f);
			coneTransform.Position.x += 8f;

			if (!Input.VR.IsActive)
				AddAsClothes("models/citizen_props/roadcone01.vmdl", "head", coneTransform);
		}

		public override Transform GetSpawnpoint()
		{
			PlayerSpawnpoint spawnpoint = Entity.All.OfType<PlayerSpawnpoint>().Where(x => x.PlayerType == PlayerSpawnpoint.SpawnType.MachinePlayer && x.Name == ParentMachine.SpawnpointName).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
			Transform transform = Transform.Zero;

			if (spawnpoint != null)
			{
				return spawnpoint.Transform;
			}
			else if (ParentMachine != null)
			{
				transform = ParentMachine.Transform;
				transform.Position = ParentMachine.Position + ParentMachine.Rotation.Right * 32;
				transform.Scale = ParentMachine.Scale;
			}

			return transform;
		}

		public override PawnController GetActiveController()
		{
			if (ParentMachine?.BeingPlayed == false || UsingMachine?.BeingPlayed == true)
				return m_machineController;

			return base.GetActiveController();
		}

		protected override void UseFail()
		{
			if (!IsUseDisabled() && ParentMachine != null)
			{
				ParentMachine.ExitMachine();
				return;
			}

			base.UseFail();
		}
	}
}
