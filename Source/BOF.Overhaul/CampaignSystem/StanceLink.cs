using System.Collections.Generic;

namespace BOF.Overhaul.CampaignSystem
{
  public class StanceLink
  {
    //[SaveableField(0)]
    private StanceType _stanceType;
    //[SaveableField(1)]
    private bool _isAtConstantWar;
    //[SaveableField(3)]
    public int BehaviorPriority;
    //[SaveableField(40)]
    private CampaignTime _warStartDate;
    //[SaveableField(50)]
    private CampaignTime _peaceDeclarationDate;
    //[SaveableField(60)]
    private int _casualties1;
    //[SaveableField(70)]
    private int _casualties2;
    //[SaveableField(100)]
    private List<Settlement> _capturedSettlements = new List<Settlement>();
    //[SaveableField(110)]
    private int _successfulSieges1;
    //[SaveableField(120)]
    private int _successfulSieges2;
    //[SaveableField(130)]
    private int _successfulRaids1;
    //[SaveableField(140)]
    private int _successfulRaids2;
    //[SaveableField(200)]
    private int _totalTributePaidby1;
    //[SaveableField(210)]
    private int _totalTributePaidby2;
    //[SaveableField(220)]
    private int _dailyTributeFrom1To2;

    public bool IsAtConstantWar
    {
      get => this._isAtConstantWar;
      set => this._isAtConstantWar = value;
    }

    public StanceType StanceType
    {
      get => this._stanceType;
      set
      {
        if (this._stanceType == value)
          return;
        if (this._stanceType == StanceType.War)
        {
          this._dailyTributeFrom1To2 = 0;
          this.ResetStats();
          this.PeaceDeclarationDate = CampaignTime.Now;
        }
        this._stanceType = value;
        if (this._stanceType == StanceType.War)
        {
          this._dailyTributeFrom1To2 = 0;
          this.ResetStats();
          this.WarStartDate = CampaignTime.Now;
        }
        StanceLink.ConsiderSiegesAndMapEvents(this.Faction1, this.Faction2);
        StanceLink.ConsiderSiegesAndMapEvents(this.Faction2, this.Faction1);
      }
    }

    private static void ConsiderSiegesAndMapEvents(IFaction faction, IFaction otherFaction)
    {
      if (faction is Kingdom)
        (faction as Kingdom).ConsiderSiegesAndMapEvents(otherFaction);
      else
        (faction as Clan).ConsiderSiegesAndMapEvents(otherFaction);
    }

    private void ResetStats()
    {
      this.Casualties1 = 0;
      this.Casualties2 = 0;
      this.SuccessfulRaids1 = 0;
      this.SuccessfulRaids2 = 0;
      this.SuccessfulSieges1 = 0;
      this.SuccessfulSieges2 = 0;
      this.TotalTributePaidby1 = 0;
      this.TotalTributePaidby2 = 0;
    }

    public bool IsAtWar
    {
      get => this._stanceType == StanceType.War;
      set => this._stanceType = !value ? StanceType.Neutral : StanceType.War;
    }

    public bool IsAllied
    {
      get => this._stanceType == StanceType.Alliance;
      set => this._stanceType = !value ? StanceType.Neutral : StanceType.Alliance;
    }

    public bool IsNeutral => this._stanceType == StanceType.Neutral;

    // [SaveableProperty(20)]
    public IFaction Faction1 { get; private set; }

    // [SaveableProperty(30)]
    public IFaction Faction2 { get; private set; }

    public CampaignTime WarStartDate
    {
      get => this._warStartDate;
      private set => this._warStartDate = value;
    }

    public CampaignTime PeaceDeclarationDate
    {
      get => this._peaceDeclarationDate;
      private set => this._peaceDeclarationDate = value;
    }

    public int Casualties1
    {
      get => this._casualties1;
      set => this._casualties1 = value;
    }

    public int Casualties2
    {
      get => this._casualties2;
      set => this._casualties2 = value;
    }

    public int GetCasualties(IFaction faction)
    {
      if (faction == this.Faction1)
        return this._casualties1;
      return faction != this.Faction2 ? 0 : this._casualties2;
    }

    public int SuccessfulSieges1
    {
      get => this._successfulSieges1;
      set => this._successfulSieges1 = value;
    }

    public int SuccessfulSieges2
    {
      get => this._successfulSieges2;
      set => this._successfulSieges2 = value;
    }

    public int GetSuccessfulSieges(IFaction faction)
    {
      if (faction == this.Faction1)
        return this._successfulSieges1;
      return faction != this.Faction2 ? 0 : this._successfulSieges2;
    }

    public int SuccessfulRaids1
    {
      get => this._successfulRaids1;
      set => this._successfulRaids1 = value;
    }

    public int SuccessfulRaids2
    {
      get => this._successfulRaids2;
      set => this._successfulRaids2 = value;
    }

    public int GetSuccessfulRaids(IFaction faction)
    {
      if (faction == this.Faction1)
        return this._successfulRaids1;
      return faction != this.Faction2 ? 0 : this._successfulRaids2;
    }

    public int TotalTributePaidby1
    {
      get => this._totalTributePaidby1;
      set => this._totalTributePaidby1 = value;
    }

    public int TotalTributePaidby2
    {
      get => this._totalTributePaidby2;
      set => this._totalTributePaidby2 = value;
    }

    public int GetTotalTributePaid(IFaction faction)
    {
      if (faction == this.Faction1)
        return this._totalTributePaidby1;
      return faction != this.Faction2 ? 0 : this._totalTributePaidby2;
    }

    private int DailyTributeFrom1To2
    {
      get => this._dailyTributeFrom1To2;
      set => this._dailyTributeFrom1To2 = value;
    }

    private int DailyTributeFrom2To1
    {
      get => -this._dailyTributeFrom1To2;
      set => this._dailyTributeFrom1To2 = -value;
    }

    public int GetDailyTributePaid(IFaction faction)
    {
      if (faction == this.Faction1)
        return this.DailyTributeFrom1To2;
      return faction != this.Faction2 ? 0 : this.DailyTributeFrom2To1;
    }

    public void SetDailyTributePaid(IFaction payer, int dailyTribute) => this.DailyTributeFrom1To2 = payer == this.Faction1 ? dailyTribute : (payer == this.Faction2 ? -dailyTribute : 0);

    public StanceLink(StanceType stanceType, IFaction faction1, IFaction faction2)
    {
      this._stanceType = stanceType;
      this.Faction1 = faction1;
      this.Faction2 = faction2;
    }
  }
}