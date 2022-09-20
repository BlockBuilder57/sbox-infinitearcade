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
		public float Size { get; set; }

		public float Spread { get; set; }
		public float Damage { get; set; }
		public float Force { get; set; }
		
		public int Pellets { get; set; }
		public bool DividedAcrossPellets { get; set; }

		public bool CanBounce { get; set; }
		public ICKProjectile.BounceParameters BounceParams { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allProjectiles.Contains(this))
				_allProjectiles.Add(this);
		}
	}


}
