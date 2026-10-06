using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    // Story chapters are assets too, but only their opening chapters enter random draws.
    public static class StoryContent
    {
        public static void Ensure(CampaignDefinition deck)
        {
            Story(deck,"grid","Grid Engineer",PortraitDesign.Engineer,PolicyId.EmergencyGrid,
                "Another city has lost power. Investigate?","Send engineers","Engineers find failing backup equipment.","Trust the utility","The utility promises to fix the outage.",
                "Engineers propose a backup power reserve.","Fund the reserve","Congress funds backup power for essential services.",
                "The utility wants more time. Press them?","Demand a deadline","The utility publishes a repair schedule.","Accept the delay","Repairs drift. Local organizers hear complaints.",
                "The new reserve kept a hospital running.","Expand the audit","Inspectors look for the next weak link.","Credit local crews","Local crews become the face of the recovery.");
            Story(deck,"care","Community Nurse",PortraitDesign.Medic,PolicyId.PaidLeave,
                "Carers are quitting work. Hear them out?","Hold a listening day","Carers describe the price of unpaid time off.","Meet employers","Employers warn that small firms need help.",
                "Carers ask for a paid family leave law.","Put the law to a vote","Congress passes paid family leave.",
                "Small firms want a voluntary leave plan.","Broker a pilot","Several firms agree to test a leave scheme.","Leave it to firms","Access to leave still depends on the employer.",
                "Paid leave is law. Who explains the rules?","Fund advice desks","Advice desks help families claim their leave.","Ask employers","Employers distribute guides to their staff.");
            Story(deck,"nursery","School Principal",PortraitDesign.Teacher,PolicyId.Childcare,
                "Parents cannot find childcare. Meet them?","Visit the parents","Parents bring a petition for public childcare.","Meet providers","Private providers ask for fewer barriers.",
                "Turn empty classrooms into public childcare?","Seek public funding","Congress funds new public childcare places.",
                "Providers offer a smaller childcare pilot.","Support the pilot","A few neighborhoods gain extra childcare places.","Keep talking","Parents wait while the negotiations continue.",
                "The childcare program needs trained staff.","Train local carers","Local carers fill the new childcare posts.","Recruit nationally","Experienced staff move into the new centers.");
            Story(deck,"presslaw","Investigative Reporter",PortraitDesign.Reporter,PolicyId.PressFreedom,
                "A reporter has files on party donors.","Hear the evidence","The files reveal a pattern of intimidation.","Call the donors","Donors insist the allegations are misleading.",
                "Protect public-interest reporting by law?","Back press protections","Congress passes protections for public-interest reporting.",
                "The newsroom asks for a public response.","Release a statement","The party answers questions on the record.","Stay silent","The newsroom publishes without your response.",
                "The new press law protects a harsh critic.","Defend the principle","The party defends protections even for its critics.","Challenge the story","Your lawyers contest the facts, not the press law.");
            Story(deck,"soil","Farm Cooperative",PortraitDesign.Farmer,PolicyId.SoilProtection,
                "Topsoil is washing away. Visit the farms?","Visit the farms","Farmers show you fields damaged by erosion.","Call the lenders","Lenders want a plan before extending credit.",
                "Fund a national soil restoration program?","Ask Congress","Congress funds soil restoration and cover crops.",
                "A farm cooperative proposes a local trial.","Back the trial","Farmers test cover crops on a small acreage.","Wait for research","Farmers wait for evidence before changing methods.",
                "Restored fields survived the heavy rain.","Share the findings","Other cooperatives adopt the tested methods.","Celebrate growers","Growers welcome credit for their hard work.");
            Story(deck,"ballot","Field Organizer",PortraitDesign.Organizer,PolicyId.ElectionAccess,
                "Voters report inaccessible polling sites.","Check the reports","Organizers document stairs and distant polling sites.","Ask local officials","Local officials promise to inspect their locations.",
                "Fund accessible polling places nationwide?","Bring a bill","Congress funds accessible polling places.",
                "Volunteers offer transport to polling sites.","Coordinate rides","Volunteers help voters reach existing polling sites.","Publish access guides","Voters get clear information about accessible routes.",
                "Accessible sites are open. Spread the word?","Inform all voters","Nonpartisan guides explain the new access options.","Brief local branches","Party branches share the access information locally.");

            var broadband=Card(deck,"rural_signal","Network Engineer",PortraitDesign.Engineer,"Rural towns want reliable internet.","Fund rural broadband","Congress funds new rural internet connections.","Invite private bids","Providers compete for local contracts.");
            Law(broadband,PolicyId.RuralBroadband);
            var apprentices=Card(deck,"paid_training","Union Apprentice",PortraitDesign.Engineer,"Trainees cannot afford unpaid work.","Fund paid training","Congress funds paid apprenticeship places.","Ask firms to pay","Some firms agree to pay trainees voluntarily.");
            Law(apprentices,PolicyId.Apprenticeships);
            Card(deck,"clinic_bus","Community Nurse",PortraitDesign.Medic,"A mobile clinic needs volunteer drivers.","Recruit volunteers","Volunteers bring the clinic to remote communities.","Ask a charity","A charity offers a smaller transport service.");
            Card(deck,"seed_bank","Farm Cooperative",PortraitDesign.Farmer,"Farmers want to share drought-proof seeds.","Host a seed exchange","Growers share seeds and practical advice.","Seek a sponsor","A sponsor funds the exchange and asks for publicity.");
            Card(deck,"student_debate","School Principal",PortraitDesign.Teacher,"Students invite your party to a debate.","Send a young member","A young member faces a lively student audience.","Send a veteran","A veteran reassures supporters but faces tough questions.");
            Card(deck,"leaked_memo","Investigative Reporter",PortraitDesign.Reporter,"An internal party memo has leaked.","Explain the memo","An honest explanation limits the damage.","Find the source","The search for the leak unsettles party staff.");
            Card(deck,"branch_rent","Field Organizer",PortraitDesign.Organizer,"A local party office cannot pay its rent.","Pool small donations","Small donors keep the branch office open.","Seek a wealthy patron","A patron saves the office and gains influence.");
            Card(deck,"repair_cafe","Union Apprentice",PortraitDesign.Engineer,"Volunteers want a neighborhood repair day.","Offer party space","Residents repair household goods together.","Find a business host","A local business hosts the repair day.");
        }

        static void Story(CampaignDefinition deck,string id,string advisor,PortraitDesign art,PolicyId law,
            string question,string yes,string yesResult,string no,string noResult,
            string proposal,string vote,string passed,
            string fallback,string fallbackLeft,string fallbackResult,string fallbackRight,string fallbackOther,
            string aftermath,string afterLeft,string afterResult,string afterRight,string afterOther)
        {
            var start=Card(deck,id+"_start",advisor,art,question,yes,yesResult,no,noResult);
            var bill=Card(deck,id+"_bill",advisor,art,proposal,vote,passed,"Seek a smaller plan","Your party returns to local negotiations.");
            var local=Card(deck,id+"_local",advisor,art,fallback,fallbackLeft,fallbackResult,fallbackRight,fallbackOther);
            var result=Card(deck,id+"_result",advisor,art,aftermath,afterLeft,afterResult,afterRight,afterOther);
            bill.followUpOnly=local.followUpOnly=result.followUpOnly=true;
            start.left.followUp=bill; start.right.followUp=local;
            Law(bill,law); bill.left.followUp=result; bill.left.blockedFollowUp=local; bill.right.followUp=local;
            foreach(var c in new[]{start,bill,local,result}) EditorUtility.SetDirty(c);
        }
        static void Law(DecisionCard card,PolicyId policy)
        {
            card.left.enactPolicy=policy; card.left.institution=InstitutionRule.Congress;
            card.left.change=new SupportChange(5,4,-3,-4);
            card.left.blockedChange=new SupportChange(-2,-2,0,1);
            card.left.blockedConsequence="The bill fails. Your party returns to a smaller local plan.";
            EditorUtility.SetDirty(card);
        }
        static DecisionCard Card(CampaignDefinition deck,string id,string advisor,PortraitDesign art,string question,string left,string result,string right,string other)
        {
            string path=PresidencyContent.Root+"/Cards/"+id+".asset";
            var card=AssetDatabase.LoadAssetAtPath<DecisionCard>(path);
            if(card==null)
            {
                card=ScriptableObject.CreateInstance<DecisionCard>(); card.id=id; card.headline=question; card.briefing=question;
                card.advisor=advisor; card.category="PARTY & COMMUNITY"; card.design=art;
                card.left=new PolicyChoice {label=left,consequence=result,change=new SupportChange(3,2,-1,-3)};
                card.right=new PolicyChoice {label=right,consequence=other,change=new SupportChange(-2,-1,2,3)};
                AssetDatabase.CreateAsset(card,path);
            }
            if(!deck.cards.Contains(card)) deck.cards.Add(card); return card;
        }
    }
}
