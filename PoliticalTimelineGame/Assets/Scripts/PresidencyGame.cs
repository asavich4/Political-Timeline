using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PoliticalTimeline
{
    public class PresidencyGame : MonoBehaviour
    {
        public CampaignDefinition campaign;
        public TMP_Text briefing, choiceLabel, helpText, helpTitle;
        public Button[] powerButtons;
        public TMP_Text monthLabel;
        public Image background;
        public Image[] powerIcons;
        public NationalPanels nationalPanels;
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
        public bool IsTransitioning => transitionPhase!=0;
        int transitionPhase;
        float transitionTime;
        Vector2 departurePosition;
        float departureAngle, departureDirection;
        readonly float[] previousSupport=new float[4];
        GameObject outcomePanel;
        TMP_Text outcomeText;
        public int Decisions => state == null ? 0 : state.decisions;
        float SwipeThreshold => cardTransform.rect.width * .22f;
        bool HelpOpen => (helpPanel != null && helpPanel.activeSelf) || (nationalPanels != null && nationalPanels.IsOpen);

        void Start() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            BuildCardFace();
            BuildOutcomePanel();
            home = cardTransform.anchoredPosition;
            restartButton.onClick.AddListener(Restart);
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
            nationalPanels.ResetElectionNight();
            ResetCard(); ClearFeedback(); helpPanel.SetActive(false);
            nationalPanels.gameObject.SetActive(false);
            state = new CampaignState(campaign, System.Environment.TickCount);
            Render();
        }
        public void Refresh() { if(!IsTransitioning) Render(); }
        public void CancelInteraction() => CancelDrag();

        void Update()
        {
            if(IsTransitioning) { AdvanceTransition(Time.unscaledDeltaTime); return; }
            if (!dragging && feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil) ClearFeedback();
            if (state == null || state.ended || dragging || HelpOpen || Time.unscaledTime < inputReady) return;
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
            if (state == null || state.ended || IsTransitioning || state.ElectionPending || HelpOpen || dragging || Time.unscaledTime < inputReady) return;
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
            cardTransform.anchoredPosition = home + new Vector2(Mathf.Clamp(delta * .45f,-110,110), 0);
            cardTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-delta * .06f,-12,12));
            float distance = Mathf.Abs(delta);
            choiceOverlay.alpha = Mathf.Clamp01((distance - 8) / 28f);
            commitIndicator.enabled = distance > SwipeThreshold;
            if (distance > 8)
            {
                var choice = delta > 0 ? state.current.right : state.current.left;
                choiceLabel.text = choice.label;
                bool passes=state.nation.CanResolve(choice.institution);
                resultLabel.text=choice.institution==InstitutionRule.None?state.lastResult:
                    choice.institution==InstitutionRule.Congress?(passes?"You hold both chambers. The bill can pass.":"You need 218 House seats and 51 senators."):
                    choice.institution==InstitutionRule.CourtReview?(passes?"Five justices are aligned. The policy can stand.":"Fewer than five aligned justices. The policy will fall."):
                    (passes?"The Senate can confirm your nominee.":"Confirmation needs a vacancy and 51 senators.");
                ShowFeedback(state.nation.CanResolve(choice.institution)?choice.change:choice.blockedChange, false);
            }
            else { choiceLabel.text = ""; resultLabel.text=state.lastResult; ClearFeedback(); }
        }
        public void EndDrag(PointerEventData data)
        {
            if (!dragging || pointerId != data.pointerId) return;
            Vector2 delta = LocalPoint(data) - dragStart;
            dragging = false;
            if (Mathf.Abs(delta.x) > SwipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) Decide(delta.x > 0);
            else { ResetCard(); ClearFeedback(); }
            
        }
        void ResetCard() { cardTransform.anchoredPosition = home; cardTransform.localRotation = Quaternion.identity; choiceOverlay.alpha = 0; choiceLabel.text = ""; commitIndicator.enabled = false; if(state!=null && resultLabel!=null) resultLabel.text=state.lastResult; }
        void OnApplicationFocus(bool focused) { if (!focused && initialized) CancelDrag(); }
        void OnDisable() { if (initialized) CancelDrag(); }
        void CancelDrag() { keyboardDirection = 0; dragging = false; if(IsTransitioning) return; ResetCard(); ClearFeedback(); }

        public void Decide(bool right)
        {
            if (state == null || state.ended || state.ElectionPending || IsTransitioning || HelpOpen || Time.unscaledTime < inputReady) return;
            var choice=right ? state.current.right : state.current.left;
            var change = state.nation.CanResolve(choice.institution)?choice.change:choice.blockedChange;
            for(int i=0;i<4;i++) previousSupport[i]=supportFills[i].fillAmount;
            departurePosition=cardTransform.anchoredPosition;
            departureAngle=cardTransform.localEulerAngles.z;
            departureDirection=right?1:-1;
            state.Choose(right); dragging = false; keyboardDirection=0;
            transitionPhase=1; transitionTime=0;
            choiceOverlay.alpha=0; commitIndicator.enabled=false;
            ShowFeedback(change, true); feedbackUntil=0;
            for(int i=0;i<4;i++) { int delta=state.support[i]-Mathf.RoundToInt(previousSupport[i]*100); supportChanges[i].text=delta==0?"":delta>0?"+"+delta:delta.ToString(); }
        }

        // Explicit time steps keep playback deterministic and testable without real-time waits.
        public void AdvanceTransition(float seconds)
        {
            while(IsTransitioning && seconds>0)
            {
                float duration=transitionPhase==1?.30f:transitionPhase==2?1.15f:.25f;
                float step=Mathf.Min(seconds,duration-transitionTime); transitionTime+=step; seconds-=step;
                float t=Mathf.Clamp01(transitionTime/duration);
                if(transitionPhase==1)
                {
                    float distance=((RectTransform)cardTransform.parent).rect.width+cardTransform.rect.width;
                    cardTransform.anchoredPosition=Vector2.Lerp(departurePosition,home+new Vector2(departureDirection*distance,-45),t*t);
                    cardTransform.localRotation=Quaternion.Euler(0,0,Mathf.LerpAngle(departureAngle,-departureDirection*28,t));
                }
                else if(transitionPhase==2)
                {
                    float eased=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2));
                    for(int i=0;i<4;i++)
                    {
                        supportFills[i].fillAmount=Mathf.Lerp(previousSupport[i],state.support[i]/100f,eased);
                        supportFills[i].color=state.support[i]<20||state.support[i]>80?new Color(.83f,.36f,.29f):new Color(.72f,.62f,.39f);
                    }
                }
                else cardTransform.anchoredPosition=Vector2.Lerp(home+new Vector2(0,-32),home,Mathf.SmoothStep(0,1,t));
                if(t<1) break;
                transitionTime=0;
                if(transitionPhase==1)
                {
                    transitionPhase=2; cardTransform.gameObject.SetActive(false);
                    outcomeText.text=state.lastResult; outcomePanel.SetActive(true);
                }
                else if(transitionPhase==2)
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
            outcomePanel=new GameObject("Decision outcome",typeof(RectTransform),typeof(Image));
            var rect=(RectTransform)outcomePanel.transform; rect.SetParent(cardTransform.parent,false);
            rect.anchorMin=cardTransform.anchorMin; rect.anchorMax=cardTransform.anchorMax; rect.pivot=cardTransform.pivot;
            rect.anchoredPosition=cardTransform.anchoredPosition; rect.sizeDelta=cardTransform.sizeDelta;
            outcomePanel.GetComponent<Image>().color=new Color(.91f,.87f,.77f);
            outcomePanel.GetComponent<Image>().raycastTarget=false;
            Caption(rect,"Outcome heading",24,275,302,26,16).text="THE CONSEQUENCES";
            outcomeText=Caption(rect,"Outcome",24,65,302,190,25);
            outcomePanel.transform.SetSiblingIndex(cardTransform.GetSiblingIndex());
            outcomePanel.SetActive(false);
        }

        public void SetHelp(bool visible)
        {
            if(visible && IsTransitioning) return;
            if (visible) CancelDrag();
            helpPanel.SetActive(visible);
            if (state == null) return;
            helpTitle.text="Keys of power";
            helpText.text = "Keep every group above 0 and below 100.\n\n"
                + "Hold the card left or right to see a choice. Release to choose.\n\n"
                + "One swipe is one month. Win 270 electoral votes to be reelected.\n\n"
                + $"Term {state.Term}  ·  Approval {state.Approval}%";
        }

        public void ShowPower(int index)
        {
            if(state==null || IsTransitioning || index<0 || index>=4) return;
            SetHelp(true);
            string[] names={ "Workers", "Middle class", "Economy", "Elites" };
            string[] descriptions={ "Workers and unions.", "Families and the middle class.", "Jobs, growth and economic stability.", "Wealthy donors and business leaders." };
            helpTitle.text=names[index];
            helpText.text=descriptions[index]+$"\n\nSupport: {state.support[index]} / 100\n\nKeep this group above 0 and below 100.\n\nTerm {state.Term} · Approval {state.Approval}%";
        }

        void Render()
        {
            bool electionYear=state.IsElectionYear;
            background.color=electionYear?new Color(.98f,.975f,.95f):new Color(.13f,.18f,.19f);
            var ink=electionYear?new Color(.10f,.15f,.16f):new Color(.96f,.93f,.83f);
            briefing.color=ink; monthLabel.color=ink;
            monthLabel.text=state.DisplayMonth.ToString("MMMM yyyy")+(electionYear?" · Election year":"");
            foreach(var icon in powerIcons) icon.color=Color.white;
            for (int i = 0; i < 4; i++)
            {
                supportFills[i].fillAmount = state.support[i] / 100f;
                supportFills[i].color = state.support[i] < 20 || state.support[i] > 80 ? new Color(.83f,.36f,.29f) : new Color(.72f,.62f,.39f);
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
            briefing.text = c.briefing;
            portrait.enabled = false;
            flatArt.Present(c);
            advisorLabel.text=c.advisor+"  /  "+c.category;
            resultLabel.text=state.lastResult;
        }

        public void BuildCardFace()
        {
            flatArt=cardTransform.GetComponentInChildren<AdvisorArt>(true);
            if(flatArt!=null)
            {
                advisorLabel=cardTransform.Find("Paper caption/Advisor").GetComponent<TMP_Text>();
                resultLabel=cardTransform.Find("Paper caption/Last decision").GetComponent<TMP_Text>();
                return;
            }
            var face=new GameObject("Flat advisor",typeof(RectTransform),typeof(CanvasRenderer),typeof(AdvisorArt));
            var r=(RectTransform)face.transform; r.SetParent(cardTransform,false);
            r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=new Vector2(0,76); r.offsetMax=Vector2.zero;
            flatArt=face.GetComponent<AdvisorArt>(); flatArt.raycastTarget=false;
            var footer=new GameObject("Paper caption",typeof(RectTransform),typeof(Image));
            var f=(RectTransform)footer.transform; f.SetParent(cardTransform,false);
            f.anchorMin=Vector2.zero; f.anchorMax=new Vector2(1,0); f.pivot=new Vector2(.5f,0); f.sizeDelta=new Vector2(0,76); f.anchoredPosition=Vector2.zero;
            footer.GetComponent<Image>().color=new Color(.91f,.87f,.77f); footer.GetComponent<Image>().raycastTarget=false;
            advisorLabel=Caption(f,"Advisor",12,51,326,18,11);
            resultLabel=Caption(f,"Last decision",12,6,326,42,14);
            portrait.enabled=false;
            if(campaign!=null && campaign.cards.Count>0)
            {
                flatArt.Present(campaign.cards[0]); advisorLabel.text=campaign.cards[0].advisor;
                resultLabel.text="Hold left or right to reveal a choice.";
            }
            choiceOverlay.transform.SetAsLastSibling();
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
