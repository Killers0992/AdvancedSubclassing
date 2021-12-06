﻿using CommandSystem;
using Exiled.API.Features;
using PlayerStatsSystem;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.AbilityCommands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	class Punch : ICommand
	{
		public string Command { get; } = "punch";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Punch the player you're looking at.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = "";
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) ||
				!TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Punch) ||
				player.IsCuffed)
			{
				Log.Debug($"Player {player.Nickname} could not use the punch command", Subclass.Instance.Config.Debug);
				response = "";
				return true;
			}

			SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];

			if (!subClass.Abilities.Contains(AbilityType.InfiniteSprint))
			{
				if ((player.Stamina.RemainingStamina * 100) - (subClass.FloatOptions.ContainsKey("PunchStaminaUse") ? subClass.FloatOptions["PunchStaminaUse"] : 10) <= 0)
				{
					Log.Debug($"Player {player.Nickname} failed to use the punch command", Subclass.Instance.Config.Debug);
					player.Broadcast(5, Subclass.Instance.Config.OutOfStaminaMessage);
					return true;
				}

				player.Stamina.RemainingStamina = Mathf.Clamp(
					player.Stamina.RemainingStamina - (subClass.FloatOptions.ContainsKey("PunchStaminaUse") ? subClass.FloatOptions["PunchStaminaUse"] / 100 : .1f), 0, 1);
				player.Stamina._regenerationTimer = 0;
			}

			if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Punch, subClass))
			{
				TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Punch, subClass, "punch");
				response = "";
				return true;
			}

			if (TrackingAndMethods.OnCooldown(player, AbilityType.Punch, subClass))
			{
				Log.Debug($"Player {player.Nickname} failed to use punch", Subclass.Instance.Config.Debug);
				TrackingAndMethods.DisplayCooldown(player, AbilityType.Punch, subClass, "punch", Time.time);
				response = "";
				return true;
			}

			if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out RaycastHit hit,
				(subClass.FloatOptions.ContainsKey("PunchRange") ? subClass.FloatOptions["PunchRange"] : 1.3f)))
			{
				Player target = Player.Get(hit.collider.gameObject) ?? Player.Get(hit.collider.GetComponentInParent<ReferenceHub>());
				if (target == null || target.Id == player.Id) return true;
				TrackingAndMethods.AddCooldown(player, AbilityType.Punch);
				TrackingAndMethods.UseAbility(player, AbilityType.Punch, subClass);
				target.Hurt(new UniversalDamageHandler(subClass.FloatOptions["PunchDamage"], new DeathTranslation(0, 0, 0, "")));
			}

			return true;
		}
	}
}
