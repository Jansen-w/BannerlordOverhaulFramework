using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BOF.Overhaul.CampaignSystem
{
    public abstract class CrimeModel : GameModel
    {
        public abstract float GetMaxCrimeRating();

        public abstract float GetMinAcceptableCrimeRating(IFaction faction);

        public abstract bool DoesPlayerHaveAnyCrimeRating(IFaction faction);

        public abstract bool IsPlayerCrimeRatingSevere(IFaction faction);

        public abstract bool IsPlayerCrimeRatingModerate(IFaction faction);

        public abstract bool IsPlayerCrimeRatingMild(IFaction faction);

        public abstract float GetCost(
            IFaction faction,
            CrimeModel.PaymentMethod paymentMethod,
            float minimumCrimeRating);

        public abstract ExplainedNumber GetDailyCrimeRatingChange(
            IFaction faction,
            bool includeDescriptions = false);

        public abstract float GetCrimeRatingOf(
            CrimeModel.CrimeType crime,
            params object[] additionalArgs);

        [Flags]
        public enum PaymentMethod : uint
        {
            ExMachina = 4096, // 0x00001000
            Gold = 1,
            Influence = 2,
            Punishment = 4,
            Execution = 8,
        }

        public enum CrimeType
        {
            Murder,
            KnockUnconcious,
            AttackCaravan,
            AttackVillagerParty,
            GrandTheft,
            Smuggling,
            RaidVillage,
        }
    }
}