using System;
using System.Linq;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class CampaignChecks
    {
        public static void Run()
        {
            var config=ScriptableObject.CreateInstance<CampaignDefinition>(); config.decisionImpact=1;
            var nationConfig=ScriptableObject.CreateInstance<NationalDefinition>(); nationConfig.startingHouseSeats=222; nationConfig.startingSenateSeats=52; config.nation=nationConfig;
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
                card.left.change=new SupportChange(0,0,60,0); state=new CampaignState(config,1); state.Choose(false); Check(!state.ended && state.support[2]==100,"Full meter is not a loss");
                card.left.change=new SupportChange(); state=new CampaignState(config,1); Advance(state,12); Check(state.IsElectionYear,"White election year starts in January");
                Advance(state,11); Check(state.ElectionPending && !state.lastElection.presidential && state.lastElection.year==2026,"November midterm");
                int month=state.decisions; state.Choose(false); Check(state.decisions==month,"Election report blocks swipes");
                Check(state.lastElection.houseSeats>=0&&state.lastElection.houseSeats<=435&&state.lastElection.senateSeats<=100,"Congress bounds");
                state.AcknowledgeElection(); Check(!state.ElectionPending && state.CurrentMonth.Month==12,"Election dismissal");
                config.startingSupport=95; state=new CampaignState(config,1); Advance(state,47);
                Check(state.ElectionPending && state.lastElection.presidential && state.lastElection.electoralVotes>=270 && !state.ended,"Presidential reelection");
                Advance(state,1); Check(state.Term==2 && state.CurrentMonth.Year==2029,"Second term starts in January");
                Advance(state,48); Check(!state.ended && state.decisions==96,"Party continues beyond two terms");
                config.startingSupport=30; state=new CampaignState(config,1); Advance(state,47); Check(!state.ended && !state.nation.HoldsPresidency && state.ElectionPending,"Election defeat continues in opposition");
                config.startingSupport=50; state=new CampaignState(config,1); Advance(state,18); Check(state.nation.Vacancy>=0 && state.nation.Nominate() && state.nation.Vacancy<0,"Court retirement and confirmation");
                card.left.followUp=followUp; state=new CampaignState(config,1); state.Choose(false); Check(state.current==followUp,"Follow-up"); card.left.followUp=null;
                card.oncePerRun=true; state=new CampaignState(config,1); state.Choose(false); Check(!state.ended && state.current==null,"One-shot deck has no eligible event");
                card.oncePerRun=false; card.earliestDecision=5; state=new CampaignState(config,1); Check(!state.ended && state.current==null,"Availability gating");
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
                card.condition=EventCondition.CourtVacancy; state=new CampaignState(config,1); Check(state.current==null,"Vacancy cards excluded without vacancy");
                card.condition=EventCondition.DividedCongress; state=new CampaignState(config,1); Check(state.current==null,"Divided Congress cards excluded with majority");
                card.condition=EventCondition.Always; card.left.institution=InstitutionRule.Congress; card.left.enactPolicy=PolicyId.SchoolMeals; card.left.change=new SupportChange();
                state=new CampaignState(config,1); state.Choose(false); Check(state.Policies.Count==1,"Event enacts policy");
                state.Choose(false); Check(state.Policies.Count==1,"Policy enactment is unique");
                state.nation.HoldsPresidency=false; state.Choose(false); Check(state.Policies.Count==1 && !state.ended,"Policy persists in opposition");
                state=new CampaignState(config,1); state.nation.houseSeats=200; state.Choose(false); Check(state.Policies.Count==0,"Blocked event cannot enact policy");
                state=new CampaignState(config,1); state.nation.HoldsPresidency=false; state.Choose(false); Check(state.Policies.Count==0,"Opposition cannot enact federal policy");
                state=new CampaignState(config,1); state.Choose(false); card.left.enactPolicy=PolicyId.None; card.left.repealPolicy=PolicyId.SchoolMeals;
                state.Choose(false); Check(state.Policies.Count==0,"Successful repeal event removes policy");
                card.left.repealPolicy=PolicyId.None; card.left.institution=InstitutionRule.None; config.startingSupport=30;
                state=new CampaignState(config,1); Advance(state,47); Check(!state.nation.HoldsPresidency && !state.ended,"Party survives losing presidency");
                for(int i=0;i<4;i++) state.support[i]=85; Advance(state,48);
                Check(state.nation.HoldsPresidency && !state.ended,"Party can return to power after defeat");
                Advance(state,145); Check(!state.ended && state.decisions==240,"Party can survive twenty years");
                var hardNation=new NationalState(null); Check(!hardNation.ControlsCongress,"Harder starting congressional minority");
                Check(hardNation.ProjectedVotes(new[]{50,50,50,50})<270,"Neutral support no longer guarantees presidency");
                config.cards.Clear(); state=new CampaignState(config,1); Check(!state.ended && state.current==null,"Empty deck is a configuration issue, not a loss");
                foreach(int approval in new[]{20,50,80}) foreach(int year in new[]{2026,2028})
                {
                    var nation=new NationalState(null);
                    var election=nation.Elect(year,new[]{approval,approval,approval,approval});
                    var night=new ElectionNight(election,nation.states);
                    Check(night.Count==0 && night.Votes==0 && night.House==0,"Night begins uncalled");
                    night.Advance(.5f); Check(night.Count==0,"Opening pause");
                    night.Advance(.51f); Check(night.Count==1,"First state arrives");
                    night.Advance(100);
                    Check(night.Complete && night.called.All(c=>c),"All states report naturally");
                    Check(night.Votes==election.electoralVotes && night.Votes+night.OppositionVotes==538,"Electoral returns reconcile");
                    Check(night.House==election.houseSeats && night.House+night.OppositionHouse==435,"House returns reconcile");
                    Check(night.Senate==election.senateSeats && night.Senate+night.OppositionSenate==100,"Senate returns reconcile");
                    var skipped=new ElectionNight(election,nation.states); skipped.Finish(); skipped.Finish();
                    Check(skipped.Votes==night.Votes && skipped.House==night.House && skipped.Count==51,"Skip cannot double-count results");
                }
                Debug.Log("Political Timeline: monthly calendar, power, elections, Congress, court and deck checks passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(card); UnityEngine.Object.DestroyImmediate(followUp); UnityEngine.Object.DestroyImmediate(config); UnityEngine.Object.DestroyImmediate(nationConfig); }
        }
        static void Advance(CampaignState state,int months)
        { for(int i=0;i<months && !state.ended;i++) { if(state.ElectionPending) state.AcknowledgeElection(); state.Choose(false); } }
        static void Check(bool pass,string test) { if(!pass) throw new Exception("Campaign check failed: "+test); }
    }
}
