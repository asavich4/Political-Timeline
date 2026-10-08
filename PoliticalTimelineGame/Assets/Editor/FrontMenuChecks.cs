using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace PoliticalTimeline.Editor
{
    public static class FrontMenuChecks
    {
        static void Check(bool value,string label) { if(!value) throw new Exception(label); }
        public static void BatchCheck()
        {
            SaveSlots.TestDirectory=Path.Combine(Application.temporaryCachePath,"MenuChecks-"+Guid.NewGuid().ToString("N"));
            PresidencyPreview.BatchCheck();
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>(); var menu=game.frontMenu;
            foreach(PartyTeam party in Enum.GetValues(typeof(PartyTeam)))
            {
                var original=new CampaignState(game.campaign,731,party);
                for(int i=0;i<32;i++) { if(original.ElectionPending) original.AcknowledgeElection(); for(int j=0;j<4;j++) original.support[j]=55; original.Choose(i%2==0); }
                var data=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(original.Export(true)));
                var loaded=CampaignState.Restore(game.campaign,data);
                Check(JsonUtility.ToJson(original.Export(true))==JsonUtility.ToJson(loaded.Export(true)),"Complete save round trip");
                for(int i=0;i<30;i++) { if(original.ElectionPending) {original.AcknowledgeElection(); loaded.AcknowledgeElection();} for(int j=0;j<4;j++) original.support[j]=loaded.support[j]=55; original.Choose(i%2==0); loaded.Choose(i%2==0); Check(original.current==loaded.current,"Random sequence restored"); }
            }
            menu.ShowStart(); Check(menu.slotLoad.All(b=>!b.interactable),"Empty slots cannot load"); Capture("StartMenuPreview.png");
            for(int i=0;i<3;i++)
            {
                menu.slotNew[i].onClick.Invoke(); (i==1?menu.republican:menu.democrat).onClick.Invoke(); menu.begin.onClick.Invoke();
                Check(menu.ActiveSlot==i && SaveSlots.Exists(i) && !menu.IsOpen,"Create slot "+i);
                Check(game.State.Party==(i==1?PartyTeam.Republican:PartyTeam.Democrat),"Chosen party");
                game.Decide(true); Check(SaveSlots.Read(i).unreadOutcome,"Decision autosaved before animation");
                menu.Continue(i); Check(game.AwaitingAcknowledgement,"Unread consequence restored");
                game.AcknowledgeOutcome(true); game.AdvanceTransition(3); menu.Save(); menu.ShowStart();
            }
            string first=File.ReadAllText(Path.Combine(SaveSlots.DirectoryPath,"slot-1.json"));
            menu.slotNew[0].onClick.Invoke(); menu.back.onClick.Invoke(); Check(first==File.ReadAllText(Path.Combine(SaveSlots.DirectoryPath,"slot-1.json")),"Cancel replacement preserves save");
            File.WriteAllText(Path.Combine(SaveSlots.DirectoryPath,"slot-3.json"),"broken save"); menu.ShowStart(); Check(!menu.slotLoad[2].interactable,"Corrupt slot disabled");
            menu.Continue(1); Check(game.State.Party==PartyTeam.Republican,"Republican load");
            var electionState=new CampaignState(game.campaign,79,PartyTeam.Republican); electionState.decisions=22; electionState.Choose(false);
            Check(electionState.ElectionPending,"Election pending for restore test");
            game.UseCampaign(electionState,false); game.nationalPanels.Night.Advance(3);
            int reports=game.nationalPanels.Night.Count; menu.Save(); menu.Continue(1);
            Check(game.State.ElectionPending && game.nationalPanels.Night.Count==reports,"Election reveal progress restored");
            var enriched=game.State.Export(); enriched.policies=new[]{(int)PolicyId.PaidLeave}; enriched.policyDates=new[]{new DateTime(2026,3,1).Ticks}; enriched.court[0]=2; enriched.voterSupport[0]=7; enriched.holdsPresidency=false;
            var restored=CampaignState.Restore(game.campaign,JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(enriched)));
            Check(restored.Policies[0].Id==PolicyId.PaidLeave && restored.Policies[0].Enacted==new DateTime(2026,3,1) && restored.nation.court[0]==2 && restored.nation.VoterSupportBonus(0)==7 && !restored.nation.HoldsPresidency,"Policies, vacancies, outreach and opposition persist");
            menu.ShowStart();
            Capture("PopulatedSlotsPreview.png");
            menu.slotNew[0].onClick.Invoke(); menu.republican.onClick.Invoke(); Capture("PartySelectionPreview.png");
            foreach(var text in menu.panel.GetComponentsInChildren<TMP_Text>()) {text.ForceMeshUpdate(); Check(!text.isTextOverflowing,"Menu text fits: "+text.name);}
            File.AppendAllText("Validation.txt","\nStart menu, party choice, three independent disk slots, replacement cancellation, corrupt-save handling, unread consequence restore, full state round trip and deterministic next cards passed.");
            SaveSlots.TestDirectory=null;
        }
        static void Capture(string path) => typeof(PresidencyPreview).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{path,375,667,new Rect(0,0,375,647)});
        public static void SaveMenuScene()
        {
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            EditablePresentationEditor.UpgradeCurrent();
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>(); game.frontMenu.panel.SetActive(true);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            File.Copy(PresidencySceneBuilder.ScenePath,"Library/MenuSceneValidated.unity",true);
        }
    }
}
