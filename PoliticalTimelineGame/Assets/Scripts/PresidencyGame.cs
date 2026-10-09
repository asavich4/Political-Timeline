using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PoliticalTimeline
{
    [ExecuteAlways]
    public class PresidencyGame : MonoBehaviour
    {
        public CampaignDefinition campaign;
        public TMP_Text briefing, choiceLabel, helpText, helpTitle;
        public Button[] powerButtons;
        public TMP_Text monthLabel;
        public Image background;
        public Image[] powerIcons;
        public NationalPanels nationalPanels;
        public FrontMenu frontMenu;
        public PartyAudio partyAudio;
        public bool HasUnreadOutcome => transitionPhase==1 || transitionPhase==2 || transitionPhase==4;
        public CampaignState State => state;
        public CanvasGroup choiceOverlay;
        public Image commitIndicator;
        public TMP_Text[] supportChanges;
        public Image[] supportFills;
        public Image portrait;
        public RectTransform cardTransform;
        public Button restartButton, helpButton, closeHelpButton;
        public GameObject helpPanel;
        CampaignState state;
        Vector2 home;
        bool dragging, initialized;
        int pointerId, keyboardDirection;
        Vector2 dragStart;
        float inputReady, feedbackUntil;
        AdvisorArt flatArt;
        TMP_Text advisorLabel, resultLabel;
        [SerializeField] PowerIcon[] flatPowerIcons;
        public DecisionCard editorPreviewCard;
        [HideInInspector] public int editablePresentationVersion;
        string voterFeedback;
        public bool IsTransitioning => transitionPhase!=0;
        public bool AwaitingAcknowledgement => transitionPhase==4;
        int transitionPhase;
        float transitionTime;
        Vector2 departurePosition;
        float departureAngle, departureDirection;
        readonly float[] previousSupport=new float[4];
        [SerializeField] GameObject outcomePanel;
        [SerializeField] TMP_Text outcomeText;
        public int Decisions => state == null ? 0 : state.decisions;
        float SwipeThreshold => cardTransform.rect.width * .22f;
        bool HelpOpen => (frontMenu!=null && frontMenu.IsOpen) || (helpPanel != null && helpPanel.activeSelf) || (nationalPanels != null && nationalPanels.IsOpen);

        void Start() { if(Application.isPlaying) { Initialize(); frontMenu.ShowStart(); } }
        public void BuildEditablePresentation()
        {
            BuildCardFace(); BuildPowerIcons(); BuildOutcomePanel(); nationalPanels.BuildEditablePresentation();
            if(frontMenu==null) frontMenu=gameObject.GetComponent<FrontMenu>()??gameObject.AddComponent<FrontMenu>();
            frontMenu.Build(this);
            editablePresentationVersion=3;
        }
        public void PreviewCardForEditing(DecisionCard card)
        {
            editorPreviewCard=card; BuildEditablePresentation();
            frontMenu.panel.SetActive(false);
            nationalPanels.gameObject.SetActive(false); helpPanel.SetActive(false); outcomePanel.SetActive(false);
            cardTransform.gameObject.SetActive(true); restartButton.gameObject.SetActive(false);
            choiceOverlay.alpha=0; RenderEditorCard();
        }
        public void PreviewOutcomeForEditing()
        {
            BuildEditablePresentation(); nationalPanels.gameObject.SetActive(false); helpPanel.SetActive(false);
            frontMenu.panel.SetActive(false);
            cardTransform.gameObject.SetActive(false); outcomePanel.SetActive(true);
            outcomeText.text=editorPreviewCard!=null?editorPreviewCard.left.consequence:"Your decision's consequence appears here.";
        }
        void RenderEditorCard()
        {
            if(editorPreviewCard==null || cardTransform==null) return;
            var art=cardTransform.GetComponentInChildren<AdvisorArt>(true);
            if(art!=null) art.gameObject.SetActive(false);
            portrait.enabled=editorPreviewCard.portrait!=null; portrait.sprite=editorPreviewCard.portrait;
            briefing.text=editorPreviewCard.briefing;
            cardTransform.Find("Paper caption/Advisor").GetComponent<TMP_Text>().text=editorPreviewCard.advisor+" / "+editorPreviewCard.category;
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            BuildCardFace();
            BuildPowerIcons();
            BuildOutcomePanel();
            if(frontMenu==null) frontMenu=gameObject.GetComponent<FrontMenu>()??gameObject.AddComponent<FrontMenu>();
            frontMenu.Build(this); frontMenu.Bind(); frontMenu.panel.SetActive(false);
            home = cardTransform.anchoredPosition;
            restartButton.onClick.AddListener(()=>frontMenu.ShowStart());
            for(int i=0;i<powerButtons.Length;i++)
            {
                int power=i;
                powerButtons[i].onClick.AddListener(() => ShowPower(power));
            }
            closeHelpButton.onClick.AddListener(() => SetHelp(false));
            nationalPanels.Initialize(this);
            Restart();
        }

        public void Restart()
        {
            if (campaign == null) { briefing.text = "Assign a campaign in the Inspector"; return; }
            inputReady = 0; feedbackUntil = 0; dragging = false; keyboardDirection = 0;
            transitionPhase=0; outcomePanel.SetActive(false);
            ResetOutcome();
            nationalPanels.ResetElectionNight();
            ResetCard(); ClearFeedback(); helpPanel.SetActive(false);
            nationalPanels.gameObject.SetActive(false);
            state = new CampaignState(campaign, System.Environment.TickCount);
            Render();
        }
        public void Refresh() { if(!IsTransitioning) Render(); }
        public void UseCampaign(CampaignState restored,bool unread)
        {
            CancelDrag(); transitionPhase=0; transitionTime=0; inputReady=0;
            state=restored; outcomePanel.SetActive(false); helpPanel.SetActive(false); nationalPanels.ResetElectionNight(); nationalPanels.gameObject.SetActive(false);
            ResetCard(); ResetOutcome(); ClearFeedback(); Render();
            if(unread) { transitionPhase=4; cardTransform.gameObject.SetActive(false); outcomeText.text=state.lastResult; outcomePanel.SetActive(true); }
            else if(state.ElectionPending) nationalPanels.OpenElection();
        }
        public void CancelInteraction() => CancelDrag();

        void Update()
        {
            if(!Application.isPlaying) { if(!initialized) RenderEditorCard(); return; }
            if(frontMenu!=null && frontMenu.IsOpen) return;
            if(IsTransitioning && !AwaitingAcknowledgement) { AdvanceTransition(Time.unscaledDeltaTime); return; }
            if (!dragging && feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil) ClearFeedback();
            if (state == null || state.current==null || (state.ended && !AwaitingAcknowledgement) || dragging || HelpOpen || Time.unscaledTime < inputReady) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) keyboardDirection = -1;
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) keyboardDirection = 1;
                bool held = keyboardDirection < 0 ? keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed : keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;
                if (keyboardDirection != 0 && held) PreviewDirection(keyboardDirection * (SwipeThreshold + 5));
                else if (keyboardDirection != 0) { bool right = keyboardDirection > 0; keyboardDirection = 0; Decide(right); }
            }
        }

        Vector2 LocalPoint(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)cardTransform.parent,
                data.position, data.pressEventCamera, out var local);
            return local;
        }
        public void BeginDrag(PointerEventData data)
        {
            if (state == null || (!AwaitingAcknowledgement && (state.ended || IsTransitioning || state.ElectionPending)) || HelpOpen || dragging || Time.unscaledTime < inputReady) return;
            keyboardDirection = 0; dragging = true; pointerId = data.pointerId; dragStart = LocalPoint(data);
        }
        public void Drag(PointerEventData data)
        {
            if (!dragging || pointerId != data.pointerId) return;
            float delta = LocalPoint(data).x - dragStart.x;
            PreviewDirection(delta);
        }

        void PreviewDirection(float delta)
        {
            if(AwaitingAcknowledgement)
            {
                var rect=(RectTransform)outcomePanel.transform;
                rect.anchoredPosition=home+new Vector2(Mathf.Clamp(delta*.45f,-110,110),0);
                rect.localRotation=Quaternion.Euler(0,0,Mathf.Clamp(-delta*.06f,-12,12));
                return;
            }
            cardTransform.anchoredPosition = home + new Vector2(Mathf.Clamp(delta * .45f,-110,110), 0);
            cardTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-delta * .06f,-12,12));
            float distance = Mathf.Abs(delta);
            choiceOverlay.alpha = Mathf.Clamp01((distance - 8) / 28f);
            commitIndicator.enabled = distance > SwipeThreshold;
            if (distance > 8)
            {
                var choice = delta > 0 ? state.current.right : state.current.left;
                choiceLabel.text = choice.label;
                bool passes=state.nation.CanResolve(choice);
                resultLabel.text=choice.institution==InstitutionRule.None?state.lastResult:
                    choice.institution==InstitutionRule.Congress?(passes?"You hold both chambers. The bill can pass.":"You need 218 House seats and 51 senators."):
                    choice.institution==InstitutionRule.CourtReview?(passes?"Five justices are aligned. The policy can stand.":"Fewer than five aligned justices. The policy will fall."):
                    (passes?"The Senate can confirm your nominee.":"Confirmation needs a vacancy and 51 senators.");
                if(!passes && !state.nation.HoldsPresidency && (choice.enactPolicy!=PolicyId.None || choice.repealPolicy!=PolicyId.None || choice.institution==InstitutionRule.Congress || choice.institution==InstitutionRule.ConfirmJustice)) resultLabel.text="Your party must win the presidency first.";
                if(choice.institution==InstitutionRule.BlockGovernment) resultLabel.text=passes?"Your party can block this bill.":"Blocking needs the House or 41 senators.";
                if(passes && choice.voterStates!=null && choice.voterStates.Length>0)
                    resultLabel.text=string.Join(" / ",choice.voterStates)+$": up to {choice.voterSupportChange:+0;-0} points support";
                ShowFeedback(state.nation.CanResolve(choice)?choice.change:choice.blockedChange, false);
            }
            else { choiceLabel.text = ""; resultLabel.text=state.lastResult; ClearFeedback(); }
        }
        public void EndDrag(PointerEventData data)
        {
            if (!dragging || pointerId != data.pointerId) return;
            Vector2 delta = LocalPoint(data) - dragStart;
            dragging = false;
            if (Mathf.Abs(delta.x) > SwipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) Decide(delta.x > 0);
            else if(AwaitingAcknowledgement) ResetOutcome();
            else { ResetCard(); ClearFeedback(); }
            
        }
        void ResetCard() { cardTransform.anchoredPosition = home; cardTransform.localRotation = Quaternion.identity; choiceOverlay.alpha = 0; choiceLabel.text = ""; commitIndicator.enabled = false; if(state!=null && resultLabel!=null) resultLabel.text=state.lastResult; }
        void OnApplicationFocus(bool focused) { if (!focused && initialized) CancelDrag(); }
        void OnDisable() { if (initialized) CancelDrag(); }
        void ResetOutcome() { var rect=(RectTransform)outcomePanel.transform; rect.anchoredPosition=home; rect.localRotation=Quaternion.identity; }
        void CancelDrag() { keyboardDirection = 0; dragging = false; if(AwaitingAcknowledgement) ResetOutcome(); if(IsTransitioning) return; ResetCard(); ClearFeedback(); }

        public void Decide(bool right)
        {
            if(frontMenu!=null && frontMenu.IsOpen) return;
            if(AwaitingAcknowledgement) { AcknowledgeOutcome(right); return; }
            if (state == null || state.current==null || state.ended || state.ElectionPending || IsTransitioning || HelpOpen || Time.unscaledTime < inputReady) return;
            var choice=right ? state.current.right : state.current.left;
            var change = state.nation.CanResolve(choice)?choice.change:choice.blockedChange;
            bool passed=state.nation.CanResolve(choice); var localBefore=new float[state.nation.states.Length];
            for(int i=0;i<localBefore.Length;i++) localBefore[i]=state.nation.VoterSupportBonus(i);
            for(int i=0;i<4;i++) previousSupport[i]=supportFills[i].fillAmount;
            departurePosition=cardTransform.anchoredPosition;
            departureAngle=cardTransform.localEulerAngles.z;
            departureDirection=right?1:-1;
            partyAudio?.PlaySwipe(right);
            state.Choose(right); nationalPanels.RefreshPolicyNotice(); dragging = false; keyboardDirection=0;
            voterFeedback="";
            if(passed && choice.voterStates!=null) for(int i=0;i<localBefore.Length;i++) if(System.Array.IndexOf(choice.voterStates,state.nation.states[i].abbreviation)>=0)
            {
                float delta=(state.nation.VoterSupportBonus(i)-localBefore[i]*.97f)*.375f;
                voterFeedback+=(voterFeedback==""?"":" · ")+state.nation.states[i].abbreviation+$" {delta:+0.0;-0.0;0}pp";
            }
            transitionPhase=1; transitionTime=0;
            frontMenu?.Save();
            choiceOverlay.alpha=0; commitIndicator.enabled=false;
            ShowFeedback(change, true); feedbackUntil=0;
            for(int i=0;i<4;i++) { int delta=state.support[i]-Mathf.RoundToInt(previousSupport[i]*100); supportChanges[i].text=delta==0?"":delta>0?"+"+delta:delta.ToString(); }
        }

        public void AcknowledgeOutcome(bool right)
        {
            if(!AwaitingAcknowledgement) return;
            partyAudio?.PlaySwipe(right,true);
            var rect=(RectTransform)outcomePanel.transform;
            departurePosition=rect.anchoredPosition; departureAngle=rect.localEulerAngles.z;
            departureDirection=right?1:-1; transitionPhase=5; transitionTime=0;
            dragging=false; keyboardDirection=0;
            frontMenu?.Save();
        }

        // Explicit time steps keep playback deterministic and testable without real-time waits.
        public void AdvanceTransition(float seconds)
        {
            while(IsTransitioning && !AwaitingAcknowledgement && seconds>0)
            {
                float duration=transitionPhase==1?.30f:transitionPhase==2?1.15f:.25f;
                float step=Mathf.Min(seconds,duration-transitionTime); transitionTime+=step; seconds-=step;
                float t=Mathf.Clamp01(transitionTime/duration);
                if(transitionPhase==1 || transitionPhase==5)
                {
                    var moving=transitionPhase==5?(RectTransform)outcomePanel.transform:cardTransform;
                    float distance=((RectTransform)cardTransform.parent).rect.width+cardTransform.rect.width;
                    moving.anchoredPosition=Vector2.Lerp(departurePosition,home+new Vector2(departureDirection*distance,-45),t*t);
                    moving.localRotation=Quaternion.Euler(0,0,Mathf.LerpAngle(departureAngle,-departureDirection*28,t));
                }
                else if(transitionPhase==2)
                {
                    float eased=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2));
                    for(int i=0;i<4;i++)
                    {
                        supportFills[i].fillAmount=Mathf.Lerp(previousSupport[i],state.support[i]/100f,eased);
                        supportFills[i].color=state.support[i]<20?new Color(.83f,.36f,.29f):new Color(.72f,.62f,.39f);
                    }
                }
                else cardTransform.anchoredPosition=Vector2.Lerp(home+new Vector2(0,-32),home,Mathf.SmoothStep(0,1,t));
                if(t<1) break;
                transitionTime=0;
                if(transitionPhase==1)
                {
                    transitionPhase=2; cardTransform.gameObject.SetActive(false);
                    ResetOutcome(); outcomeText.text=state.lastResult+(voterFeedback==""?"":"\n\n<size=18>State support: "+voterFeedback+"</size>"); outcomePanel.SetActive(true);
                }
                else if(transitionPhase==2)
                {
                    transitionPhase=4;
                }
                else if(transitionPhase==5)
                {
                    outcomePanel.SetActive(false); ClearFeedback(); ResetCard(); Render();
                    if(state.ElectionPending || state.ended)
                    {
                        transitionPhase=0;
                        if(state.ElectionPending) nationalPanels.OpenElection();
                    }
                    else { transitionPhase=3; cardTransform.anchoredPosition=home+new Vector2(0,-32); }
                }
                else { transitionPhase=0; ResetCard(); }
            }
        }

        void BuildOutcomePanel()
        {
            if(outcomePanel!=null) return;
            outcomePanel=new GameObject("Decision outcome",typeof(RectTransform),typeof(Image));
            var rect=(RectTransform)outcomePanel.transform; rect.SetParent(cardTransform.parent,false);
            rect.anchorMin=cardTransform.anchorMin; rect.anchorMax=cardTransform.anchorMax; rect.pivot=cardTransform.pivot;
            rect.anchoredPosition=cardTransform.anchoredPosition; rect.sizeDelta=cardTransform.sizeDelta;
            outcomePanel.GetComponent<Image>().color=new Color(.91f,.87f,.77f);
            outcomePanel.GetComponent<Image>().raycastTarget=true;
            outcomePanel.AddComponent<CardDrag>().game=this;
            Caption(rect,"Outcome heading",24,275,302,26,16).text="THE CONSEQUENCES";
            outcomeText=Caption(rect,"Outcome",24,65,302,190,25);
            Caption(rect,"Read confirmation",24,18,302,30,17).text="Swipe either way to continue";
            outcomePanel.transform.SetSiblingIndex(cardTransform.GetSiblingIndex());
            outcomePanel.SetActive(false);
        }

        public void SetHelp(bool visible)
        {
            if(visible && IsTransitioning) return;
            if (visible) CancelDrag();
            helpPanel.SetActive(visible);
            if (state == null) return;
            helpTitle.text="Behind the party";
            helpText.text = "You lead the party from behind the scenes. Presidents come and go.\n\nKeep every top meter above zero. Election losses do not end your run.\n\nEnact policies through events. Review them under Policies.";
        }

        public void ShowPower(int index)
        {
            if(state==null || IsTransitioning || index<0 || index>=4) return;
            SetHelp(true);
            string[] names={ "Workers", "Middle class", "Economy", "Elites" };
            string[] descriptions={ "Workers and unions.", "Families and the middle class.", "Jobs, growth and economic stability.", "Wealthy donors and business leaders." };
            helpTitle.text=names[index];
            helpText.text=descriptions[index]+$"\n\nSupport: {state.support[index]} / 100\n\nKeep this group above zero. A full meter is safe.\n\nParty cycle {state.Term} · Support {state.Approval}%";
        }

        void Render()
        {
            nationalPanels.RefreshPolicyNotice();
            bool electionYear=state.IsElectionYear;
            background.color=electionYear?new Color(.98f,.975f,.95f):new Color(.13f,.18f,.19f);
            var ink=electionYear?new Color(.10f,.15f,.16f):new Color(.96f,.93f,.83f);
            briefing.color=ink; monthLabel.color=ink;
            monthLabel.text=state.DisplayMonth.ToString("MMMM yyyy")+(state.nation.HoldsPresidency?" · Governing":" · Opposition");
            monthLabel.fontSize=electionYear?12:16;
            monthLabel.rectTransform.anchoredPosition=new Vector2(20,electionYear?-79:-82);
            monthLabel.rectTransform.sizeDelta=new Vector2(350,electionYear?28:22);
            if(electionYear) monthLabel.text="<b>"+state.ElectionYearLabel+"</b>\n"+monthLabel.text;
            foreach(var icon in flatPowerIcons) icon.color=ink;
            for (int i = 0; i < 4; i++)
            {
                supportFills[i].fillAmount = state.support[i] / 100f;
                supportFills[i].color = state.support[i] < 20 ? new Color(.83f,.36f,.29f) : new Color(.72f,.62f,.39f);
            }
            portrait.gameObject.SetActive(!state.ended);
            cardTransform.gameObject.SetActive(!state.ended);
            restartButton.gameObject.SetActive(state.ended);
            if (state.ended)
            {
                briefing.text = state.ending;
                return;
            }
            var c = state.current;
            if(c==null) { cardTransform.gameObject.SetActive(false); briefing.text="No available events. Check the campaign deck."; return; }
            briefing.text = c.briefing;
            portrait.sprite=c.portrait; portrait.enabled=c.portrait!=null;
            if(flatArt!=null) flatArt.gameObject.SetActive(false);

            advisorLabel.text=c.advisor+"  /  "+c.category;
            resultLabel.text=state.lastResult;
        }

        public void BuildCardFace()
        {
            flatArt=cardTransform.GetComponentInChildren<AdvisorArt>(true);
            if(flatArt!=null) flatArt.gameObject.SetActive(false);
            if(cardTransform.Find("Paper caption")!=null)
            {
                advisorLabel=cardTransform.Find("Paper caption/Advisor").GetComponent<TMP_Text>();
                resultLabel=cardTransform.Find("Paper caption/Last decision").GetComponent<TMP_Text>();
                return;
            }
            var footer=new GameObject("Paper caption",typeof(RectTransform),typeof(Image));
            var f=(RectTransform)footer.transform; f.SetParent(cardTransform,false);
            f.anchorMin=Vector2.zero; f.anchorMax=new Vector2(1,0); f.pivot=new Vector2(.5f,0); f.sizeDelta=new Vector2(0,76); f.anchoredPosition=Vector2.zero;
            footer.GetComponent<Image>().color=new Color(.91f,.87f,.77f); footer.GetComponent<Image>().raycastTarget=false;
            advisorLabel=Caption(f,"Advisor",12,51,326,18,11);
            resultLabel=Caption(f,"Last decision",12,6,326,42,14);
            portrait.enabled=false;
            if(campaign!=null && campaign.cards.Count>0)
            {
                advisorLabel.text=campaign.cards[0].advisor;
                resultLabel.text="Hold left or right to reveal a choice.";
            }
            choiceOverlay.transform.SetAsLastSibling();
        }
        void BuildPowerIcons()
        {
            if(flatPowerIcons!=null && flatPowerIcons.Length==powerIcons.Length && System.Array.TrueForAll(flatPowerIcons,i=>i!=null)) return;
            flatPowerIcons=new PowerIcon[powerIcons.Length];
            for(int i=0;i<powerIcons.Length;i++)
            {
                var legacy=powerIcons[i]; legacy.enabled=false;
                var badge=legacy.transform.parent.GetComponent<Image>(); if(badge!=null) badge.enabled=false;
                var go=new GameObject("Geometric power symbol",typeof(RectTransform),typeof(CanvasRenderer),typeof(PowerIcon));
                var rect=(RectTransform)go.transform; rect.SetParent(legacy.transform.parent,false);
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=new Vector2(40,40);
                var icon=go.GetComponent<PowerIcon>(); icon.symbol=(PowerIcon.Symbol)i; icon.raycastTarget=false; icon.color=new Color(.96f,.93f,.83f); flatPowerIcons[i]=icon;
            }
        }
        TMP_Text Caption(RectTransform parent,string title,float x,float y,float w,float h,int size)
        {
            var go=new GameObject(title,typeof(RectTransform),typeof(TextMeshProUGUI));
            var r=(RectTransform)go.transform; r.SetParent(parent,false); r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;
            r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h);
            var text=go.GetComponent<TextMeshProUGUI>(); text.font=briefing.font; text.fontSharedMaterial=briefing.fontSharedMaterial;
            text.fontSize=size; text.color=new Color(.16f,.21f,.21f); text.raycastTarget=false;
            return text;
        }

        void ShowFeedback(SupportChange change, bool committed)
        {
            var values = change.Values;
            for (int i=0;i<supportChanges.Length;i++)
            {
                supportChanges[i].text = values[i] == 0 ? "" : (values[i] > 0 ? "+" : "−");
                supportChanges[i].color = state.IsElectionYear
                    ? (values[i]>0?new Color(.15f,.42f,.22f):new Color(.65f,.20f,.14f))
                    : (values[i]>0?new Color(.7f,.8f,.67f):new Color(.9f,.58f,.43f));
            }
            feedbackUntil = committed ? Time.unscaledTime + 1.2f : 0;
        }
        void ClearFeedback()
        {
            feedbackUntil = 0;
            foreach (var change in supportChanges) change.text = "";
        }
    }
}

