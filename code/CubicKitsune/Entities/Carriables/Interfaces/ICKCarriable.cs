using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace CubicKitsune
{
	public interface ICKCarriable
	{
		public enum BucketCategory
		{
			Default = 0,
			Primary = 100,
			Secondary = 200,
			Melee = 300,
			Tool = 400
		}

		public string Identifier { get; set; }
		public BucketCategory Bucket { get; set; }
		public int SubBucket { get; set; }

		public Model ViewModel { get; set; }
		public Model WorldModel { get; set; }
		public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
		public CitizenAnimationHelper.Hand Handedness { get; set; }
	}
}
