using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public interface ICKProjectile
	{
		public string Identifier { get; set; }
		public string TypeLibraryName { get; set; }
		public Model WorldModel { get; set; }
		
		public struct SpawnStats
		{
			public float Size { get; set; }
			public float Health { get; set; }
			public float Lifetime { get; set; }

			public Vector3 ForceLinear { get; set; }
			public Vector3 ForceLinearRandom { get; set; }
			public Angles ForceAngular { get; set; }
			public Angles ForceAngularRandom { get; set; }
		}

		public SpawnStats Stats { get; set; }
		public float Damage { get; set; }

		public int Count { get; set; }
		public bool StatsDividedAcrossCount { get; set; }

		public struct BounceParameters
		{
			public int MaxBounces { get; set; }
			public int MaxGlanceAngle { get; set; }
			public int VelocityMultiplier { get; set; }
		}

		public BounceParameters BounceParams { get; set; }
	}
}
