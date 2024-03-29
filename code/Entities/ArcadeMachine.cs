﻿using infinitearcade.UI;
using Sandbox;
using SandboxEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;

namespace infinitearcade
{
	[Library("prop_arcademachine")]
	[Title("Arcade Machine"), Icon("point_of_sale"), Description("An arcade machine.")]
	[HammerEntity]
	[RenderFields, VisGroup(VisGroup.Physics)]
	[Model(Archetypes = ModelArchetype.physics_prop_model | ModelArchetype.breakable_prop_model)]
	public partial class ArcadeMachine : ModelEntity, IUse
	{
		[Net]
		public ArcadeMachinePlayer CreatedPlayer { get; set; }
		[Net]
		public ArcadePlayer CreatorPlayer { get; set; }

		public Client CurrentClient { get; set; }

		public bool BeingPlayed
		{
			get
			{
				// so, as client is an interface, it can't be networked. because of this, even if
				// we do stuff on both the client and server for some reason, it just doesn't work
				// at all. so, hacky fix, if we're the client, just test for CreatedPlayer
				// yay hacks wooooooo

				if (IsServer)
					return CurrentClient.IsValid();
				else if (IsClient)
					return CreatedPlayer.IsValid();
				return false;
			}
		}

		[Property("target_destination", "Spawnpoint", "Player Spawnpoint")]
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
			if (DebugFlags.HasFlag(EntityDebugFlags.Text))
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
		}

		[Event.Tick]
		public void OnTick()
		{
			ArcadePlayer player = BeingPlayed ? CreatorPlayer : CreatedPlayer;

			if (player.IsValid())
			{
				player.GetActiveController()?.Simulate(CurrentClient, player, player.GetActiveAnimator());
				//DebugOverlay.Text(player.Position, $"{(BeingPlayed ? "Creator" : "Created")} player", 0f);
			}
		}

		public bool IsUsable(Entity user)
		{
			if (user is not ArcadePlayer)
				return false;
			
			return true;
		}

		public bool OnUse(Entity user)
		{
			if (IsServer && user == CreatedPlayer)
			{
				DestroyCreatedPlayer();
				return false;
			}

			EnterMachine(user);

			return false;
		}

		public void EnterMachine(Entity creator)
		{
			if (!creator.IsValid() || creator is not ArcadePlayer player)
				return;

			if (IsServer)
				CreatePlayer();

			if (!CurrentClient.IsValid())
			{
				CreatorPlayer = player;
				CreatorPlayer.UsingMachine = this;

				CurrentClient = player.Client;
				if (CurrentClient != null)
					CurrentClient.Pawn = CreatedPlayer;
			}

			if (CreatedPlayer.Inventory is CKInventory inv)
				InfiniteArcadeHud.InventoryFullUpdate(To.Single(CreatedPlayer), inv.List.ToArray());
		}

		public void ExitMachine()
		{
			if (IsServer)
			{
				if (CurrentClient.IsValid())
				{
					if (CreatorPlayer.IsValid())
						CreatorPlayer.UsingMachine = null;

					CurrentClient.Pawn = CreatorPlayer;
					CurrentClient = null;
				}
			}

			if (CreatorPlayer.Inventory is CKInventory inv)
				InfiniteArcadeHud.InventoryFullUpdate(To.Single(CreatorPlayer), inv.List.ToArray());
		}

		public void CreatePlayer()
		{
			if (!CreatedPlayer.IsValid())
			{
				CreatedPlayer = new ArcadeMachinePlayer();
				(CreatedPlayer as ArcadeMachinePlayer).ParentMachine = this;
				CreatedPlayer.Respawn();
				CreatedPlayer.RenderColor = Rand.Color();
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
