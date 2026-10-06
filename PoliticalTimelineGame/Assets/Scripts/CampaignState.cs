using System;
using System.Collections.Generic;
using System.Linq;

namespace PoliticalTimeline
{
    public sealed class CampaignState
    {
        public readonly int[] support=new int[4];
        public readonly NationalState nation;
        public int decisions;
        public bool ended;
        public string ending;
        public string lastResult="Your first month in office begins.";
        public DecisionCard current;
        public ElectionResult lastElection;
        public bool ElectionPending { get; private set; }
        readonly CampaignDefinition definition;
        readonly Random random;
        readonly HashSet<DecisionCard> used=new HashSet<DecisionCard>();
        public DateTime CurrentMonth => new DateTime(definition.startYear,1,1).AddMonths(decisions);
        public DateTime DisplayMonth => ElectionPending ? CurrentMonth.AddMonths(-1) : CurrentMonth;
        public bool IsElectionYear => DisplayMonth.Year%2==0;
        public int Term => Math.Min(decisions/48+1,definition.termLimit);
        public int Approval => (support[0]+support[1]+support[2]+support[3])/4;

        public CampaignState(CampaignDefinition campaign,int seed)
        {
            definition=campaign; random=new Random(seed); nation=new NationalState(campaign.nation);
            for(int i=0;i<4;i++) support[i]=campaign.startingSupport;
            Draw(null);
        }
        public void AcknowledgeElection() => ElectionPending=false;
        public void Choose(bool right)
        {
            if(ended || ElectionPending || current==null) return;
            var choice=right?current.right:current.left; used.Add(current);
            bool passed=nation.CanResolve(choice.institution);
            if(passed && choice.institution==InstitutionRule.ConfirmJustice) nation.Nominate();
            var deltas=(passed?choice.change:choice.blockedChange).Values;
            for(int i=0;i<4;i++) support[i]=Math.Max(0,Math.Min(100,support[i]+deltas[i]));
            var month=CurrentMonth; decisions++; lastResult=passed?choice.consequence:choice.blockedConsequence;
            nation.AdvanceMonth(decisions);
            string[] losses={"A nation on strike","The center collapses","Economic collapse","The donors walk away"};
            string[] excesses={"A movement beyond your control","A mandate without limits","The economy overheats","A captured presidency"};
            for(int i=0;i<4;i++) if(support[i]==0||support[i]==100) { Finish(support[i]==0?losses[i]:excesses[i]); return; }
            if(month.Month==11 && month.Year%2==0)
            {
                lastElection=nation.Elect(month.Year,support); ElectionPending=true;
                if(lastElection.presidential && Term<definition.termLimit)
                {
                    if(lastElection.electoralVotes<270) { Finish("The voters choose change"); return; }
                    lastResult="Reelected. Your coalition earns another term.";
                }
            }
            if(decisions>=48*definition.termLimit) { Finish("A legacy secured"); return; }
            Draw(choice.followUp);
        }
        void Draw(DecisionCard followUp)
        {
            if(followUp!=null) { current=followUp; return; }
            var eligible=definition.cards.Where(c=>c!=null && c.earliestDecision<=decisions+1 && nation.Allows(c.condition) && (!c.oncePerRun||!used.Contains(c))).ToList();
            if(eligible.Count>1) eligible.Remove(current);
            if(eligible.Count==0) { Finish("The briefing deck is exhausted"); return; }
            int roll=random.Next(eligible.Sum(c=>Math.Max(1,c.weight)));
            foreach(var card in eligible) { roll-=Math.Max(1,card.weight); if(roll<0) {current=card; return;} }
        }
        void Finish(string title) { ended=true; ending=title; }
    }
}
