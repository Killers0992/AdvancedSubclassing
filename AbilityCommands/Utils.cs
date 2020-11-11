﻿using Exiled.API.Features;
using Grenades;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.AbilityCommands
{
	static class Utils
	{
        public static void AttemptRevive(Player player, SubClass subClass, bool necro = false)
        {
            Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} attempt", Subclass.Instance.Config.Debug);
            AbilityType ability = necro ? AbilityType.Necromancy : AbilityType.Revive;
            if (Tracking.OnCooldown(player, ability, subClass))
            {
                Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} on cooldown", Subclass.Instance.Config.Debug);
                Tracking.DisplayCooldown(player, necro ? AbilityType.Necromancy : AbilityType.Revive, subClass, necro ? "necromancy" : "revive", Time.time);
                return;
            }

            List<Collider> colliders = Physics.OverlapSphere(player.Position, 3f).Where(e => e.gameObject.GetComponentInParent<Ragdoll>() != null).ToList();

            colliders.Sort((Collider x, Collider y) =>
            {
                return Vector3.Distance(x.gameObject.transform.position, player.Position).CompareTo(Vector3.Distance(y.gameObject.transform.position, player.Position));
            });

            if (colliders.Count == 0)
            {
                player.Broadcast(2, Subclass.Instance.Config.ReviveFailedNoBodyMessage);
                Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} overlap did not hit a ragdoll", Subclass.Instance.Config.Debug);
                return;
            }

            Ragdoll doll = colliders[0].gameObject.GetComponentInParent<Ragdoll>();
            if (doll.owner == null)
            {
                Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
                return;
            }

            if (doll.owner.DeathCause.GetDamageType() == DamageTypes.Lure)
            {
                Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
                return;
            }

            Player owner = Player.Get(colliders[0].gameObject.GetComponentInParent<Ragdoll>().owner.PlayerId);
            if (owner != null && !owner.IsAlive)
            {
                bool revived = false;
                if (!necro && Tracking.GetPreviousTeam(owner) != null &&
                Tracking.GetPreviousTeam(owner) == player.Team)
                {
                    if (Tracking.PlayersThatJustGotAClass.ContainsKey(owner)) Tracking.PlayersThatJustGotAClass[owner] = Time.time + 3f;
                    else Tracking.PlayersThatJustGotAClass.Add(owner, Time.time + 3f);

                    owner.SetRole((RoleType)Tracking.GetPreviousRole(owner), true);

                    if (Tracking.PreviousSubclasses.ContainsKey(owner) && Tracking.PreviousSubclasses[owner].AffectsRoles.Contains((RoleType)Tracking.GetPreviousRole(owner)))
                        Tracking.AddClass(owner, Tracking.PreviousSubclasses[owner], false, true);

                    owner.Inventory.Clear();
                    revived = true;
                }
                else if (necro)
                {
                    owner.Role = RoleType.Scp0492;
                    Tracking.AddZombie(player, owner);
                    owner.IsFriendlyFireEnabled = true;
                    revived = true;
                }
                if (revived)
                {
                    Timing.CallDelayed(0.2f, () =>
                    {
                        owner.ReferenceHub.playerMovementSync.OverridePosition(player.Position + new Vector3(0.3f, 1f, 0), 0, true);
                    });
                    UnityEngine.Object.DestroyImmediate(doll.gameObject, true);
                    Tracking.AddCooldown(player, ability);
                    Tracking.UseAbility(player, ability, subClass);
                    Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} succeeded", Subclass.Instance.Config.Debug);
                }
                else
                {
                    Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                    player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
                }
            }
            else
            {
                Log.Debug($"Player {player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
            }
        }

        public static void SpawnGrenade(ItemType type, Player player, SubClass subClass)
        {
            // Credit to KoukoCocoa's AdminTools for the grenade spawn script below, I was lost. https://github.com/KoukoCocoa/AdminTools/
            GrenadeManager grenadeManager = player.ReferenceHub.gameObject.GetComponent<GrenadeManager>();
            GrenadeSettings settings = grenadeManager.availableGrenades.FirstOrDefault(g => g.inventoryID == type);
            Grenades.Grenade grenade = UnityEngine.Object.Instantiate(settings.grenadeInstance).GetComponent<Grenades.Grenade>();
            if (type == ItemType.GrenadeFlash) grenade.fuseDuration = subClass.FloatOptions.ContainsKey("FlashOnCommandFuseTimer") ? subClass.FloatOptions["FlashOnCommandFuseTimer"] : 0.3f;
            else grenade.fuseDuration = subClass.FloatOptions.ContainsKey("GrenadeOnCommandFuseTimer") ? subClass.FloatOptions["GrenadeOnCommandFuseTimer"] : 0.3f;
            grenade.FullInitData(grenadeManager, player.Position, Quaternion.Euler(grenade.throwStartAngle),
                grenade.throwLinearVelocityOffset, grenade.throwAngularVelocity);
            NetworkServer.Spawn(grenade.gameObject);
        }
    }
}
