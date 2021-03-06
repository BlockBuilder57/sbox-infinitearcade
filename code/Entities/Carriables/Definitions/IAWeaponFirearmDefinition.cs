using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace infinitearcade
{
	[GameResource("Firearm Definition", "firearm", "The definition for a firearm.")]
	public partial class IAWeaponFirearmDefinition : IAToolDefinition
	{
		[HideInEditor] public static IReadOnlyList<IAToolDefinition> AllFirearms => _allFirearms;
		[HideInEditor] internal static List<IAToolDefinition> _allFirearms = new();

		public class BulletSetting
		{
			public int Pellets { get; set; } = 0;
			public float Spread { get; set; } = 0.05f;
			public float Force { get; set; } = 0.6f;
			public float Damage { get; set; } = 5f;
			public float BulletSize { get; set; } = 2f;
			public bool DividedAcrossPellets { get; set; } = false;
		}

		public class CapacitySetting
		{
			public int MaxClip { get; set; } = -1;
			public int MaxAmmo { get; set; } = -1;
			public BulletSetting BulletSettings { get; set; }
		}

		public CapacitySetting PrimaryCapacity { get; set; }
		public CapacitySetting SecondaryCapacity { get; set; }

		public IAWeaponFirearm.InputFunction PrimaryFunction { get; set; }
		public IAWeaponFirearm.InputFunction SecondaryFunction { get; set; }
		public IAWeaponFirearm.InputFunction ReloadFunction { get; set; }

		public float ReloadTime { get; set; } = 1.0f;

		[FGDType("sound")] public string PrimaryFireSound { get; set; }
		[FGDType("sound")] public string SecondaryFireSound { get; set; }

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allFirearms.Contains(this))
				_allFirearms.Add(this);
		}
	}


}
