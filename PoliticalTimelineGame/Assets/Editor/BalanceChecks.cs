using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace PoliticalTimeline.Editor
{
    public static class BalanceChecks
    {
        static void Check(bool value,string message) { if(!value) throw new Exception(message); }
        public static void BatchCheck()
        {
            var deck=PresidencyContent.CreateStarter();
            deck.decisionImpact=1.75f; EditorUtility.SetDirty(deck); AssetDatabase.SaveAssets();
            Check(deck.cards.Where(c=>c.id.StartsWith("rival_")||c.id.StartsWith("rebuild_")).Count()==16,"Sixteen editable opposition events");
            Check(deck.cards.All(c=>c.portrait!=null),"All cards have sprites");
            CampaignChecks.Run(); StoryChecks.Run(deck); ElectoralChecks.Run(deck);
            foreach(PartyTeam party in Enum.GetValues(typeof(PartyTeam)))
            {
                var state=new CampaignState(deck,731,party);
                state.current=deck.cards.Find(c=>c.id=="bridges");
                int before=state.support[0]; int change=(state.nation.CanResolve(state.current.left)?state.current.left.change:state.current.left.blockedChange).workers;
                state.Choose(false);
                Check(state.support[0]==before+(int)Math.Round(change*1.75,MidpointRounding.AwayFromZero),"Larger meter effects");
                var nation=new NationalState(deck.nation,party);
                int peak=nation.ProjectedVotes(new[]{100,100,100,100});
                Check(peak<350,"Full national meters cannot produce a landslide");
                Check(nation.ProjectedVotes(new[]{50,50,50,50})<270,"Neutral national support cannot guarantee victory");
                foreach(var profile in nation.states) nation.ApplyVoterEffect(new PolicyChoice {voterStates=new[]{profile.abbreviation},voterSupportChange=12});
                Check(nation.ProjectedVotes(new[]{70,70,70,70})>=270,"Organizing can win an election");
                Debug.Log(party+" max-meter EV without campaigning: "+peak);
                state.nation.HoldsPresidency=false;
                state.current=deck.cards.Find(c=>c.id=="rival_enact_CleanAir");
                state.Choose(false); Check(!state.Policies.Any(p=>p.Id==PolicyId.CleanAir),"Successful blockade prevents law");
                state.current=deck.cards.Find(c=>c.id=="rival_enact_CleanAir");
                state.Choose(true); Check(state.Policies.Single(p=>p.Id==PolicyId.CleanAir).Party!=party,"Rival party credited");
                Check(state.UnreadPolicies && state.lastResult.Contains("Enacted"),"Enactment notification");
                state.current=deck.cards.Find(c=>c.id=="rival_repeal_CleanAir");
                state.Choose(true); Check(!state.Policies.Any(p=>p.Id==PolicyId.CleanAir) && state.PolicyHistory[0].action=="Repealed","Repeal recorded");
                var weak=state.Export(); weak.senate=new bool[100]; weak.houseSeats=100; state.nation.Restore(weak);
                state.current=deck.cards.Find(c=>c.id=="rival_enact_CleanAir"); state.Choose(false);
                Check(state.Policies.Any(p=>p.Id==PolicyId.CleanAir),"Failed blockade permits rival law");
                for(int i=0;i<72;i++)
                {
                    if(state.ElectionPending) state.AcknowledgeElection();
                    state.nation.HoldsPresidency=false;
                    for(int j=0;j<4;j++) state.support[j]=50;
                    state.Choose(i%2==0);
                    if(!state.nation.HoldsPresidency) Check(state.current.condition==EventCondition.InOpposition || state.current.condition==EventCondition.CampaignSeason,"Opposition draws campaign and obstruction events only");
                    Check(!state.ended,"Losing office does not end game");
                }
                var data=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(state.Export(true)));
                var restored=CampaignState.Restore(deck,data);
                Check(JsonUtility.ToJson(state.Export(true))==JsonUtility.ToJson(restored.Export(true)),"Save round trip includes policy ownership and history");
                for(int i=0;i<10;i++)
                {
                    state.AcknowledgeElection(); restored.AcknowledgeElection();
                    for(int j=0;j<4;j++) state.support[j]=restored.support[j]=50;
                    state.Choose(i%2==0); restored.Choose(i%2==0);
                    Check(state.current==restored.current,"Reload preserves future draws");
                }
                data.policyHistory=null; data.policyParties=null;
                Check(CampaignState.Restore(deck,data).Policies.All(p=>p.Party==party),"Legacy save ownership migration");
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Presidency.unity");
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>(); game.Initialize();
            var view=new CampaignState(deck,731); view.nation.HoldsPresidency=false;
            foreach(var id in new[]{"rival_enact_CleanAir","rival_repeal_CleanAir","rival_enact_SchoolMeals"})
            { view.current=deck.cards.Find(c=>c.id==id); view.Choose(true); }
            view.PolicyHistory.Add(new PolicyRecord {id=PolicyId.PaidLeave,party=PartyTeam.Democrat,action="Enacted",date=view.CurrentMonth.Ticks});
            game.UseCampaign(view,false); game.frontMenu.panel.SetActive(false);
            game.nationalPanels.Open(3,false);
            Check(!view.UnreadPolicies,"Opening ledger clears unread notice");
            Capture("Library/PolicyRecordDark.png");
            game.nationalPanels.actionButton.onClick.Invoke(); Capture("Library/PolicyRecordPage2.png");
            game.nationalPanels.Close(); view.decisions=12; game.Refresh(); game.nationalPanels.Open(3,false); Capture("Library/PolicyRecordLight.png");
            game.nationalPanels.Close(); view.current=deck.cards.Find(c=>c.id=="rival_enact_SurveillancePowers"); game.Refresh(); game.Decide(true);
            game.AdvanceTransition(.31f); game.AdvanceTransition(1.2f);
            Check(game.AwaitingAcknowledgement,"Policy notification waits for swipe acknowledgement");
            Check(game.nationalPanels.navigation[3].GetComponentInChildren<TMP_Text>().text.Contains("*"),"Unread badge updates while ledger is closed");
            Capture("Library/PolicyNotification.png");
            File.WriteAllText("Library/BalanceValidation.txt","PASS: effects, competitive elections, recoverable opposition, block/pass/repeal, party records, save migration, deterministic reload, policy UI and swipe notification.");
            Debug.Log("BALANCE CHECKS PASSED");
        }
        static void Capture(string path)
        {
            typeof(PresidencyPreview).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{path,390,844,new Rect(0,34,390,763)});
        }
    }
}
