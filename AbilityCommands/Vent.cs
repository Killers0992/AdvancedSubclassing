using CommandSystem;
using CustomPlayerEffects;
using Exiled.API.Features;
using MEC;
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
	class Vent : ICommand
	{
		public string Command { get; } = "vent";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Begin venting, if you have the vent ability.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			Invisible scp268 = player.ReferenceHub.playerEffectsController.GetEffect<Invisible>();
			if (TrackingAndMethods.PlayersVenting.Contains(player) && scp268 != null && scp268.IsEnabled)
			{
				TrackingAndMethods.PlayersVenting.Remove(player);
				scp268.Disabled();
				response = "";
				return true;
			}

			if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) || !TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Vent))
			{
				Log.Debug($"Player {player.Nickname} could not vent", Subclass.Instance.Config.Debug);
				response = "";
				return true;
			}
			SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
			if (TrackingAndMethods.OnCooldown(player, AbilityType.Vent, subClass))
			{
				Log.Debug($"Player {player.Nickname} failed to vent", Subclass.Instance.Config.Debug);
				TrackingAndMethods.DisplayCooldown(player, AbilityType.Vent, subClass, "vent", Time.time);
				response = "";
				return true;
			}

			if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Vent, subClass))
			{
				TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Vent, subClass, "vent");
				response = "";
				return true;
			}

			if (scp268 != null)
			{
				if (scp268.IsEnabled)
				{
					Log.Debug($"Player {player.Nickname} failed to vent", Subclass.Instance.Config.Debug);
					player.Broadcast(3, Subclass.Instance.Config.AlreadyInvisibleMessage);
					response = "";
					return true;
				}

				//scp268.Duration = subClass.FloatOptions.ContainsKey("InvisibleOnCommandDuration") ?
				//    subClass.FloatOptions["InvisibleOnCommandDuration"]*2f : 30f*2f;

				//player.ReferenceHub.playerEffectsController.EnableEffect(scp268);

				player.ReferenceHub.playerEffectsController.EnableEffect<Invisible>();
				TrackingAndMethods.PlayersInvisibleByCommand.Add(player);
				TrackingAndMethods.PlayersVenting.Add(player);
				Timing.CallDelayed(subClass.FloatOptions.ContainsKey("VentDuration") ?
					subClass.FloatOptions["VentDuration"] : 15f, () =>
					{
						if (TrackingAndMethods.PlayersVenting.Contains(player)) TrackingAndMethods.PlayersVenting.Remove(player);
						if (TrackingAndMethods.PlayersInvisibleByCommand.Contains(player)) TrackingAndMethods.PlayersInvisibleByCommand.Remove(player);
						if (scp268.IsEnabled) player.ReferenceHub.playerEffectsController.DisableEffect<Invisible>();
					});

				TrackingAndMethods.AddCooldown(player, AbilityType.Vent);
				TrackingAndMethods.UseAbility(player, AbilityType.Vent, subClass);

			}
			response = "";
			return true;
		}
	}
}
