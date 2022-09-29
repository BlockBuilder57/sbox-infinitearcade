using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[GameResource("Projectile Definition", "proj", "The definition for a projectile.", Icon = "merge")]
	public class CKProjectileDefinition : GameResource, ICKProjectile
	{
		[HideInEditor] public static IReadOnlyList<CKProjectileDefinition> All => _allProjectiles;
		[HideInEditor] internal static List<CKProjectileDefinition> _allProjectiles = new();

		public string Identifier { get; set; }
		[Description("Library name of the projectile to spawn. Leave blank for hitscan.")]
		public string TypeLibraryName { get; set; }
		public Model WorldModel { get; set; }

		public ICKProjectile.SpawnStats Stats { get; set; }
		public float Damage { get; set; } = -1;

		public int Count { get; set; }
		public bool StatsDividedAcrossCount { get; set; }

		public ICKProjectile.BounceParameters BounceParams { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allProjectiles.Contains(this))
				_allProjectiles.Add(this);
		}
	}


}
