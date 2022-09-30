using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CubicKitsune;
using infinitearcade.UI;
using Sandbox;

namespace CubicKitsune
{
	[Library("carriable_generic")]
	public partial class CKCarriable : BaseCarriable, ICKCarriable
	{
		[Net] public string Identifier { get; set; } = "ia_carriable_new";
		[Net] public ICKCarriable.BucketCategory Bucket { get; set; } = 0;
		[Net] public int SubBucket { get; set; } = 0;

		[Net] public Model ViewModel { get; set; }
		[Net] public Model WorldModel { get; set; }
		[Net] public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
		[Net] public CitizenAnimationHelper.Hand Handedness { get; set; }

		[Net, Predicted] public TimeSince TimeSinceDeployed { get; set; }
		[Net] public TimeSince TimeSinceDropped { get; set; }

		public PickupTrigger PickupTrigger { get; set; } // server only?

		[Net] protected Player OwnerPlayer { get; set; }

		public CKCarriable SetupFromInterface(ICKCarriable def)
		{
			if (def == null)
			{
				Log.Error($"{this} trying to set up with a null definition!");
				Delete();
				return null;
			}

			Identifier = def.Identifier;
			Bucket = def.Bucket;
			SubBucket = def.SubBucket;

			ViewModel = def.ViewModel;
			WorldModel = def.WorldModel;
			HoldType = def.HoldType;
			Handedness = def.Handedness;

			Model = def.WorldModel;

			return this;
		}
		public virtual CKCarriable SetupFromDefinition(CKCarriableDefinition def) => SetupFromInterface(def);

		public override void Spawn()
		{
			base.Spawn();

			Tags.Add("weapon");

			PickupTrigger = new PickupTrigger
			{
				Parent = this,
				Position = Position,
				EnableTouch = true,
				EnableSelfCollisions = false
			};

			PickupTrigger.PhysicsBody.AutoSleep = false;

			if (Model != null && Model.IsError && WorldModel != null && !WorldModel.IsError)
				Model = WorldModel;
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(Identifier))
				return base.ToString();
			return $"{Identifier} {NetworkIdent}";
		}

		public override void ActiveStart(Entity ent)
		{
			base.ActiveStart(ent);

			if (Host.IsClient && Owner == Local.Pawn && Local.Hud is InfiniteArcadeHud hud)
				hud.InventorySwitchActive(this);

			TimeSinceDeployed = 0;
		}

		public override void OnCarryStart(Entity carrier)
		{
			base.OnCarryStart(carrier);

			OwnerPlayer = carrier as Player;
			PickupTrigger.EnableAllCollisions = false;
		}

		public override void OnCarryDrop(Entity dropper)
		{
			base.OnCarryDrop(dropper);

			TimeSinceDropped = 0;
			OwnerPlayer = null;
			PickupTrigger.EnableAllCollisions = true;
		}

		public bool OnUse(Entity user)
		{
			if (!user.IsValid() || Owner != null)
				return false;

			// pretend we touched it
			user.StartTouch(this);

			return false;
		}

		public virtual bool IsUsable(Entity user)
		{
			if (Owner != null)
				return false;

			return true;
		}

		public void Remove()
		{
			if (PhysicsGroup != null)
				PhysicsGroup.Sleeping = false;
			Delete();
		}

		public override void SimulateAnimator(PawnAnimator anim)
		{
			anim.SetAnimParameter("holdtype", (int)HoldType);
			anim.SetAnimParameter("holdtype_handedness", (int)Handedness);
		}

		public override void CreateViewModel()
		{
			Host.AssertClient();

			ViewModelEntity = new CKViewModel
			{
				Position = Position,
				Owner = Owner,
				Model = ViewModel,
				EnableViewmodelRendering = true
			};
		}
	}
}
