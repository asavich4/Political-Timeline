using System;
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
                config.cards.Add(card); config.decisionsPerTerm=2;
                card.left.consequence=card.right.consequence="Recorded.";
                var state=new CampaignState(config,1); Check(state.support[0]==50,"Starting support");
                card.left.change=new SupportChange(-60,0,0,0); state.Choose(false); Check(state.ended && state.support[0]==0,"Low support ending and clamp");
                card.left.change=new SupportChange(60,0,0,0); state=new CampaignState(config,1); state.Choose(false); Check(state.ended && state.support[0]==100,"High support ending and clamp");
                card.left.change=new SupportChange(); state=new CampaignState(config,1); state.Choose(false); state.Choose(false); Check(!state.ended && state.Term==2,"Reelection"); state.Choose(false); state.Choose(false); Check(state.ended && state.ending=="A legacy secured","Term limit victory");
                config.electionThreshold=60; state=new CampaignState(config,1); state.Choose(false); state.Choose(false); Check(state.ending=="The voters choose change","Election defeat");
                card.left.followUp=followUp; state=new CampaignState(config,1); state.Choose(false); Check(state.current==followUp,"Branching follow-up"); card.left.followUp=null;
                card.oncePerRun=true; state=new CampaignState(config,1); state.Choose(false); Check(state.ended && state.ending=="The briefing deck is exhausted","One-shot card exhaustion");
                card.oncePerRun=false; card.earliestDecision=5; state=new CampaignState(config,1); Check(state.ended,"Availability gating");
                config.cards.Clear(); state=new CampaignState(config,1); Check(state.ended,"Empty deck");
                Debug.Log("Political Timeline: 9 campaign rule checks passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(card); UnityEngine.Object.DestroyImmediate(followUp); UnityEngine.Object.DestroyImmediate(config); }
        }
        static void Check(bool pass,string test) { if(!pass) throw new Exception("Campaign check failed: "+test); }
    }
}
