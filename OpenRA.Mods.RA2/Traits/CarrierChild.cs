#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Can be bound to a spawner.")]
	public class CarrierChildInfo : BaseSpawnerChildInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5 * 1024);

		public override object Create(ActorInitializer init) { return new CarrierChild(init, this); }
	}

	public class CarrierChild : BaseSpawnerChild, INotifyIdle
	{
		readonly AmmoPool[] ammoPools;
		public readonly CarrierChildInfo Info;

		CarrierParent spawnerParent;

		public CarrierChild(ActorInitializer init, CarrierChildInfo info)
			: base(init, info)
		{
			Info = info;
			ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray();
		}

		public void EnterSpawner(Actor self)
		{
			if (Parent == null || Parent.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is EnterCarrierParent)
				return;

			// Cancel whatever else self was doing and return.
			var target = Target.FromActor(Parent);
			self.QueueActivity(false, new EnterCarrierParent(self, Parent, spawnerParent, EnterBehaviour.Exit));
		}

		public override void LinkParent(Actor self, Actor parent, BaseSpawnerParent spawnerParent)
		{
			base.LinkParent(self, parent, spawnerParent);
			this.spawnerParent = spawnerParent as CarrierParent;
		}

		bool NeedsToReload(Actor self)
		{
			// The unit may not have ammo but will have unlimited ammunitions.
			if (ammoPools.Length == 0)
				return false;

			return ammoPools.All(x => !x.HasAmmo);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			EnterSpawner(self);
		}

		public override void Stop(Actor self)
		{
			base.Stop(self);
			EnterSpawner(self);
		}
	}
}
