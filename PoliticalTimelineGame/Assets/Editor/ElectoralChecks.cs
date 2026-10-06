using System;
using System.Linq;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class ElectoralChecks
    {
        public static void Run(CampaignDefinition deck)
        {
            var nation=new NationalState(null); int pa=Array.FindIndex(nation.states,s=>s.abbreviation=="PA"), ca=Array.FindIndex(nation.states,s=>s.abbreviation=="CA");
            int[] neutral={50,50,50,50}; float before=nation.Margin(pa,neutral), untouched=nation.Margin(ca,neutral);
            var effect=new PolicyChoice {voterStates=new[]{"PA"},voterSupportChange=4}; nation.ApplyVoterEffect(effect);
            Check(Math.Abs(nation.Margin(pa,neutral)-before-8)<.001f,"Four support points move the margin by eight");
            Check(nation.Margin(ca,neutral)==untouched,"Untargeted states do not move");
            Check(nation.Elect(2028,neutral).margins[pa]==nation.Margin(pa,neutral),"Local support reaches election results");
            nation.AdvanceMonth(1); Check(Math.Abs(nation.VoterSupportBonus(pa)-3.88f)<.001f,"Local gains fade gradually");
            for(int i=0;i<10;i++) nation.ApplyVoterEffect(effect);
            Check(nation.VoterSupportBonus(pa)==12,"Local support is bounded");
            effect.voterSupportChange=-12; for(int i=0;i<4;i++) nation.ApplyVoterEffect(effect);
            Check(nation.VoterSupportBonus(pa)==-12,"Negative local effects are bounded");
            var block=deck.cards.Find(c=>c.id=="opp_spending");
            var state=new CampaignState(deck,3); state.nation.HoldsPresidency=false; state.current=block; state.Choose(false);
            Check(state.nation.BlockedGovernmentBills==1 && state.nation.VoterSupportBonus(pa)==3,"Opposition block changes voters and records success");
            state=new CampaignState(deck,3); state.nation.HoldsPresidency=false;
            foreach(int year in new[]{2026,2028,2030}) state.nation.Elect(year,new[]{0,0,0,0});
            state.current=block; state.Choose(false);
            Check(state.nation.BlockedGovernmentBills==0 && state.nation.VoterSupportBonus(pa)==0,"Failed obstruction cannot claim success or votes");
            foreach(var card in deck.cards) foreach(var choice in new[]{card.left,card.right})
                foreach(string code in choice.voterStates??new string[0]) Check(nation.states.Any(s=>s.abbreviation==code),"Voter target is a real modeled state");
            int campaigns=0, oppositions=0;
            for(int seed=0;seed<20;seed++)
            {
                state=new CampaignState(deck,seed);
                for(int month=0;month<60;month++)
                {
                    if(state.ElectionPending) state.AcknowledgeElection();
                    Check(state.current!=null,"Expanded deck stays available");
                    if(state.current.condition==EventCondition.CampaignSeason) { campaigns++; Check(state.IsCampaignSeason,"Campaign cards only appear January-November in even years"); }
                    if(state.current.condition==EventCondition.InOpposition) { oppositions++; Check(!state.nation.HoldsPresidency,"Opposition cards only appear out of power"); }
                    for(int i=0;i<4;i++) state.support[i]=35;
                    state.Choose(month%2==0);
                }
            }
            Check(campaigns>0 && oppositions>0,"Simulation encounters both campaign and opposition content");
            Debug.Log("Electoral checks passed: season gating, localized voter effects, decay, caps, blocking, and election tallies.");
        }
        static void Check(bool pass,string message) { if(!pass) throw new Exception("Electoral check failed: "+message); }
    }
}
