// -----------------------------------------------------------------------
// <copyright file="HierarchyHandler.CompareResult.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.CustomHierarchii
{
    public partial class HierarchyHandler
    {
        /// <summary>
        /// Result of comparing 2 players.
        /// </summary>
        public enum CompareResult
        {
#pragma warning disable CS1591 // Brak komentarza XML dla widocznego publicznie typu lub składowej
            FOLLOW_ORDERS,
            GIVE_ORDERS,
            SAME_RANK,
            DO_NOT_COMPARE,
            NO_ACTION,
#pragma warning restore CS1591 // Brak komentarza XML dla widocznego publicznie typu lub składowej
        }
    }
}
