using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class ElectoralContent
    {
        public static void Ensure(CampaignDefinition deck)
        {
            Campaign(deck,"factory_tour","Which factory towns should we visit?","Tour the Great Lakes","MI,WI,PA","Visit Ohio plants","OH,IN");
            Campaign(deck,"desert_tour","Where should the desert tour begin?","Start in Arizona","AZ","Start in Nevada","NV");
            Campaign(deck,"southern_rally","Where should we hold our next rallies?","Visit Georgia","GA","Visit the Carolinas","NC,SC");
            Campaign(deck,"campus_drive","Students want campaign town halls.","Tour Midwest campuses","MI,WI,MN","Tour western campuses","CO,NV,AZ");
            Campaign(deck,"suburban_doors","Which suburbs get more canvassers?","Send teams east","PA,VA,NH","Send teams south","GA,NC,FL");
            Campaign(deck,"farm_campaign","Farm communities invite your candidate.","Tour the plains","IA,KS,NE","Tour the Midwest","WI,MN,OH");
            Campaign(deck,"coastal_campaign","Coastal towns want to hear your plans.","Visit Atlantic towns","NC,VA,FL","Visit Pacific towns","OR,WA,CA");
            Campaign(deck,"union_endorsement","Union locals offer campaign meetings.","Visit northern locals","MI,PA,OH","Visit western locals","NV,CO,WA");
            Campaign(deck,"rural_radio","Where should we buy local radio time?","Buy mountain slots","MT,ID,WY","Buy southern slots","AR,MS,LA");
            Campaign(deck,"candidate_debate","Two states invite your candidate to debate.","Accept Pennsylvania","PA","Accept Wisconsin","WI");
            Campaign(deck,"volunteer_weekend","Where do our volunteers spend the weekend?","Work close eastern races","PA,NC,GA","Work close western races","AZ,NV,CO");
            Campaign(deck,"final_push","Where should the final campaign push go?","Focus on the Great Lakes","MI,WI,PA","Focus on the Sun Belt","AZ,GA,NC");

            Opposition(deck,"opp_spending","The government wants rushed spending cuts.","Block the cuts","Seek concessions","MI,WI,PA",true);
            Opposition(deck,"opp_records","The government wants to seal public records.","Block the secrecy bill","Demand a public hearing","VA,PA,NH",true);
            Opposition(deck,"opp_labor","The government proposes weaker labor rules.","Block the labor bill","Meet affected workers","OH,MI,WI",true);
            Opposition(deck,"opp_services","The government wants to cut local services.","Block the service cuts","Organize public testimony","GA,NC,AZ",true);
            Opposition(deck,"opp_listening","Voters say our party stopped listening.","Hold listening tours","Meet local business owners","PA,MI,WI",false);
            Opposition(deck,"opp_candidates","Local branches need credible new candidates.","Recruit local leaders","Train younger organizers","AZ,NV,CO",false);
            Opposition(deck,"opp_rebuild","Former supporters want a reason to return.","Rebuild neighborhood teams","Publish a practical agenda","GA,NC,FL",false);
            Opposition(deck,"opp_watchdog","A government contract looks wasteful.","Publish the evidence","Request an independent audit","VA,PA,OH",false);

            Law(deck,"transit_law","Fund affordable public transport?","Fund public transit",PolicyId.PublicTransit,PortraitDesign.Engineer);
            Law(deck,"rent_law","Protect renters from abusive lease terms?","Pass renter protections",PolicyId.RentProtection,PortraitDesign.Organizer);
            Law(deck,"food_law","Food inspectors cannot cover every plant.","Hire more inspectors",PolicyId.FoodInspection,PortraitDesign.Medic);
            Law(deck,"disaster_law","Offer public disaster insurance support?","Back public insurance",PolicyId.DisasterInsurance,PortraitDesign.Farmer);
            Law(deck,"cyber_law","Public services need stronger cyber defenses.","Fund cyber defenses",PolicyId.CyberSecurity,PortraitDesign.Engineer);
            Law(deck,"credit_law","Small firms cannot get affordable loans.","Open a credit program",PolicyId.SmallBusinessCredit,PortraitDesign.Organizer);
            Law(deck,"veterans_law","Veterans face long waits for care.","Expand veterans care",PolicyId.VeteransCare,PortraitDesign.Medic);
            Law(deck,"water_saving_law","Pay communities to conserve water?","Fund conservation",PolicyId.WaterConservation,PortraitDesign.Farmer);
            Law(deck,"records_law","Set firm deadlines for public records?","Pass an access law",PolicyId.PublicRecords,PortraitDesign.Reporter);
            Law(deck,"wages_law","Employers are withholding earned wages.","Enforce wage claims",PolicyId.WageTheft,PortraitDesign.Organizer);
            Law(deck,"rural_clinics_law","Build a network of small rural clinics?","Fund rural clinics",PolicyId.RuralClinics,PortraitDesign.Medic);
            Law(deck,"mental_health_law","Expand access to mental health treatment?","Fund more treatment",PolicyId.MentalHealth,PortraitDesign.Medic);
        }
        static void Campaign(CampaignDefinition deck,string id,string body,string left,string ls,string right,string rs)
        {
            var c=Create(deck,id,body,PortraitDesign.Organizer,"CAMPAIGN",left,right); if(c==null) return;
            c.condition=EventCondition.CampaignSeason; c.weight=3;
            c.left.consequence="Organizers build local support in "+ls.Replace(",",", ")+".";
            c.right.consequence="Your candidate reaches new voters in "+rs.Replace(",",", ")+".";
            Voters(c.left,ls,4); Voters(c.right,rs,3);
            c.left.change=new SupportChange(2,1,-2,-3); c.right.change=new SupportChange(-1,2,-1,-2);
            EditorUtility.SetDirty(c);
        }
        static void Opposition(CampaignDefinition deck,string id,string body,string left,string right,string states,bool block)
        {
            var c=Create(deck,id,body,PortraitDesign.Organizer,"OPPOSITION",left,right); if(c==null) return;
            c.condition=EventCondition.InOpposition; c.weight=4;
            c.left.consequence=block?"Your party blocks the bill. Local supporters welcome the stand.":"Your party rebuilds trust through local work.";
            c.right.consequence="Your party makes its case locally and reconnects with voters.";
            Voters(c.left,states,3); Voters(c.right,states,2);
            c.left.change=new SupportChange(3,1,-2,-3); c.right.change=new SupportChange(1,2,-1,-1);
            if(block)
            {
                c.left.institution=InstitutionRule.BlockGovernment;
                c.left.blockedChange=new SupportChange(-2,-2,0,1);
                c.left.blockedConsequence="The government passes the bill. Your party lacks blocking votes.";
            }
            EditorUtility.SetDirty(c);
        }
        static void Law(CampaignDefinition deck,string id,string body,string left,PolicyId policy,PortraitDesign design)
        {
            var c=Create(deck,id,body,design,"POLICY AGENDA",left,"Seek a smaller deal"); if(c==null) return;
            c.condition=EventCondition.InGovernment;
            c.left.enactPolicy=policy; c.left.institution=InstitutionRule.Congress;
            c.left.consequence="Congress enacts "+PolicyLedger.Title(policy).ToLowerInvariant()+".";
            c.left.change=new SupportChange(4,5,-3,-4);
            c.left.blockedChange=new SupportChange(-2,-2,0,1); c.left.blockedConsequence="Congress rejects the bill. Your party must build more support.";
            c.right.consequence="The party negotiates a smaller proposal. No new law is enacted.";
            c.right.change=new SupportChange(-1,1,2,1); EditorUtility.SetDirty(c);
        }
        static void Voters(PolicyChoice choice,string states,float change) { choice.voterStates=states.Split(','); choice.voterSupportChange=change; }
        static DecisionCard Create(CampaignDefinition deck,string id,string body,PortraitDesign design,string category,string left,string right)
        {
            string path=PresidencyContent.Root+"/Cards/"+id+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<DecisionCard>(path);
            if(existing!=null) { if(!deck.cards.Contains(existing)) deck.cards.Add(existing); return null; }
            var c=ScriptableObject.CreateInstance<DecisionCard>(); c.id=id; c.headline=c.briefing=body;
            c.advisor=category=="POLICY AGENDA"?"Policy Director":"Campaign Organizer"; c.design=design; c.category=category;
            c.left=new PolicyChoice {label=left}; c.right=new PolicyChoice {label=right};
            AssetDatabase.CreateAsset(c,path); deck.cards.Add(c); return c;
        }
    }
}
