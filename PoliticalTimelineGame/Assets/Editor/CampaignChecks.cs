using System;
using System.Linq;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class CampaignChecks
    {
        public static void Run()
        {
            var config=ScriptableObject.CreateInstance<CampaignDefinition>();
            var card=ScriptableObject.CreateInstance<DecisionCard>();
            var followUp=ScriptableObject.CreateInstance<DecisionCard>();
            try
            {
                config.cards.Add(card); card.left.consequence=card.right.consequence="Recorded.";
                var state=new CampaignState(config,1);
                Check(state.CurrentMonth==new DateTime(2025,1,1),"January start");
                state.Choose(false); Check(state.CurrentMonth==new DateTime(2025,2,1),"One swipe is one month");
                Check(state.nation.states.Length==51 && state.nation.states.Sum(s=>s.electoralVotes)==538,"All states and DC / electoral total");
                Check(state.nation.states.Where(s=>s.abbreviation!="DC").Sum(s=>s.electoralVotes-2)==435,"House allocation");
                float before=state.nation.Margin(0,state.support); state.support[2]+=10;
                Check(state.nation.Margin(0,state.support)>before,"Economy changes state outlook");
                card.left.change=new SupportChange(-60,0,0,0); state=new CampaignState(config,1); state.Choose(false); Check(state.ended && state.support[0]==0,"Low support ending");
                card.left.change=new SupportChange(0,0,60,0); state=new CampaignState(config,1); state.Choose(false); Check(state.ending=="The economy overheats","Economy high ending");
                card.left.change=new SupportChange(); state=new CampaignState(config,1); Advance(state,12); Check(state.IsElectionYear,"White election year starts in January");
                Advance(state,11); Check(state.ElectionPending && !state.lastElection.presidential && state.lastElection.year==2026,"November midterm");
                int month=state.decisions; state.Choose(false); Check(state.decisions==month,"Election report blocks swipes");
                Check(state.lastElection.houseSeats>=0&&state.lastElection.houseSeats<=435&&state.lastElection.senateSeats<=100,"Congress bounds");
                state.AcknowledgeElection(); Check(!state.ElectionPending && state.CurrentMonth.Month==12,"Election dismissal");
                config.startingSupport=70; state=new CampaignState(config,1); Advance(state,47);
                Check(state.ElectionPending && state.lastElection.presidential && state.lastElection.electoralVotes>=270 && !state.ended,"Presidential reelection");
                Advance(state,1); Check(state.Term==2 && state.CurrentMonth.Year==2029,"Second term starts in January");
                Advance(state,48); Check(state.ending=="A legacy secured" && state.decisions==96,"Two full four-year terms");
                config.startingSupport=30; state=new CampaignState(config,1); Advance(state,47); Check(state.ending=="The voters choose change" && state.ElectionPending,"Election defeat still shows results");
                config.startingSupport=50; state=new CampaignState(config,1); Advance(state,18); Check(state.nation.Vacancy>=0 && state.nation.Nominate() && state.nation.Vacancy<0,"Court retirement and confirmation");
                card.left.followUp=followUp; state=new CampaignState(config,1); state.Choose(false); Check(state.current==followUp,"Follow-up"); card.left.followUp=null;
                card.oncePerRun=true; state=new CampaignState(config,1); state.Choose(false); Check(state.ended,"One-shot exhaustion");
                card.oncePerRun=false; card.earliestDecision=5; state=new CampaignState(config,1); Check(state.ended,"Availability gating");
                card.earliestDecision=1;
                card.left.institution=InstitutionRule.Congress; card.left.change=new SupportChange(7,0,0,0);
                card.left.blockedChange=new SupportChange(-3,0,0,0); card.left.blockedConsequence="Blocked.";
                state=new CampaignState(config,1); state.Choose(false); Check(state.support[0]==57,"Congress passes with both majorities");
                state=new CampaignState(config,1); state.nation.houseSeats=217; state.Choose(false);
                Check(state.support[0]==47 && state.lastResult=="Blocked.","Opposition House blocks bill with alternate effects");
                card.left.institution=InstitutionRule.CourtReview;
                state=new CampaignState(config,1); state.Choose(false); Check(state.support[0]==47,"Unaligned court rejects policy");
                state=new CampaignState(config,1); state.nation.court[4]=1; state.Choose(false); Check(state.support[0]==57,"Aligned court upholds policy");
                card.left.institution=InstitutionRule.ConfirmJustice;
                state=new CampaignState(config,1); state.nation.court[4]=2; state.Choose(false); Check(state.nation.Vacancy<0 && state.support[0]==57,"Card confirms justice");
                state=new CampaignState(config,1); state.Choose(false); Check(state.support[0]==47,"No vacancy cannot confirm");
                card.condition=EventCondition.CourtVacancy; state=new CampaignState(config,1); Check(state.ended,"Vacancy cards excluded without vacancy");
                card.condition=EventCondition.DividedCongress; state=new CampaignState(config,1); Check(state.ended,"Divided Congress cards excluded with majority");
                config.cards.Clear(); state=new CampaignState(config,1); Check(state.ended,"Empty deck");
                Debug.Log("Political Timeline: monthly calendar, power, elections, Congress, court and deck checks passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(card); UnityEngine.Object.DestroyImmediate(followUp); UnityEngine.Object.DestroyImmediate(config); }
        }
        static void Advance(CampaignState state,int months)
        { for(int i=0;i<months && !state.ended;i++) { if(state.ElectionPending) state.AcknowledgeElection(); state.Choose(false); } }
        static void Check(bool pass,string test) { if(!pass) throw new Exception("Campaign check failed: "+test); }
    }
}
