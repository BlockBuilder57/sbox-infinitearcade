using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public interface ICKTool
	{
		[Flags]
		public enum InputMode
		{
			None = 0,
			Single = 1 << 0,
			FullAuto = 1 << 1,
			Burst = 1 << 2,
		}

		public struct InputSettings
		{
			public float Rate { get; set; }
			public int BurstAmount { get; set; }
			[BitFlags] public InputMode AllowedModes { get; set; }
		}

		public struct Sounds
		{
			public string Key { get; set; }
			public SoundEvent Event { get; set; }
		}

		public InputSettings PrimaryInputSettings { get; set; }
		public InputSettings SecondaryInputSettings { get; set; }
		public InputSettings ReloadInputSettings { get; set; }

		//public IDictionary<string, SoundEvent> SoundEvents { get; set; }
	}
}
