using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace PoliticalTimeline.Editor
{
    [InitializeOnLoad]
    public static class EditablePresentationEditor
    {
        static EditablePresentationEditor() { EditorApplication.delayCall+=UpgradeOpenScene; }
        static void UpgradeOpenScene()
        {
            if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var game=Object.FindFirstObjectByType<PresidencyGame>();
            if(game==null || game.editablePresentationVersion>=3) return;
            bool dirty=game.gameObject.scene.isDirty;
            Upgrade(game);
            if(!dirty) EditorSceneManager.SaveScene(game.gameObject.scene);
            File.WriteAllText("Library/EditablePresentationStatus.txt",dirty?"Updated open scene; existing unsaved edits preserved. Save when ready.":"Updated and saved the open scene with editable UI objects.");
        }
        [MenuItem("Political Timeline/Update Editable Scene")]
        public static void UpgradeCurrent()
        {
            var game=Object.FindFirstObjectByType<PresidencyGame>();
            if(game==null) { Debug.LogWarning("Open Assets/Scenes/Presidency.unity, then choose Update Editable Scene."); return; }
            Upgrade(game); Selection.activeGameObject=game.gameObject;
        }
        static void Upgrade(PresidencyGame game)
        {
            Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Create editable presentation");
            game.BuildEditablePresentation(); game.nationalPanels.PreviewForEditing(1,game.campaign);
            foreach(var legacyArt in game.cardTransform.GetComponentsInChildren<AdvisorArt>(true)) Undo.DestroyObjectImmediate(legacyArt.gameObject);
            game.PreviewCardForEditing(game.editorPreviewCard!=null?game.editorPreviewCard:game.campaign.cards[0]);
            game.frontMenu.panel.SetActive(true);
            EditorUtility.SetDirty(game); EditorUtility.SetDirty(game.nationalPanels);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
        public static void BatchUpgrade()
        {
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            var game=Object.FindFirstObjectByType<PresidencyGame>(); Upgrade(game);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            File.Copy(PresidencySceneBuilder.ScenePath,"Library/EditableSceneValidated.unity",true);
            // Reload to prove these are saved scene objects, not temporary Play-mode objects.
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            game=Object.FindFirstObjectByType<PresidencyGame>();
            int objects=game.GetComponentsInChildren<Transform>(true).Length;
            game.BuildEditablePresentation(); game.BuildEditablePresentation();
            if(objects!=game.GetComponentsInChildren<Transform>(true).Length || game.nationalPanels.navigation.Length!=4 || game.GetComponentsInChildren<PowerIcon>(true).Length!=4)
                throw new System.Exception("Editable UI duplicated or lost references after scene reload.");
            game.Initialize();
            if(objects!=game.GetComponentsInChildren<Transform>(true).Length) throw new System.Exception("Play initialization recreated saved UI.");
            PresidencyPreview.BatchCheck();
            File.AppendAllText("Validation.txt","\nEditable scene save/reload, idempotent upgrade and Play-mode reuse verified.");
        }
    }

    [CustomEditor(typeof(PresidencyGame))]
    public class PresidencyGameInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var game=(PresidencyGame)target;
            EditorGUILayout.HelpBox("These visuals are saved in the scene. Preview a page, then edit its objects in the Hierarchy. Preview Card uses the asset assigned below.",MessageType.Info);
            using(new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if(GUILayout.Button("Preview start menu")) { game.BuildEditablePresentation(); game.frontMenu.panel.SetActive(true); game.frontMenu.slotsPage.SetActive(true); game.frontMenu.partyPage.SetActive(false); Dirty(game); }
                if(GUILayout.Button("Preview party selection")) { game.BuildEditablePresentation(); game.frontMenu.panel.SetActive(true); game.frontMenu.slotsPage.SetActive(false); game.frontMenu.partyPage.SetActive(true); Dirty(game); }
                if(GUILayout.Button("Preview card")) { Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Preview card"); game.PreviewCardForEditing(game.editorPreviewCard!=null?game.editorPreviewCard:game.campaign.cards[0]); Dirty(game); }
                if(GUILayout.Button("Preview consequence")) { game.PreviewOutcomeForEditing(); Dirty(game); }
                string[] pages={"Map","Congress","Court","Policies"};
                using(new EditorGUILayout.HorizontalScope()) for(int i=0;i<pages.Length;i++) if(GUILayout.Button(pages[i])) { game.BuildEditablePresentation(); game.frontMenu.panel.SetActive(false); game.nationalPanels.PreviewForEditing(i,game.campaign); Dirty(game); }
            }
            DrawDefaultInspector();
        }
        static void Dirty(PresidencyGame game) { EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(game.gameObject.scene); }
    }

    [CustomEditor(typeof(DecisionCard))]
    public class DecisionCardInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var card=(DecisionCard)target;
            PortraitLibrary.Draw(GUILayoutUtility.GetRect(180,200,GUILayout.ExpandWidth(true)),card);
            if(card.portrait!=null && GUILayout.Button("Select portrait sprite")) Selection.activeObject=card.portrait;
            if(GUILayout.Button("Browse every portrait")) PortraitGallery.Open();
            if(!string.IsNullOrEmpty(card.sourceUrl) && GUILayout.Button("Open historical source")) Application.OpenURL(card.sourceUrl);
            if(!Application.isPlaying && GUILayout.Button("Preview this card in the open scene"))
            {
                var game=Object.FindFirstObjectByType<PresidencyGame>();
                if(game!=null) { Undo.RegisterFullObjectHierarchyUndo(game.gameObject,"Preview card art"); game.PreviewCardForEditing((DecisionCard)target); EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(game.gameObject.scene); }
                else Debug.LogWarning("Open the Presidency scene to preview this card.");
            }
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,"m_Script","artwork","usePortraitSprite","design");
            serializedObject.ApplyModifiedProperties();
        }
    }
}

