// -----------------------------------------------------------------------
// <copyright file="HierarchyHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Mistaken.API;
using Mistaken.API.Diagnostics;

namespace Mistaken.CustomHierarchy
{
    /// <inheritdoc/>
    public partial class HierarchyHandler : Module
    {
        /// <summary>
        /// Dictionary containing Custom Comparers with priority (Dictionary's key is only to delete or modify existing custom comparers).
        /// </summary>
        public static readonly Dictionary<string, (int Priority, Func<Player, Player, CompareResult> Comparer)> CustomPlayerComperers
            = new Dictionary<string, (int Priority, Func<Player, Player, CompareResult> Comparer)>();

        /// <summary>
        /// Compares <paramref name="player1"/> to <paramref name="player2"/>.
        /// </summary>
        /// <param name="player1">Player 1.</param>
        /// <param name="player2">Player 2.</param>
        /// <returns>Result as <paramref name="player1"/>'s power status.</returns>
        public static CompareResult ComparePlayer(Player player1, Player player2)
        {
            if (player1 == player2)
                return CompareResult.DO_NOT_COMPARE;

            bool doNotCompare = false;

            foreach (var comparer in CustomPlayerComperers.Values.OrderByDescending(x => x.Priority).Select(x => x.Comparer))
            {
                var result = comparer(player1, player2);
                if (result == CompareResult.DO_NOT_COMPARE)
                    doNotCompare = true;
                else if (result != CompareResult.NO_ACTION)
                    return result;
            }

            if (doNotCompare)
                return CompareResult.DO_NOT_COMPARE;

            if (player1.Side != player2.Side)
                return CompareResult.DO_NOT_COMPARE;

            if (player1.Role == RoleType.ClassD)
                return CompareResult.DO_NOT_COMPARE;

            var player1Value = GetPlayerValue(player1);
            var player2Value = GetPlayerValue(player2);

            if (player1Value > player2Value)
                return CompareResult.GIVE_ORDERS;
            else if (player1Value < player2Value)
                return CompareResult.FOLLOW_ORDERS;
            else
                return CompareResult.SAME_RANK;
        }

        /// <summary>
        /// Calculates <paramref name="player"/>'s hierarchy level.
        /// It's calculated based on <see cref="Player.Role"/> and <see cref="GetUnitValue(Player)"/>.
        /// </summary>
        /// <param name="player">Player to check.</param>
        /// <returns>Hierarchy level.</returns>
        public static short GetPlayerValue(Player player)
        {
            short value = GetUnitValue(player);

            switch (player.Role)
            {
                case RoleType.Scientist:
                    value += 0;
                    break;

                case RoleType.FacilityGuard:
                    value += 100;
                    break;

                case RoleType.ChaosConscript:
                case RoleType.ChaosRifleman:
                case RoleType.NtfPrivate:
                    value += 200;
                    break;

                case RoleType.ChaosRepressor:
                case RoleType.NtfSpecialist:
                case RoleType.NtfSergeant:
                    value += 300;
                    break;

                case RoleType.ChaosMarauder:
                case RoleType.NtfCaptain:
                    value += 400;
                    break;
            }

            Exiled.API.Features.Log.Debug($"[Hierarchy] {player.Nickname}'s lvl: {value}", PluginHandler.Instance.Config.VerbouseOutput);
            return value;
        }

        /// <summary>
        /// Calculates <paramref name="player"/>'s unit's hierarchy level.
        /// For Chaos Insurgency it's always 0.
        /// For MTF it's unit 99 - unit index.
        /// </summary>
        /// <param name="player">Player to check.</param>
        /// <returns>Unit hierarchy level.</returns>
        public static byte GetUnitValue(Player player)
        {
            if (player.IsCHI)
                return 0;
            int index = Respawning.RespawnManager.Singleton.NamingManager.AllUnitNames.FindIndex(x => x.UnitName == player.UnitName);
            Exiled.API.Features.Log.Debug($"[Hierarchy] {player.Nickname}'s unit lvl: {99 - index}", PluginHandler.Instance.Config.VerbouseOutput);
            return (byte)(99 - index);
        }

        /// <summary>
        /// Forces resync of hierarchy for <paramref name="p1"/>.
        /// </summary>
        /// <param name="p1">Player to update hierarchy for.</param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="ComparePlayer(Player, Player)"/> returns <see cref="CompareResult.NO_ACTION"/>.</exception>
        public static void UpdatePlayer(Player p1)
        {
            if (!p1.IsAlive)
                return;
            foreach (var p2 in RealPlayers.List)
            {
                if (p1 == p2)
                    continue;
                if (!p2.IsAlive)
                    continue;
                var diff = ComparePlayer(p1, p2);
                Exiled.API.Features.Log.Debug($"[Hierarchy] {p1.Nickname} vs {p2.Nickname}: {diff}", PluginHandler.Instance.Config.VerbouseOutput);
                switch (diff)
                {
                    case CompareResult.FOLLOW_ORDERS:
                        CustomInfoHandler.SetTarget(p1, "hierarchy", PluginHandler.Instance.Translation.FollowOrders, p2);
                        CustomInfoHandler.SetTarget(p2, "hierarchy", PluginHandler.Instance.Translation.GiveOrders, p1);
                        break;

                    case CompareResult.GIVE_ORDERS:
                        CustomInfoHandler.SetTarget(p1, "hierarchy", PluginHandler.Instance.Translation.GiveOrders, p2);
                        CustomInfoHandler.SetTarget(p2, "hierarchy", PluginHandler.Instance.Translation.FollowOrders, p1);
                        break;

                    case CompareResult.SAME_RANK:
                        CustomInfoHandler.SetTarget(p1, "hierarchy", PluginHandler.Instance.Translation.SameRank, p2);
                        CustomInfoHandler.SetTarget(p2, "hierarchy", PluginHandler.Instance.Translation.SameRank, p1);
                        break;

                    case CompareResult.DO_NOT_COMPARE:
                        CustomInfoHandler.SetTarget(p1, "hierarchy", null, p2);
                        CustomInfoHandler.SetTarget(p2, "hierarchy", null, p1);
                        break;

                    case CompareResult.NO_ACTION:
                        throw new InvalidOperationException($"Can't set hierarchy when diffrence is {CompareResult.NO_ACTION}");
                }
            }
        }

        /// <inheritdoc/>
        public override string Name => "Hierarchy";

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
            Exiled.Events.Handlers.Player.Verified -= this.Handle<Exiled.Events.EventArgs.VerifiedEventArgs>((ev) => this.Player_Verified(ev));
            Exiled.Events.Handlers.Server.RespawningTeam -= this.Handle<Exiled.Events.EventArgs.RespawningTeamEventArgs>((ev) => this.Server_RespawningTeam(ev));
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Player.ChangingRole += this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
            Exiled.Events.Handlers.Player.Verified += this.Handle<Exiled.Events.EventArgs.VerifiedEventArgs>((ev) => this.Player_Verified(ev));
            Exiled.Events.Handlers.Server.RespawningTeam += this.Handle<Exiled.Events.EventArgs.RespawningTeamEventArgs>((ev) => this.Server_RespawningTeam(ev));
        }

        /// <inheritdoc cref="Module.Module(Exiled.API.Interfaces.IPlugin{Exiled.API.Interfaces.IConfig})"/>
        internal HierarchyHandler(PluginHandler p)
            : base(p)
        {
        }

        private void Server_RespawningTeam(Exiled.Events.EventArgs.RespawningTeamEventArgs ev)
        {
            this.CallDelayed(.2f, () =>
            {
                foreach (var item in ev.Players)
                    UpdatePlayer(item);
            });
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleType.Spectator)
                return;
            if (ev.Reason == Exiled.API.Enums.SpawnReason.Respawn)
                return;
            this.CallDelayed(.2f, () => UpdatePlayer(ev.Player));
        }

        private void Player_Verified(Exiled.Events.EventArgs.VerifiedEventArgs ev)
        {
            ev.Player.InfoArea &= ~PlayerInfoArea.PowerStatus;
        }
    }
}
