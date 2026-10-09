using System.Collections.Generic;
using UnityEngine;

namespace PoliticalTimeline
{
    [CreateAssetMenu(menuName = "Political Timeline/Campaign")]
    public class CampaignDefinition : ScriptableObject
    {
        public List<DecisionCard> cards = new List<DecisionCard>();
        [Range(1, 99)] public int startingSupport = 50;
        [Min(2001)] public int startYear = 2025;
        public NationalDefinition nation;
        [Range(1,3)] public float decisionImpact=1.75f;
        [HideInInspector] public int termLimit = 2; // Legacy asset data; party campaigns have no term limit.
    }
}
