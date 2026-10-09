using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class OppositionContent
    {
        public static void Ensure(CampaignDefinition deck)
        {
            foreach(var policy in new[]{PolicyId.SurveillancePowers,PolicyId.TradeAgreement,PolicyId.EnergyPriceCap,PolicyId.PublicHousing,PolicyId.CleanAir,PolicyId.SchoolMeals})
            {
                AddLaw(deck,policy,false);
                AddLaw(deck,policy,true);
            }
            Recovery(deck,"fundraiser","Donors offer chocolate fountains. And demands.","Take small donations",new SupportChange(3,1,2,4),"Work the donor tables",new SupportChange(-3,-2,3,7));
            Recovery(deck,"volunteers","Even our mascot wants a day off. Rest the volunteers?","Give the team a break",new SupportChange(4,3,2,-2),"Recruit business sponsors",new SupportChange(-2,1,5,4));
            Recovery(deck,"budget","Our campaign bus is eating our budget.","Trade it for a minivan",new SupportChange(-2,2,6,2),"Crowdfund the bus",new SupportChange(4,2,-2,4));
            Recovery(deck,"local_allies","Shopkeepers offer meeting rooms and strong coffee.","Build a local alliance",new SupportChange(2,4,4,-1),"Invite major backers",new SupportChange(-2,-1,5,5));
        }
        static DecisionCard Create(CampaignDefinition deck,string id)
        {
            string path=PresidencyContent.Root+"/Cards/"+id+".asset";
            var old=AssetDatabase.LoadAssetAtPath<DecisionCard>(path);
            if(old!=null) { if(!deck.cards.Contains(old)) deck.cards.Add(old); return null; }
            var card=ScriptableObject.CreateInstance<DecisionCard>();
            card.id=id; card.condition=EventCondition.InOpposition; card.weight=4;
            card.advisor="Opposition Leader"; card.portrait=CharacterCastEditor.ForAdvisor(card.advisor);
            if(card.portrait==null) card.portrait=CharacterCastEditor.ForAdvisor("Campaign Organizer");
            card.usePortraitSprite=true; card.castRevision=1; card.category="OPPOSITION";
            AssetDatabase.CreateAsset(card,path); deck.cards.Add(card); return card;
        }
        static void AddLaw(CampaignDefinition deck,PolicyId policy,bool repeal)
        {
            var card=Create(deck,"rival_"+(repeal?"repeal_":"enact_")+policy); if(card==null) return;
            card.headline=card.briefing="They want to "+(repeal?"repeal ":"pass ")+PolicyLedger.Title(policy).ToLowerInvariant()+". Block it?";
            if(repeal) card.requiredPolicy=policy; else card.excludedPolicy=policy;
            card.left=new PolicyChoice {label="Organize a blockade",institution=InstitutionRule.BlockGovernment,
                consequence="Your party blocks the government's bill. The law stays unchanged.",change=new SupportChange(3,2,-2,-3),
                blockedConsequence="The government has the votes to proceed.",blockedChange=new SupportChange(-3,-2,1,1)};
            card.right=new PolicyChoice {label="Campaign against it",consequence="Your organizers take the fight to voters.",change=new SupportChange(2,3,-1,-2),
                voterStates=new[]{"PA","WI","MI","AZ","GA","NC"},voterSupportChange=2};
            foreach(var choice in new[]{card.left,card.right})
            { if(repeal) choice.rivalRepealPolicy=policy; else choice.rivalEnactPolicy=policy; }
            EditorUtility.SetDirty(card);
        }
        static void Recovery(CampaignDefinition deck,string id,string body,string left,SupportChange a,string right,SupportChange b)
        {
            var card=Create(deck,"rebuild_"+id); if(card==null) return;
            card.weight=8; card.headline=card.briefing=body;
            card.left=new PolicyChoice {label=left,change=a,consequence="The party regroups. Your next campaign has a stronger foundation."};
            card.right=new PolicyChoice {label=right,change=b,consequence="New backing keeps the party in the fight. Not everyone likes the bargain."};
            EditorUtility.SetDirty(card);
        }
    }
}
