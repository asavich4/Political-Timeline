using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class ExpansionContent
    {
        public const string Folder="Assets/Content/Presidency/Cards/Expansion";
        static Sprite Portrait(CampaignDefinition deck,string advisor)
        {
            string file=advisor=="Congressional Clerk"?"Clerk":advisor=="Park Ranger"?"Ranger":advisor=="Party Goose"?"Goose":null;
            if(file!=null) return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CharacterSprites/"+file+".png");
            return deck.cards.First(c=>c.advisor==advisor && c.portrait!=null).portrait;
        }
        public static void ImportArt()
        {
            foreach(string name in new[]{"Clerk","Ranger","Goose"})
            {
                var importer=AssetImporter.GetAtPath("Assets/Art/CharacterSprites/"+name+".png") as TextureImporter;
                if(importer==null) throw new Exception("Missing expansion sprite: "+name);
                if(importer.textureType==TextureImporterType.Sprite) continue;
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
                importer.SaveAndReimport();
            }
        }
        [MenuItem("Political Timeline/Add Expansion Cards")]
        public static void Install()
        {
            var deck=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset");
            Ensure(deck); EditorUtility.SetDirty(deck); AssetDatabase.SaveAssets();
        }
        public static void Ensure(CampaignDefinition deck)
        {
            ImportArt(); if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(PresidencyContent.Root+"/Cards","Expansion");
            AddRows(deck,General,"GENERAL"); AddRows(deck,Funny,"PARTY NONSENSE");
            AddRows(deck,Congress,"CONGRESS"); AddRows(deck,Court,"THE COURT");
            foreach(string line in Laws.Trim().Split('\n'))
            {
                var p=line.Trim().Split('|'); var law=(PolicyId)Enum.Parse(typeof(PolicyId),p[0]);
                var c=Create(deck,"exp_law_"+p[0],p[1],"POLICY AGENDA",p[2],p[3],p[4],p[5],p[6],p[7],p[8]);
                if(c==null) continue;
                c.condition=EventCondition.InGovernment; c.excludedPolicy=law; c.left.enactPolicy=law; Vote(c.left); c.weight=2;
                if(law==PolicyId.DepositInsurance) History(c,"Inspired by US deposit insurance created in 1933. This proposal and its tradeoffs are fictional.","https://www.fdic.gov/history/1930-1939");
                EditorUtility.SetDirty(c);
            }
            foreach(string line in Repeals.Trim().Split('\n'))
            {
                var p=line.Trim().Split('|'); var law=(PolicyId)Enum.Parse(typeof(PolicyId),p[0]);
                var c=Create(deck,"exp_repeal_"+p[0],"Policy Director","POLICY REPEAL",p[1],p[2],p[3],p[4],p[5],p[6],p[7]);
                if(c==null) continue;
                c.condition=EventCondition.InGovernment; c.requiredPolicy=law; c.left.repealPolicy=law; Vote(c.left); c.weight=1;
                c.left.blockedConsequence="The repeal fails in Congress. "+PolicyLedger.Title(law)+" remains in force.";
                EditorUtility.SetDirty(c);
            }
            EditorUtility.SetDirty(deck);
        }
        static void AddRows(CampaignDefinition deck,string rows,string category)
        {
            foreach(string row in rows.Trim().Split('\n'))
            {
                var p=row.Trim().Split('|'); if(p.Length!=9) throw new Exception("Malformed expansion row: "+p[0]);
                var c=Create(deck,p[0],p[1],category,p[2],p[3],p[4],p[5],p[6],p[7],p[8]); if(c==null) continue;
                c.weight=category=="PARTY NONSENSE"?1:2;
                if(c.id=="exp_g_bankqueue") History(c,"Inspired by the US banking panic and national bank holiday of March 1933. This is a fictional modern recurrence, not a replay of the historical decisions.","https://www.federalreservehistory.org/essays/banking-panics-1931-33");
                if(c.id=="exp_g_burningriver") History(c,"Inspired by the June 1969 Cuyahoga River fire and the environmental debate around polluted waterways. Choices and outcomes are fictional.","https://www.epa.gov/sciencematters/putting-out-fire-50-years-science-protect-americas-water");
                if(c.id=="exp_c_papers") History(c,"Inspired by publication of the Pentagon Papers in 1971. The fictional litigation here uses the game's simplified court rules.","https://www.archives.gov/research/pentagon-papers");
                if(new[]{"exp_h_stopgap","exp_h_audit","exp_h_railbudget","exp_h_disastervote"}.Contains(c.id)) { Vote(c.left); c.condition=EventCondition.InGovernment; }
                PolicyId reviewed=c.id=="exp_c_privacy"?PolicyId.DataPrivacy:c.id=="exp_c_air"?PolicyId.CleanAir:c.id=="exp_c_papers"?PolicyId.PressFreedom:c.id=="exp_c_access"?PolicyId.ElectionAccess:PolicyId.None;
                if(reviewed!=PolicyId.None)
                {
                    c.requiredPolicy=reviewed; c.left.institution=InstitutionRule.CourtReview; c.left.blockedRepealPolicy=reviewed;
                    c.left.blockedConsequence="The court rejects the defense and strikes down "+PolicyLedger.Title(reviewed).ToLowerInvariant()+".";
                    c.left.blockedChange=new SupportChange(-4,-3,1,2);
                }
                if(c.id=="exp_c_nominee") { c.condition=EventCondition.CourtVacancy; c.left.institution=InstitutionRule.ConfirmJustice; c.left.blockedConsequence="The nomination stalls. Your party lacks the Senate votes."; c.left.blockedChange=new SupportChange(-1,-2,0,1); }
                EditorUtility.SetDirty(c);
            }
        }
        static void Vote(PolicyChoice choice) { choice.institution=InstitutionRule.Congress; choice.blockedConsequence="Congress rejects the proposal. No law or funding changes."; choice.blockedChange=new SupportChange(-2,-2,0,1); }
        static void History(DecisionCard c,string note,string url) { c.historicalNote=note; c.sourceUrl=url; }
        static SupportChange Effects(string raw) { var v=raw.Split(',').Select(int.Parse).ToArray(); return new SupportChange(v[0],v[1],v[2],v[3]); }
        static DecisionCard Create(CampaignDefinition deck,string id,string advisor,string category,string question,string left,string outcome,string right,string other,string lc,string rc)
        {
            string path=Folder+"/"+id+".asset"; var existing=AssetDatabase.LoadAssetAtPath<DecisionCard>(path);
            if(existing!=null) { if(!deck.cards.Contains(existing)) deck.cards.Add(existing); return null; }
            var c=ScriptableObject.CreateInstance<DecisionCard>(); c.id=id; c.headline=c.briefing=question; c.advisor=advisor; c.category=category;
            c.portrait=Portrait(deck,advisor); c.usePortraitSprite=true; c.earliestDecision=1;
            c.left=new PolicyChoice {label=left,consequence=outcome,change=Effects(lc)}; c.right=new PolicyChoice {label=right,consequence=other,change=Effects(rc)};
            AssetDatabase.CreateAsset(c,path); deck.cards.Add(c); return c;
        }
        const string General=@"
exp_g_nightbus|Field Organizer|Night workers want a later bus. Back them?|Join the petition|Shift workers fill the town hall. Business donors question the cost.|Ask employers to help|Employers fund a limited shuttle. Some neighborhoods miss out.|4,2,-1,-3|1,-2,2,2
exp_g_bankqueue|Treasury Secretary|Bank queues are growing. Address the panic?|Explain safeguards|A calm briefing reassures savers. Bankers dislike the scrutiny.|Meet bank leaders|Banks coordinate a response behind closed doors. Depositors want answers.|1,4,1,-3|-2,-2,4,3
exp_g_burningriver|Park Ranger|An oily river caught fire. Visit the banks?|Meet the residents|Residents show the damage. Industrial donors brace for scrutiny.|Meet the factories|Managers discuss cleanup costs. Residents resent being kept outside.|3,4,-2,-4|-3,-2,3,4
exp_g_blooddrive|Community Nurse|A blood drive needs a well-known volunteer.|Roll up a sleeve|The photo is useful. So is the blood. Meetings run late.|Recruit local heroes|Paramedics lead the drive. Your party gets less of the spotlight.|3,2,-1,-1|2,3,0,-2
exp_g_layoffs|Labor Secretary|A factory is closing. Who gets your time?|Meet the workforce|Workers welcome a hearing. Executives complain you skipped their briefing.|Meet the buyer|A buyer discusses saving part of the plant. Workers fear secret deals.|5,1,-2,-3|-3,1,3,3
exp_g_floodhall|Field Organizer|The branch office flooded. Reopen it where?|Share a community hall|The branch stays accessible. Its meetings now compete with bingo.|Rent downtown|The new office impresses donors. Bus riders face a longer trip.|3,2,-1,-3|-2,1,-2,4
exp_g_marketstall|Farm Cooperative|Farmers invite the party to their market.|Work a stall|You learn the price of potatoes. Farmers appreciate the effort.|Host the wholesalers|Buyers discuss distribution. Small growers feel overlooked.|3,2,1,-3|-2,-1,3,3
exp_g_translation|Party Organizer|Members cannot read our leaflets. Translate?|Hire translators|More residents can participate. The printing budget gets tight.|Use volunteers|Volunteers help, but some languages must wait.|3,4,-2,-3|2,-1,1,-1
exp_g_heatwalk|Park Ranger|Volunteers plan a town cleanup in a heatwave.|Move it to dawn|Volunteers stay safer. Several sponsors cannot attend.|Provide shaded breaks|The event keeps its sponsors. Equipment and water cost more.|2,3,-1,-2|1,2,-3,2
exp_g_seniors|Policy Director|Retirees want an afternoon party meeting.|Change the schedule|Retirees turn up. Shift workers ask for an evening option.|Offer a second session|Both groups get heard. Staff need another paid evening.|1,3,0,-2|3,3,-3,-2
exp_g_libraryroom|School Principal|The library offers space for a public forum.|Invite every party|The debate earns trust. Your own activists dislike sharing the stage.|Host our members|Members get a focused meeting. Outsiders see a closed club.|-1,4,0,-2|3,-3,0,2
exp_g_smallpaper|Investigative Reporter|A local newspaper is losing its last reporter.|Buy ordinary ads|The newsroom keeps reporting, including on you. Donors grumble.|Share their stories|Their audience grows. The missing salary still needs funding.|2,3,-2,-2|1,1,1,-1
exp_g_apprentice|Union Apprentice|An apprentice wants to speak at the conference.|Give them the stage|A practical speech lands well. Senior figures lose speaking time.|Arrange a workshop|The workshop helps trainees but gets little press coverage.|4,1,0,-3|2,2,-1,-1
exp_g_donoraccess|Treasury Secretary|A donor asks for a private policy dinner.|Insist on open minutes|The discussion becomes public. The donor trims their contribution.|Decline the dinner|You avoid the appearance of favoritism and lose a fundraising chance.|1,3,-1,-4|2,2,-2,-3
exp_g_schoolroof|School Principal|A school roof leaks during our local meeting.|Move the meeting|The pupils get their room back. The party pays for another venue.|Help staff set buckets|Staff appreciate the help. Parents want repairs, not a photo.|1,3,-2,-1|3,-2,0,1
exp_g_trailcrew|Park Ranger|A damaged trail needs a volunteer workday.|Send our organizers|The trail opens sooner. Canvassing loses a weekend.|Find a sponsor|A sponsor provides tools and requests a prominent sign.|3,2,-1,-2|-1,1,2,3
exp_g_publicwifi|Network Engineer|Our meeting hall has no usable internet.|Pay for a connection|Residents can join remotely. The branch budget shrinks.|Keep meetings offline|Costs stay low. People unable to travel lose their voice.|2,3,-3,-1|1,-3,2,1
exp_g_localshops|Policy Director|A street of small shops asks for a listening day.|Walk the whole street|Owners share problems. The afternoon overruns every diary slot.|Meet the association|A quick meeting produces a brief. Independent shops feel excluded.|2,3,-1,-2|-1,1,2,2
exp_g_foodbank|Community Nurse|A food bank asks for help without party branding.|Send quiet volunteers|Families get help. Your publicity team has nothing to post.|Fund a delivery|Supplies arrive quickly. Donors must cover the bill.|4,1,-1,-2|2,3,-3,-1
exp_g_badpoll|Campaign Chair|A terrible poll lands before the party meeting.|Publish it|Members face the problem. Some donors become nervous.|Brief organizers first|Branches prepare a response. A leak makes you look evasive.|1,3,-1,-3|3,-3,0,2
exp_g_accessramp|Field Organizer|Wheelchair users cannot enter our branch hall.|Move to an accessible hall|More members can attend. Rent rises.|Ask the landlord to fix it|The landlord agrees to talks. Members still face the stairs.|3,4,-3,-2|-2,-2,2,2
exp_g_coolingcenter|Community Nurse|A charity needs space for a cooling center.|Lend the branch office|Neighbors escape the heat. Party staff squeeze into another room.|Raise money instead|The charity can rent fans. Your donors receive another appeal.|4,2,-1,-2|1,3,-2,-2
exp_g_broadchurch|Chief of Staff|Two party factions refuse to share a panel.|Put both onstage|The argument is messy but members hear both sides.|Hold separate meetings|Both factions relax. Critics say the party has two manifestos.|2,2,-1,-3|1,-2,1,2
exp_g_receipts|Congressional Clerk|An expense claim contains no receipts.|Request the paperwork|The books stay clean. A senior member misses their reimbursement.|Cover only verified costs|A smaller payment clears. The member complains to donors.|1,3,1,-3|2,1,0,-2";
        const string Funny=@"
exp_f_goose|Party Goose|The party goose has occupied the lectern.|Let it speak|A long honk becomes the most shared speech of the month.|Bribe it with peas|The lectern is freed. The goose now expects an appearance fee.|2,2,-1,-2|1,-1,-1,2
exp_f_printer|Congressional Clerk|The printer has eaten the entire manifesto.|Publish it online|The manifesto survives. The printer is reassigned to envelopes.|Print a shorter one|Voters actually read it. Three committees demand their footnotes back.|-1,3,1,-2|3,1,1,-3
exp_f_mascotpoll|Campaign Chair|Our mascot polls better than our candidate.|Put it on posters|The posters sell. Commentators ask about the mascot's tax plan.|Keep a serious tone|Donors relax. The mascot starts an unofficial fan club.|3,2,-1,-4|-2,-1,1,3
exp_f_mutebutton|Press Secretary|Our leader gave a whole interview on mute.|Release the outtakes|The laugh helps. One donor says leadership should have sound.|Redo it solemnly|The message gets through. The internet keeps the silent version.|2,3,0,-3|-1,-1,1,2
exp_f_chili|Field Organizer|The local chili contest has become a faction war.|Taste every bowl|Both factions claim victory. Your afternoon schedule is cancelled.|Appoint a neutral judge|Peace returns until the judge chooses the vegetarian chili.|3,1,-1,-2|-1,3,0,-1
exp_f_password|Network Engineer|The office password is still password.|Reset everything|The network is safer. Half the staff are locked out of the kettle app.|Hold a training day|Staff learn security. The password becomes password2 before lunch.|-1,3,2,-2|2,1,-2,-1
exp_f_hat|Party Organizer|A donor sent 400 hats in the wrong party color.|Donate them unbranded|A shelter gets warm hats. The donor gets a careful thank-you.|Pay to replace them|The photos look correct. The budget does not.|3,2,-1,-3|0,-1,-3,3
exp_f_duckpond|Park Ranger|A ribbon cutting is blocked by furious ducks.|Wait for the ducks|The ceremony runs late. The ducks receive excellent coverage.|Move the ribbon|The opening proceeds beside a bin. The ducks remain undefeated.|2,2,-1,-2|-1,1,1,1
exp_f_replyall|Chief of Staff|Someone replied all to 900 party emails.|Turn off the thread|Silence returns. A committee asks where its discussion went.|Ask for restraint|Members send 900 messages agreeing to stop replying.|1,2,1,-2|2,-2,-1,1
exp_f_cake|Treasury Secretary|A donor cake says Happy Bribeday.|Return it|Nobody can accuse you of eating the evidence. The donor sulks.|Auction it for charity|The charity wins. The photo becomes a permanent campaign souvenir.|1,3,0,-3|3,-2,-1,2
exp_f_dance|Campaign Organizer|A volunteer invented a party dance.|Try it once|Young volunteers cheer. Your knees submit a formal protest.|Let volunteers lead|The dance spreads without you. The leader looks slightly wooden.|3,2,-1,-3|1,-1,1,1
exp_f_goosereceipt|Party Goose|The goose submitted a travel expense claim.|Pay in birdseed|The goose approves. The treasurer introduces a waterfowl spending cap.|Demand receipts|The goose produces a wet leaf and a parking ticket.|2,1,-2,-1|-1,2,1,1
exp_f_teleprompter|Press Secretary|The teleprompter says insert inspiring story.|Tell the truth|A short honest admission gets a laugh. The speechwriter hides.|Improvise boldly|The story is moving. Nobody believes the bit about the submarine.|1,3,0,-2|3,-3,0,2
exp_f_mug|Congressional Clerk|The office mug says World's Best Opposition.|Use it anyway|Members appreciate thrift. The press photographs it immediately.|Order a neutral mug|The new mug says World's Best Organization. Nobody is inspired.|2,-1,1,-1|-1,1,-1,2
exp_f_goosechief|Party Goose|The goose demands a place on the executive.|Offer mascot status|The goose gains a badge and no voting rights. Honking continues.|Refuse negotiations|The executive stays human. The goose pickets the biscuits.|2,2,-1,-2|-2,1,1,1
exp_f_census|Party Organizer|Our branch counted a houseplant as a member.|Correct the total|The figures improve. The branch loses its fastest-growing member.|Audit every branch|The audit finds two ferns and a photocopier with voting rights.|-1,3,1,-1|-2,4,-2,-2";
        const string Congress=@"
exp_h_stopgap|Congressional Clerk|A stopgap budget needs the party's votes.|Back the stopgap|Congress approves temporary funding. Hardliners call it surrender.|Reject the deal|Your hardliners cheer. Public services face renewed uncertainty.|2,3,3,-4|3,-4,-3,2
exp_h_audit|Committee Chair|A procurement audit needs congressional funding.|Fund the audit|Congress pays for investigators. Contractors prepare their lawyers.|Request existing files|Staff review what they can. Missing records remain missing.|1,4,-2,-4|-1,1,2,2
exp_h_railbudget|House Speaker|A rail repair amendment reaches the floor.|Back the amendment|Congress funds urgent repairs. Other projects lose budget space.|Seek a cheaper version|The amendment waits while staff recalculate costs.|3,3,-3,-2|-2,1,2,2
exp_h_disastervote|Senate Leader|Storm relief is tied to a disputed spending bill.|Accept the package|Congress releases relief with the attached spending. Fiscal allies object.|Demand a clean bill|Your position is clearer. Communities wait for another vote.|4,2,-3,-3|-2,2,1,2
exp_h_whip|Congressional Clerk|Three members threaten to skip a close vote.|Hear their demands|The whip learns what they need. Senior allies resent the attention.|Demand party loyalty|The message is firm. The three members stop answering calls.|2,1,-1,-2|-2,-2,1,3
exp_h_hearing|Committee Chair|A hearing witness asks to testify remotely.|Support remote access|The witness is heard. Traditionalists complain about the screen.|Offer travel help|The hearing stays in person. Someone must cover the fare.|2,3,1,-3|1,1,-2,2
exp_h_readbill|Congressional Clerk|Nobody has read all 900 pages of the bill.|Request more time|Staff find a costly clause. Leaders accuse you of stalling.|Publish a summary|Members get the essentials. Critics demand the missing detail.|1,3,-1,-2|2,-2,2,1
exp_h_lunch|House Speaker|A rival offers lunch before negotiations.|Accept without cameras|The conversation improves. Activists suspect a secret deal.|Bring the whole team|The talks stay accountable. The restaurant runs out of chairs.|-2,3,2,2|3,1,-1,-2";
        const string Court=@"
exp_c_privacy|Solicitor General|The data privacy law faces a court challenge.|Defend the law|The court upholds the privacy law. Compliance costs remain.|Seek a settlement|Lawyers negotiate narrower enforcement. The law remains enacted.|2,3,-1,-3|-1,1,2,2
exp_c_air|Chief Justice|Industry challenges the clean-air law.|Defend the standard|The court upholds the standard. Factories must comply.|Negotiate enforcement|The parties discuss a slower timetable. The law remains enacted.|3,3,-2,-4|-2,1,3,3
exp_c_papers|Solicitor General|A leaked report tests the press protection law.|Defend publication|The court upholds the press protections despite political embarrassment.|Seek limited redactions|Editors discuss narrow redactions. The law stays in force.|1,4,-1,-4|-1,1,2,2
exp_c_access|Chief Justice|Polling access rules face a legal challenge.|Defend equal access|The court upholds accessible polling requirements.|Seek a local agreement|Officials discuss implementation. The enacted law remains unchanged.|3,3,-1,-3|1,1,1,-1
exp_c_nominee|Senate Leader|A judicial vacancy divides the party.|Back the nominee|The Senate confirms an aligned justice.|Restart the search|The vacancy remains while the party interviews other candidates.|2,1,-1,3|-1,2,-1,-2
exp_c_ethics|Chief Justice|A judge's luxury holiday raises questions.|Request disclosure|Your party calls for transparent records. Powerful friends object.|Wait for the facts|You avoid premature accusations. Reformers dislike the silence.|1,4,0,-4|-2,-2,1,3
exp_c_steps|Solicitor General|Supporters want a rally on courthouse steps.|Hold a peaceful rally|Supporters gather without disrupting the hearing. Critics call it pressure.|Use a nearby square|The court stays clear. Some supporters call the move timid.|3,-1,-1,-2|-1,3,0,1
exp_c_opinion|Congressional Clerk|A court opinion is 200 pages. Explain it how?|Publish a plain guide|More members understand it. Lawyers argue over every simplified sentence.|Share the whole ruling|The record is complete. Almost nobody gets past page six.|2,3,-2,-1|-1,-2,1,2";
        const string Laws=@"
DepositInsurance|Treasury Secretary|Insure household deposits against bank failure?|Back deposit cover|Congress enacts deposit insurance. Savers welcome protection.|Ask banks for pledges|Banks make voluntary promises. No insurance law is enacted.|2,5,-2,-3|-2,-2,2,3
RightToRepair|Union Apprentice|Let independent shops access repair manuals?|Back repair rights|Congress enacts repair access. Local repair shops gain business.|Seek voluntary access|Manufacturers offer selected manuals. No legal right is created.|4,3,1,-4|-1,-2,2,3
AntitrustEnforcement|Policy Director|Fund tougher reviews of giant mergers?|Fund competition|Congress funds competition enforcement. Large firms object.|Seek industry guidance|Industry advises on deals. No enforcement law is enacted.|3,3,-2,-4|-2,-1,3,4
SchoolCooling|School Principal|Classrooms overheat. Fund cooling upgrades?|Fund cool classrooms|Congress funds school cooling and ventilation.|Seek local sponsors|Some schools find sponsors. No national fund is enacted.|3,4,-3,-2|-1,1,2,2
PostalAccess|Field Organizer|Keep postal counters in remote communities?|Protect postal access|Congress funds rural postal access despite delivery costs.|Promote online access|Digital help expands, but no postal guarantee is enacted.|4,3,-3,-3|-2,1,3,2
DisasterReserves|Park Ranger|Build reserves of emergency supplies?|Stock the reserves|Congress creates rotating emergency supply reserves.|Rely on suppliers|Suppliers promise rapid delivery. No public reserve is created.|3,3,-3,-2|-2,-1,3,3
CampaignDisclosure|Congressional Clerk|Publish the names of major campaign donors?|Require disclosure|Congress requires large-donor disclosure. Some patrons withdraw.|Invite voluntary lists|Some donors agree. Disclosure remains voluntary.|2,4,-1,-5|-1,-2,2,4
WorkerSafety|Labor Secretary|Inspectors cannot cover unsafe workplaces.|Fund safety checks|Congress funds workplace safety standards and inspections.|Ask firms to self-audit|Some firms publish audits. No new safety law is enacted.|5,2,-2,-4|-3,-1,3,4
PublicBroadcasting|Investigative Reporter|Fund independent public-interest broadcasting?|Back public media|Congress creates a public broadcasting fund.|Seek private sponsors|Sponsors support selected shows. No public fund is enacted.|1,4,-3,-3|-1,-2,3,3
NationalParks|Park Ranger|Park facilities are crumbling. Create a fund?|Fund park repairs|Congress funds national park maintenance.|Seek concession deals|Private operators discuss projects. No maintenance fund is enacted.|3,3,-3,-3|-2,1,3,3
ConsumerRefunds|Policy Director|Customers want refunds for cancelled services.|Guarantee refunds|Congress enacts clear consumer refund rights.|Encourage store credit|Businesses offer credit voluntarily. No refund right is created.|3,4,-1,-4|-2,-3,3,4
WhistleblowerProtection|Solicitor General|Workers fear reporting serious wrongdoing.|Protect reporting|Congress enacts whistleblower protections.|Offer private advice|Lawyers advise individuals. No new protection is enacted.|3,4,-2,-4|-2,1,2,2";
        const string Repeals=@"
DepositInsurance|Bank lobbyists want deposit insurance repealed.|Vote to repeal|Congress repeals deposit insurance. Savers lose that guarantee.|Keep the guarantee|The insurance remains. Bank lobbyists reduce their support.|-3,-5,2,4|2,3,-1,-3
RightToRepair|Manufacturers want repair access reversed.|Repeal repair rights|Congress removes repair access rights. Shops lose guaranteed manuals.|Defend repair shops|Repair rights remain. Manufacturers threaten to cut donations.|-4,-3,2,4|3,2,-1,-3
CampaignDisclosure|Donors want the disclosure law scrapped.|Repeal disclosure|Congress repeals large-donor disclosure. Campaign finances grow less visible.|Keep disclosure|The law stays. Several major donors cancel meetings.|-2,-4,1,5|1,3,-1,-4
WorkerSafety|Employers call safety inspections too costly.|Repeal the standards|Congress repeals the workplace safety policy. Unions protest.|Keep inspections|The standards remain. Employers object to their costs.|-5,-2,2,4|4,1,-2,-3
NationalParks|Budget hawks target the park maintenance fund.|End the fund|Congress repeals the park fund. Planned repairs lose support.|Keep park funding|Maintenance funding remains. Budget hawks criticize the expense.|-2,-3,3,3|2,2,-2,-2
ConsumerRefunds|Retailers lobby to remove refund guarantees.|Repeal the guarantee|Congress removes the consumer refund policy. Store credit returns.|Keep refund rights|Refund rights remain. Retail lobbyists turn against the party.|-3,-4,2,4|2,3,-1,-3
PaidLeave|Employers ask Congress to undo paid leave.|Repeal paid leave|Congress repeals the leave law. Families lose its protection.|Defend carers|Paid leave remains in force. Employers complain about costs.|-5,-3,2,4|4,2,-2,-3
SurveillancePowers|Civil groups demand an end to expanded spying.|Repeal surveillance|Congress repeals the expanded surveillance powers.|Keep the powers|Surveillance powers remain. Civil groups organize against them.|2,4,-2,-2|-3,-3,2,3
WealthTax|Wealthy donors demand the wealth tax be removed.|Repeal the tax|Congress repeals the wealth tax. Its household relief funding shrinks.|Keep the tax|The wealth tax remains. Several donors close their wallets.|-4,-3,2,5|3,2,-1,-4
EnergyPriceCap|Utilities want the household energy cap removed.|Lift the cap|Congress repeals the household cap. Families face market prices.|Keep the cap|The cap remains. Utilities warn about investment costs.|-4,-4,3,4|3,3,-2,-4
GiftBan|Officials say the gift ban is too strict.|Repeal the ban|Congress repeals the gift ban. Ethics groups condemn the decision.|Keep the ban|The gift ban remains. The invitation list gets shorter.|-2,-4,1,4|1,3,-1,-3
SchoolMeals|Budget hawks want universal meals rolled back.|Repeal meal funding|Congress repeals universal school meals. Schools lose that funding.|Keep school meals|Universal meals remain. Fiscal allies criticize the cost.|-4,-4,3,3|3,3,-2,-3";
    }
}
