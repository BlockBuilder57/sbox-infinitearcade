using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace infinitearcade.UI
{
	public class TargetStatus : Panel
	{
		public Panel InfoHolder;
		public Label Name;
		public Image Avatar;

		public Panel DisplayHolder;
		public HealthDisplay Health;

		private Entity target;

		public TargetStatus()
		{
			StyleSheet.Load("/UI/TargetStatus.scss");

			InfoHolder = Add.Panel("infoHolder");
			Avatar = InfoHolder.Add.Image(null);
			Avatar.AddClass("avatar");
			Name = InfoHolder.Add.Label("Dubious Little Creature", "targetname");

			DisplayHolder = Add.Panel("displayHolder");
		}

		public override void Tick()
		{
			if (!Local.Pawn.IsValid())
			{
				SetClass("invalid", true);
				return;
			}

			if (HasClass("hidden"))
				return;

			Vector3 eyePos = Local.Pawn.EyePosition;
			Vector3 eyeDir = Local.Pawn.EyeRotation.Forward;

			var tr = Trace.Ray(eyePos, eyePos + eyeDir * 512)
						.WithoutTags("trigger", "water")
						.Ignore(Local.Pawn, false)
						.Run();

			bool valid = tr.Entity.IsValid() && !tr.Entity.IsWorld;

			if (valid)
			{
				// base entities
				if (tr.Entity is BrushEntity || tr.Entity is KeyframeEntity || tr.Entity is AnimatedMapEntity || tr.Entity is IUse)
					valid = false;

				// specific entities
				if (tr.Entity is GlassShard)
					valid = false;
			}

			SetClass("invalid", !valid);

			if (!valid)
				return;

			if (tr.Entity != target)
				UpdateTarget(tr.Entity);
		}

		private void UpdateTarget(Entity ent)
		{
			if (!ent.IsValid())
				return;

			Name.Text = ent.ToString();

			switch (ent)
			{
				case Prop prop:
					string[] modelPath = prop.Model.Name.Split('/', '\\', '.');
					Name.Text = modelPath[modelPath.Length-2];
					break;
			}
				

			Health?.Delete(true);

			if (ent.Health > 0)
			{
				Health = new HealthDisplay(ent);
				Health.Parent = this;
			}

			if (ent is Player player)
			{
				Avatar?.SetClass("invalid", false);
				Avatar?.SetTexture($"avatar:{player.Client.PlayerId}");
				Name.Text = player.Client.Name;
			}
			else
			{
				Avatar?.SetClass("invalid", true);
			}

			target = ent;
		}
	}
}
