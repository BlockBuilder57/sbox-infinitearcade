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
			Shotgun,
			Pistol,
			Flashlight
		}

		public string Identifier { get; set; } = "ia_carriable_new";
		public string BucketIdentifier { get; set; } = "none";
		public BaseTypes BaseType { get; set; } = BaseTypes.Carriable;

		[ResourceType("vmdl")] public string WorldModelPath { get; set; }
		[ResourceType("vmdl")] public string ViewModelPath { get; set; }

		[Hammer.Skip] public Model WorldModel { get; private set; }
		[Hammer.Skip] public Model ViewModel { get; private set; }

		public static IACarriable GetEntity(string path)
		{
			IACarriable entity = null;

			IACarriableDefinition def = FromPath<IACarriableDefinition>(path);

			if (def == null)
			{
				Log.Error("Couldn't get definition from path!");
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

				case BaseTypes.Shotgun:
					entity = new Shotgun();
					break;
				case BaseTypes.Pistol:
					entity = new Pistol();
					break;
				case BaseTypes.Flashlight:
					entity = new Flashlight();
					break;
			}

			entity.SetupFromDefinition(def);

			//Log.Info($"returning {entity}");
			return entity;
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
