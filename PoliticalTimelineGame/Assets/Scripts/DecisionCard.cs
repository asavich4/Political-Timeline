using System;
using UnityEngine;

namespace PoliticalTimeline
{
    public enum InstitutionRule { None, Congress, CourtReview, ConfirmJustice, BlockGovernment }
    public enum EventCondition { Always, DividedCongress, CourtVacancy, InOpposition, CampaignSeason, InGovernment }
    public enum PortraitDesign { Default, Engineer, Medic, Farmer, Reporter, Teacher, Organizer }
    [Serializable]
    public struct SupportChange
    {
        public int workers, middleClass, elites;
        [UnityEngine.Serialization.FormerlySerializedAs("security")] public int economy;
        public SupportChange(int w, int m, int s, int e) { workers = w; middleClass = m; economy = s; elites = e; }
        public int[] Values => new[] { workers, middleClass, economy, elites };
    }

    [Serializable]
    public class PolicyChoice
    {
        public string label;
        [TextArea(2, 4)] public string consequence;
        public SupportChange change;
        public DecisionCard followUp;
        public DecisionCard blockedFollowUp;
        public InstitutionRule institution;
        public PolicyId enactPolicy;
        public PolicyId repealPolicy;
        public string[] voterStates = new string[0];
        [Range(-12,12)] public float voterSupportChange;
        public SupportChange blockedChange;
        [TextArea(2, 4)] public string blockedConsequence;
    }

    [CreateAssetMenu(menuName = "Political Timeline/Decision Card")]
    public class DecisionCard : ScriptableObject
    {
        public string id;
        public string advisor = "Chief of Staff";
        public string category = "DOMESTIC POLICY";
        public string headline = "A decision awaits";
        [TextArea(3, 6)] public string briefing;
        public Sprite portrait;
        public PortraitArtwork artwork;
        [Tooltip("Use the assigned Sprite instead of the geometric character.")] public bool usePortraitSprite;
        [Min(1)] public int earliestDecision = 1;
        [Min(1)] public int weight = 1;
        public bool oncePerRun;
        public EventCondition condition;
        public PolicyId requiredPolicy;
        public bool followUpOnly;
        public PortraitDesign design;
        public PolicyChoice left = new PolicyChoice();
        public PolicyChoice right = new PolicyChoice();
    }
}
