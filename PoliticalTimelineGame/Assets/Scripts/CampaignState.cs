using System;
using System.Collections.Generic;
using System.Linq;

namespace PoliticalTimeline
{
    // Pure campaign rules: shared by the game and the editor's simulation checks.
    public sealed class CampaignState
    {
        public readonly int[] support = new int[4];
        public int decisions;
        public bool ended;
        public string ending;
        public string lastResult = "Your inauguration is over. The first briefing is on your desk.";
        public DecisionCard current;
        readonly CampaignDefinition definition;
        readonly Random random;
        readonly HashSet<DecisionCard> used = new HashSet<DecisionCard>();
        public int Term => Math.Min(decisions / Math.Max(1, definition.decisionsPerTerm) + 1, definition.termLimit);
        public int Approval => (support[0] + support[1] + support[2] + support[3]) / 4;

        public CampaignState(CampaignDefinition campaign, int seed)
        {
            definition = campaign;
            random = new Random(seed);
            for (int i = 0; i < 4; i++) support[i] = campaign.startingSupport;
            Draw(null);
        }

        public void Choose(bool right)
        {
            if (ended || current == null) return;
            var choice = right ? current.right : current.left;
            used.Add(current);
            var deltas = choice.change.Values;
            for (int i = 0; i < 4; i++) support[i] = Math.Max(0, Math.Min(100, support[i] + deltas[i]));
            decisions++;
            lastResult = choice.consequence;
            string[] losses = { "A nation on strike", "The center collapses", "A cabinet revolt", "The donors walk away" };
            string[] excesses = { "A movement beyond your control", "A mandate without limits", "Emergency rule", "A captured presidency" };
            for (int i = 0; i < 4; i++)
                if (support[i] == 0 || support[i] == 100)
                { Finish(support[i] == 0 ? losses[i] : excesses[i]); return; }
            if (decisions % Math.Max(1, definition.decisionsPerTerm) == 0)
            {
                if (decisions >= definition.decisionsPerTerm * definition.termLimit)
                { Finish("A legacy secured"); return; }
                if (Approval < definition.electionThreshold)
                { Finish("The voters choose change"); return; }
                lastResult += "\nREELECTED. Your coalition has earned another term.";
            }
            Draw(choice.followUp);
        }

        void Draw(DecisionCard followUp)
        {
            if (followUp != null) { current = followUp; return; }
            var eligible = definition.cards.Where(c => c != null && c.earliestDecision <= decisions + 1 && (!c.oncePerRun || !used.Contains(c))).ToList();
            if (eligible.Count > 1) eligible.Remove(current);
            if (eligible.Count == 0) { Finish("The briefing deck is exhausted"); return; }
            int roll = random.Next(eligible.Sum(c => Math.Max(1, c.weight)));
            foreach (var card in eligible)
            { roll -= Math.Max(1, card.weight); if (roll < 0) { current = card; return; } }
        }

        void Finish(string title) { ended = true; ending = title; }
    }
}
