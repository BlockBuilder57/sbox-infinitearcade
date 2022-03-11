using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace infinitearcade
{
	public partial class InfiniteArcadeGame : Sandbox.Game
	{
		[AdminCmd("respawn_entities")]
		public static void RespawnEntities()
		{
			Map.Reset(DefaultCleanupFilter);
		}

		[ServerCmd("spawn")]
		public static async Task SpawnCommand(string modelname)
		{
			if (ConsoleSystem.Caller == null)
				return;
			var owner = ConsoleSystem.Caller.Pawn;

			// just treat vmdl_c as normal so you can paste stuff in from the asset browser
			if (modelname.EndsWith(".vmdl_c", System.StringComparison.OrdinalIgnoreCase))
				modelname = modelname.Remove(modelname.Length - 2, 2);

			var tr = Trace.Ray(owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 500)
				.UseHitboxes()
				.Ignore(owner)
				.Run();

			var modelRotation = Rotation.From(new Angles(0, owner.EyeRotation.Angles().yaw, 0)) * Rotation.FromAxis(Vector3.Up, 180);

			// Does this look like a package?
			if (modelname.Count(x => x == '.') == 1 && !modelname.EndsWith(".vmdl", System.StringComparison.OrdinalIgnoreCase))
			{
				modelname = await SpawnPackageModel(modelname, tr.EndPosition, modelRotation, owner);
				if (modelname == null)
					return;
			}

			var model = Model.Load(modelname);
			if (model == null || model.IsError)
				return;

			var ent = new Prop
			{
				Position = tr.EndPosition + Vector3.Down * model.PhysicsBounds.Mins.z,
				Rotation = modelRotation,
				Model = model
			};

			// Let's make sure physics are ready to go instead of waiting
			ent.SetupPhysicsFromModel(PhysicsMotionType.Dynamic);
		}

		static async Task<string> SpawnPackageModel(string packageName, Vector3 pos, Rotation rotation, Entity source)
		{
			DebugOverlay.Text(pos, $"Spawning {packageName}", 5.0f);

			var package = await Package.Fetch(packageName, false);
			if (package == null || package.PackageType != Package.Type.Model || package.Revision == null)
			{
				// spawn error particles
				return null;
			}

			if (!source.IsValid) return null; // source entity died or disconnected or something

			var model = package.GetMeta("PrimaryAsset", "models/dev/error.vmdl");
			var mins = package.GetMeta("RenderMins", Vector3.Zero);
			var maxs = package.GetMeta("RenderMaxs", Vector3.Zero);

			DebugOverlay.Box(10, pos, rotation, mins, maxs, Color.White);
			DebugOverlay.Text(pos + Vector3.Up * 20, $"Found {package.Title}", 5.0f);

			// downloads if not downloads, mounts if not mounted
			await package.MountAsync();

			return model;
		}

		[ClientCmd("query_sandworks_packages")]
		public static async Task QuerySandworksPackagesCommand(Package.Type type, string search = "", int take = 64, Package.Order order = Package.Order.Newest)
		{
			var q = new Package.Query();
			q.Type = type;
			q.SearchText = search;
			q.Take = take;
			q.Order = order;

			var found = await q.RunAsync(default);

			List<string> results = new();
			foreach (var package in found)
			{
				results.Add(package.FullIdent);
			}

			Log.Info($"Search results of {search}, type {type}, order {order}");
			Log.Info(string.Join(", ", results));
		}

		[ServerCmd("spawncarriable")]
		public static void SpawnWeaponCommand(string path)
		{
			if (ConsoleSystem.Caller == null)
				return;

			var owner = ConsoleSystem.Caller.Pawn;

			IACarriable carriable = IACarriableDefinition.GetEntity(path);
			if (carriable == null)
				return;

			var tr = Trace.Ray(owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 200)
				.UseHitboxes()
				.Ignore(owner)
				.Size(2)
				.Run();

			carriable.Position = tr.EndPosition + Vector3.Up * 8;
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

		[AdminCmd("respawn_pawn")]
		public static void RespawnPawnCommand()
		{
			Client cl = ConsoleSystem.Caller;

			Game.Current.ClientDisconnect(cl, NetworkDisconnectionReason.UNUSUAL);
			Game.Current.ClientJoined(cl);
		}

		[ServerCmd("create_death_ragdoll")]
		public static void CreateDeathRagdollCommand(float force = 0, bool physical = false)
		{
			Client cl = ConsoleSystem.Caller;

			if (cl?.Pawn is ArcadePlayer player)
			{
				ModelEntity ent = player.CreateDeathRagdoll();
				if (ent == null)
					return;

				ent.SetRagdollVelocityFrom(player);
				ent.PhysicsGroup.Velocity = player.Velocity + (player.EyeRotation.Forward * force);

				/*if (physical)
				{
					ent.
				}*/
			}
		}
	}
}
