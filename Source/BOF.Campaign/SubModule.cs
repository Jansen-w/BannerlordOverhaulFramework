using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace BOF.Campaign
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            // Add button to start our custom campaign to main menu
            var bofCampaignOption = new InitialStateOption(
                "CreateCampaign",
                new TextObject("Start a New Campaign"),
                -100,
                () => { MBGameManager.StartNewGame(new BOFGameManager()); },
                () => (false, new TextObject("This shouldn't be disabled."))
                );
            
            Module.CurrentModule.AddInitialStateOption(bofCampaignOption);
        }

    }
}