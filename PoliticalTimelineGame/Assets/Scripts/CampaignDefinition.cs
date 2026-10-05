using System.Collections.Generic;
using UnityEngine;

namespace PoliticalTimeline
{
    [CreateAssetMenu(menuName = "Political Timeline/Campaign")]
    public class CampaignDefinition : ScriptableObject
    {
        public List<DecisionCard> cards = new List<DecisionCard>();
        [Range(1, 99)] public int startingSupport = 50;
        [Min(1)] public int decisionsPerTerm = 16;
        [Range(1, 99)] public int electionThreshold = 45;
        [Min(1)] public int termLimit = 2;
    }
}
