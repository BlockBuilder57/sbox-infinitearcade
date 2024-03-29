﻿using CubicKitsune;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CubicKitsune
{
	[GameResource("Firearm", "firearm", "The definition for a firearm.", Icon = "smoking_rooms")]
	public class CKWeaponFirearmResource : CKToolResource, ICKWeaponFirearm
	{
		[HideInEditor] public static IReadOnlyList<CKWeaponFirearmResource> AllFirearms => _allFirearms;
		[HideInEditor] internal static List<CKWeaponFirearmResource> _allFirearms = new();

		public ICKWeaponFirearm.CapacitySettings PrimaryCapacitySettings { get; set; }
		public ICKWeaponFirearm.CapacitySettings SecondaryCapacitySettings { get; set; }

		public ICKWeaponFirearm.InputFunction PrimaryFunction { get; set; }
		public ICKWeaponFirearm.InputFunction SecondaryFunction { get; set; }
		public ICKWeaponFirearm.InputFunction ReloadFunction { get; set; }

		public float ReloadTime { get; set; } = 1.0f;

		protected override void PostLoad()
		{
			base.PostLoad();

			if (!_allFirearms.Contains(this))
				_allFirearms.Add(this);
		}
	}


}
