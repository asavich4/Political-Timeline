using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PoliticalTimeline
{
    public class NationalPanels : MonoBehaviour
    {
        public GameObject mapGroup, congressGroup, courtGroup;
        public TMP_Text title, subtitle, mapSummary, mapDetail, houseText, senateText, congressNote, courtSummary, footnote, actionLabel;
        public Image background, houseFill, senateFill;
        public Image[] mapTiles, courtTiles;
        public TMP_Text[] courtLabels;
        public Button[] mapButtons, navigation;
        public Button[] stateShortcuts;
        public int[] shortcutStates;
        public Button closeButton, actionButton;
        PresidencyGame game;
        int tab;
        bool results, bound;
        [field: SerializeField] public ChamberSeats HouseChart { get; private set; }
        [field: SerializeField] public ChamberSeats SenateChart { get; private set; }
        [field: SerializeField] public Image StateSupportFill { get; private set; }
        [SerializeField] GameObject supportBar;
        [SerializeField] TMP_Text supportLabel;
        int selectedState=-1;
        [SerializeField] GameObject policiesGroup;
        [SerializeField] TMP_Text policiesText;
        int policyPage;
        public ElectionNight Night { get; private set; }
        public bool IsOpen => gameObject.activeSelf;
        static readonly Color Blue=new Color(.18f,.40f,.70f), Red=new Color(.73f,.30f,.22f), Tossup=new Color(.50f,.49f,.43f);
        Color Allied => game!=null && game.State!=null && game.State.Party==PartyTeam.Republican?Red:Blue;
        Color Opposed => game!=null && game.State!=null && game.State.Party==PartyTeam.Republican?Blue:Red;

        public void Initialize(PresidencyGame owner)
        {
            if(bound) return; bound=true; game=owner;
            BuildDetailGraphics();
            BuildPolicies();
            for(int i=0;i<navigation.Length;i++) { int index=i; navigation[i].onClick.AddListener(()=>Open(index,false)); }
            for(int i=0;i<mapButtons.Length;i++) { int index=i; mapButtons[i].onClick.AddListener(()=>SelectState(index)); }
            for(int i=0;i<stateShortcuts.Length;i++) { int index=shortcutStates[i]; stateShortcuts[i].onClick.AddListener(()=>SelectState(index)); }
            closeButton.onClick.AddListener(Close); actionButton.onClick.AddListener(Action);
        }
        public void BuildEditablePresentation() { BuildDetailGraphics(); BuildPolicies(); }
        public void PreviewForEditing(int page,CampaignDefinition campaign)
        {
            BuildEditablePresentation(); gameObject.SetActive(true);
            mapGroup.SetActive(page==0); congressGroup.SetActive(page==1); courtGroup.SetActive(page==2); policiesGroup.SetActive(page==3);
            title.text=new[]{"Election map","Congress","Supreme Court","Policy record"}[page];
            subtitle.text="Editor preview · Starting party"; closeButton.gameObject.SetActive(true); actionButton.gameObject.SetActive(false);
            var nation=new NationalState(campaign.nation);
            HouseChart.Present(435,nation.houseSeats); SenateChart.Present(100,nation.SenateSeats);
            houseText.text=$"House · {nation.houseSeats} / 435 yours"; senateText.text=$"Senate · {nation.SenateSeats} / 100 yours";
            congressNote.text="Each dot is one seat. Blue: Democrats · Red: Republicans\nHouse majority: 218 · Senate majority: 51";
            policiesText.text="<b>Policy list</b>\nEnacted policies and dates appear here.\n\nEdit event assets to choose which policies they enact.";
            policiesText.color=mapDetail.color;
            courtSummary.text="4 aligned · 4 opposed · 1 independent";
            for(int i=0;i<9;i++) { courtTiles[i].color=nation.court[i]==1?Allied:nation.court[i]==-1?Opposed:Tossup; courtLabels[i].text=$"Seat {i+1}\n"+(nation.court[i]==1?"Aligned":nation.court[i]==-1?"Opposed":"Independent"); }
            var starting=new[]{campaign.startingSupport,campaign.startingSupport,campaign.startingSupport,campaign.startingSupport};
            for(int i=0;i<mapTiles.Length;i++) { float margin=nation.Margin(i,starting); mapTiles[i].color=Mathf.Abs(margin)<3?Tossup:margin>=0?Allied:Opposed; }
            mapSummary.text=$"Projected electoral votes: {nation.ProjectedVotes(starting)}";
            Place(mapDetail.rectTransform,24,427,342,50); mapDetail.text="Selected state details\nSupport bar preview";
            supportBar.SetActive(page==0); supportLabel.gameObject.SetActive(page==0); StateSupportFill.fillAmount=.5f; supportLabel.text="Projected support: 50.0%"; supportLabel.color=mapDetail.color;
            footnote.text="Editor preview · No campaign state changed";
        }
        public void Open(int page,bool election)
        {
            if(game.State==null || game.IsTransitioning) return;
            game.SetHelp(false); game.CancelInteraction();
            results=election || game.State.ElectionPending;
            selectedState=-1;
            policyPage=0;
            if(results && (Night==null || Night.result!=game.State.lastElection)) Night=new ElectionNight(game.State.lastElection,game.State.nation.states);
            tab=results && !Night.Complete?0:page;
            if(tab==3) { game.State.UnreadPolicies=false; RefreshPolicyNotice(); game.frontMenu?.Save(); }
            gameObject.SetActive(true); Render();
        }
        public void ResetElectionNight() { Night=null; results=false; }
        public void RefreshPolicyNotice() { if(game!=null && game.State!=null && navigation.Length>3) navigation[3].GetComponentInChildren<TMP_Text>().text=game.State.UnreadPolicies?"Policies *":"Policies"; }
        void Update() { if(game!=null && (game.frontMenu==null || !game.frontMenu.IsOpen)) AdvanceElectionNight(Time.unscaledDeltaTime); }
        public void AdvanceElectionNight(float seconds)
        { if(results && Night!=null && !Night.Complete && Night.Advance(seconds)) Render(); }
        public void Close()
        {
            if(results) return;
            gameObject.SetActive(false);
        }
        public void OpenElection() => Open(0,true);
        void Action()
        {
            if(!results && tab==3) { policyPage=(policyPage+1)%Mathf.Max(1,(game.State.PolicyHistory.Count+2)/3); Render(); return; }
            if(results)
            {
                if(!Night.Complete) { Night.Finish(); Render(); return; }
                if(tab==0) {tab=1; Render();}
                else { game.State.AcknowledgeElection(); results=false; gameObject.SetActive(false); game.Refresh(); game.frontMenu?.Save(); }
                return;
            }
            if(tab==2) { game.State.nation.Nominate(); Render(); game.frontMenu?.Save(); }
        }
        void Render()
        {
            StateSupportFill.color=Allied; supportBar.GetComponent<Image>().color=Opposed;
            var state=game.State; var nation=state.nation; var election=state.lastElection;
            bool white=state.IsElectionYear;
            background.color=white?new Color(.98f,.975f,.95f):new Color(.13f,.18f,.19f);
            Color ink=white?new Color(.10f,.15f,.16f):new Color(.96f,.93f,.83f);
            foreach(var text in new[]{title,subtitle,mapSummary,mapDetail,houseText,senateText,congressNote,courtSummary,footnote}) text.color=ink;
            mapGroup.SetActive(tab==0); congressGroup.SetActive(tab==1); courtGroup.SetActive(tab==2);
            policiesGroup.SetActive(tab==3);
            supportBar.SetActive(false); supportLabel.gameObject.SetActive(false);
            Place(mapDetail.rectTransform,24,437,342,80);
            closeButton.gameObject.SetActive(!results);
            title.text=tab==0 ? (results?"Election results":"Election map") : tab==1?"Congress":tab==2?"Supreme Court":"Policy record";
            subtitle.text=results ? $"November {election.year} · "+(election.presidential?"Presidential election":"Midterm election") : state.DisplayMonth.ToString("MMMM yyyy");
            footnote.text=tab==2?"Policy reviews need 5 aligned justices.":"Fictional game simulation";
            if(tab==3)
            {
                policiesText.color=ink;
                var policies=state.PolicyHistory; int pages=Mathf.Max(1,(policies.Count+2)/3);
                policyPage=Mathf.Clamp(policyPage,0,pages-1);
                policiesText.text=policies.Count==0?"No policies enacted yet.\n\nPass a policy through an event to add it here.\n\nLaws stay in place across elections until a repeal event succeeds.":"";
                for(int i=policyPage*3;i<Mathf.Min(policies.Count,policyPage*3+3);i++)
                {
                    var p=policies[i];
                    string actor=p.action=="Court struck down"?"Supreme Court":p.party.ToString();
                    string color=p.party==PartyTeam.Democrat?(white?"285CA0":"83B8FF"):(white?"A93229":"FF9A87");
                    bool active=false; foreach(var law in state.Policies) if(law.Id==p.id) active=true;
                    policiesText.text+=$"<b>{PolicyLedger.Title(p.id)}</b>\n<color=#{color}><size=15>{actor} · {p.action}\n{new System.DateTime(p.date):MMM yyyy} · {(active?"Currently active":"Not in force")}</size></color>\n{PolicyLedger.Description(p.id)}\n\n";
                }
                subtitle.text=$"{state.Policies.Count} active · Page {policyPage+1} of {pages}";
                footnote.text="Blue: Democrats · Red: Republicans\nNewest policy changes first";
            }
            if(tab==0)
            {
                int votes=results?election.electoralVotes:nation.ProjectedVotes(state.support);
                for(int i=0;i<mapTiles.Length;i++)
                {
                    float margin=results?election.margins[i]:nation.Margin(i,state.support);
                    mapTiles[i].color=!results && Mathf.Abs(margin)<3?Tossup:margin>=0?Allied:Opposed;
                }
                for(int i=0;i<stateShortcuts.Length;i++) stateShortcuts[i].GetComponent<Image>().color=mapTiles[shortcutStates[i]].color;
                mapSummary.text=$"You {votes}  ·  Opposition {538-votes}";
                mapDetail.text=results
                    ? $"House: {election.houseSeats}/435\nSenate: {election.senateSeats}/100\n"+(election.presidential?(votes>=270?"Your coalition wins.":"Opposition wins."):"Congress has been elected.")
                    : "Blue: Democrats · Red: Republicans\nGray: close race\nTap a state for its outlook.";
                if(results && !election.presidential) mapSummary.text="Presidential outlook";
            }
            if(tab==1)
            {
                int house=results?election.houseSeats:nation.houseSeats, senate=results?election.senateSeats:nation.SenateSeats;
                houseText.text=$"House · {house} / 435 yours"; houseFill.fillAmount=house/435f;
                senateText.text=$"Senate · {senate} / 100 yours"; senateFill.fillAmount=senate/100f;
                HouseChart.coalitionColor=SenateChart.coalitionColor=Allied; HouseChart.oppositionColor=SenateChart.oppositionColor=Opposed; HouseChart.Present(435,house); SenateChart.Present(100,senate);
                congressNote.text="Each dot is one seat. Blue: Democrats · Red: Republicans\n"+
                    $"House majority: 218 · Senate majority: 51\n"+(house>=218 && senate>=51?"You control both chambers.":"You need a deal across the aisle.");
                if(!nation.HoldsPresidency) congressNote.text=$"Opposition can block with the House or 41 senators.\nBills blocked by your party: {nation.BlockedGovernmentBills}\nYour party versus the government";
            }
            if(tab==2)
            {
                for(int i=0;i<9;i++)
                {
                    int stance=nation.court[i];
                    courtTiles[i].color=stance==1?Allied:stance==-1?Opposed:Tossup;
                    courtLabels[i].text=$"Seat {i+1}\n"+(stance==2?"Vacant":stance==1?"Aligned":stance==0?"Independent":"Opposed");
                }
                courtSummary.text=$"{nation.AlignedJustices} of 9 justices aligned\n\n"+(nation.Vacancy<0?"No vacancies right now.":!nation.HoldsPresidency?"Your party is in opposition.\nWin the presidency to nominate.":nation.SenateSeats>=51?"A vacancy is open.\nThe Senate can confirm your nominee.":"A vacancy is open.\nYou need a Senate majority.");
            }
            bool nomination=tab==2 && nation.Vacancy>=0;
            actionButton.gameObject.SetActive(results||nomination||(tab==3 && state.PolicyHistory.Count>3));
            actionButton.interactable=results || tab==3 || (nation.SenateSeats>=51 && nation.HoldsPresidency);
            actionLabel.text=results ? (tab==0?"View Congress":"Continue as the party") : tab==3?"Next page":"Nominate a justice";
            if(results && tab==0) RenderElectionNight();
            if(tab==0 && selectedState>=0) SelectState(selectedState);
        }
        void BuildPolicies()
        {
            if(policiesGroup!=null && policiesText!=null && navigation.Length==4) return;
            var policyButton=Instantiate(navigation[0],navigation[0].transform.parent); policyButton.name="Policies";
            policyButton.transform.SetSiblingIndex(navigation[2].transform.GetSiblingIndex()+1);
            policyButton.onClick.RemoveAllListeners(); policyButton.GetComponentInChildren<TMP_Text>().text="Policies";
            navigation=new[]{navigation[0],navigation[1],navigation[2],policyButton};
            for(int i=0;i<navigation.Length;i++) { Place((RectTransform)navigation[i].transform,20+i*89,582,83,54); var label=navigation[i].GetComponentInChildren<TMP_Text>(); label.fontSize=16; Place(label.rectTransform,4,6,75,42); }
            policiesGroup=NewRect(transform,"Policies",0,0,390,520).gameObject;
            policiesText=NewRect(policiesGroup.transform,"Policy list",24,116,342,395).gameObject.AddComponent<TextMeshProUGUI>();
            policiesText.font=mapDetail.font; policiesText.fontSharedMaterial=mapDetail.fontSharedMaterial; policiesText.fontSize=18; policiesText.raycastTarget=false;
            policiesText.textWrappingMode=TextWrappingModes.Normal;
        }
        void BuildDetailGraphics()
        {
            if(HouseChart!=null && SenateChart!=null && StateSupportFill!=null && supportLabel!=null) return;
            foreach(string name in new[]{"House Opposition","House Coalition","Senate Opposition","Senate Coalition"})
            { var old=congressGroup.transform.Find(name); if(old!=null) old.gameObject.SetActive(false); }
            Place(houseText.rectTransform,24,116,342,30); houseText.fontSize=22;
            Place(senateText.rectTransform,24,290,342,30); senateText.fontSize=22;
            Place(congressNote.rectTransform,24,461,342,60); congressNote.fontSize=14;
            HouseChart=NewRect(congressGroup.transform,"House hemicycle",24,151,342,127).gameObject.AddComponent<ChamberSeats>();
            SenateChart=NewRect(congressGroup.transform,"Senate hemicycle",24,325,342,127).gameObject.AddComponent<ChamberSeats>();
            HouseChart.raycastTarget=SenateChart.raycastTarget=false;
            var track=NewRect(mapGroup.transform,"State support bar",24,506,342,10).gameObject.AddComponent<Image>();
            track.color=Opposed; track.raycastTarget=false; supportBar=track.gameObject;
            StateSupportFill=NewRect(track.transform,"Your support",0,0,342,10).gameObject.AddComponent<Image>();
            StateSupportFill.color=Allied; StateSupportFill.raycastTarget=false;
            StateSupportFill.sprite=houseFill.sprite; StateSupportFill.type=Image.Type.Filled; StateSupportFill.fillMethod=Image.FillMethod.Horizontal;
            supportLabel=NewRect(mapGroup.transform,"State support percent",24,479,342,24).gameObject.AddComponent<TextMeshProUGUI>();
            supportLabel.font=mapDetail.font; supportLabel.fontSharedMaterial=mapDetail.fontSharedMaterial; supportLabel.fontSize=17;
            supportLabel.alignment=TextAlignmentOptions.Center; supportLabel.raycastTarget=false;
            var midpoint=NewRect(track.transform,"50 percent marker",170,0,2,10).gameObject.AddComponent<Image>(); midpoint.color=Color.white; midpoint.raycastTarget=false;
        }
        static RectTransform NewRect(Transform parent,string name,float x,float y,float width,float height)
        { var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform; rect.SetParent(parent,false); Place(rect,x,y,width,height); return rect; }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height); }
        void RenderElectionNight()
        {
            var nation=game.State.nation; bool presidential=Night.result.presidential;
            title.text=Night.Complete?"Final returns":"Election night";
            subtitle.text=$"November {Night.result.year} · "+(presidential?"Presidential election":"Midterm election");
            for(int i=0;i<mapTiles.Length;i++) mapTiles[i].color=!Night.called[i]?Tossup:Night.result.margins[i]>=0?Allied:Opposed;
            for(int i=0;i<stateShortcuts.Length;i++) stateShortcuts[i].GetComponent<Image>().color=mapTiles[shortcutStates[i]].color;
            mapSummary.text=presidential?$"You {Night.Votes}  ·  Opposition {Night.OppositionVotes}":$"House {Night.House}  ·  Opposition {Night.OppositionHouse}";
            string latest=Night.Latest<0?"Waiting for the first returns…":nation.states[Night.Latest].stateName+
                (presidential?": "+(Night.result.margins[Night.Latest]>=0?"you":"opposition")+" +"+nation.states[Night.Latest].electoralVotes+" EV":" reports");
            mapDetail.text=presidential
                ? latest+"\n"+Night.PresidentialCall
                : latest+$"\nSenate: you {Night.Senate} · opposition {Night.OppositionSenate}\n"+(Night.Complete?"All chamber totals confirmed.":"Chamber totals build as states report.");
            footnote.text=$"{Night.Count} / {Night.called.Length} reporting · Gray: awaiting returns";
            actionLabel.text=Night.Complete?"View Congress":"Skip to final results";
        }
        void SelectState(int index)
        {
            selectedState=index; supportBar.SetActive(false); supportLabel.gameObject.SetActive(false);
            Place(mapDetail.rectTransform,24,437,342,80);
            var state=game.State; var profile=state.nation.states[index];
            if(results && !Night.called[index]) { mapDetail.text=profile.stateName+"\nAwaiting returns.\nThis state has not reported yet."; return; }
            float margin=results?state.lastElection.margins[index]:state.nation.Margin(index,state.support);
            float percent=Mathf.Clamp(50+margin/2,0,100);
            Place(mapDetail.rectTransform,24,427,342,50);
            supportBar.SetActive(true); supportLabel.gameObject.SetActive(true); StateSupportFill.fillAmount=percent/100;
            supportLabel.color=mapDetail.color; supportLabel.text=(results?"Election support":"Projected support")+$": {percent:0.0}% · 50% to lead";
            if(!results && Mathf.Abs(state.nation.VoterSupportBonus(index))>.05f)
                supportLabel.text=$"Support {percent:0.0}% · Outreach {state.nation.VoterSupportBonus(index):+0.0;-0.0}pp";
            if(results && !Night.result.presidential)
            {
                mapDetail.text=profile.stateName+(profile.abbreviation=="DC"?"\nNo congressional seats":
                    $"\nHouse: {Night.result.houseByState[index]} / {profile.electoralVotes-2} · Senate: {Night.result.senateByState[index]} / 2");
                return;
            }
            string lead=Mathf.Abs(margin)<3&&!results?"Close race":margin>=0?"Your coalition leads":"Opposition leads";
            mapDetail.text=$"{profile.stateName} · {profile.electoralVotes} electoral votes\n{lead}";
        }
    }
}
