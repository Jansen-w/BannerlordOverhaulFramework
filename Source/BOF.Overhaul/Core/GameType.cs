using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem.Core
{
  public abstract class GameType
  {
    private GameTypeLoadingStates _stepNo;

    public virtual bool SupportsSaving => false;

    public Game CurrentGame { get; set; }

    public MBObjectManager ObjectManager => this.CurrentGame.ObjectManager;

    public GameManagerBase GameManager => this.CurrentGame.GameManager;

    public virtual bool IsInventoryAccessibleAtMission => false;

    public virtual bool IsQuestScreenAccessibleAtMission => false;

    public virtual bool IsCharacterWindowAccessibleAtMission => false;

    public virtual bool IsPartyWindowAccessibleAtMission => false;

    public virtual bool IsKingdomWindowAccessibleAtMission => false;

    public virtual bool IsClanWindowAccessibleAtMission => false;

    public virtual bool IsEncyclopediaWindowAccessibleAtMission => false;

    public virtual bool IsBannerWindowAccessibleAtMission => false;

    public virtual bool IsDevelopment => false;

    public virtual bool IsCoreOnlyGameMode => false;

    public GameType() => this._stepNo = GameTypeLoadingStates.InitializeFirstStep;

    public abstract void OnStateChanged(GameState oldState);

    public abstract void BeforeRegisterTypes(MBObjectManager objectManager);

    public abstract void OnRegisterTypes(MBObjectManager objectManager);

    public abstract void OnInitialize();

    protected abstract void DoLoadingForGameType(
      GameTypeLoadingStates gameTypeLoadingState,
      out GameTypeLoadingStates nextState);

    public bool DoLoadingForGameType()
    {
      bool flag = false;
      GameTypeLoadingStates nextState = GameTypeLoadingStates.None;
      switch (this._stepNo)
      {
        case GameTypeLoadingStates.InitializeFirstStep:
          this.DoLoadingForGameType(GameTypeLoadingStates.InitializeFirstStep, out nextState);
          if (nextState == GameTypeLoadingStates.WaitSecondStep)
          {
            ++this._stepNo;
            break;
          }
          break;
        case GameTypeLoadingStates.WaitSecondStep:
          this.DoLoadingForGameType(GameTypeLoadingStates.WaitSecondStep, out nextState);
          if (nextState == GameTypeLoadingStates.LoadVisualsThirdState)
          {
            ++this._stepNo;
            break;
          }
          break;
        case GameTypeLoadingStates.LoadVisualsThirdState:
          this.DoLoadingForGameType(GameTypeLoadingStates.LoadVisualsThirdState, out nextState);
          if (nextState == GameTypeLoadingStates.PostInitializeFourthState)
          {
            ++this._stepNo;
            break;
          }
          break;
        case GameTypeLoadingStates.PostInitializeFourthState:
          this.DoLoadingForGameType(GameTypeLoadingStates.PostInitializeFourthState, out nextState);
          if (nextState == GameTypeLoadingStates.None)
          {
            ++this._stepNo;
            flag = true;
            break;
          }
          break;
      }
      return flag;
    }

    public abstract void OnDestroy();

    public virtual void OnMissionIsStarting(string missionName, MissionInitializerRecord rec)
    {
    }

    public virtual void InitializeParameters()
    {
    }
  }
}