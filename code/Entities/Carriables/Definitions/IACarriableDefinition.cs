using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[Library("carry"), AutoGenerate]
	public partial class IACarriableDefinition : Asset
	{
		[Hammer.Skip] public static IReadOnlyList<IACarriableDefinition> AllCarriables => _allCarriables;
		[Hammer.Skip] internal static List<IACarriableDefinition> _allCarriables = new();

		public enum BaseTypes
		{
			// bases
			Carriable,
			Tool,

			// individual classes

			// weapons
			Shotgun,
			Pistol,

			// tools
			Flashlight,
			PhysicsManipulator
		}

		public enum AnimGraphTypes
		{
			Bool,
			Int,
			Float,
			Vector3
		}

		public class AnimGraphSetting
		{
			public string Key { get; set; }
			public string Value { get; set; }
			public AnimGraphTypes Type { get; set; }
		}

		public BaseTypes BaseType { get; set; } = BaseTypes.Carriable;
		public string Identifier { get; set; } = "ia_carriable_new";
		public IACarriable.BucketCategory Bucket { get; set; } = 0;
		public int SubBucket { get; set; } = 0;

		[ResourceType("vmdl")] public string WorldModelPath { get; set; }
		public AnimGraphSetting[] AnimGraphSettings { get; set; } = new AnimGraphSetting[] { new() { Key = "holdtype", Value = "0", Type = AnimGraphTypes.Int } };
		[ResourceType("vmdl")] public string ViewModelPath { get; set; }

		[Hammer.Skip] public Model WorldModel { get; private set; }
		[Hammer.Skip] public Model ViewModel { get; private set; }

		public static IACarriable GetEntity(string path)
		{
			IACarriable entity = null;

			IACarriableDefinition def = FromPath<IACarriableDefinition>(path);

			if (def == null)
			{
				Log.Error($"Couldn't get definition from path {path}!");
				return null;
			}

			switch (def.BaseType)
			{
				case BaseTypes.Carriable:
					entity = new IACarriable();
					break;
				case BaseTypes.Tool:
					entity = new IATool();
					break;

				// weapons
				case BaseTypes.Shotgun:
					entity = new Shotgun();
					break;
				case BaseTypes.Pistol:
					entity = new Pistol();
					break;

				// tools
				case BaseTypes.Flashlight:
					entity = new Flashlight();
					break;
				case BaseTypes.PhysicsManipulator:
					entity = new PhysicsManipulator();
					break;
			}

			entity.SetupFromDefinition(def);

			//Log.Info($"returning {entity}");
			return entity;
		}

		protected override void PostReload()
		{
			base.PostReload();

			WorldModel = Model.Load(WorldModelPath);
			ViewModel = Model.Load(ViewModelPath);

			foreach (IACarriable carry in Entity.All.OfType<IACarriable>())
				if (carry.Definition == this)
					carry.SetupFromDefinition(this);
		}

		protected override void PostLoad()
		{
			WorldModel = Model.Load(WorldModelPath);
			ViewModel = Model.Load(ViewModelPath);

			if (!_allCarriables.Contains(this))
				_allCarriables.Add(this);
		}
	}
}
