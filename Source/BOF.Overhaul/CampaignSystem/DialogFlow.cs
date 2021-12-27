using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BOF.CampaignSystem.CampaignSystem
{
  public class DialogFlow
  {
    internal readonly List<DialogFlowLine> Lines = new List<DialogFlowLine>();
    internal readonly int Priority;
    private string _currentToken;
    private DialogFlowLine _lastLine;
    private DialogFlowContext _curDialogFlowContext;

    private DialogFlow(string startingToken, int priority = 100)
    {
      this._currentToken = startingToken;
      this.Priority = priority;
    }

    private DialogFlow Line(
      TextObject text,
      bool byPlayer,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null,
      bool isRepeatable = false)
    {
      string token = BOFCampaign.Current.ConversationManager.CreateToken();
      this.AddLine(text, this._currentToken, token, byPlayer, speakerDelegate, listenerDelegate, isRepeatable);
      this._currentToken = token;
      return this;
    }

    public DialogFlow Variation(string text, params object[] propertiesAndWeights) => this.Variation(new TextObject(text), propertiesAndWeights);

    public DialogFlow Variation(TextObject text, params object[] propertiesAndWeights)
    {
      for (int index = 0; index < propertiesAndWeights.Length; index += 2)
      {
        string propertiesAndWeight = (string) propertiesAndWeights[index];
        int int32 = Convert.ToInt32(propertiesAndWeights[index + 1]);
        this.Lines[this.Lines.Count - 1].AddVariation(text, new List<GameTextManager.ChoiceTag>()
        {
          new GameTextManager.ChoiceTag(propertiesAndWeight, int32)
        });
      }
      return this;
    }

    public DialogFlow NpcLine(
      string npcText,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      return this.NpcLine(new TextObject(npcText), speakerDelegate, listenerDelegate);
    }

    public DialogFlow NpcLine(
      TextObject npcText,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      return this.Line(npcText, false, speakerDelegate, listenerDelegate);
    }

    public DialogFlow NpcLineWithVariation(
      string npcText,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      DialogFlow dialogFlow = this.Line(TextObject.Empty, false, speakerDelegate, listenerDelegate);
      this.Lines[this.Lines.Count - 1].AddVariation(new TextObject(npcText), new List<GameTextManager.ChoiceTag>()
      {
        new GameTextManager.ChoiceTag("DefaultTag", 1)
      });
      return dialogFlow;
    }

    public DialogFlow NpcLineWithVariation(
      TextObject npcText,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      DialogFlow dialogFlow = this.Line(TextObject.Empty, false, speakerDelegate, listenerDelegate);
      this.Lines[this.Lines.Count - 1].AddVariation(npcText, new List<GameTextManager.ChoiceTag>()
      {
        new GameTextManager.ChoiceTag("DefaultTag", 1)
      });
      return dialogFlow;
    }

    public DialogFlow PlayerLine(
      string playerText,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      return this.Line(new TextObject(playerText), true, listenerDelegate: listenerDelegate);
    }

    public DialogFlow PlayerLine(
      TextObject playerText,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      return this.Line(playerText, true, listenerDelegate: listenerDelegate);
    }

    private DialogFlow BeginOptions(bool byPlayer)
    {
      this._curDialogFlowContext = new DialogFlowContext(this._currentToken, byPlayer, this._curDialogFlowContext);
      return this;
    }

    public DialogFlow BeginPlayerOptions() => this.BeginOptions(true);

    public DialogFlow BeginNpcOptions() => this.BeginOptions(false);

    private DialogFlow Option(
      TextObject text,
      bool byPlayer,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null,
      bool isRepeatable = false,
      bool isSpecialOption = false)
    {
      string token = BOFCampaign.Current.ConversationManager.CreateToken();
      this.AddLine(text, this._curDialogFlowContext.Token, token, byPlayer, speakerDelegate, listenerDelegate, isRepeatable, isSpecialOption);
      this._currentToken = token;
      return this;
    }

    public DialogFlow PlayerOption(
      string text,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      return this.PlayerOption(new TextObject(text), listenerDelegate);
    }

    public DialogFlow PlayerOption(
      TextObject text,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.Option(text, true, listenerDelegate: listenerDelegate);
      return this;
    }

    public DialogFlow PlayerSpecialOption(
      TextObject text,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.Option(text, true, listenerDelegate: listenerDelegate, isSpecialOption: true);
      return this;
    }

    public DialogFlow PlayerRepeatableOption(
      TextObject text,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.Option(text, true, listenerDelegate: listenerDelegate, isRepeatable: true);
      return this;
    }

    public DialogFlow NpcOption(
      string text,
      ConversationSentence.OnConditionDelegate conditionDelegate,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.Option(new TextObject(text), false, speakerDelegate, listenerDelegate);
      this._lastLine.ConditionDelegate = conditionDelegate;
      return this;
    }

    public DialogFlow NpcOption(
      TextObject text,
      ConversationSentence.OnConditionDelegate conditionDelegate,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.Option(text, false, speakerDelegate, listenerDelegate);
      this._lastLine.ConditionDelegate = conditionDelegate;
      return this;
    }

    public DialogFlow NpcOptionWithVariation(
      string text,
      ConversationSentence.OnConditionDelegate conditionDelegate,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.NpcOptionWithVariation(new TextObject(text), conditionDelegate, speakerDelegate, listenerDelegate);
      return this;
    }

    public DialogFlow NpcOptionWithVariation(
      TextObject text,
      ConversationSentence.OnConditionDelegate conditionDelegate,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = null,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = null)
    {
      this.Option(TextObject.Empty, false, speakerDelegate, listenerDelegate);
      this._lastLine.AddVariation(text, new List<GameTextManager.ChoiceTag>()
      {
        new GameTextManager.ChoiceTag("DefaultTag", 1)
      });
      this._lastLine.ConditionDelegate = conditionDelegate;
      return this;
    }

    private DialogFlow EndOptions(bool byPlayer)
    {
      this._curDialogFlowContext = this._curDialogFlowContext.Parent;
      return this;
    }

    public DialogFlow EndPlayerOptions() => this.EndOptions(true);

    public DialogFlow EndNpcOptions() => this.EndOptions(false);

    public DialogFlow Condition(
      ConversationSentence.OnConditionDelegate conditionDelegate)
    {
      this._lastLine.ConditionDelegate = conditionDelegate;
      return this;
    }

    public DialogFlow ClickableCondition(
      ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate)
    {
      this._lastLine.ClickableConditionDelegate = clickableConditionDelegate;
      return this;
    }

    public DialogFlow Consequence(
      ConversationSentence.OnConsequenceDelegate consequenceDelegate)
    {
      this._lastLine.ConsequenceDelegate = consequenceDelegate;
      return this;
    }

    public static DialogFlow CreateDialogFlow(string inputToken = null, int priority = 100) => new DialogFlow(inputToken ?? BOFCampaign.Current.ConversationManager.CreateToken(), priority);

    private DialogFlowLine AddLine(
      TextObject text,
      string inputToken,
      string outputToken,
      bool byPlayer,
      ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate,
      ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate,
      bool isRepeatable,
      bool isSpecialOption = false)
    {
      DialogFlowLine dialogFlowLine = new DialogFlowLine();
      dialogFlowLine.Text = text;
      dialogFlowLine.InputToken = inputToken;
      dialogFlowLine.OutputToken = outputToken;
      dialogFlowLine.ByPlayer = byPlayer;
      dialogFlowLine.SpeakerDelegate = speakerDelegate;
      dialogFlowLine.ListenerDelegate = listenerDelegate;
      dialogFlowLine.IsRepeatable = isRepeatable;
      dialogFlowLine.IsSpecialOption = isSpecialOption;
      this.Lines.Add(dialogFlowLine);
      this._lastLine = dialogFlowLine;
      return dialogFlowLine;
    }

    public DialogFlow NpcDefaultOption(string text) => this.NpcOption(text, (ConversationSentence.OnConditionDelegate) null);

    public DialogFlow GotoDialogState(string input)
    {
      this._lastLine.OutputToken = input;
      this._currentToken = input;
      return this;
    }

    public DialogFlow GetOutputToken(out string oState)
    {
      oState = this._lastLine.OutputToken;
      return this;
    }

    public DialogFlow GoBackToDialogState(string iState)
    {
      this._currentToken = iState;
      return this;
    }

    public DialogFlow CloseDialog()
    {
      this.GotoDialogState("close_window");
      return this;
    }

    private ConversationSentence AddDialogLine(ConversationSentence dialogLine)
    {
      BOFCampaign.Current.ConversationManager.AddDialogLine(dialogLine);
      return dialogLine;
    }

    public ConversationSentence AddPlayerLine(
      string id,
      string inputToken,
      string outputToken,
      string text,
      ConversationSentence.OnConditionDelegate conditionDelegate,
      ConversationSentence.OnConsequenceDelegate consequenceDelegate,
      object relatedObject,
      int priority = 100,
      ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null,
      ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = null)
    {
      string idString = id;
      TextObject text1 = new TextObject(text);
      string inputToken1 = inputToken;
      string outputToken1 = outputToken;
      ConversationSentence.OnConditionDelegate conditionDelegate1 = conditionDelegate;
      ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate1 = clickableConditionDelegate;
      ConversationSentence.OnConsequenceDelegate consequenceDelegate1 = consequenceDelegate;
      int priority1 = priority;
      ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate1 = persuasionOptionDelegate;
      object relatedObject1 = relatedObject;
      ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate2 = persuasionOptionDelegate1;
      return this.AddDialogLine(new ConversationSentence(idString, text1, inputToken1, outputToken1, conditionDelegate1, clickableConditionDelegate1, consequenceDelegate1, 1U, priority1, relatedObject: relatedObject1, persuasionOptionDelegate: persuasionOptionDelegate2));
    }

    public ConversationSentence AddDialogLine(
      string id,
      string inputToken,
      string outputToken,
      string text,
      ConversationSentence.OnConditionDelegate conditionDelegate,
      ConversationSentence.OnConsequenceDelegate consequenceDelegate,
      object relatedObject,
      int priority = 100,
      ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
    {
      return this.AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken, conditionDelegate, clickableConditionDelegate, consequenceDelegate, priority: priority, relatedObject: relatedObject));
    }
  }
}
