namespace BOF.CampaignSystem.CampaignSystem
{
    public class DialogFlowContext
    {
        public readonly string Token;
        public readonly bool ByPlayer;
        public readonly DialogFlowContext Parent;

        public DialogFlowContext(string token, bool byPlayer, DialogFlowContext parent)
        {
            this.Token = token;
            this.ByPlayer = byPlayer;
            this.Parent = parent;
        }
    }
}