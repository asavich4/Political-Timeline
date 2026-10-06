using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class PresidencyContent
    {
        public const string Root = "Assets/Content/Presidency";
        public static CampaignDefinition CreateStarter()
        {
            System.IO.Directory.CreateDirectory(Root + "/Cards");
            AssetDatabase.Refresh();
            var campaign = AssetDatabase.LoadAssetAtPath<CampaignDefinition>(Root + "/FirstAdministration.asset");
            if(campaign==null)
            {
                campaign = ScriptableObject.CreateInstance<CampaignDefinition>();
                AssetDatabase.CreateAsset(campaign, Root + "/FirstAdministration.asset");
            }
            campaign.cards.RemoveAll(card=>card==null);
            Add(campaign, "bridges", "The bridges are failing", "Should we pay to fix the bridges?", "Fund repairs", new SupportChange(9,6,-3,-8), "Road crews mobilize. The business lobby demands a meeting.", "Let states pay", new SupportChange(-7,-5,2,8), "The federal ledger holds. Commuters are less forgiving.");
            Add(campaign, "strike", "The docks fall silent", "The docks are on strike. Back the workers?", "Back the workers", new SupportChange(10,-4,-3,-7), "Workers cheer. Importers warn of empty shelves.", "Push for a deal", new SupportChange(-4,7,3,3), "Cargo moves again, but the union feels abandoned.");
            Add(campaign, "surveillance", "Eyes on the nation", "Give security more surveillance powers?", "Protect privacy", new SupportChange(4,7,-10,-2), "Privacy advocates celebrate. Your security brief grows tense.", "Grant the powers", new SupportChange(-5,-7,10,4), "Intelligence agencies expand their reach. Trust erodes.");
            Add(campaign, "tax", "The price of prosperity", "Tax the wealthy to help families?", "Tax the wealthy", new SupportChange(7,9,-2,-11), "Households get breathing room. Wealthy backers close their wallets.", "Protect investment", new SupportChange(-7,-6,2,10), "Markets rally. Families ask where their share went.");
            Add(campaign, "storm", "Landfall", "A hurricane hit. Release emergency funds?", "Send emergency aid", new SupportChange(8,5,5,-8), "Rescue teams reach the coast. The bill comes due later.", "Limit spending", new SupportChange(-10,-6,-3,8), "Fiscal allies approve. Images of flooded homes fill the news.");
            Add(campaign, "press", "An uncomfortable question", "A scandal broke. Face the press?", "Take questions", new SupportChange(4,8,-5,-3), "A bruising briefing restores some public trust.", "Cancel the briefing", new SupportChange(-4,-8,6,5), "The story grows in your silence. Loyalists welcome the hard line.");
            Add(campaign, "health", "The waiting room", "Rural hospitals are closing. Fund them?", "Fund hospitals", new SupportChange(8,7,-3,-9), "Clinics reopen. Insurers mount a lobbying campaign.", "Offer incentives", new SupportChange(-6,-4,3,8), "Investors take interest. Patients wait for results.");
            Add(campaign, "defense", "The procurement file", "The fleet is over budget. Approve it?", "Order an audit", new SupportChange(3,7,-8,-5), "The auditors arrive. Defense contractors call your office.", "Approve the fleet", new SupportChange(-5,-6,9,7), "Shipyards hire, and the deficit debate intensifies.");
            Add(campaign, "housing", "A place to call home", "Rents are rising. Build public housing?", "Build more homes", new SupportChange(7,8,-2,-8), "New housing breaks ground. Property groups pull their support.", "Let markets act", new SupportChange(-8,-6,2,9), "Investors stay confident. Renters organize.");
            Add(campaign, "trade", "Across the negotiating table", "Cheaper imports could cost jobs. Sign the deal?", "Protect local jobs", new SupportChange(8,-4,4,-6), "Factory towns celebrate. Shoppers face higher prices.", "Sign the deal", new SupportChange(-8,7,-2,8), "Prices ease. Industrial states demand a transition plan.");
            Add(campaign, "schools", "Tomorrow's voters", "Our schools need repairs. Invest now?", "Fund the repairs", new SupportChange(6,9,-4,-7), "Classrooms receive new equipment. Budget hearings turn hostile.", "Delay spending", new SupportChange(-5,-8,3,7), "Spending slows. Parent groups plan demonstrations.");
            Add(campaign, "ethics", "A friend in trouble", "Your donor faces corruption charges. Investigate?", "Open an inquiry", new SupportChange(5,8,-3,-9), "The inquiry earns public trust and powerful enemies.", "Defend your ally", new SupportChange(-6,-9,4,9), "Your circle stays loyal. The scandal refuses to fade.");
            Add(campaign, "energy", "Keeping the lights on", "Energy bills are soaring. Cap prices?", "Cap household bills", new SupportChange(9,6,-2,-9), "Families turn the heat back on. Utilities threaten cuts.", "Fund suppliers", new SupportChange(-6,-5,4,9), "Supply stabilizes. Household bills remain painful.");
            Add(campaign, "protest", "The avenue fills", "Protesters fill the street. Meet them?", "Meet their leaders", new SupportChange(7,5,-8,-3), "Dialogue cools the crowd. Security chiefs feel sidelined.", "Send security", new SupportChange(-8,-5,9,4), "The avenue empties. The images become tomorrow's headlines.");
            Add(campaign, "science", "A longer horizon", "Invest in new clean technology?", "Fund the research", new SupportChange(3,7,-5,-4), "Universities begin hiring. Critics call it a distant promise.", "Back current firms", new SupportChange(4,-6,3,7), "Established firms applaud. Young voters are disappointed.");
            Add(campaign, "budget", "Midnight on Capitol Hill", "A shutdown looms. Accept a compromise?", "Accept the deal", new SupportChange(5,7,-9,-3), "Government stays open. Security leaders demand guarantees.", "Hold your ground", new SupportChange(-6,-8,8,5), "Your allies praise your resolve as services close.");
            Add(campaign, "pensions", "A promise made", "Raise pension payments for retirees?", "Raise payments", new SupportChange(7,8,-3,-8), "Retirees welcome relief. Financing becomes the next fight.", "Freeze payments", new SupportChange(-8,-7,4,8), "The balance sheet improves. Town halls grow angry.");
            Add(campaign, "diplomacy", "A seat at the table", "A rival offers peace talks. Accept?", "Begin talks", new SupportChange(4,6,-8,3), "Markets settle. Hawks accuse you of giving too much away.", "Impose sanctions", new SupportChange(-4,-5,8,-3), "Security allies approve. Exporters count their losses.");
            InstitutionalContent.Ensure(campaign);
            EditorUtility.SetDirty(campaign); AssetDatabase.SaveAssets(); return campaign;
        }

        static void Add(CampaignDefinition campaign, string id, string title, string body, string left, SupportChange lc, string lr, string right, SupportChange rc, string rr)
        {
            var existing=AssetDatabase.LoadAssetAtPath<DecisionCard>(Root+"/Cards/"+id+".asset");
            if(existing!=null) { if(!campaign.cards.Contains(existing)) campaign.cards.Add(existing); return; }
            var c = ScriptableObject.CreateInstance<DecisionCard>();
            c.id = id; c.headline = title; c.briefing = body;
            string stamp = "01_31_49 PM";
            switch(id)
            {
                case "bridges": case "strike": case "trade": stamp = "10_00_00 AM"; c.advisor = "Labor Secretary"; break;
                case "surveillance": case "defense": case "protest": stamp = "09_43_29 AM"; c.advisor = "Security Adviser"; c.category = "NATIONAL SECURITY"; break;
                case "tax": case "budget": case "pensions": stamp = "09_51_55 AM"; c.advisor = "Treasury Secretary"; c.category = "THE ECONOMY"; break;
                case "health": case "science": stamp = "11_25_22 AM"; c.advisor = "Science Adviser"; break;
                case "press": case "ethics": stamp = "11_05_07 AM"; c.advisor = "Press Secretary"; c.category = "PUBLIC TRUST"; break;
                case "diplomacy": stamp = "01_42_19 PM"; c.advisor = "Secretary of State"; c.category = "FOREIGN AFFAIRS"; break;
            }
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/ChatGPT Image Jul 2, 2025, " + stamp + ".png"))
                if(asset is Sprite sprite) { c.portrait = sprite; break; }
            c.left = new PolicyChoice { label = left, change = lc, consequence = lr };
            c.right = new PolicyChoice { label = right, change = rc, consequence = rr };
            AssetDatabase.CreateAsset(c, Root + "/Cards/" + id + ".asset"); campaign.cards.Add(c);
        }
    }
}
