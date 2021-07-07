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
		ArcadePlayer CreatedPlayer { get; set; }
		[Net]
		ArcadePlayer CreatorPlayer { get; set; }
		[Net]
		Client CurrentClient { get; set; }

		[Property("Spawnpoint", "Player Spawnpoint", FGDType = "target_destination")]
		public string SpawnpointName { get; set; }

		public override void Spawn()
		{
			base.Spawn();

			if (string.IsNullOrEmpty(GetModelName()))
				SetModel("models/sbox_props/street_lamp/street_lamp_open.vmdl");

			SetupPhysicsFromModel(PhysicsMotionType.Static, true);
		}

		[Event.Frame]
		public void OnFrame()
		{
			if (HasDebugBitsSet(DebugOverlayBits.OVERLAY_TEXT_BIT) && CreatedPlayer.IsValid() && CreatorPlayer.IsValid())
			{
				ArcadePlayer loopCreated = CreatedPlayer;
				ArcadePlayer loopCreator = CreatorPlayer;
				ArcadeMachine loopMachine = this;
				//while (loopPlayer != null && loopMachine != null)
				{
					Vector3 machineBottom = new Vector3(loopMachine.WorldSpaceBounds.Center.x, loopMachine.WorldSpaceBounds.Center.y, loopMachine.WorldSpaceBounds.Mins.z);
					Vector3 machineTop = new Vector3(loopMachine.WorldSpaceBounds.Center.x, loopMachine.WorldSpaceBounds.Center.y, loopMachine.WorldSpaceBounds.Maxs.z);

					DebugOverlay.Line(loopCreated.WorldSpaceBounds.Center, machineTop, Color.Red, depthTest: true);
					DebugOverlay.Line(machineBottom, loopCreator.WorldSpaceBounds.Center, Color.Yellow, depthTest: true);
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
				ExitMachine();
				CreatedPlayer.Delete();
				CreatedPlayer = null;
				return false;
			}

			if (!CreatedPlayer.IsValid())
			{
				CreatedPlayer = new ArcadeMachinePlayer();
				(CreatedPlayer as ArcadeMachinePlayer).ParentMachine = this;
				CreatedPlayer.Respawn();
				CreatedPlayer.RenderColor = Color.Red;
				CreatorPlayer = (ArcadePlayer)user;
				CurrentClient = Client.All.First(x => x.Pawn == user);
				CurrentClient.Pawn = CreatedPlayer;
			}
			else
			{
				CreatorPlayer = (ArcadePlayer)user;
				CurrentClient = Client.All.First(x => x.Pawn == user);
				CurrentClient.Pawn = CreatedPlayer;
			}

			return false;
		}

		public void ExitMachine()
		{
			if (CreatedPlayer.IsValid() && CreatorPlayer.IsValid() && IsServer)
			{
				Client.All.First(x => x.Pawn == CreatedPlayer).Pawn = CreatorPlayer;
			}
		}
	}
}
