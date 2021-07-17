using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	//[Hammer.EditorModel( "models/sbox_props/street_lamp/street_lamp_open.vmdl" )]
	[Hammer.Model]
	[Library("prop_arcademachine", Description = "An arcade machine.")]
	public partial class ArcadeMachine : ModelEntity, IUse
	{
		[Net]
		public ArcadePlayer CreatedPlayer { get; set; }
		[Net]
		public ArcadePlayer CreatorPlayer { get; set; }
		[Net]
		public Client CurrentClient { get; set; }

		public bool BeingPlayed => CurrentClient != null;

		[Property("Spawnpoint", "Player Spawnpoint", FGDType = "target_destination")]
		public string SpawnpointName { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			if (string.IsNullOrEmpty(GetModelName()))
				SetModel("models/sbox_props/street_lamp/street_lamp_open.vmdl");

			SetupPhysicsFromModel(PhysicsMotionType.Static, true);
		}

		[Event.Tick]
		public void OnFrame()
		{
			if (HasDebugBitsSet(DebugOverlayBits.OVERLAY_TEXT_BIT))
			{
				Vector3 machineBottom = new Vector3(this.WorldSpaceBounds.Center.x, this.WorldSpaceBounds.Center.y, this.WorldSpaceBounds.Mins.z);
				Vector3 machineTop = new Vector3(this.WorldSpaceBounds.Center.x, this.WorldSpaceBounds.Center.y, this.WorldSpaceBounds.Maxs.z);

				if (CreatedPlayer.IsValid())
				{
					DebugOverlay.Line(CreatedPlayer.Position, machineTop, Color.Red, depthTest: true);
					if (CreatorPlayer.IsValid())
						DebugOverlay.Line(machineBottom, CreatorPlayer.Position, Color.Yellow, depthTest: true);
				}
			}

			if (BeingPlayed)
			{
				if (CreatorPlayer.IsValid())
				{
					CreatorPlayer.GetActiveController()?.Simulate(CurrentClient, CreatorPlayer, CreatorPlayer.GetActiveAnimator());
					//DebugOverlay.Text(CreatorPlayer.Position, "Creator player", 0f);
				}
			}
			else
			{
				if (CreatedPlayer.IsValid())
				{
					CreatedPlayer.GetActiveController()?.Simulate(CurrentClient, CreatedPlayer, CreatedPlayer.GetActiveAnimator());
					//DebugOverlay.Text(CreatedPlayer.Position, "Created player", 0f);
				}
			}
		}

		public bool IsUsable(Entity user)
		{
			return true;
		}

		public bool OnUse(Entity user)
		{
			if (!IsServer)
				return false;

			if (user == CreatedPlayer)
			{
				DestroyCreatedPlayer();
				return false;
			}

			EnterMachine(user);

			return false;
		}

		public void EnterMachine(Entity creator)
		{
			if (!creator.IsValid())
				return;

			if (!CreatedPlayer.IsValid())
			{
				CreatedPlayer = new ArcadeMachinePlayer();
				(CreatedPlayer as ArcadeMachinePlayer).ParentMachine = this;
				CreatedPlayer.Respawn();
				CreatedPlayer.RenderColor = Rand.Color();
			}

			if (!CurrentClient.IsValid())
			{
				CreatorPlayer = (ArcadePlayer)creator;
				CreatorPlayer.CurrentMachine = this;

				CurrentClient = Client.All.FirstOrDefault(x => x.Pawn == creator);
				CurrentClient.Pawn = CreatedPlayer;
			}
		}

		public void ExitMachine()
		{
			if (IsServer)
			{
				if (CurrentClient.IsValid())
				{
					if (CreatorPlayer.IsValid())
						CreatorPlayer.CurrentMachine = null;

					CurrentClient.Pawn = CreatorPlayer;
					CurrentClient = null;
				}
			}
		}

		public void DestroyCreatedPlayer()
		{
			if (IsServer)
			{
				if (CurrentClient.IsValid())
					ExitMachine();

				if (CreatedPlayer.IsValid())
				{
					CreatedPlayer.Delete();
					CreatedPlayer = null;
				}
			}
		}
	}
}
