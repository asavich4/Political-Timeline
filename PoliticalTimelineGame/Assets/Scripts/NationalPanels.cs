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
        public bool IsOpen => gameObject.activeSelf;
        static readonly Color Allied=new Color(.12f,.43f,.48f), Opposed=new Color(.73f,.30f,.22f), Tossup=new Color(.50f,.49f,.43f);

        public void Initialize(PresidencyGame owner)
        {
            if(bound) return; bound=true; game=owner;
            for(int i=0;i<navigation.Length;i++) { int index=i; navigation[i].onClick.AddListener(()=>Open(index,false)); }
            for(int i=0;i<mapButtons.Length;i++) { int index=i; mapButtons[i].onClick.AddListener(()=>SelectState(index)); }
            for(int i=0;i<stateShortcuts.Length;i++) { int index=shortcutStates[i]; stateShortcuts[i].onClick.AddListener(()=>SelectState(index)); }
            closeButton.onClick.AddListener(Close); actionButton.onClick.AddListener(Action);
        }
        public void Open(int page,bool election)
        {
            if(game.State==null) return;
            game.SetHelp(false); game.CancelInteraction();
            tab=page; results=election || game.State.ElectionPending; gameObject.SetActive(true); Render();
        }
        public void Close()
        {
            if(results) return;
            gameObject.SetActive(false);
        }
        public void OpenElection() => Open(0,true);
        void Action()
        {
            if(results)
            {
                if(tab==0) {tab=1; Render();}
                else { game.State.AcknowledgeElection(); results=false; gameObject.SetActive(false); game.Refresh(); }
                return;
            }
            if(tab==2) { game.State.nation.Nominate(); Render(); }
        }
        void Render()
        {
            var state=game.State; var nation=state.nation; var election=state.lastElection;
            bool white=state.IsElectionYear;
            background.color=white?new Color(.98f,.975f,.95f):new Color(.13f,.18f,.19f);
            Color ink=white?new Color(.10f,.15f,.16f):new Color(.96f,.93f,.83f);
            foreach(var text in new[]{title,subtitle,mapSummary,mapDetail,houseText,senateText,congressNote,courtSummary,footnote}) text.color=ink;
            mapGroup.SetActive(tab==0); congressGroup.SetActive(tab==1); courtGroup.SetActive(tab==2);
            closeButton.gameObject.SetActive(!results);
            title.text=tab==0 ? (results?"Election results":"Election map") : tab==1?"Congress":"Supreme Court";
            subtitle.text=results ? $"November {election.year} · "+(election.presidential?"Presidential election":"Midterm election") : state.DisplayMonth.ToString("MMMM yyyy");
            footnote.text=tab==2?"Policy reviews need 5 aligned justices.":"Fictional game simulation";
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
                    : "Teal: you · Red: opposition\nGray: close race\nTap a state for its outlook.";
                if(results && !election.presidential) mapSummary.text="Presidential outlook";
            }
            if(tab==1)
            {
                int house=results?election.houseSeats:nation.houseSeats, senate=results?election.senateSeats:nation.SenateSeats;
                houseText.text=$"House\n{house} / 435 seats"; houseFill.fillAmount=house/435f;
                senateText.text=$"Senate\n{senate} / 100 seats"; senateFill.fillAmount=senate/100f;
                congressNote.text=(house>=218?"You hold the House.":"Opposition holds the House.")+"\n"+(senate>=51?"You hold the Senate.":"No Senate majority.")+"\n\nBills need both majorities.\nCourt nominees need 51 senators.";
            }
            if(tab==2)
            {
                for(int i=0;i<9;i++)
                {
                    int stance=nation.court[i];
                    courtTiles[i].color=stance==1?Allied:stance==-1?Opposed:Tossup;
                    courtLabels[i].text=$"Seat {i+1}\n"+(stance==2?"Vacant":stance==1?"Aligned":stance==0?"Independent":"Opposed");
                }
                courtSummary.text=$"{nation.AlignedJustices} of 9 justices aligned\n\n"+(nation.Vacancy<0?"No vacancies right now.":nation.SenateSeats>=51?"A vacancy is open.\nThe Senate can confirm your nominee.":"A vacancy is open.\nYou need a Senate majority.");
            }
            bool nomination=tab==2 && nation.Vacancy>=0;
            actionButton.gameObject.SetActive(results||nomination);
            actionButton.interactable=results || nation.SenateSeats>=51;
            actionLabel.text=results ? (tab==0?"View Congress":"Continue") : "Nominate a justice";
        }
        void SelectState(int index)
        {
            var state=game.State; var profile=state.nation.states[index];
            float margin=results?state.lastElection.margins[index]:state.nation.Margin(index,state.support);
            string lead=Mathf.Abs(margin)<3&&!results?"Close race":margin>=0?"Your coalition leads":"Opposition leads";
            mapDetail.text=$"{profile.stateName}\n{profile.electoralVotes} electoral votes\n{lead} · {Mathf.Abs(margin):0.0} point margin";
        }
    }
}
