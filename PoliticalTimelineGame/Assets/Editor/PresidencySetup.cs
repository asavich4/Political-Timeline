using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    [InitializeOnLoad]
    public static class PresidencySetup
    {
        static PresidencySetup() { EditorApplication.delayCall += FirstImport; }
        static void FirstImport()
        {
            if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Library/PresidencyInitialized.txt")) return;
            if(EditorSceneManager.GetActiveScene().isDirty) return;
            try
            {
                if(!File.Exists(PresidencySceneBuilder.ScenePath)) PresidencySceneBuilder.Build();
                else EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
                CampaignChecks.Run(); ContentWorkshop.Open();
                File.WriteAllText("Library/PresidencyInitialized.txt","Starter scene opened.");
            }
            catch(System.Exception e) { Debug.LogException(e); }
        }
        public static void BatchSetup()
        {
            PresidencySceneBuilder.Build(); CampaignChecks.Run();
            var campaign=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset");
            string result=ContentWorkshop.Validate(campaign);
            if(!result.StartsWith("Deck valid")) throw new System.Exception(result);
            File.WriteAllText("Validation.txt","Unity compilation passed.\nMonthly calendar, election, Congress, court and deck rule checks passed.\n"+result);
        }
    }
}
