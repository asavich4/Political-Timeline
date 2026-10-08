using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PoliticalTimeline
{
    public class FrontMenu : MonoBehaviour
    {
        public PresidencyGame game;
        public GameObject panel;
        public TMP_Text heading, description, status;
        public Button[] slotLoad, slotNew;
        public TMP_Text[] slotLabels;
        public Button democrat, republican, begin, back, resume, saveMenu;
        public GameObject slotsPage, partyPage;
        public TMP_Text partyNote;
        int activeSlot=-1, selectedSlot;
        PartyTeam selectedParty;
        bool bound;
        public bool IsOpen => panel!=null && panel.activeSelf;
        public int ActiveSlot => activeSlot;
        public void Build(PresidencyGame owner)
        {
            game=owner; if(panel!=null) return;
            var board=(RectTransform)game.cardTransform.parent;
            board.sizeDelta=new Vector2(390,690); game.GetComponent<PortraitLayout>().designSize=new Vector2(390,690);
            saveMenu=Button(board,"Save and Menu",20,646,350,32,"SAVE & MENU");
            panel=Box(board,"Start Menu",0,0,390,690,new Color(.10f,.15f,.17f)).gameObject;
            heading=Text(panel.transform,"Title",24,34,342,100,"POLITICAL\nTIMELINE",36);
            description=Text(panel.transform,"Subtitle",24,140,342,58,"Presidents change.\nYour party endures.",19);
            slotsPage=Box(panel.transform,"Three save slots",0,210,390,350,Color.clear).gameObject;
            slotLoad=new Button[3]; slotNew=new Button[3]; slotLabels=new TMP_Text[3];
            for(int i=0;i<3;i++)
            {
                float y=i*112;
                slotLabels[i]=Text(slotsPage.transform,"Slot "+(i+1),24,y,342,48,"SLOT "+(i+1)+"  /  EMPTY",16);
                slotLoad[i]=Button(slotsPage.transform,"Continue "+(i+1),24,y+51,163,42,"CONTINUE");
                slotNew[i]=Button(slotsPage.transform,"New "+(i+1),203,y+51,163,42,"NEW GAME");
            }
            partyPage=Box(panel.transform,"Choose party",0,210,390,350,Color.clear).gameObject;
            Text(partyPage.transform,"Choose title",24,0,342,45,"CHOOSE YOUR PARTY",23);
            democrat=Button(partyPage.transform,"Democrat",24,65,163,68,"DEMOCRAT\nDEM"); democrat.image.color=new Color(.16f,.34f,.57f);
            republican=Button(partyPage.transform,"Republican",203,65,163,68,"REPUBLICAN\nREP"); republican.image.color=new Color(.62f,.22f,.19f);
            partyNote=Text(partyPage.transform,"Selection",24,148,342,76,"",18);
            begin=Button(partyPage.transform,"Start campaign",24,235,342,48,"START CAMPAIGN");
            back=Button(partyPage.transform,"Back",24,296,342,40,"BACK");
            resume=Button(panel.transform,"Resume current game",24,574,342,42,"BACK TO GAME");
            status=Text(panel.transform,"Save status",24,623,342,52,"Three local save slots. Autosaves after decisions.",14);
            partyPage.SetActive(false); resume.gameObject.SetActive(false);
        }
        public void Bind()
        {
            if(bound) return; bound=true;
            for(int i=0;i<3;i++) { int slot=i; slotLoad[i].onClick.AddListener(()=>Continue(slot)); slotNew[i].onClick.AddListener(()=>ChooseSlot(slot)); }
            democrat.onClick.AddListener(()=>ChooseParty(PartyTeam.Democrat)); republican.onClick.AddListener(()=>ChooseParty(PartyTeam.Republican));
            begin.onClick.AddListener(StartNew); back.onClick.AddListener(ShowStart);
            resume.onClick.AddListener(()=>panel.SetActive(false));
            saveMenu.onClick.AddListener(()=>{ if(Save()) ShowStart(); });
        }
        public void ShowStart()
        {
            panel.SetActive(true); panel.transform.SetAsLastSibling(); slotsPage.SetActive(true); partyPage.SetActive(false);
            resume.gameObject.SetActive(activeSlot>=0);
            for(int i=0;i<3;i++)
            {
                bool exists=SaveSlots.Exists(i); slotLoad[i].interactable=false;
                slotLabels[i].text=$"SLOT {i+1}  /  EMPTY";
                slotNew[i].GetComponentInChildren<TMP_Text>().text=exists?"NEW / REPLACE":"NEW GAME";
                if(exists) try { var s=SaveSlots.Read(i); var date=new DateTime(s.startYear,1,1).AddMonths(s.decisions-(s.electionPending?1:0)); slotLabels[i].text=$"SLOT {i+1}  /  {(PartyTeam)s.party}\n{date:MMMM yyyy} · "+(s.ended?"Campaign ended":s.holdsPresidency?"In government":"In opposition"); slotLoad[i].interactable=true; }
                catch(Exception) { slotLabels[i].text=$"SLOT {i+1}  /  UNAVAILABLE\nSave could not be read. Replace to start over."; }
            }
        }
        void ChooseSlot(int slot) { selectedSlot=slot; slotsPage.SetActive(false); partyPage.SetActive(true); ChooseParty(PartyTeam.Democrat); }
        void ChooseParty(PartyTeam party)
        {
            selectedParty=party; partyNote.text=$"Selected: {party}\n"+(SaveSlots.Exists(selectedSlot)?$"Starting will replace Slot {selectedSlot+1}.":$"A new campaign in Slot {selectedSlot+1}.");
            begin.GetComponentInChildren<TMP_Text>().text=SaveSlots.Exists(selectedSlot)?"REPLACE & START":"START CAMPAIGN";
        }
        void StartNew()
        {
            try
            {
                var state=new CampaignState(game.campaign,Environment.TickCount,selectedParty);
                SaveSlots.Write(selectedSlot,state.Export()); activeSlot=selectedSlot;
                game.UseCampaign(state,false); panel.SetActive(false); UpdateBadge();
            }
            catch(Exception e) { status.text="Could not start: "+e.Message; }
        }
        public void Continue(int slot)
        {
            try { var s=SaveSlots.Read(slot); var state=CampaignState.Restore(game.campaign,s); activeSlot=slot; game.UseCampaign(state,s.unreadOutcome); game.nationalPanels.Night?.RestoreReports(s.electionReports); panel.SetActive(false); UpdateBadge(); status.text="Game loaded."; }
            catch(Exception e) { status.text="Could not load: "+e.Message; }
        }
        void UpdateBadge() { saveMenu.GetComponentInChildren<TMP_Text>().text=(game.State.Party==PartyTeam.Democrat?"DEM":"REP")+$"  ·  SLOT {activeSlot+1}  ·  SAVE & MENU"; }
        public bool Save()
        {
            if(activeSlot<0 || game.State==null) return true;
            try { var save=game.State.Export(game.HasUnreadOutcome); save.electionReports=game.nationalPanels.Night?.Count??0; SaveSlots.Write(activeSlot,save); status.text="Progress saved."; return true; }
            catch(Exception e) { ShowStart(); status.text="Save failed: "+e.Message; return false; }
        }
        void OnApplicationPause(bool paused) { if(paused && Application.isPlaying) Save(); }
        void OnApplicationQuit() { if(Application.isPlaying) Save(); }
        Image Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); var r=(RectTransform)go.transform; r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h);
            var image=go.GetComponent<Image>(); image.color=color; return image;
        }
        TMP_Text Text(Transform parent,string name,float x,float y,float w,float h,string value,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); var r=(RectTransform)go.transform; r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h);
            var text=go.GetComponent<TextMeshProUGUI>(); text.font=game.briefing.font; text.fontSharedMaterial=game.briefing.fontSharedMaterial;
            text.text=value; text.fontSize=size; text.color=new Color(.96f,.93f,.83f); text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; return text;
        }
        Button Button(Transform parent,string name,float x,float y,float w,float h,string label)
        {
            var image=Box(parent,name,x,y,w,h,new Color(.26f,.34f,.34f)); var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            Text(image.transform,"Label",6,3,w-12,h-6,label,16); return button;
        }
    }
}
