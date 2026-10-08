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
        public string lastResult="Lead the party from behind the scenes. Keep your coalition alive.";
        readonly List<EnactedPolicy> policies=new List<EnactedPolicy>();
        public IReadOnlyList<EnactedPolicy> Policies => policies.AsReadOnly();
        public DecisionCard current;
        public ElectionResult lastElection;
        public bool ElectionPending { get; private set; }
        readonly CampaignDefinition definition;
        readonly Random random;
        readonly int seed;
        int draws;
        int startYear;
        public PartyTeam Party { get; private set; }
        readonly HashSet<DecisionCard> used=new HashSet<DecisionCard>();
        public DateTime CurrentMonth => new DateTime(startYear,1,1).AddMonths(decisions);
        public DateTime DisplayMonth => ElectionPending ? CurrentMonth.AddMonths(-1) : CurrentMonth;
        public bool IsElectionYear => DisplayMonth.Year%2==0;
        public bool IsCampaignSeason => CurrentMonth.Year%2==0 && CurrentMonth.Month<=11;
        public string ElectionYearLabel => !IsElectionYear?"":DisplayMonth.Year%4==0?"Presidential election year":"Midterm election year";
        public int Term => decisions/48+1;
        public int Approval => (support[0]+support[1]+support[2]+support[3])/4;

        public CampaignState(CampaignDefinition campaign,int seed,PartyTeam party=PartyTeam.Democrat)
        {
            this.seed=seed; Party=party;
            startYear=campaign.startYear;
            definition=campaign; random=new Random(seed); nation=new NationalState(campaign.nation,party);
            for(int i=0;i<4;i++) support[i]=campaign.startingSupport;
            Draw(null);
        }
        public void AcknowledgeElection() => ElectionPending=false;
        public void Choose(bool right)
        {
            if(ended || ElectionPending || current==null) return;
            var choice=right?current.right:current.left; used.Add(current);
            bool passed=nation.CanResolve(choice);
            if(passed && choice.institution==InstitutionRule.ConfirmJustice) nation.Nominate();
            var deltas=(passed?choice.change:choice.blockedChange).Values;
            for(int i=0;i<4;i++) support[i]=Math.Max(0,Math.Min(100,support[i]+deltas[i]));
            var month=CurrentMonth; decisions++; lastResult=passed?choice.consequence:choice.blockedConsequence;
            if(!passed && !nation.HoldsPresidency && (choice.institution==InstitutionRule.Congress || choice.institution==InstitutionRule.ConfirmJustice || choice.enactPolicy!=PolicyId.None || choice.repealPolicy!=PolicyId.None))
                lastResult="Your party is in opposition. The administration blocks your proposal.";
            if(passed)
            {
                if(choice.repealPolicy!=PolicyId.None) policies.RemoveAll(p=>p.Id==choice.repealPolicy);
                if(choice.enactPolicy!=PolicyId.None && !policies.Any(p=>p.Id==choice.enactPolicy)) policies.Add(new EnactedPolicy(choice.enactPolicy,month));
            }
            else if(choice.institution==InstitutionRule.CourtReview && choice.blockedRepealPolicy!=PolicyId.None)
                policies.RemoveAll(p=>p.Id==choice.blockedRepealPolicy);
            nation.AdvanceMonth(decisions);
            if(passed) nation.ApplyVoterEffect(choice);
            string[] losses={"A nation on strike","The center collapses","Economic collapse","The donors walk away"};
            for(int i=0;i<4;i++) if(support[i]==0) { Finish(losses[i]); return; }
            if(month.Month==11 && month.Year%2==0)
            {
                lastElection=nation.Elect(month.Year,support); ElectionPending=true;
                if(lastElection.presidential)
                {
                    nation.HoldsPresidency=lastElection.electoralVotes>=270;
                }
            }
            Draw(passed?choice.followUp:choice.blockedFollowUp);
        }
        void Draw(DecisionCard followUp)
        {
            if(followUp!=null) { current=followUp; return; }
            var eligible=definition.cards.Where(c=>c!=null && !c.followUpOnly && c.earliestDecision<=decisions+1 && nation.Allows(c.condition) && (c.condition!=EventCondition.CampaignSeason || IsCampaignSeason) && (nation.HoldsPresidency || (c.left.enactPolicy==PolicyId.None && c.right.enactPolicy==PolicyId.None)) && (c.requiredPolicy==PolicyId.None || policies.Any(p=>p.Id==c.requiredPolicy)) && (!c.oncePerRun||!used.Contains(c))).ToList();
            eligible.RemoveAll(c=>c.excludedPolicy!=PolicyId.None && policies.Any(p=>p.Id==c.excludedPolicy));
            if(eligible.Count>1) eligible.Remove(current);
            if(eligible.Count==0) { current=null; lastResult="No events are available. Check the campaign deck."; return; }
            int roll=random.Next(eligible.Sum(DrawWeight)); draws++;
            foreach(var card in eligible) { roll-=DrawWeight(card); if(roll<0) {current=card; return;} }
        }
        int DrawWeight(DecisionCard card) => Math.Max(1,card.weight)*(!nation.HoldsPresidency && card.condition==EventCondition.InOpposition?3:1);
        void Finish(string title) { ended=true; ending=title; }
        public CampaignSave Export(bool unread=false)
        {
            var save=new CampaignSave { party=(int)Party, seed=seed, draws=draws, decisions=decisions, support=(int[])support.Clone(), currentCard=current?.id,
                lastResult=lastResult, ended=ended, ending=ending, election=lastElection, electionPending=ElectionPending, unreadOutcome=unread,
                usedCards=used.Select(c=>c.id).ToArray(), policies=policies.Select(p=>(int)p.Id).ToArray(), policyDates=policies.Select(p=>p.Enacted.Ticks).ToArray() };
            save.startYear=startYear; nation.Export(save); return save;
        }
        public static CampaignState Restore(CampaignDefinition definition,CampaignSave save)
        {
            var state=new CampaignState(definition,save.seed,(PartyTeam)save.party);
            state.startYear=save.startYear;
            // One Random.Next call per draw, regardless of its upper bound.
            while(state.draws<save.draws) { state.random.Next(1); state.draws++; }
            state.decisions=save.decisions; Array.Copy(save.support,state.support,4);
            state.current=definition.cards.Find(c=>c.id==save.currentCard);
            if(state.current==null && !save.ended) throw new System.IO.InvalidDataException("The saved event is missing from this campaign.");
            state.lastResult=save.lastResult; state.ended=save.ended; state.ending=save.ending;
            state.lastElection=save.election; state.ElectionPending=save.electionPending;
            foreach(var id in save.usedCards) { var c=definition.cards.Find(card=>card.id==id); if(c!=null) state.used.Add(c); }
            for(int i=0;i<save.policies.Length;i++) state.policies.Add(new EnactedPolicy((PolicyId)save.policies[i],new DateTime(save.policyDates[i])));
            state.nation.Restore(save); return state;
        }
    }
}
