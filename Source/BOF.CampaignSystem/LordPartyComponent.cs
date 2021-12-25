using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BOF.CampaignSystem
{
  public class LordPartyComponent : WarPartyComponent
  {
    // [CachedData]
    private TextObject _cachedName;
    // [SaveableField(30)]
    private Hero _leader;

    public override Hero PartyOwner => this.Owner;

    public override TextObject Name => this._cachedName ?? (this._cachedName = this.Owner != null ? this.GetPartyName() : new TextObject("{=!}unnamedMobileParty"));

    public override Settlement HomeSettlement => this.Owner.HomeSettlement;

    // [SaveableProperty(20)]
    public Hero Owner { get; private set; }

    public override Hero Leader => this._leader;

    public static MobileParty CreateLordParty(
      string stringId,
      Hero hero,
      Vec2 position,
      float spawnRadius,
      Settlement spawnSettlement,
      Hero partyLeader)
    {
      return MobileParty.CreateParty(hero.CharacterObject.StringId + "_party_1", new LordPartyComponent(hero, partyLeader), mobileParty => mobileParty.LordPartyComponent.InitializeLordPartyProperties(mobileParty, position, spawnRadius, spawnSettlement));
    }

    public LordPartyComponent(Hero owner, Hero leader)
    {
      this.Owner = owner;
      this._leader = leader;
    }

    public void ChangePartyOwner(Hero owner)
    {
      this.ClearCachedName();
      this.Owner = owner;
    }

    public override void ChangePartyLeader(Hero newLeader) => this._leader = newLeader;

    // [LoadInitializationCallback]
    // private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    // {
    //   if (this.Owner != null)
    //     return;
    //   this.Owner = objectLoadData.GetDataBySaveId(20) as Hero;
    // }

    public override void ClearCachedName() => this._cachedName = (TextObject) null;

    private TextObject GetPartyName()
    {
      TextObject text = GameTexts.FindText("str_lord_party_name");
      text.SetCharacterProperties("TROOP", this.Owner.CharacterObject);
      text.SetTextVariable("IS_LORDPARTY", 1);
      return text;
    }

    private void InitializeLordPartyProperties(
      MobileParty mobileParty,
      Vec2 position,
      float spawnRadius,
      Settlement spawnSettlement)
    {
      mobileParty.AddElementToMemberRoster(this.Owner.CharacterObject, 1, true);
      mobileParty.ActualClan = this.Owner.Clan;
      int troopNumberLimit = this.Owner == Hero.MainHero || this.Owner.Clan == Clan.PlayerClan ? 0 : (int) MathF.Min(this.Owner.Clan.IsRebelClan ? 40f : 19f, (this.Owner.Clan.IsRebelClan ? 0.2f : 0.1f) * (float) mobileParty.Party.PartySizeLimit);
      if (!TaleWorlds.CampaignSystem.Campaign.Current.GameStarted)
      {
        float num = (float) (1.0 - (double) MBRandom.RandomFloat * (double) MathF.Sqrt(MBRandom.RandomFloat));
        troopNumberLimit = (int) ((double) mobileParty.Party.PartySizeLimit * (double) num);
      }
      mobileParty.InitializeMobilePartyAroundPosition(this.Owner.Clan.DefaultPartyTemplate, position, spawnRadius, troopNumberLimit: troopNumberLimit);
      mobileParty.Party.Visuals.SetMapIconAsDirty();
      if (spawnSettlement != null)
        mobileParty.SetMoveGoToSettlement(spawnSettlement);
      mobileParty.Aggressiveness = (float) (0.899999976158142 + 0.100000001490116 * (double) this.Owner.GetTraitLevel(DefaultTraits.Valor) - 0.0500000007450581 * (double) this.Owner.GetTraitLevel(DefaultTraits.Mercy));
      mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, MBRandom.RandomInt(15, 30)));
      this.Owner.PassedTimeAtHomeSettlement = (float) (int) ((double) MBRandom.RandomFloat * 100.0);
      if (spawnSettlement == null)
        return;
      mobileParty.Ai.SetAIState(AIState.VisitingNearbyTown);
      mobileParty.SetMoveGoToSettlement(spawnSettlement);
    }
  }
}
