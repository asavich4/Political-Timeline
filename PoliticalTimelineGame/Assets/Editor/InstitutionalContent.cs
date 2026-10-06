using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class InstitutionalContent
    {
        public static void Ensure(CampaignDefinition deck)
        {
            Add(deck,"rail_vote","The last vote","House Speaker","CONGRESS","Fund a national rail network?","Bring it to a vote",new SupportChange(7,4,5,-7),"Congress funds the railway. Construction begins.","Leave it to states",new SupportChange(-3,-2,2,4),"States draw up separate rail plans.",InstitutionRule.Congress);
            Add(deck,"school_lunch","An empty lunch tray","House Speaker","CONGRESS","Ask Congress to fund school meals?","Fund school meals",new SupportChange(6,6,-3,-5),"Congress funds meals. Cafeterias reopen.","Keep local funding",new SupportChange(-4,-3,2,4),"Districts keep paying. Some shorten lunch service.",InstitutionRule.Congress);
            Add(deck,"drug_prices","The pharmacy bill","Health Secretary","CONGRESS","Cap drug prices through Congress?","Put it to a vote",new SupportChange(6,7,-2,-7),"The price cap passes. Patients pay less.","Negotiate discounts",new SupportChange(2,2,1,-2),"Drug makers offer a smaller discount.",InstitutionRule.Congress);
            Add(deck,"flood_vote","Before the next storm","House Speaker","CONGRESS","Ask Congress to fund flood defenses?","Fund the defenses",new SupportChange(5,5,4,-6),"Congress approves new levees and drainage.","Wait for next year",new SupportChange(-4,-4,2,3),"Coastal towns face another season unprotected.",InstitutionRule.Congress);
            Add(deck,"oversight","Under oath","Committee Chair","CONGRESS","Congress wants your aides to testify.","Let them testify",new SupportChange(2,6,-2,-4),"Your aides testify. The hearing stays public.","Claim privilege",new SupportChange(-3,-6,2,4),"Congress challenges your claim. Trust falls.");
            Add(deck,"compromise","Across the aisle","Opposition Leader","CONGRESS","Trade a smaller budget for a deal?","Accept the cuts",new SupportChange(-3,4,5,-2),"A cross-party deal keeps essential services open.","Refuse the deal",new SupportChange(3,-5,-4,3),"The talks stall. Your base backs your stand.",condition:EventCondition.DividedCongress);
            Add(deck,"ethics_vote","No more gifts","Committee Chair","CONGRESS","Ban gifts to federal officials?","Ask Congress",new SupportChange(4,7,-2,-6),"Congress passes the ban. Lobbyists lose access.","Publish a gift list",new SupportChange(1,3,0,-2),"The gifts become public. They remain legal.",InstitutionRule.Congress);
            Add(deck,"court_nominee","An empty chair","Senate Leader","THE COURT","A justice retired. Send your nominee?","Seek confirmation",new SupportChange(-2,4,1,3),"The Senate confirms your nominee. The court shifts.","Keep searching",new SupportChange(2,-2,0,-1),"The seat stays empty while you seek a compromise.",InstitutionRule.ConfirmJustice,EventCondition.CourtVacancy);
            Add(deck,"clean_air_case","The air we share","Solicitor General","THE COURT","Your emissions rule faces the court.","Defend the rule",new SupportChange(4,6,-3,-5),"The court upholds your rule. Factories must adapt.","Rewrite it narrowly",new SupportChange(-2,2,3,2),"A narrower rule avoids the case but covers less.",InstitutionRule.CourtReview);
            Add(deck,"privacy_case","A locked phone","Solicitor General","THE COURT","Defend your new data privacy rule?","Fight in court",new SupportChange(3,6,-2,-4),"The court upholds the rule. Personal data is protected.","Settle the case",new SupportChange(1,-3,2,4),"The settlement leaves companies more freedom.",InstitutionRule.CourtReview);
            Add(deck,"land_case","A line through the farm","Solicitor General","THE COURT","A land seizure for rail faces review.","Defend the seizure",new SupportChange(-3,4,6,-2),"The court allows the project. The rail route proceeds.","Move the route",new SupportChange(4,2,-4,-2),"The farms stay intact. A longer route costs more.",InstitutionRule.CourtReview);
            Add(deck,"legal_aid","A lawyer for everyone","Chief Justice","THE COURT","Legal aid offices are overwhelmed.","Fund more lawyers",new SupportChange(5,4,-3,-4),"Legal aid hires staff. More people get a hearing.","Keep the budget",new SupportChange(-4,-3,2,3),"The queues grow. Court budgets stay flat.");
            Add(deck,"water","The tap runs brown","Health Secretary","DOMESTIC POLICY","Replace a town's unsafe water pipes?","Replace the pipes",new SupportChange(6,4,-3,-5),"Crews replace the pipes. Safe water returns.","Send bottled water",new SupportChange(2,-2,1,-1),"Water deliveries help while the pipes keep leaking.");
            Add(deck,"harvest","A failed harvest","Agriculture Secretary","DOMESTIC POLICY","Drought hit farms. Offer relief?","Offer relief",new SupportChange(5,2,-3,-3),"Relief keeps farms open through the dry season.","Back crop insurers",new SupportChange(-3,-2,3,4),"Insurers stay solvent. Uninsured farms close.");
            Add(deck,"cyber","The servers go dark","Security Adviser","NATIONAL SECURITY","A cyberattack has shut down hospitals.","Send federal teams",new SupportChange(3,5,-4,-2),"Federal teams restore hospital systems.","Hire contractors",new SupportChange(-2,2,-2,5),"Contractors restore service at a higher price.");
            Add(deck,"library","After school","Education Secretary","DOMESTIC POLICY","Keep libraries open in the evening?","Extend their hours",new SupportChange(4,5,-2,-4),"Libraries welcome evening readers and students.","Keep current hours",new SupportChange(-2,-3,2,2),"Budgets hold. Evening study spaces stay closed.");
            EditorUtility.SetDirty(deck); AssetDatabase.SaveAssets();
        }
        static void Add(CampaignDefinition deck,string id,string title,string advisor,string category,string body,string left,SupportChange lc,string result,string right,SupportChange rc,string other,InstitutionRule rule=InstitutionRule.None,EventCondition condition=EventCondition.Always)
        {
            string path=PresidencyContent.Root+"/Cards/"+id+".asset";
            var card=AssetDatabase.LoadAssetAtPath<DecisionCard>(path);
            if(card==null)
            {
                card=ScriptableObject.CreateInstance<DecisionCard>(); card.id=id; card.headline=title;
                card.advisor=advisor; card.category=category; card.briefing=body; card.condition=condition;
                card.left=new PolicyChoice {label=left,change=lc,consequence=result,institution=rule,
                    blockedChange=new SupportChange(-2,-3,-1,2),
                    blockedConsequence=rule==InstitutionRule.Congress?"Congress blocks the bill. Your coalition lacks the votes.":rule==InstitutionRule.CourtReview?"The court strikes down the policy. You must revise it.":"The Senate blocks confirmation. The seat stays empty."};
                card.right=new PolicyChoice {label=right,change=rc,consequence=other};
                AssetDatabase.CreateAsset(card,path);
            }
            if(!deck.cards.Contains(card)) deck.cards.Add(card);
        }
    }
}
