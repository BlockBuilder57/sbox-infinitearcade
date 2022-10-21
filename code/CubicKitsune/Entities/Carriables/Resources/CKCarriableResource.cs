using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[GameResource("Carriable", "carry", "The definition for a carriable object.", Icon = "iron")]
	public class CKCarriableResource : GameResource, ICKCarriable
	{
		[HideInEditor] public static IReadOnlyList<CKCarriableResource> AllCarriables => _allCarriables;
		[HideInEditor] internal static List<CKCarriableResource> _allCarriables = new();

		public string Identifier { get; set; }
		public string LibraryType { get; set; } = "carriable_generic";
		public ICKCarriable.BucketCategory Bucket { get; set; }
		public int SubBucket { get; set; }

		public Model ViewModel { get; set; }
		public Model WorldModel { get; set; }
		public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
		public CitizenAnimationHelper.Hand Handedness { get; set; }

		public static CKCarriable CreateFromResource(string path)
		{
			CKCarriableResource def = ResourceLibrary.Get<CKCarriableResource>(path);
			if (def == null)
			{
				Log.Error("Trying to create a carriable from a null resource!");
				return null;
			}

			CKCarriable carriable = TypeLibrary.Create<CKCarriable>(def.LibraryType);

			if (carriable != null)
				carriable.SetupFromResource(def);

			return carriable;
		}

		protected override void PostReload()
		{
			base.PostReload();

			//Log.Info((Host.IsClient ? "client" : "server") + "POst reload haha");

			foreach (CKCarriable carry in Entity.All.OfType<CKCarriable>())
				if (carry.Identifier == Identifier)
					carry.SetupFromResource(this);
				
		}

		protected override void PostLoad()
		{
			if (!_allCarriables.Contains(this))
				_allCarriables.Add(this);
		}
	}
}
