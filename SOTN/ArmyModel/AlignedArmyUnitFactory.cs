namespace SOTN.ArmyModel {
    internal static class AlignedArmyUnitFactory {
        /// <summary>
        /// Ensure at least one identity is provided and, if a nation is present,
        /// resolve its faction. Returns a resolved, non-null faction plus the
        /// (possibly null) nation to be used by callers.
        /// </summary>
        public static (Faction ResolvedFaction, Nation? ResolvedNation) ResolveIdentity(Faction? faction, Nation? nation) {
            if (faction == null && nation == null) {
                throw new ArgumentException("faction and nation cannot both be null");
            }

            if (nation != null) {
                if (!FactionRegistry.TryGetFactionOf(nation.Value, out var factionInfo) || factionInfo == null) {
                    throw new ArgumentException($"nation '{nation}' has no mapped faction");
                }

                if (faction != null && faction != factionInfo.Id) {
                    throw new ArgumentException($"provided faction '{faction}' does not match nation '{nation}' faction: '{factionInfo.Id}'");
                }

                faction = factionInfo.Id;
            }

            return (faction ?? throw new ArgumentNullException(nameof(faction), $"faction null after validation (nation = '{nation}')"), nation);
        }
    }
}