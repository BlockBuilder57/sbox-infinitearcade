using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using SandboxEditor;

namespace infinitearcade
{
	[Library("prop_targetdummy")]
	[Title("Target Dummy"), Icon("outlet"), Description("A target dummy, for damage testing.")]
	[HammerEntity]
	[RenderFields, VisGroup(VisGroup.Physics)]
	[Model(Archetypes = ModelArchetype.physics_prop_model | ModelArchetype.breakable_prop_model)]
	public partial class TargetDummy : Prop
	{
		[Property("respawn_at_home", "Respawn At Home", "When the target dummy is broken, should it respawn at the place it was initially spawned at?")]
		[Net] public bool RespawnAtHome { get; set; } = true;

		[Property("healthoverride", "Health Override", "Overrides how much health the target dummy has. -1 or below will make the dummy invincible.")]
		[Net] public float HealthOverride { get; set; } = 0;

		[Net] private Transform m_homeTransform { get; set; }

		private bool m_init = false;

		public override void Spawn()
		{
			base.Spawn();

			if (string.IsNullOrEmpty(GetModelName()))
				SetModel("models/targetdummy/targetdummy_a.vmdl");

			m_homeTransform = Transform;
		}

		[Event.Tick]
		public void OnTick()
		{
			if (!m_init)
			{
				// for some reason, netvars are set *after* Spawn()
				// so just as a safety precaution, and to make sure
				// client and server match, do extra things in here

				if (HealthOverride > 0)
					Health = HealthOverride;
				else if (HealthOverride < 0)
					Health = -1;

				m_init = true;
			}

			if (Host.IsClient)
			{
				//Vector3 textPos = WorldSpaceBounds.ClosestPoint(Game.Current.FindActiveCamera().Position.WithZ(WorldSpaceBounds.Center.z));
				Vector3 textPos = Position + (Rotation.Forward * 8);

				Transform? healthDisplay = GetAttachment("healthDisplay");
				if (healthDisplay != null)
					textPos = healthDisplay.Value.Position;

				DebugOverlay.Text(Health.ToString(), textPos, Color.Yellow, 0, 128);
			}
		}

		protected override void UpdatePropData(Model model)
		{
			base.UpdatePropData(model);

			// reset init so that we don't nuke invincible targets
			m_init = false;
		}

		public override void TakeDamage(DamageInfo info)
		{
			Vector3 textPos = info.Position == Vector3.Zero ? Position : info.Position;
			DebugOverlay.Text(info.Damage.ToString(), textPos, Color.Yellow, .75f, 4096);

			base.TakeDamage(info);
		}

		public override void OnKilled()
		{
			_ = RespawnMe(m_homeTransform, Transform, Model, RespawnAtHome, HealthOverride);

			base.OnKilled();
		}

		public async Task RespawnMe(Transform homeTransform, Transform curTransform, Model model, bool respawnAtHome, float healthOverride)
		{
			await Task.Delay(500);

			TargetDummy dummy = new()
			{
				Model = model,
				RespawnAtHome = respawnAtHome,
				HealthOverride = healthOverride,
				m_homeTransform = homeTransform,
			};

			// reminder that the home transform will be passed along even if it shouldn't
			// respawn at home. that way if a future dummy in the lineage is told to do so,
			// it'll spawn back at its ancenstor's home.
			// this sounds like the people that travel somewhere because they have 0.02% of the place in their DNA

			if (respawnAtHome)
				dummy.Transform = homeTransform;
			else
				dummy.Transform = curTransform;
		}
	}
}
