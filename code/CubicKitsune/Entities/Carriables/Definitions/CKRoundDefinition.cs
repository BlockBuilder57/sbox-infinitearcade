using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[GameResource("Round Definition", "round", "The definition for a firearm round.", Icon = "label")]
	public class CKRoundDefinition : GameResource
	{
		[HideInEditor] public static IReadOnlyList<CKRoundDefinition> All => _allRounds;
		[HideInEditor] internal static List<CKRoundDefinition> _allRounds = new();

		public int Pellets { get; set; } = 0;
		public float Spread { get; set; } = 0.05f;
		public float Force { get; set; } = 0.6f;
		public float Damage { get; set; } = 5f;
		public float BulletSize { get; set; } = 2f;
		public bool DividedAcrossPellets { get; set; } = false;

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allRounds.Contains(this))
				_allRounds.Add(this);
		}
	}


}
