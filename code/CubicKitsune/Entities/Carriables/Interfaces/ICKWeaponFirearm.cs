using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public interface ICKWeaponFirearm
	{
		public struct CapacitySettings
		{
			public int MaxClip { get; set; }
			public int MaxAmmo { get; set; }
			public string ProjectileAsset { get; set; }
		}

		public enum InputFunction
		{
			None,
			FirePrimary,
			FireSecondary, // projectiles?
			ModeSelector,
			Reload
		}

		public CapacitySettings PrimaryCapacitySettings { get; set; }
		public CapacitySettings SecondaryCapacitySettings { get; set; }

		public InputFunction PrimaryFunction { get; set; }
		public InputFunction SecondaryFunction { get; set; }
		public InputFunction ReloadFunction { get; set; }

		public float ReloadTime { get; set; }
	}
}
