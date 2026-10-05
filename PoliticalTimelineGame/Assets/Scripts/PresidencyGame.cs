using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PoliticalTimeline
{
    public class PresidencyGame : MonoBehaviour
    {
        public CampaignDefinition campaign;
        public Text briefing, leftLabel, rightLabel, helpText;
        public Text[] supportChanges;
        public Image[] supportFills;
        public Image portrait;
        public RectTransform cardTransform;
        public Button leftButton, rightButton, restartButton, helpButton, closeHelpButton;
        public GameObject helpPanel;
        CampaignState state;
        Vector2 home;
        bool dragging, initialized;
        int pointerId;
        Vector2 dragStart;
        float inputReady, feedbackUntil;
        public int Decisions => state == null ? 0 : state.decisions;
        float SwipeThreshold => cardTransform.rect.width * .22f;
        bool HelpOpen => helpPanel != null && helpPanel.activeSelf;

        void Start() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            home = cardTransform.anchoredPosition;
            leftButton.onClick.AddListener(() => Decide(false));
            rightButton.onClick.AddListener(() => Decide(true));
            restartButton.onClick.AddListener(Restart);
            helpButton.onClick.AddListener(() => SetHelp(true));
            closeHelpButton.onClick.AddListener(() => SetHelp(false));
            Restart();
        }

        public void Restart()
        {
            if (campaign == null) { briefing.text = "Assign a campaign in the Inspector"; return; }
            inputReady = 0; feedbackUntil = 0; dragging = false;
            ResetCard(); ClearFeedback(); helpPanel.SetActive(false);
            state = new CampaignState(campaign, System.Environment.TickCount);
            Render();
        }

        void Update()
        {
            if (!dragging && feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil) ClearFeedback();
            if (state == null || state.ended || dragging || HelpOpen || Time.unscaledTime < inputReady) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Decide(false);
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Decide(true);
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
            if (state == null || state.ended || HelpOpen || dragging || Time.unscaledTime < inputReady) return;
            dragging = true; pointerId = data.pointerId; dragStart = LocalPoint(data);
        }
        public void Drag(PointerEventData data)
        {
            if (!dragging || pointerId != data.pointerId) return;
            float delta = LocalPoint(data).x - dragStart.x;
            cardTransform.anchoredPosition = home + new Vector2(Mathf.Clamp(delta * .55f,-140,140), 0);
            cardTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-delta * .04f,-12,12));
            if (Mathf.Abs(delta) > 20)
            {
                var choice = delta > 0 ? state.current.right : state.current.left;
                ShowFeedback(choice.change, false);
            }
            else ClearFeedback();
        }
        public void EndDrag(PointerEventData data)
        {
            if (!dragging || pointerId != data.pointerId) return;
            Vector2 delta = LocalPoint(data) - dragStart;
            dragging = false; ResetCard(); ClearFeedback();
            if (Mathf.Abs(delta.x) > SwipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) Decide(delta.x > 0);
            
        }
        void ResetCard() { cardTransform.anchoredPosition = home; cardTransform.localRotation = Quaternion.identity; }
        void OnApplicationFocus(bool focused) { if (!focused && initialized) CancelDrag(); }
        void OnDisable() { if (initialized) CancelDrag(); }
        void CancelDrag() { dragging = false; ResetCard(); ClearFeedback(); }

        public void Decide(bool right)
        {
            if (state == null || state.ended || HelpOpen || Time.unscaledTime < inputReady) return;
            var change = (right ? state.current.right : state.current.left).change;
            state.Choose(right); dragging = false;
            inputReady = Time.unscaledTime + .22f;
            ResetCard(); Render(); ShowFeedback(change, true);
        }

        public void SetHelp(bool visible)
        {
            if (visible) CancelDrag();
            helpPanel.SetActive(visible);
            if (state == null) return;
            helpText.text = "Keep every group above 0 and below 100.\n\n"
                + "Swipe the card or tap a choice.\n\n"
                + $"Win reelection with {campaign.electionThreshold}% approval.\n\n"
                + $"Term {state.Term}  ·  Approval {state.Approval}%";
        }

        void Render()
        {
            for (int i = 0; i < 4; i++)
            {
                supportFills[i].fillAmount = state.support[i] / 100f;
                supportFills[i].color = state.support[i] < 20 || state.support[i] > 80 ? new Color(.83f,.36f,.29f) : new Color(.72f,.62f,.39f);
            }
            leftButton.gameObject.SetActive(!state.ended);
            rightButton.gameObject.SetActive(!state.ended);
            restartButton.gameObject.SetActive(state.ended);
            if (state.ended)
            {
                briefing.text = state.ending;
                return;
            }
            var c = state.current;
            briefing.text = c.briefing;
            portrait.sprite = c.portrait; portrait.enabled = c.portrait != null;
            leftLabel.text = c.left.label; rightLabel.text = c.right.label;
        }

        void ShowFeedback(SupportChange change, bool committed)
        {
            var values = change.Values;
            for (int i=0;i<supportChanges.Length;i++)
            {
                supportChanges[i].text = values[i] == 0 ? "" : (values[i] > 0 ? "+" : "−");
                supportChanges[i].color = values[i] > 0 ? new Color(.7f,.8f,.67f) : new Color(.9f,.58f,.43f);
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
