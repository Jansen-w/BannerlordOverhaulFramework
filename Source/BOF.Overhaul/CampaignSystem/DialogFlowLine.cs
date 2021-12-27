using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BOF.CampaignSystem.CampaignSystem
{
    public class DialogFlowLine
    {
        public TextObject Text;
        public string InputToken;
        public string OutputToken;
        public bool ByPlayer;
        public ConversationSentence.OnConditionDelegate ConditionDelegate;
        public ConversationSentence.OnClickableConditionDelegate ClickableConditionDelegate;
        public ConversationSentence.OnConsequenceDelegate ConsequenceDelegate;
        public ConversationSentence.OnMultipleConversationConsequenceDelegate SpeakerDelegate;
        public ConversationSentence.OnMultipleConversationConsequenceDelegate ListenerDelegate;
        public bool IsRepeatable;
        public bool IsSpecialOption;

        public List<KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>> Variations { private set; get; }

        public bool HasVariation => this.Variations.Count > 0;

        public DialogFlowLine() => this.Variations = new List<KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>>();

        public void AddVariation(TextObject text, List<GameTextManager.ChoiceTag> list) => this.Variations.Add(new KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>>(text, list));
    }
}