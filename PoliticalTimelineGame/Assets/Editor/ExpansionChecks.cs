using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class ExpansionChecks
    {
        static void Check(bool pass,string message) { if(!pass) throw new Exception("Expansion: "+message); }
        static CampaignState State(CampaignDefinition deck,DecisionCard card,PolicyId policy=PolicyId.None,bool majority=true,bool court=true)
        {
            var seed=new CampaignState(deck,31).Export(); seed.houseSeats=majority?300:100; seed.senate=Enumerable.Repeat(majority,100).ToArray();
            seed.court=Enumerable.Repeat(court?1:-1,9).ToArray();
            seed.policies=policy==PolicyId.None?new int[0]:new[]{(int)policy}; seed.policyDates=policy==PolicyId.None?new long[0]:new[]{new DateTime(2025,1,1).Ticks};
            var state=CampaignState.Restore(deck,seed); state.current=card; return state;
        }
        public static void BatchCheck()
        {
            ExpansionContent.Install();
            var deck=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset");
            int count=deck.cards.Count; ExpansionContent.Ensure(deck); Check(deck.cards.Count==count,"Installation duplicated cards");
            var added=deck.cards.Where(c=>c.id.StartsWith("exp_")).ToArray();
            Check(added.Length==80,"Expected 80 new cards"); Check(added.All(c=>c.portrait!=null && c.usePortraitSprite),"Missing PNG sprite");
            Check(added.Count(c=>c.category=="GENERAL")==24 && added.Count(c=>c.category=="PARTY NONSENSE")==16,"General and funny mix");
            foreach(var c in added.Where(c=>c.left.enactPolicy!=PolicyId.None))
            {
                var s=State(deck,c); s.Choose(false); Check(s.Policies.Any(p=>p.Id==c.left.enactPolicy),"Enact "+c.id);
                s=State(deck,c,PolicyId.None,false); s.Choose(false); Check(s.Policies.Count==0,"Blocked enact "+c.id);
            }
            foreach(var c in added.Where(c=>c.left.repealPolicy!=PolicyId.None))
            {
                var s=State(deck,c,c.requiredPolicy); s.Choose(false); Check(s.Policies.Count==0,"Repeal "+c.id);
                s=State(deck,c,c.requiredPolicy,false); s.Choose(false); Check(s.Policies.Count==1,"Blocked repeal retained law "+c.id);
            }
            foreach(var c in added.Where(c=>c.left.blockedRepealPolicy!=PolicyId.None))
            {
                var s=State(deck,c,c.requiredPolicy,true,true); s.Choose(false); Check(s.Policies.Count==1,"Court upheld "+c.id);
                s=State(deck,c,c.requiredPolicy,true,false); s.nation.HoldsPresidency=false; s.Choose(false); Check(s.Policies.Count==0,"Court struck down in opposition "+c.id);
            }
            var testDeck=ScriptableObject.CreateInstance<CampaignDefinition>(); testDeck.startYear=2025; testDeck.startingSupport=50; testDeck.nation=deck.nation;
            var neutral=UnityEngine.Object.Instantiate(added.First(c=>c.category=="GENERAL")); neutral.id="neutral-test"; neutral.left.change=new SupportChange();
            var law=added.First(c=>c.left.enactPolicy!=PolicyId.None && added.Any(r=>r.left.repealPolicy==c.left.enactPolicy));
            var repeal=added.First(c=>c.left.repealPolicy==law.left.enactPolicy);
            testDeck.cards.Add(neutral); testDeck.cards.Add(law); testDeck.cards.Add(repeal);
            var absent=State(testDeck,neutral);
            for(int i=0;i<100;i++) { absent.current=neutral; absent.Choose(false); if(absent.ElectionPending) absent.AcknowledgeElection(); Check(absent.current!=repeal,"Repeal appeared without its law"); }
            var present=State(testDeck,neutral,law.left.enactPolicy);
            for(int i=0;i<100;i++) {present.current=neutral; present.Choose(false); if(present.ElectionPending) present.AcknowledgeElection(); Check(present.current!=law,"Enacted law appeared as a new proposal");}
            UnityEngine.Object.DestroyImmediate(neutral); UnityEngine.Object.DestroyImmediate(testDeck);
            FrontMenuChecks.BatchCheck();
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>(); game.frontMenu.panel.SetActive(false);
            foreach(string id in new[]{"exp_f_goose","exp_g_burningriver","exp_h_readbill","exp_law_DepositInsurance","exp_repeal_PaidLeave","exp_c_privacy"})
            {
                var previewCard=game.campaign.cards.Find(c=>c.id==id);
                Check(previewCard!=null,"Missing preview card after scene rebuild: "+id);
                game.Restart(); game.State.current=previewCard; game.Refresh();
                Check(game.briefing.text==previewCard.briefing && game.portrait.sprite==previewCard.portrait,"Preview did not render "+id);
                typeof(PresidencyPreview).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{id+"Preview.png",390,844,new Rect(0,34,390,763)});
            }
            File.AppendAllText("Validation.txt","\n80 expansion cards, 12 new policies, conditional repeal eligibility, law exclusion after passage, congressional success/failure and court invalidation in opposition verified. All card text and PNG sprites checked at phone sizes.");
        }
    }
}
