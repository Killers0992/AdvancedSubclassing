using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.Permissions.Extensions;
using EPlayer = Exiled.API.Features.Player;
using System.Collections;
using Exiled.API.Enums;
using System;
using Respawning;
using PlayerStatsSystem;
using Exiled.API.Features.Items;

namespace Subclass.Handlers
{
	public class Server
	{
		System.Random rnd = new System.Random();
		public void OnRoundStarted()
		{
			TrackingAndMethods.RoundStartedAt = Time.time;
			Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () =>
			{
				Log.Debug("Round started!", Subclass.Instance.Config.Debug);
				foreach (EPlayer player in EPlayer.List)
				{
					TrackingAndMethods.MaybeAddRoles(player);
				}
				foreach (string message in TrackingAndMethods.QueuedCassieMessages)
				{
					Cassie.Message(message, true, false);
					Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
				}
				TrackingAndMethods.QueuedCassieMessages.Clear();
			});
		}

		public void OnRoundEnded(RoundEndedEventArgs ev)
		{
			// I may just consider using reflection and just loop over all members and clear them if I can.
			TrackingAndMethods.KillAllCoroutines();
			TrackingAndMethods.Coroutines.Clear();
			TrackingAndMethods.PlayersWithSubclasses.Clear();
			TrackingAndMethods.Cooldowns.Clear();
			TrackingAndMethods.FriendlyFired.Clear();
			TrackingAndMethods.PlayersThatBypassedTeslaGates.Clear();
			TrackingAndMethods.PreviousRoles.Clear();
			TrackingAndMethods.PlayersWithZombies.Clear();
			TrackingAndMethods.PlayersThatHadZombies.Clear();
			TrackingAndMethods.QueuedCassieMessages.Clear();
			TrackingAndMethods.NextSpawnWave.Clear();
			TrackingAndMethods.NextSpawnWaveGetsRole.Clear();
			TrackingAndMethods.PlayersThatJustGotAClass.Clear();
			TrackingAndMethods.SubClassesSpawned.Clear();
			TrackingAndMethods.PreviousSubclasses.Clear();
			TrackingAndMethods.PreviousBadges.Clear();
			TrackingAndMethods.RagdollRoles.Clear();
			TrackingAndMethods.AbilityUses.Clear();
			TrackingAndMethods.PlayersInvisibleByCommand.Clear();
			TrackingAndMethods.PlayersVenting.Clear();
			TrackingAndMethods.NumSpawnWaves.Clear();
			TrackingAndMethods.SpawnWaveSpawns.Clear();
			TrackingAndMethods.ClassesGiven.Clear();
			TrackingAndMethods.DontGiveClasses.Clear();
			TrackingAndMethods.PlayersBloodLusting.Clear();
			TrackingAndMethods.Zombie106Kills.Clear();
			API.EnableAllClasses();
		}

		public void OnRespawningTeam(RespawningTeamEventArgs ev)
		{
			if (ev.Players.Count == 0 || !ev.IsAllowed) return;
			Team spawnedTeam = ev.NextKnownTeam == SpawnableTeamType.NineTailedFox ? Team.MTF : Team.CHI;
			if (!TrackingAndMethods.NumSpawnWaves.ContainsKey(spawnedTeam)) TrackingAndMethods.NumSpawnWaves.Add(spawnedTeam, 0);
			TrackingAndMethods.NumSpawnWaves[spawnedTeam]++;
			Timing.CallDelayed(5f, () => // Clear them after the wave spawns instead.
			{
				TrackingAndMethods.NextSpawnWave.Clear();
				TrackingAndMethods.NextSpawnWaveGetsRole.Clear();
				TrackingAndMethods.SpawnWaveSpawns.Clear();
			});
			bool ntfSpawning = ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox;
			if (!Subclass.Instance.Config.AdditiveChance)
			{
				List<RoleType> hasRole = new List<RoleType>();
				foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] &&
				(!e.IntOptions.ContainsKey("MaxSpawnPerRound") || TrackingAndMethods.ClassesSpawned(e) < e.IntOptions["MaxSpawnPerRound"]) &&
				(ntfSpawning ? (e.AffectsRoles.Contains(RoleType.NtfPrivate) || e.AffectsRoles.Contains(RoleType.NtfCaptain) ||
				e.AffectsRoles.Contains(RoleType.NtfSergeant)) : e.AffectsRoles.Contains(RoleType.ChaosConscript) || e.AffectsRoles.Contains(RoleType.ChaosMarauder) || e.AffectsRoles.Contains(RoleType.ChaosRepressor) || e.AffectsRoles.Contains(RoleType.ChaosRifleman)) &&
				((e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.BoolOptions["OnlyAffectsSpawnWave"]) ||
				(e.BoolOptions.ContainsKey("AffectsSpawnWave") && e.BoolOptions["AffectsSpawnWave"])) &&
				(!e.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.BoolOptions["WaitForSpawnWaves"] &&
				TrackingAndMethods.GetNumWavesSpawned(e.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
				(Team)Enum.Parse(typeof(Team), e.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.IntOptions["NumSpawnWavesToWait"])) &&
				TrackingAndMethods.EvaluateSpawnParameters(e)))
				{
					if ((ntfSpawning ? (subClass.AffectsRoles.Contains(RoleType.NtfPrivate) ||
					subClass.AffectsRoles.Contains(RoleType.NtfCaptain) || subClass.AffectsRoles.Contains(RoleType.NtfSergeant))
					: subClass.AffectsRoles.Contains(RoleType.ChaosRifleman) || subClass.AffectsRoles.Contains(RoleType.ChaosRepressor) || subClass.AffectsRoles.Contains(RoleType.ChaosMarauder) || subClass.AffectsRoles.Contains(RoleType.ChaosConscript)) && (rnd.NextDouble() * 100) < subClass.FloatOptions["ChanceToGet"])
					{
						if (ntfSpawning)
						{
							if (!hasRole.Contains(RoleType.NtfPrivate) && subClass.AffectsRoles.Contains(RoleType.NtfPrivate))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.NtfPrivate, subClass);
								hasRole.Add(RoleType.NtfPrivate);
							}

							if (!hasRole.Contains(RoleType.NtfSergeant) && subClass.AffectsRoles.Contains(RoleType.NtfSergeant))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.NtfSergeant, subClass);
								hasRole.Add(RoleType.NtfSergeant);
							}

							if (!hasRole.Contains(RoleType.NtfPrivate) && subClass.AffectsRoles.Contains(RoleType.NtfPrivate))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.NtfPrivate, subClass);
								hasRole.Add(RoleType.NtfPrivate);
							}

							if (hasRole.Count == 3) break;
						}
						else
						{
							if (subClass.AffectsRoles.Contains(RoleType.ChaosConscript))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.ChaosConscript, subClass);
								break;
							}

							if (subClass.AffectsRoles.Contains(RoleType.ChaosMarauder))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.ChaosMarauder, subClass);
								break;
							}

							if (subClass.AffectsRoles.Contains(RoleType.ChaosRepressor))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.ChaosRepressor, subClass);
								break;
							}

							if (subClass.AffectsRoles.Contains(RoleType.ChaosRifleman))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.ChaosRifleman, subClass);
								break;
							}
						}
					}
				}
			}
			else
			{
				double num = (rnd.NextDouble() * 100);
				if (!ntfSpawning && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.ChaosConscript) && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.ChaosMarauder) && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.ChaosRepressor) && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.ChaosRifleman)) return;
				else if (ntfSpawning && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfPrivate) &&
					!Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfCaptain) && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfSergeant))
					return;

				if (!ntfSpawning)
				{
					RoleType[] roles = { RoleType.ChaosRifleman, RoleType.ChaosRepressor, RoleType.ChaosMarauder };
					foreach (RoleType role in roles)
					{
						foreach (var possibity in Subclass.Instance.ClassesAdditive[role].Where(e => e.Key.BoolOptions["Enabled"] &&
						(!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || TrackingAndMethods.ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
						((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) ||
						(e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"])) &&
						(!e.Key.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.Key.BoolOptions["WaitForSpawnWaves"] &&
						TrackingAndMethods.GetNumWavesSpawned(e.Key.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
						(Team)Enum.Parse(typeof(Team), e.Key.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.Key.IntOptions["NumSpawnWavesToWait"]))
						&& TrackingAndMethods.EvaluateSpawnParameters(e.Key)))
						{
							Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for next spawn wave", Subclass.Instance.Config.Debug);
							if (num < possibity.Value)
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(role, possibity.Key);
								break;
							}
							else
							{
								Log.Debug($"Next spawn wave did not get subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
							}
						}
					}
				}
				else
				{
					RoleType[] roles = { RoleType.NtfCaptain, RoleType.NtfSergeant, RoleType.NtfPrivate };
					foreach (RoleType role in roles)
					{
						foreach (var possibity in Subclass.Instance.ClassesAdditive[role].Where(e => e.Key.BoolOptions["Enabled"] &&
						(!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || TrackingAndMethods.ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
						((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) ||
						(e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"])) &&
						(!e.Key.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.Key.BoolOptions["WaitForSpawnWaves"] &&
						TrackingAndMethods.GetNumWavesSpawned(e.Key.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
						(Team)Enum.Parse(typeof(Team), e.Key.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.Key.IntOptions["NumSpawnWavesToWait"]))
						&& TrackingAndMethods.EvaluateSpawnParameters(e.Key)))
						{
							Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for next spawn wave", Subclass.Instance.Config.Debug);
							if (num < possibity.Value)
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(role, possibity.Key);
								break;
							}
							else
							{
								Log.Debug($"Next spawn wave did not get subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
							}
						}
					}
				}
			}
			TrackingAndMethods.NextSpawnWave = ev.Players;
		}

		

		public void SpawnGrenade(ItemType type, EPlayer player, SubClass subClass)
		{
			Throwable throwable = null;
			switch (type)
			{
				case ItemType.GrenadeFlash:
					var flash = new FlashGrenade(ItemType.GrenadeFlash);
					if (subClass.FloatOptions.ContainsKey("FlashOnCommandFuseTimer"))
						flash.FuseTime = subClass.FloatOptions["FlashOnCommandFuseTimer"];
					throwable = flash;
					break;
				case ItemType.GrenadeHE:
					var gren = new ExplosiveGrenade(ItemType.GrenadeHE);
					if (subClass.FloatOptions.ContainsKey("GrenadeOnCommandFuseTimer"))
						gren.FuseTime = subClass.FloatOptions["GrenadeOnCommandFuseTimer"];
					throwable = gren;
					break;
				case ItemType.SCP018:
					throwable = new ExplosiveGrenade(ItemType.SCP018);
					break;
			}

			player.ThrowItem(throwable, false);
		}
	}
}
