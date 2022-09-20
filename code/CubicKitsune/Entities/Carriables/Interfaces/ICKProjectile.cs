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
		public float Size { get; set; }

		public float Spread { get; set; }
		public float Damage { get; set; }
		public float Force { get; set; }

		public int Pellets { get; set; }
		public bool DividedAcrossPellets { get; set; }

		public struct BounceParameters
		{
			public int MaxBounces { get; set; }
			public int MaxGlanceAngle { get; set; }
			public int VelocityMultiplier { get; set; }
		}

		public bool CanBounce { get; set; }
		public BounceParameters BounceParams { get; set; }
	}
}
