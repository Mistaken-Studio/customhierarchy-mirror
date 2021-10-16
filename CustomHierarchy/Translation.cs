// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.CustomHierarchy
{
    internal class Translation : ITranslation
    {
        public string FollowOrders { get; set; } = "<color=yellow>Follow orders</color>";

        public string GiveOrders { get; set; } = "<color=yellow>Give orders</color>";

        public string SameRank { get; set; } = "<color=yellow>Same rank</color>";
    }
}
