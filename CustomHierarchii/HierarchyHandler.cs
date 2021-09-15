// -----------------------------------------------------------------------
// <copyright file="HierarchyHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Mistaken.API;
using Mistaken.API.Diagnostics;

namespace Mistaken.CustomHierarchii
{
    /// <inheritdoc/>
    public class HierarchyHandler : Module
    {
        /// <inheritdoc cref="Module.Module(Exiled.API.Interfaces.IPlugin{Exiled.API.Interfaces.IConfig})"/>
        public HierarchyHandler(PluginHandler p)
            : base(p)
        {
        }

        /// <inheritdoc/>
        public override string Name => "Hierarchy";

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
            Exiled.Events.Handlers.Player.Verified -= this.Handle<Exiled.Events.EventArgs.VerifiedEventArgs>((ev) => this.Player_Verified(ev));
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Player.ChangingRole += this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
            Exiled.Events.Handlers.Player.Verified += this.Handle<Exiled.Events.EventArgs.VerifiedEventArgs>((ev) => this.Player_Verified(ev));
        }

        private static readonly Dictionary<Player, short> HierarchiiLevel = new Dictionary<Player, short>();

        private static void UpdatePlayer(Player player)
        {
            if (HierarchiiLevel[player] == -1)
            {
                foreach (var p in RealPlayers.List)
                {
                    if (player == p)
                        continue;
                    CustomInfoHandler.SetTarget(p, "unit", null, player);
                    CustomInfoHandler.SetTarget(p, "hierarchii", null, player);

                    CustomInfoHandler.SetTarget(player, "unit", null, p);
                    CustomInfoHandler.SetTarget(player, "hierarchii", null, p);
                }

                return;
            }

            foreach (var p in RealPlayers.List)
            {
                if (player == p)
                    continue;
                if (HierarchiiLevel[p] == -1)
                {
                    CustomInfoHandler.SetTarget(p, "unit", null, player);
                    CustomInfoHandler.SetTarget(p, "hierarchii", null, player);

                    CustomInfoHandler.SetTarget(player, "unit", null, p);
                    CustomInfoHandler.SetTarget(player, "hierarchii", null, p);
                    continue;
                }

                CustomInfoHandler.SetTarget(p, "hierarchii", GetDiff(player, p), player);
                CustomInfoHandler.SetTarget(player, "hierarchii", GetDiff(p, player), p);
                if (p.Team == Team.MTF)
                    CustomInfoHandler.SetTarget(p, "unit", "Unit: " + p.UnitName, player);
                else
                    CustomInfoHandler.SetTarget(p, "unit", null, player);
            }
        }

        private static string GetDiff(Player player1, Player player2)
        {
            int player1Lvl = HierarchiiLevel[player1];
            int player2Lvl = HierarchiiLevel[player2];

            if (player1Lvl == -1 || player2Lvl == -1)
                return null;
            if (player1Lvl > player2Lvl)
                return $"<b>Wydawaj rozkazy</b>";
            else if (player1Lvl == player2Lvl)
                return $"<b>Ten sam poziom uprawnień</b>";
            else if (player1Lvl < player2Lvl)
                return $"<b>Wykonuj rozkazy</b>";

            return $"<b>Wykryto błąd (Niewykonalny kod się wykonał) ({player1Lvl})|({player2Lvl})</b>";
        }

        private static short GetHierarchiiLevel(Player player)
        {
            short lvl = 0;
            switch (player.Role)
            {
                case RoleType.FacilityGuard:
                    lvl = 100;
                    break;
                case RoleType.NtfPrivate:
                    lvl = 200;
                    break;
                case RoleType.NtfSpecialist:
                case RoleType.NtfSergeant:
                    lvl = 300;
                    break;
                case RoleType.NtfCaptain:
                    lvl = 400;
                    break;
                default:
                    return -1;
            }

            int index = Respawning.RespawnManager.Singleton.NamingManager.AllUnitNames.FindIndex(x => x.UnitName == player.UnitName);
            lvl += (short)(99 - index);
            return lvl;
        }

        private void Player_Verified(Exiled.Events.EventArgs.VerifiedEventArgs ev)
        {
            ev.Player.InfoArea &= ~PlayerInfoArea.UnitName;
            foreach (var item in RealPlayers.List.Where(p => p != ev.Player && p.Connection != null))
            {
                try
                {
                    ev.Player.SendFakeSyncVar(item.Connection.identity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurSpawnableTeamType), 0);
                }
                catch (System.Exception ex)
                {
                    this.Log.Error(ex.Message);
                    this.Log.Error(ex.StackTrace);
                }
            }
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole.GetTeam() == Team.MTF)
            {
                this.CallDelayed(
                    .1f,
                    () =>
                    {
                        foreach (var item in RealPlayers.List.Where(p => p != ev.Player && p.Connection != null))
                            item.SendFakeSyncVar(ev.Player.Connection.identity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurSpawnableTeamType), 0);
                    },
                    "LateForceNoBaseGameHierarchy");
            }

            if (ev.Player.IsAlive)
            {
                this.CallDelayed(.1f, () => HierarchiiLevel[ev.Player] = GetHierarchiiLevel(ev.Player), "ChangedRoleLate");
                this.CallDelayed(.5f, () => UpdatePlayer(ev.Player), "ChangedRoleLate");
            }
        }
    }
}
