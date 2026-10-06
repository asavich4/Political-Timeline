using System;
using System.Linq;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class StoryChecks
    {
        public static void Run(CampaignDefinition deck)
        {
            foreach(string id in new[]{"grid","care","nursery","presslaw","soil","ballot"})
            {
                var state=new CampaignState(deck,17);
                var start=deck.cards.Find(c=>c.id==id+"_start");
                var bill=deck.cards.Find(c=>c.id==id+"_bill");
                var local=deck.cards.Find(c=>c.id==id+"_local");
                var result=deck.cards.Find(c=>c.id==id+"_result");
                Check(start!=null && bill.followUpOnly && local.followUpOnly && result.followUpOnly,"Chapters only appear through their story");
                state.current=start; state.Choose(false); Check(state.current==bill,"Investigating opens policy proposal");
                state.nation.HoldsPresidency=true; state.nation.Elect(2026,new[]{95,95,95,95});
                state.Choose(false); Check(state.current==result && state.Policies.Any(p=>p.Id==bill.left.enactPolicy),"Passed bill unlocks enacted-policy aftermath");
                state=new CampaignState(deck,17); state.current=start; state.Choose(true); Check(state.current==local,"Alternate opening branches to local response");
                state=new CampaignState(deck,17); state.current=bill; state.nation.houseSeats=100; state.Choose(false);
                Check(state.current==local && state.Policies.Count==0,"Blocked bill uses local branch without enacting law");
                state=new CampaignState(deck,17); state.current=bill; state.nation.HoldsPresidency=false; state.Choose(false);
                Check(state.current==local && state.Policies.Count==0,"Opposition gets blocked branch");
                // Election-night interruption must not discard the next chapter.
                state=new CampaignState(deck,17); state.decisions=22; state.current=start; state.Choose(false);
                Check(state.ElectionPending && state.current==bill,"Story survives November election");
                state.AcknowledgeElection(); Check(state.current==bill,"Election dismissal resumes story");
            }
            for(int seed=0;seed<100;seed++) Check(!new CampaignState(deck,seed).current.followUpOnly,"Random draws cannot start in a later chapter");
            foreach(PolicyId id in Enum.GetValues(typeof(PolicyId))) if(id!=PolicyId.None)
                Check(!string.IsNullOrEmpty(PolicyLedger.Title(id)) && !string.IsNullOrEmpty(PolicyLedger.Description(id)),"Policy has ledger text");
            Debug.Log("Branching story checks passed: alternate choices, blocked votes, policy enactment, election interruption and random-draw isolation.");
        }
        static void Check(bool pass,string message) { if(!pass) throw new Exception("Story check failed: "+message); }
    }
}
