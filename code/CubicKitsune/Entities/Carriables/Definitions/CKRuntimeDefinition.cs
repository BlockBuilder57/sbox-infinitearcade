using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using Sandbox;

namespace CubicKitsune
{
	public class CKRuntimeDefinition : ICKCarriable, ICKTool, ICKWeaponFirearm
	{
		public ICKWeaponFirearm.CapacitySettings PrimaryCapacitySettings { get; set; }
		public ICKWeaponFirearm.CapacitySettings SecondaryCapacitySettings { get; set; }

		public ICKWeaponFirearm.InputFunction PrimaryFunction { get; set; }
		public ICKWeaponFirearm.InputFunction SecondaryFunction { get; set; }
		public ICKWeaponFirearm.InputFunction ReloadFunction { get; set; }

		public float ReloadTime { get; set; } = 1.0f;

		public ICKTool.InputSettings PrimaryInputSettings { get; set; }
		public ICKTool.InputSettings SecondaryInputSettings { get; set; }
		public ICKTool.InputSettings ReloadInputSettings { get; set; }

		public IDictionary<string, SoundEvent> SoundEvents { get; set; }

		public string Identifier { get; set; } = "RUNTIME_DEFINITION";
		public ICKCarriable.BucketCategory Bucket { get; set; } = 0;
		public int SubBucket { get; set; } = 0;

		public Model WorldModel { get; set; }
		public Model ViewModel { get; set; }
		public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
		public CitizenAnimationHelper.Hand Handedness { get; set; }
	}
}
