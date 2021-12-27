using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BOF.CampaignSystem.CampaignSystem
{
    public class ConversationManager
    {
        private int _currentRepeatedDialogSetIndex;
        private int _currentRepeatIndex;
        private int _autoId;
        private int _autoToken;
        private int _numConversationSentencesCreated;
        private List<ConversationSentence> _sentences;
        private int _numberOfStateIndices;
        public int ActiveToken;
        private int _currentSentence;
        private TextObject _currentSentenceText;
        public List<Tuple<string, CharacterObject>> DetailedDebugLog = new List<Tuple<string, CharacterObject>>();
        public string CurrentFaceAnimationRecord;
        private object _lastSelectedDialogObject;
        private readonly List<List<object>> _dialogRepeatObjects = new List<List<object>>();
        private readonly List<TextObject> _dialogRepeatLines = new List<TextObject>();
        private bool _isActive;
        private bool _executeDoOptionContinue;
        public int LastSelectedButtonIndex;
        public string LastSelectedDialog;
        public ConversationAnimationManager ConversationAnimationManager;
        private IAgent _mainAgent;
        private IAgent _speakerAgent;
        private IAgent _listenerAgent;
        private bool _forceCameraToTurnToPlayer;
        private Dictionary<string, ConversationTag> _tags;
        private bool _sortSentenceIsDisabled;
        private Dictionary<string, int> stateMap;
        private List<IAgent> _conversationAgents = new List<IAgent>();
        public bool CurrentConversationIsFirst;
        private MobileParty _conversationParty;
        private IConversationStateHandler _handler;
        private static TaleWorlds.CampaignSystem.Conversation.Persuasion.Persuasion _persuasion;

        public int CreateConversationSentenceIndex()
        {
            int sentencesCreated = this._numConversationSentencesCreated;
            ++this._numConversationSentencesCreated;
            return sentencesCreated;
        }

        public string CurrentSentenceText
        {
            get
            {
                TextObject textObject = this._currentSentenceText;
                if (this.OneToOneConversationCharacter != null)
                    textObject = this.FindMatchingTextOrNull(textObject.GetID(), this.OneToOneConversationCharacter) ??
                                 this._currentSentenceText;
                return MBTextManager.DiscardAnimationTagsAndSoundPath(textObject.CopyTextObject().ToString());
            }
        }

        public string CurrentSentenceDebugInfo { get; private set; }

        private int DialogRepeatCount => this._dialogRepeatObjects.Count > 0
            ? this._dialogRepeatObjects[this._currentRepeatedDialogSetIndex].Count
            : 1;

        public bool IsConversationFlowActive => this._isActive;

        public ConversationManager()
        {
            this._sentences = new List<ConversationSentence>();
            this.stateMap = new Dictionary<string, int>();
            this.stateMap.Add("start", 0);
            this.stateMap.Add("event_triggered", 1);
            this.stateMap.Add("member_chat", 2);
            this.stateMap.Add("prisoner_chat", 3);
            this.stateMap.Add("close_window", 4);
            this._numberOfStateIndices = 5;
            this._isActive = false;
            this._executeDoOptionContinue = false;
            this._forceCameraToTurnToPlayer = false;
            this.InitializeTags();
            this.ConversationAnimationManager = new ConversationAnimationManager();
        }

        public List<ConversationSentenceOption> CurOptions { get; protected set; }

        public void StartNew(int startingToken, bool setActionsInstantly)
        {
            this.ActiveToken = startingToken;
            this._currentSentence = -1;
            this.ResetRepeatedDialogSystem();
            this._lastSelectedDialogObject = (object)null;
            Debug.Print("--------------- Conversation Start --------------- ", debugFilter: 4503599627370496UL);
            if (CampaignMission.Current != null)
            {
                foreach (IAgent conversationAgent in this.ConversationAgents)
                    CampaignMission.Current.OnConversationStart(conversationAgent, setActionsInstantly);
            }

            this.ProcessPartnerSentence();
        }

        private bool ProcessPartnerSentence()
        {
            List<ConversationSentenceOption> sentenceOptions = this.GetSentenceOptions(false, false);
            bool flag = false;
            if (sentenceOptions.Count > 0)
            {
                this.ProcessSentence(sentenceOptions[0]);
                flag = true;
            }

            this.Handler.OnConversationContinue();
            return flag;
        }

        public void ProcessSentence(
            ConversationSentenceOption conversationSentenceOption)
        {
            ConversationSentence sentence = this._sentences[conversationSentenceOption.SentenceNo];
            Debug.Print(conversationSentenceOption.DebugInfo, debugFilter: 4503599627370496UL);
            this.ActiveToken = sentence.OutputToken;
            this.UpdateSpeakerAndListenerAgents(sentence);
            if (CampaignMission.Current != null)
                CampaignMission.Current.OnProcessSentence();
            this._lastSelectedDialogObject = conversationSentenceOption.RepeatObject;
            this._currentSentence = conversationSentenceOption.SentenceNo;
            if (Game.Current == null)
                throw new MBNullParameterException("Game");
            this.UpdateCurrentSentenceText();
            sentence.RunConsequence(Game.Current);
            if (CampaignMission.Current != null)
            {
                string soundPath;
                string[] animationsAndSoundPath =
                    MBTextManager.GetConversationAnimationsAndSoundPath(this._currentSentenceText, out soundPath);
                CampaignMission.Current.OnConversationPlay(animationsAndSoundPath[0], animationsAndSoundPath[1],
                    animationsAndSoundPath[2], animationsAndSoundPath[3], soundPath);
            }

            if (0 <= this._currentSentence && this._currentSentence < this._sentences.Count)
                return;
            Debug.FailedAssert("CurrentSentence is not valid.",
                "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs",
                nameof(ProcessSentence), 382);
        }

        private void UpdateSpeakerAndListenerAgents(ConversationSentence sentence)
        {
            if (sentence.IsSpeaker != null)
            {
                if (sentence.IsSpeaker(this._mainAgent))
                {
                    this.SetSpeakerAgent(this._mainAgent);
                }
                else
                {
                    foreach (IAgent conversationAgent in this._conversationAgents)
                    {
                        if (sentence.IsSpeaker(conversationAgent))
                        {
                            this.SetSpeakerAgent(conversationAgent);
                            break;
                        }
                    }
                }
            }
            else
                this.SetSpeakerAgent(!sentence.IsPlayer ? this._conversationAgents[0] : this._mainAgent);

            if (sentence.IsListener != null)
            {
                if (sentence.IsListener(this._mainAgent))
                {
                    this.SetListenerAgent(this._mainAgent);
                }
                else
                {
                    foreach (IAgent conversationAgent in this._conversationAgents)
                    {
                        if (sentence.IsListener(conversationAgent))
                        {
                            this.SetListenerAgent(conversationAgent);
                            break;
                        }
                    }
                }
            }
            else
                this.SetListenerAgent(!sentence.IsPlayer ? this._mainAgent : this._conversationAgents[0]);
        }

        private void SetSpeakerAgent(IAgent agent)
        {
            if (this._speakerAgent == agent)
                return;
            this._speakerAgent = agent;
            if (this._speakerAgent == null || !(this._speakerAgent.Character is CharacterObject))
                return;
            StringHelpers.SetCharacterProperties("SPEAKER", agent.Character as CharacterObject);
        }

        private void SetListenerAgent(IAgent agent)
        {
            if (this._listenerAgent == agent)
                return;
            this._listenerAgent = agent;
            if (this._listenerAgent == null || !(this._listenerAgent.Character is CharacterObject))
                return;
            StringHelpers.SetCharacterProperties("LISTENER", agent.Character as CharacterObject);
        }

        public void UpdateCurrentSentenceText()
        {
            TextObject text;
            if (this._currentSentence >= 0)
            {
                ConversationSentence sentence = this._sentences[this._currentSentence];
                text = sentence.Text;
                this.CurrentSentenceDebugInfo = "ID: " + sentence.Id + " (Priority: " + sentence.Priority + ")";
            }
            else
            {
                if (BOFCampaign.Current == null)
                    throw new MBNullParameterException("Campaign");
                text = GameTexts.FindText("str_error_string");
            }

            this._currentSentenceText = text;
            this.CurrentSentenceDebugInfo = "";
        }

        public bool IsConversationEnded() => this.ActiveToken == 4;

        public void ClearCurrentOptions()
        {
            if (this.CurOptions == null)
                this.CurOptions = new List<ConversationSentenceOption>();
            this.CurOptions.Clear();
        }

        public void AddToCurrentOptions(
            TextObject text,
            string id,
            bool isClickable,
            TextObject hintText)
        {
            this.CurOptions.Add(new ConversationSentenceOption()
            {
                SentenceNo = 0,
                Text = text,
                Id = id,
                RepeatObject = (object)null,
                DebugInfo = (string)null,
                IsClickable = isClickable,
                HintText = hintText
            });
        }

        public void GetPlayerSentenceOptions()
        {
            this.CurOptions = this.GetSentenceOptions(true, true);
            if (!this.CurOptions.Any<ConversationSentenceOption>())
                return;
            ConversationSentenceOption conversationSentenceOption = this.CurOptions.First<ConversationSentenceOption>();
            foreach (ConversationSentenceOption curOption in this.CurOptions)
            {
                if (this._sentences[curOption.SentenceNo].IsListener != null)
                {
                    conversationSentenceOption = curOption;
                    break;
                }
            }

            this.UpdateSpeakerAndListenerAgents(this._sentences[conversationSentenceOption.SentenceNo]);
        }

        public int GetStateIndex(string str)
        {
            int num;
            if (this.stateMap.ContainsKey(str))
            {
                num = this.stateMap[str];
            }
            else
            {
                num = this._numberOfStateIndices;
                this.stateMap.Add(str, this._numberOfStateIndices++);
            }

            return num;
        }



        public void Build() => this.SortSentences();

        public void DisableSentenceSort() => this._sortSentenceIsDisabled = true;

        public void EnableSentenceSort()
        {
            this._sortSentenceIsDisabled = false;
            this.SortSentences();
        }

        private void SortSentences() => this._sentences = this._sentences
            .OrderByDescending<ConversationSentence, int>((Func<ConversationSentence, int>)(pair => pair.Priority))
            .ToList<ConversationSentence>();

        private void SortLastSentence()
        {
            int index1 = this._sentences.Count - 1;
            ConversationSentence sentence = this._sentences[index1];
            int priority = sentence.Priority;
            for (int index2 = index1 - 1; index2 >= 0 && this._sentences[index2].Priority < priority; --index2)
            {
                this._sentences[index2 + 1] = this._sentences[index2];
                index1 = index2;
            }

            this._sentences[index1] = sentence;
        }

        // private void LoadFromXML(XmlDocument doc, Type typeOfConversationCallbacks)
        // {
        //     Debug.Print("loading conversations.xml:");
        //     if (doc.ChildNodes.Count <= 1)
        //         throw new TWXmlLoadException("Incorrect XML document format.");
        //     if (doc.ChildNodes[1].Name != "base")
        //         throw new TWXmlLoadException("Incorrect XML document format.");
        //     if (doc.ChildNodes[1].ChildNodes[0].Name != "Conversations")
        //         throw new TWXmlLoadException("Incorrect XML document format.");
        //     int defaultPriority = 100;
        //     XmlAttribute attribute = doc.ChildNodes[1].Attributes["default_priority"];
        //     if (attribute != null)
        //         defaultPriority = int.Parse(attribute.InnerText);
        //     XmlNode node = (XmlNode)null;
        //     if (doc.ChildNodes[1].ChildNodes[0].Name == "Conversations")
        //         node = doc.ChildNodes[1].ChildNodes[0].ChildNodes[0];
        //     int count = this._sentences.Count;
        //     for (; node != null; node = node.NextSibling)
        //     {
        //         if (node.Name == "conversation")
        //         {
        //             ConversationSentence conversationSentence = new ConversationSentence(count);
        //             conversationSentence.Deserialize(node, typeOfConversationCallbacks, this, defaultPriority);
        //             this._sentences.Add(conversationSentence);
        //             ++count;
        //         }
        //     }
        //
        //     if (this._sortSentenceIsDisabled)
        //         return;
        //     this.SortSentences();
        // }

        // private XmlDocument LoadXmlFile(string path)
        // {
        //     Debug.Print("opening " + path);
        //     XmlDocument xmlDocument = new XmlDocument();
        //     StreamReader streamReader = new StreamReader(path);
        //     xmlDocument.LoadXml(streamReader.ReadToEnd());
        //     streamReader.Close();
        //     return xmlDocument;
        // }

        private List<ConversationSentenceOption> GetSentenceOptions(
            bool onlyPlayer,
            bool processAfterOneOption)
        {
            List<ConversationSentenceOption> conversationSentenceOptionList = new List<ConversationSentenceOption>();
            for (int index1 = 0; index1 < this._sentences.Count; ++index1)
            {
                if (this.GetSentenceMatch(index1, onlyPlayer))
                {
                    ConversationSentence sentence = this._sentences[index1];
                    int num = 1;
                    this._dialogRepeatLines.Clear();
                    this._currentRepeatIndex = 0;
                    if (sentence.IsRepeatable)
                        num = this.DialogRepeatCount;
                    for (int index2 = 0; index2 < num; ++index2)
                    {
                        this._dialogRepeatLines.Add(sentence.Text.CopyTextObject());
                        if (sentence.RunCondition())
                        {
                            sentence.IsClickable = sentence.RunClickableCondition();
                            if (sentence.IsWithVariation)
                                GameTexts.SetVariable("VARIATION_TEXT_TAGGED_LINE",
                                    this.FindMatchingTextOrNull(sentence.Id, this.OneToOneConversationCharacter));
                            string str = "";
                            ConversationSentenceOption conversationSentenceOption = new ConversationSentenceOption()
                            {
                                SentenceNo = index1,
                                Text = this.GetCurrentDialogLine(),
                                Id = sentence.Id,
                                RepeatObject = this.GetCurrentProcessedRepeatObject(),
                                DebugInfo = str,
                                IsClickable = sentence.IsClickable,
                                HasPersuasion = sentence.HasPersuasion,
                                SkillName = sentence.SkillName,
                                TraitName = sentence.TraitName,
                                IsSpecial = sentence.IsSpecial,
                                HintText = sentence.HintText,
                                PersuationOptionArgs = sentence.PersuationOptionArgs
                            };
                            conversationSentenceOptionList.Add(conversationSentenceOption);
                            if (sentence.IsRepeatable)
                                ++this._currentRepeatIndex;
                            if (!processAfterOneOption)
                                return conversationSentenceOptionList;
                        }
                    }
                }
            }

            return conversationSentenceOptionList;
        }

        private bool GetSentenceMatch(int sentenceIndex, bool onlyPlayer)
        {
            if (0 > sentenceIndex || sentenceIndex >= this._sentences.Count)
                throw new MBOutOfRangeException("Sentence index is not valid.");
            bool flag = this._sentences[sentenceIndex].InputToken != this.ActiveToken;
            if (!flag & onlyPlayer)
                flag = !this._sentences[sentenceIndex].IsPlayer;
            return !flag;
        }

        public object GetCurrentProcessedRepeatObject() => !this._dialogRepeatObjects.Any<List<object>>()
            ? (object)null
            : this._dialogRepeatObjects[this._currentRepeatedDialogSetIndex][this._currentRepeatIndex];

        public TextObject GetCurrentDialogLine() => this._dialogRepeatLines.Count <= this._currentRepeatIndex
            ? (TextObject)null
            : this._dialogRepeatLines[this._currentRepeatIndex];

        public object GetSelectedRepeatObject() => this._lastSelectedDialogObject;

        public void SetDialogRepeatCount(
            IReadOnlyList<object> dialogRepeatObjects,
            int maxRepeatedDialogsInConversation)
        {
            this._dialogRepeatObjects.Clear();
            List<object> objectList = (List<object>)null;
            for (int index = 0; index < dialogRepeatObjects.Count; ++index)
            {
                object dialogRepeatObject = dialogRepeatObjects[index];
                if (index % maxRepeatedDialogsInConversation == 0)
                {
                    objectList = new List<object>(maxRepeatedDialogsInConversation);
                    this._dialogRepeatObjects.Add(objectList);
                }

                objectList.Add(dialogRepeatObject);
            }

            this._currentRepeatedDialogSetIndex = 0;
            this._currentRepeatIndex = 0;
        }

        public static void DialogRepeatContinueListing()
        {
            ConversationManager conversationManager = BOFCampaign.Current?.ConversationManager;
            if (conversationManager == null)
                return;
            ++conversationManager._currentRepeatedDialogSetIndex;
            if (conversationManager._currentRepeatedDialogSetIndex >= conversationManager._dialogRepeatObjects.Count)
                conversationManager._currentRepeatedDialogSetIndex = 0;
            conversationManager._currentRepeatIndex = 0;
        }

        public static bool IsThereMultipleRepeatablePages()
        {
            BOFCampaign current = BOFCampaign.Current;
            if (current == null)
                return false;
            int? count = current.ConversationManager?._dialogRepeatObjects.Count;
            int num = 1;
            return count.GetValueOrDefault() > num & count.HasValue;
        }

        private void ResetRepeatedDialogSystem()
        {
            this._currentRepeatedDialogSetIndex = 0;
            this._currentRepeatIndex = 0;
            this._dialogRepeatObjects.Clear();
            this._dialogRepeatLines.Clear();
        }

        public ConversationSentence AddDialogLine(ConversationSentence dialogLine)
        {
            this._sentences.Add(dialogLine);
            if (!this._sortSentenceIsDisabled)
                this.SortLastSentence();
            return dialogLine;
        }

        public void AddDialogFlow(DialogFlow dialogFlow, object relatedObject = null)
        {
            foreach (DialogFlowLine line in dialogFlow.Lines)
            {
                string id = this.CreateId();
                uint num1 = (uint)((line.ByPlayer ? 1 : 0) | (line.IsRepeatable ? 2 : 0) |
                                   (line.IsSpecialOption ? 4 : 0));
                string idString = id;
                TextObject text = line.HasVariation
                    ? new TextObject("{=7AyjDt96}{VARIATION_TEXT_TAGGED_LINE}")
                    : line.Text;
                string inputToken = line.InputToken;
                string outputToken = line.OutputToken;
                ConversationSentence.OnConditionDelegate conditionDelegate1 = line.ConditionDelegate;
                ConversationSentence.OnClickableConditionDelegate conditionDelegate2 = line.ClickableConditionDelegate;
                ConversationSentence.OnConsequenceDelegate consequenceDelegate = line.ConsequenceDelegate;
                int num2 = (int)num1;
                object obj = relatedObject;
                int priority = dialogFlow.Priority;
                object relatedObject1 = obj;
                int num3 = line.HasVariation ? 1 : 0;
                ConversationSentence.OnMultipleConversationConsequenceDelegate speakerDelegate = line.SpeakerDelegate;
                ConversationSentence.OnMultipleConversationConsequenceDelegate listenerDelegate = line.ListenerDelegate;
                this.AddDialogLine(new ConversationSentence(idString, text, inputToken, outputToken, conditionDelegate1,
                    conditionDelegate2, consequenceDelegate, (uint)num2, priority, relatedObject: relatedObject1,
                    withVariation: (num3 != 0), speakerDelegate: speakerDelegate, listenerDelegate: listenerDelegate));
                GameText gameText = Game.Current.GameTextManager.AddGameText(id);
                foreach (KeyValuePair<TextObject, List<GameTextManager.ChoiceTag>> variation in line.Variations)
                    gameText.AddVariationWithId("", variation.Key, variation.Value);
            }
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
            return this.AddDialogLine(new ConversationSentence(id, text, inputToken, outputToken, conditionDelegate,
                clickableConditionDelegate, consequenceDelegate, priority: priority, agentIndex: agentIndex,
                nextAgentIndex: nextAgentIndex));
        }

        public string CreateToken()
        {
            string str = string.Format("atk:{0}", (object)this._autoToken);
            ++this._autoToken;
            return str;
        }

        private string CreateId()
        {
            string str = string.Format("adg:{0}", (object)this._autoId);
            ++this._autoId;
            return str;
        }

        public void SetupGameStringsForConversation() =>
            StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);

        public void OnConsequence(ConversationSentence sentence)
        {
            Action<ConversationSentence> consequenceRunned = this.ConsequenceRunned;
            if (consequenceRunned == null)
                return;
            consequenceRunned(sentence);
        }

        public void OnCondition(ConversationSentence sentence)
        {
            Action<ConversationSentence> conditionRunned = this.ConditionRunned;
            if (conditionRunned == null)
                return;
            conditionRunned(sentence);
        }

        public void OnClickableCondition(ConversationSentence sentence)
        {
            Action<ConversationSentence> clickableConditionRunned = this.ClickableConditionRunned;
            if (clickableConditionRunned == null)
                return;
            clickableConditionRunned(sentence);
        }

        public event Action<ConversationSentence> ConsequenceRunned;

        public event Action<ConversationSentence> ConditionRunned;

        public event Action<ConversationSentence> ClickableConditionRunned;

        public List<IAgent> ConversationAgents => this._conversationAgents;

        public IAgent OneToOneConversationAgent =>
            this._conversationAgents.IsEmpty<IAgent>() || this._conversationAgents.Count > 1
                ? (IAgent)null
                : this._conversationAgents[0];

        public IAgent SpeakerAgent => this._conversationAgents != null ? this._speakerAgent : (IAgent)null;

        public IAgent ListenerAgent => this._conversationAgents != null ? this._listenerAgent : (IAgent)null;

        public bool ForceCameraToTurnToPlayer
        {
            get => this._forceCameraToTurnToPlayer;
            set => this._forceCameraToTurnToPlayer = value;
        }

        public bool IsConversationInProgress { get; private set; }

        public Hero OneToOneConversationHero => this.OneToOneConversationCharacter != null
            ? this.OneToOneConversationCharacter.HeroObject
            : (Hero)null;

        public CharacterObject OneToOneConversationCharacter => this.OneToOneConversationAgent != null
            ? (CharacterObject)this.OneToOneConversationAgent.Character
            : (CharacterObject)null;

        public IEnumerable<Hero> ConversationHeroes
        {
            get
            {
                foreach (IAgent conversationAgent in this._conversationAgents)
                    yield return ((CharacterObject)conversationAgent.Character).HeroObject;
            }
        }

        public IEnumerable<CharacterObject> ConversationCharacters
        {
            get
            {
                List<CharacterObject> characterObjectList = new List<CharacterObject>();
                foreach (IAgent conversationAgent in this._conversationAgents)
                    yield return (CharacterObject)conversationAgent.Character;
            }
        }

        public bool IsAgentInConversation(IAgent agent) => this._conversationAgents.Contains(agent);

        public MobileParty ConversationParty => this._conversationParty;

        public bool NeedsToActivateForMapConversation { get; private set; }

        public event Action ConversationSetup;

        public event Action ConversationBegin;

        public event Action ConversationEnd;

        public event Action ConversationEndOneShot;

        public event Action ConversationContinued;

        private void SetupConversation()
        {
            this.IsConversationInProgress = true;
            this.Handler?.OnConversationInstall();
        }

        public void BeginConversation()
        {
            this.IsConversationInProgress = true;
            if (this.ConversationSetup != null)
                this.ConversationSetup();
            if (this.ConversationBegin != null)
                this.ConversationBegin();
            this.NeedsToActivateForMapConversation = false;
        }

        public void EndConversation()
        {
            Debug.Print("--------------- Conversation End --------------- ", debugFilter: 4503599627370496UL);
            if (CampaignMission.Current != null)
            {
                foreach (IAgent conversationAgent in this.ConversationAgents)
                    CampaignMission.Current.OnConversationEnd(conversationAgent);
            }

            foreach (Hero conversationHero in this.ConversationHeroes)
            {
                if (conversationHero != null)
                {
                    conversationHero.HasMet = true;
                    conversationHero.LastMeetingTimeWithPlayer = CampaignTime.Now;
                }
            }

            this._conversationParty = (TaleWorlds.CampaignSystem.MobileParty)null;
            if (this.ConversationEndOneShot != null)
            {
                this.ConversationEndOneShot();
                this.ConversationEndOneShot = (Action)null;
            }

            if (this.ConversationEnd != null)
                this.ConversationEnd();
            this.IsConversationInProgress = false;
            foreach (IAgent conversationAgent in this._conversationAgents)
                conversationAgent.SetAsConversationAgent(false);
            BOFCampaign.Current.CurrentConversationContext = ConversationContext.Default;
            CampaignEventDispatcher.Instance.OnConversationEnded(this.OneToOneConversationCharacter);
            if (ConversationManager.GetPersuasionIsActive())
                ConversationManager.EndPersuasion();
            this._conversationAgents.Clear();
            this._speakerAgent = (IAgent)null;
            this._listenerAgent = (IAgent)null;
            this._mainAgent = (IAgent)null;
            if (this.IsConversationFlowActive)
                this.OnConversationDeactivate();
            this.Handler?.OnConversationUninstall();
        }

        public void DoOption(int optionIndex)
        {
            this.LastSelectedButtonIndex = optionIndex;
            this.ProcessSentence(this.CurOptions[optionIndex]);
            if (this._isActive)
                this.DoOptionContinue();
            else
                this._executeDoOptionContinue = true;
        }

        public void DoOption(string optionID)
        {
            this.LastSelectedDialog = optionID;
            int count = BOFCampaign.Current.ConversationManager.CurOptions.Count;
            for (int index = 0; index < count; ++index)
            {
                if (this.CurOptions[index].Id == optionID)
                    this.ProcessSentence(this.CurOptions[index]);
            }

            if (this._isActive)
                this.DoOptionContinue();
            else
                this._executeDoOptionContinue = true;
        }

        public void DoConversationContinuedCallback()
        {
            if (this.ConversationContinued == null)
                return;
            this.ConversationContinued();
        }

        public void DoOptionContinue()
        {
            if (this.IsConversationEnded() && this._sentences[this._currentSentence].IsPlayer)
            {
                this.EndConversation();
            }
            else
            {
                this.ProcessPartnerSentence();
                this.DoConversationContinuedCallback();
            }
        }

        public void ContinueConversation()
        {
            if (this.CurOptions.Count > 1)
                return;
            if (this.IsConversationEnded())
                this.EndConversation();
            else if (!this.ProcessPartnerSentence() && this.ListenerAgent.Character == Hero.MainHero.CharacterObject)
            {
                this.EndConversation();
            }
            else
            {
                this.DoConversationContinuedCallback();
                if (CampaignMission.Current == null)
                    return;
                CampaignMission.Current.OnConversationContinue();
            }
        }

        public void SetupAndStartMissionConversation(
            IAgent agent,
            IAgent mainAgent,
            bool setActionsInstantly)
        {
            CampaignEvents.SetupPreConversation();
            this.SetupConversation();
            this._mainAgent = mainAgent;
            this._conversationAgents = new List<IAgent>();
            this.AddConversationAgent(agent);
            this._conversationParty = (TaleWorlds.CampaignSystem.MobileParty)null;
            this.StartNew(0, setActionsInstantly);
            if (!this.IsConversationFlowActive)
                this.OnConversationActivate();
            this.BeginConversation();
        }

        public void SetupAndStartMissionConversationWithMultipleAgents(
            IEnumerable<IAgent> agents,
            IAgent mainAgent)
        {
            this.SetupConversation();
            this._mainAgent = mainAgent;
            this._conversationAgents = new List<IAgent>();
            this.AddConversationAgents(agents, true);
            this._conversationParty = (TaleWorlds.CampaignSystem.MobileParty)null;
            this.StartNew(0, true);
            if (!this.IsConversationFlowActive)
                this.OnConversationActivate();
            this.BeginConversation();
        }

        public void SetupAndStartMapConversation(TaleWorlds.CampaignSystem.MobileParty party, IAgent agent,
            IAgent mainAgent)
        {
            this._conversationParty = party;
            CampaignEvents.SetupPreConversation();
            this._mainAgent = mainAgent;
            this._conversationAgents = new List<IAgent>();
            this.AddConversationAgent(agent);
            this.SetupConversation();
            this.StartNew(0, true);
            this.NeedsToActivateForMapConversation = true;
            if (this.IsConversationFlowActive)
                return;
            this.OnConversationActivate();
        }

        public void AddConversationAgents(IEnumerable<IAgent> agents, bool setActionsInstantly)
        {
            foreach (IAgent agent in agents)
            {
                if (agent.IsActive() && !this._conversationAgents.Contains(agent))
                {
                    this.AddConversationAgent(agent);
                    CampaignMission.Current.OnConversationStart(agent, setActionsInstantly);
                }
            }
        }

        private void AddConversationAgent(IAgent agent)
        {
            this._conversationAgents.Add(agent);
            agent.SetAsConversationAgent(true);
        }

        public bool IsConversationAgent(IAgent agent) =>
            this._conversationAgents != null && this._conversationAgents.Contains(agent);

        public void RemoveRelatedLines(object o) =>
            this._sentences.RemoveAll((Predicate<ConversationSentence>)(s => s.RelatedObject == o));

        public IConversationStateHandler Handler
        {
            get => this._handler;
            set => this._handler = value;
        }

        public void OnConversationDeactivate()
        {
            this._isActive = false;
            this.Handler?.OnConversationDeactivate();
        }

        public void OnConversationActivate()
        {
            this._isActive = true;
            if (this._executeDoOptionContinue)
            {
                this._executeDoOptionContinue = false;
                this.DoOptionContinue();
            }

            this.Handler.OnConversationActivate();
        }

        public TextObject FindMatchingTextOrNull(string id, CharacterObject character)
        {
            float num = (float)int.MinValue;
            TextObject textObject = (TextObject)null;
            GameText gameText = Game.Current.GameTextManager.GetGameText(id);
            if (gameText != null)
            {
                foreach (GameText.GameTextVariation variation in gameText.Variations)
                {
                    float matchingScore = this.FindMatchingScore(character, variation.Tags);
                    if ((double)matchingScore > (double)num)
                    {
                        textObject = variation.Text;
                        num = matchingScore;
                    }
                }
            }

            return textObject;
        }

        private float FindMatchingScore(
            CharacterObject character,
            GameTextManager.ChoiceTag[] choiceTags)
        {
            float num = 0.0f;
            foreach (GameTextManager.ChoiceTag choiceTag in choiceTags)
            {
                if (choiceTag.TagName != "DefaultTag")
                {
                    if (this.IsTagApplicable(choiceTag.TagName, character) == choiceTag.IsTagReversed)
                        return (float)int.MinValue;
                    uint weight = choiceTag.Weight;
                    num += (float)weight;
                }
            }

            return num;
        }

        private void InitializeTags()
        {
            this._tags = new Dictionary<string, ConversationTag>();
            foreach (Type type in this.GetType().Assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(ConversationTag)))
                {
                    ConversationTag instance = Activator.CreateInstance(type) as ConversationTag;
                    this._tags.Add(instance.StringId, instance);
                }
            }
        }

        public IEnumerable<string> GetApplicableTagNames(CharacterObject character)
        {
            foreach (ConversationTag conversationTag in this._tags.Values)
            {
                if (conversationTag.IsApplicableTo(character))
                    yield return conversationTag.StringId;
            }
        }

        public bool IsTagApplicable(string tagId, CharacterObject character)
        {
            ConversationTag conversationTag;
            if (this._tags.TryGetValue(tagId, out conversationTag))
                return conversationTag.IsApplicableTo(character);
            Debug.FailedAssert("asking for a nonexistent tag",
                "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationManager.cs",
                nameof(IsTagApplicable), 1495);
            return false;
        }

        public static void StartPersuasion(
            float goalValue,
            float successValue,
            float failValue,
            float criticalSuccessValue,
            float criticalFailValue,
            float initialProgress = -1f,
            PersuasionDifficulty difficulty = PersuasionDifficulty.Medium)
        {
            ConversationManager._persuasion = new TaleWorlds.CampaignSystem.Conversation.Persuasion.Persuasion(
                goalValue, successValue, failValue, criticalSuccessValue, criticalFailValue, initialProgress,
                difficulty);
        }

        public static void EndPersuasion() => ConversationManager._persuasion =
            (TaleWorlds.CampaignSystem.Conversation.Persuasion.Persuasion)null;

        public static void PersuasionCommitProgress(PersuasionOptionArgs persuasionOptionArgs) =>
            ConversationManager._persuasion.CommitProgress(persuasionOptionArgs);

        public static void Clear() => ConversationManager._persuasion =
            (TaleWorlds.CampaignSystem.Conversation.Persuasion.Persuasion)null;

        public void GetPersuasionChanceValues(
            out float successValue,
            out float critSuccessValue,
            out float critFailValue)
        {
            successValue = ConversationManager._persuasion.SuccessValue;
            critSuccessValue = ConversationManager._persuasion.CriticalSuccessValue;
            critFailValue = ConversationManager._persuasion.CriticalFailValue;
        }

        public static bool GetPersuasionIsActive() => ConversationManager._persuasion != null;

        public static bool GetPersuasionProgressSatisfied() => (double)ConversationManager._persuasion.Progress >=
                                                               (double)ConversationManager._persuasion.GoalValue;

        public static bool GetPersuasionIsFailure() => (double)ConversationManager._persuasion.Progress < 0.0;

        public static float GetPersuasionProgress() => ConversationManager._persuasion.Progress;

        public static float GetPersuasionGoalValue() => ConversationManager._persuasion.GoalValue;

        public static IEnumerable<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> GetPersuasionChosenOptions() =>
            ConversationManager._persuasion.GetChosenOptions();

        public void GetPersuasionChances(
            ConversationSentenceOption conversationSentenceOption,
            out float successChance,
            out float critSuccessChance,
            out float critFailChance,
            out float failChance)
        {
            ConversationSentence sentence = this._sentences[conversationSentenceOption.SentenceNo];
            if (conversationSentenceOption.HasPersuasion)
            {
                BOFCampaign.Current.Models.PersuasionModel.GetChances(
                    sentence.PersuationOptionArgs, out successChance, out critSuccessChance, out critFailChance,
                    out failChance, ConversationManager._persuasion.DifficultyMultiplier);
            }
            else
            {
                successChance = 0.0f;
                critSuccessChance = 0.0f;
                critFailChance = 0.0f;
                failChance = 0.0f;
            }
        }

        public class TaggedString
        {
            public TextObject Text;
            public List<GameTextManager.ChoiceTag> ChoiceTags = new List<GameTextManager.ChoiceTag>();
            public int FacialAnimation;
        }
    }
}