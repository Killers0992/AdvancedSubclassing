﻿using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using System.Collections.Generic;
using System.Linq;
using EPlayer = Exiled.API.Features.Player;

namespace Subclass.Handlers
{
	public class Map
	{
		public void OnExplodingGrenade(ExplodingGrenadeEventArgs ev)
		{
			if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Thrower) ||
				(!TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFlash) &&
				 !TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFrag)))
			{
				Log.Debug($"Player with name {ev.Thrower.Nickname} has no subclass", Subclass.Instance.Config.Debug);
				return;
			}
			if (TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFlash) && ev.GrenadeType == Exiled.API.Enums.GrenadeType.Flashbang)
			{
				if (!TrackingAndMethods.CanUseAbility(ev.Thrower, AbilityType.HealGrenadeFlash, TrackingAndMethods.PlayersWithSubclasses[ev.Thrower]))
				{
					TrackingAndMethods.DisplayCantUseAbility(ev.Thrower, AbilityType.HealGrenadeFlash, TrackingAndMethods.PlayersWithSubclasses[ev.Thrower], "heal flash");
					return;
				}
				TrackingAndMethods.UseAbility(ev.Thrower, AbilityType.HealGrenadeFlash, TrackingAndMethods.PlayersWithSubclasses[ev.Thrower]);
				ev.IsAllowed = false;
				UpdateHealths(ev, "HealGrenadeFlashHealAmount");
			}
			else if (TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFrag) && ev.GrenadeType == Exiled.API.Enums.GrenadeType.FragGrenade)
			{
				if (!TrackingAndMethods.CanUseAbility(ev.Thrower, AbilityType.HealGrenadeFrag, TrackingAndMethods.PlayersWithSubclasses[ev.Thrower]))
				{
					TrackingAndMethods.DisplayCantUseAbility(ev.Thrower, AbilityType.HealGrenadeFrag, TrackingAndMethods.PlayersWithSubclasses[ev.Thrower], "heal frag");
					return;
				}
				TrackingAndMethods.UseAbility(ev.Thrower, AbilityType.HealGrenadeFrag, TrackingAndMethods.PlayersWithSubclasses[ev.Thrower]);
				ev.IsAllowed = false;
				UpdateHealths(ev, "HealGrenadeFragHealAmount");
			}

			//if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Thrower))
			//{
			//    foreach (EPlayer target in ev.Targets)
			//    {
			//        if (target.Team != ev.Thrower.Team) continue;
			//        if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Thrower) && Tracking.PlayersWithSubclasses.ContainsKey(target) &&
			//            Tracking.PlayersWithSubclasses[ev.Thrower].AdvancedFFRules.Contains(Tracking.PlayersWithSubclasses[target].Name))
			//        {
			//            target.Hurt(ev.TargetToDamages[target], DamageTypes.Grenade);
			//            continue;
			//        }

			//        if (Tracking.FriendlyFired.Contains(target) || (Tracking.PlayersWithSubclasses.ContainsKey(ev.Thrower) &&
			//            !Tracking.PlayersWithSubclasses[ev.Thrower].BoolOptions["DisregardHasFF"] &&
			//            Tracking.PlayersWithSubclasses[ev.Thrower].BoolOptions["HasFriendlyFire"]) ||
			//            (Tracking.PlayersWithSubclasses.ContainsKey(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
			//            Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
			//        {
			//            if (!Tracking.FriendlyFired.Contains(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"])
			//                Tracking.AddToFF(ev.Thrower);
			//            target.Hurt(ev.TargetToDamages[target], DamageTypes.Grenade);
			//            //ev.IsAllowed = true;
			//        }
			//    }
			//}
		}

		public void UpdateHealths(ExplodingGrenadeEventArgs ev, string type)
		{
			UnityEngine.Collider[] colliders = UnityEngine.Physics.OverlapSphere(ev.Grenade.transform.position, 4);
			foreach (UnityEngine.Collider collider in colliders.Where(c => c.name == "Player"))
			{
				EPlayer player = EPlayer.Get(collider.gameObject);
				if (player != null && player.Team == ev.Thrower.Team)
				{
					if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.CantHeal)) return;
					if (TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].FloatOptions.ContainsKey(type))
					{
						if (TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].FloatOptions[type] + player.Health > player.MaxHealth) player.Health = player.MaxHealth;
						else player.Health += TrackingAndMethods.PlayersWithSubclasses[ev.Thrower].FloatOptions[type];
					}
					else
					{
						player.Health = player.MaxHealth;
					}
				}
			}
		}

		public void UpdateHealths(UnityEngine.Collider[] colliders, EPlayer thrower, string type)
		{
			foreach (UnityEngine.Collider collider in colliders.Where(c => c.name == "Player"))
			{
				EPlayer player = EPlayer.Get(collider.gameObject);
				if (player != null && player.Team == thrower.Team)
				{
					if (TrackingAndMethods.PlayersWithSubclasses[thrower].FloatOptions.ContainsKey(type))
					{
						if (TrackingAndMethods.PlayersWithSubclasses[thrower].FloatOptions[type] + player.Health > player.MaxHealth) player.Health = player.MaxHealth;
						else player.Health += TrackingAndMethods.PlayersWithSubclasses[thrower].FloatOptions[type];
					}
					else
					{
						player.Health = player.MaxHealth;
					}
				}
			}
		}
	}
}
