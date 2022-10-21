using System.Linq;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public class SpawningCommands
	{
		[ConCmd.Admin("respawn_entities")]
		public static void RespawnEntities()
		{
			if (Game.Current.IsValid())
				Map.Reset(Game.DefaultCleanupFilter);
		}

		[ConCmd.Server("spawn")]
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
			if (modelname.Count(x => x == '.') == 1 && !modelname.Contains('/') && !modelname.EndsWith(".vmdl", System.StringComparison.OrdinalIgnoreCase))
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
			DebugOverlay.Text($"Spawning {packageName}", pos, 5.0f);

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

			DebugOverlay.Box(pos, rotation, mins, maxs, Color.White, 10);
			DebugOverlay.Text($"Found {package.Title}", pos + Vector3.Up * 20, 5.0f);

			// downloads if not downloads, mounts if not mounted
			await package.MountAsync();

			return model;
		}

		[ConCmd.Server("spawn_carriable")]
		public static void SpawnCarriableCommand(string path)
		{
			if (!ConsoleSystem.Caller.IsValid())
				return;

			var owner = ConsoleSystem.Caller.Pawn;

			CKCarriable entity = CKCarriableResource.CreateFromResource(path);

			if (entity == null)
				return;

			var tr = Trace.Ray(owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 200)
				.UseHitboxes()
				.Ignore(owner)
				.Size(2)
				.Run();

			entity.Position = tr.EndPosition + Vector3.Up * 8;
			entity.Rotation = Rotation.From(new Angles(0, owner.EyeRotation.Angles().yaw, 0));
		}

		[ConCmd.Server("spawn_player_ragdoll")]
		public static void SpawnPlayerRagdollCommand(float force = 0, bool physical = false, string clientSearch = "")
		{
			Client cl = ConsoleSystem.Caller;
			if (!cl.IsValid())
				return;

			if (!string.IsNullOrWhiteSpace(clientSearch))
				cl = cl.TryGetClient(clientSearch);

			if (cl?.Pawn is CKPlayer player)
			{
				Corpse ent = player.CreateDeathRagdoll();
				if (!ent.IsValid())
					return;

				if (physical)
				{
					var tr = Trace.Ray(player.EyePosition, player.EyePosition + player.EyeRotation.Forward * 500)
								.UseHitboxes()
								.Ignore(player)
								.Run();

					ent.Position = tr.EndPosition;

					ent.Tags.Clear();
					ent.Tags.Add("prop", "solid");
				}
				else
				{
					ent.DeleteAsync(10.0f);
				}

				if (force > 0)
				{
					ent.SetRagdollVelocityFrom(player);
					ent.PhysicsGroup.Velocity = player.Velocity + (player.EyeRotation.Forward * force);
				}

				// can't do joint freezing yet
				// need to wait for joints to have swing/twist params available...
			}
		}
	}
}
