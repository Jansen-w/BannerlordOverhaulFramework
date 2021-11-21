using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BOF.Campaign
{
    public class CampaignGameStarter : IGameStarter
    {
        private readonly GameMenuManager _gameMenuManager;
        private readonly GameTextManager _gameTextManager;
        private readonly ConversationManager _conversationManager;
        private readonly List<CampaignBehaviorBase> _campaignBehaviors = new List<CampaignBehaviorBase>();
        private readonly List<GameModel> _models = new List<GameModel>();

        public ICollection<CampaignBehaviorBase> CampaignBehaviors => _campaignBehaviors;
        public IEnumerable<GameModel> Models => _models;

        public CampaignGameStarter(
            GameMenuManager gameMenuManager,
            ConversationManager conversationManager,
            GameTextManager gameTextManager)
        {
            _conversationManager = conversationManager;
            _gameTextManager = gameTextManager;
            _gameMenuManager = gameMenuManager;
        }

        public void UnregisterNonReadyObjects() => Game.Current.ObjectManager.UnregisterNonReadyObjects();

        public void AddBehavior(CampaignBehaviorBase campaignBehavior)
        {
            if (campaignBehavior == null)
                return;
            _campaignBehaviors.Add(campaignBehavior);
        }

        public void RemoveBehaviors<T>() where T : CampaignBehaviorBase
        {
            for (int index = _campaignBehaviors.Count - 1; index >= 0; --index)
            {
                if (_campaignBehaviors[index] is T)
                    _campaignBehaviors.RemoveAt(index);
            }
        }

        public bool RemoveBehavior<T>(T behavior) where T : CampaignBehaviorBase => _campaignBehaviors.Remove(behavior);

        public void AddModel(GameModel model)
        {
            if (model == null)
                return;
            if (_models.FindIndex(x => x.GetType() == model.GetType()) >= 0)
                throw new ArgumentException();
            _models.Add(model);
        }

        public void LoadGameTexts(string xmlPath) => _gameTextManager.LoadGameTexts(xmlPath);
        public void LoadGameMenus(Type typeOfGameMenusCallbacks, string xmlPath) => _gameMenuManager.AddNewGameMenus(typeOfGameMenusCallbacks, xmlPath);

        public void AddGameMenu(
            string menuId,
            string menuText,
            OnInitDelegate initDelegate,
            GameOverlays.MenuOverlayType overlay = GameOverlays.MenuOverlayType.None,
            GameMenu.MenuFlags menuFlags = GameMenu.MenuFlags.none,
            object relatedObject = null)
        {
            AddGameMenu(new GameMenu(menuId, menuText, initDelegate, overlay, menuFlags, relatedObject));
        }

        public void AddWaitGameMenu(
            string idString,
            string text,
            OnInitDelegate initDelegate,
            OnConditionDelegate condition,
            OnConsequenceDelegate consequence,
            OnTickDelegate tick,
            GameMenu.MenuAndOptionType type,
            GameOverlays.MenuOverlayType overlay = GameOverlays.MenuOverlayType.None,
            float targetWaitHours = 0.0f,
            GameMenu.MenuFlags flags = GameMenu.MenuFlags.none,
            object relatedObject = null)
        {
            AddGameMenu(new GameMenu(idString, text, initDelegate, condition, consequence, tick, type, overlay,
                targetWaitHours, flags, relatedObject));
        }

        public void AddGameMenuOption(
            string menuId,
            string optionId,
            string optionText,
            GameMenuOption.OnConditionDelegate condition,
            GameMenuOption.OnConsequenceDelegate consequence,
            bool isLeave = false,
            int index = -1,
            bool isRepeatable = false)
        {
            (_gameMenuManager.GetGameMenu(menuId) ?? throw new KeyNotFoundException()).AddOption(optionId,
                new TextObject(optionText), condition, consequence, index, isLeave, isRepeatable);
        }

        private GameMenu AddGameMenu(GameMenu gameMenu)
        {
            _gameMenuManager.AddGameMenu(gameMenu);
            return gameMenu;
        }

        public void LoadConversations(Type typeOfConversationCallbacks, string xmlPath) =>
            _conversationManager.LoadConversations(typeOfConversationCallbacks, xmlPath);

        private ConversationSentence AddDialogLine(ConversationSentence dialogLine)
        {
            _conversationManager.AddDialogLine(dialogLine);
            return dialogLine;
        }

        public ConversationSentence AddPlayerLine(
            string id,
            string inputToken,
            string outputToken,
            string text,
            ConversationSentence.OnConditionDelegate conditionDelegate,
            ConversationSentence.OnConsequenceDelegate consequenceDelegate,
            int priority = 100,
            ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null,
            ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = null)
        {
            return AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken,
                conditionDelegate, clickableConditionDelegate, consequenceDelegate, 1U, priority,
                persuasionOptionDelegate: persuasionOptionDelegate));
        }

        public ConversationSentence AddRepeatablePlayerLine(
            string id,
            string inputToken,
            string outputToken,
            string text,
            ConversationSentence.OnConditionDelegate conditionDelegate,
            ConversationSentence.OnConsequenceDelegate consequenceDelegate,
            int priority = 100,
            ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
        {
            return AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken,
                conditionDelegate, clickableConditionDelegate, consequenceDelegate, 3U, priority));
        }

        public ConversationSentence AddDialogLineWithVariation(
            string id,
            string inputToken,
            string outputToken,
            ConversationSentence.OnConditionDelegate conditionDelegate,
            ConversationSentence.OnConsequenceDelegate consequenceDelegate,
            int priority = 100,
            string idleActionId = "",
            string idleFaceAnimId = "",
            string reactionId = "",
            string reactionFaceAnimId = "",
            ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
        {
            return AddDialogLine(new ConversationSentence(id,
                new TextObject("{=7AyjDt96}{VARIATION_TEXT_TAGGED_LINE}"), inputToken, outputToken, conditionDelegate,
                clickableConditionDelegate, consequenceDelegate, priority: priority, withVariation: true));
        }

        public ConversationSentence AddDialogLine(
            string id,
            string inputToken,
            string outputToken,
            string text,
            ConversationSentence.OnConditionDelegate conditionDelegate,
            ConversationSentence.OnConsequenceDelegate consequenceDelegate,
            int priority = 100,
            ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
        {
            return AddDialogLine(new ConversationSentence(id, new TextObject(text), inputToken, outputToken,
                conditionDelegate, clickableConditionDelegate, consequenceDelegate, priority: priority));
        }

        public ConversationSentence AddDialogLineMultiAgent(
            string id,
            string inputToken,
            string outputToken,
            TextObject text,
            ConversationSentence.OnConditionDelegate conditionDelegate,
            ConversationSentence.OnConsequenceDelegate consequenceDelegate,
            int agentIndex,
            int nextAgentIndex,
            int priority = 100,
            ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = null)
        {
            return AddDialogLine(new ConversationSentence(id, text, inputToken, outputToken, conditionDelegate,
                clickableConditionDelegate, consequenceDelegate, priority: priority, agentIndex: agentIndex,
                nextAgentIndex: nextAgentIndex));
        }
    }
}