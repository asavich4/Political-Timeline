using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class CampaignRaceContent
    {
        public static void Ensure(CampaignDefinition deck)
        {
            foreach(var race in new[]{CampaignRace.Presidency,CampaignRace.House,CampaignRace.Senate})
            {
                Add(deck,race,"lakes","Great Lakes",new[]{"MI","WI","PA"});
                Add(deck,race,"sunbelt","Sun Belt",new[]{"AZ","NV","GA","NC"});
                Add(deck,race,"suburbs","suburbs",new[]{"VA","PA","NH","CO"});
                Add(deck,race,"heartland","heartland",new[]{"OH","IA","MN","KS"});
            }
            Funds(deck,"small_donors","Our campaign needs cash. Ask ordinary voters?","Launch a small-donor drive",new SupportChange(3,2,3,4),"Hold a donor dinner",new SupportChange(-3,-2,4,7));
            Funds(deck,"bus_budget","The campaign bus broke down. Again.","Rent a smaller van",new SupportChange(-1,2,6,2),"Ask volunteers for rides",new SupportChange(4,2,3,-2));
            Funds(deck,"volunteer_rest","Even our campaign mascot needs a break.","Rest the volunteers",new SupportChange(5,3,2,-1),"Recruit local shopkeepers",new SupportChange(1,4,4,3));
            Funds(deck,"debate_snacks","Our debate watch party spent everything on snacks.","Sell the leftover popcorn",new SupportChange(2,1,5,3),"Turn it into a fundraiser",new SupportChange(-1,3,3,5));
        }
        static DecisionCard Create(CampaignDefinition deck,string id)
        {
            string path=PresidencyContent.Root+"/Cards/"+id+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<DecisionCard>(path);
            if(existing!=null) { if(!deck.cards.Contains(existing)) deck.cards.Add(existing); return null; }
            var c=ScriptableObject.CreateInstance<DecisionCard>(); c.id=id; c.condition=EventCondition.CampaignSeason; c.weight=5;
            c.advisor="Campaign Organizer"; c.category="CAMPAIGN"; c.portrait=CharacterCastEditor.ForAdvisor(c.advisor); c.castRevision=1;
            AssetDatabase.CreateAsset(c,path); deck.cards.Add(c); return c;
        }
        static void Add(CampaignDefinition deck,CampaignRace race,string region,string label,string[] states)
        {
            var c=Create(deck,"race_"+race+"_"+region); if(c==null) return;
            c.presidentialCampaignOnly=race==CampaignRace.Presidency;
            c.category=race==CampaignRace.Presidency?"PRESIDENTIAL CAMPAIGN":race.ToString().ToUpperInvariant()+" CAMPAIGN";
            c.headline=c.briefing=(race==CampaignRace.Presidency?"Our presidential ticket":race==CampaignRace.House?"Our House candidates":"Our Senate candidates")+" needs help in the "+label+".";
            c.left=new PolicyChoice {label="Send the field teams",change=new SupportChange(3,2,-3,-4),campaignRace=race,voterStates=states,voterSupportChange=5,
                consequence="Field teams build support for our "+race.ToString().ToLowerInvariant()+" campaign."};
            c.right=new PolicyChoice {label="Fund local organizers",change=new SupportChange(1,3,-1,3),campaignRace=race,voterStates=states,voterSupportChange=2,
                consequence="Local organizers gain support and keep campaign funds coming in."};
            EditorUtility.SetDirty(c);
        }
        static void Funds(CampaignDefinition deck,string id,string body,string left,SupportChange a,string right,SupportChange b)
        {
            var c=Create(deck,"campaign_"+id); if(c==null) return;
            c.headline=c.briefing=body;
            c.left=new PolicyChoice {label=left,change=a,consequence="The campaign regroups. You can keep reaching voters."};
            c.right=new PolicyChoice {label=right,change=b,consequence="The campaign raises resources for the next push."};
            EditorUtility.SetDirty(c);
        }
    }
}
