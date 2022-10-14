using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using infinitearcade.UI;
using Sandbox;

namespace infinitearcade
{
	[Library("tool_medigun", Title = "Medigun")]
	public partial class Medigun : CKTool
	{
		[Net] public Entity HealTarget { get; set; }
		[Net] public float HealMax { get; set; }
		[Net] public float MaxHealingSpeed { get; set; } = 12f;

		[Net] private TimeSince TimeSinceStartedHealing { get; set; }

		public override void Simulate(Client cl)
		{
			if (!IsServer)
				return;

			Vector3 eyePos = Owner.EyePosition;
			Vector3 eyeDir = Owner.EyeRotation.Forward;
			//Rotation eyeRot = Owner.EyeRotation;

			var tr = Trace.Ray(eyePos, eyePos + eyeDir * 512)
						.WithoutTags("trigger", "water")
						.Ignore(Owner, false)
						.Run();

			if (Input.Down(InputButton.PrimaryAttack))
			{
				if (!HealTarget.IsValid())
					FindTarget(tr);
				else
					UpdateHealing(tr);
			}
			else
				EndHealing();
		}

		public override void ActiveStart(Entity ent)
		{
			base.ActiveStart(ent);

			if (IsClient)
				InfiniteArcadeHud.Current?.EnableTargetStatus();
		}

		public override void ActiveEnd(Entity ent, bool dropped)
		{
			base.ActiveEnd(ent, dropped);

			if (IsClient)
				InfiniteArcadeHud.Current?.DisableTargetStatus();
		}

		public void FindTarget(TraceResult tr)
		{
			// don't care if we don't have an entity or if that entity is the world
			if (!tr.Entity.IsValid() || tr.Entity.IsWorld)
				return;

			// already got a target
			if (HealTarget.IsValid())
				return;

			HealMax = 0;

			switch (tr.Entity)
			{
				case Prop prop:
					{
						if (prop.Model.TryGetData(out ModelPropData propInfo))
							HealMax = propInfo.Health;

						if (prop is TargetDummy dummy && dummy.HealthOverride != 0)
							HealMax = dummy.HealthOverride;

						break;
					}
				case CKPlayer player:
					{
						HealMax = player.MaxHealth;
						break;
					}
			}

			// no health to heal!
			if (HealMax <= 0)
				return;

			HealTarget = tr.Entity;
			TimeSinceStartedHealing = 0;
		}

		public void UpdateHealing(TraceResult tr)
		{
			if (!HealTarget.IsValid())
				return;

			float delta = HealTarget.Health - HealTarget.Health.Approach(HealMax, MathX.Lerp(1f, MaxHealingSpeed, TimeSinceStartedHealing) * Time.Delta);

			DamageInfo healing = DamageInfo.Generic(delta).WithAttacker(Owner, this);

			HealTarget.TakeDamage(healing);
		}

		public void EndHealing()
		{
			if (!HealTarget.IsValid())
				return;

			HealTarget = null;
			HealMax = 0;
		}
	}
}
