﻿using Exiled.API.Features;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.MonoBehaviours
{
	class EscapeBehaviour : NetworkBehaviour
	{
		private Player player;
		public RoleType EscapesAsCuffed = RoleType.None;
		public RoleType EscapesAsNotCuffed = RoleType.None;
		public bool Enabled = true;
		private Vector3 EscapePosition;

		private void Awake()
		{
			player = Player.Get(gameObject);
			EscapePosition = GetComponent<Escape>().worldPosition;
		}

		private void Update()
		{
			if (Enabled)
			{
				if (Vector3.Distance(transform.position, EscapePosition) < (Escape.radius))
				{
					if (!player.IsCuffed && EscapesAsNotCuffed != RoleType.None) player.SetRole(EscapesAsNotCuffed, Exiled.API.Enums.SpawnReason.Escaped, true);
					else if (player.IsCuffed && EscapesAsCuffed != RoleType.None) player.SetRole(EscapesAsCuffed, Exiled.API.Enums.SpawnReason.Escaped, true);
				}
			}
		}

		public void Destroy()
		{
			Enabled = false;
			DestroyImmediate(this, true);
		}
	}
}
