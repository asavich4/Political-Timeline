using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public class ContentWorkshop : EditorWindow
    {
        CampaignDefinition campaign;
        DecisionCard selected;
        UnityEditor.Editor cardEditor;
        Vector2 listScroll, detailScroll;
        string search = "";
        string message = "Create cards, choose an advisor and category for flat art, and tune both choices. Changes are saved as Unity assets.";

        [MenuItem("Political Timeline/Content Workshop")]
        public static void Open() => GetWindow<ContentWorkshop>("Content Workshop");
        [MenuItem("Political Timeline/National Simulation Settings")]
        public static void OpenNation() => Selection.activeObject=AssetDatabase.LoadAssetAtPath<NationalDefinition>(PresidencyContent.Root+"/Nation.asset");
        void OnEnable() { minSize=new Vector2(760,540); campaign=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset"); }
        void OnDisable() { if(cardEditor != null) DestroyImmediate(cardEditor); }
        void OnGUI()
        {
            GUILayout.Label("POLITICAL TIMELINE  /  CONTENT WORKSHOP",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(message,MessageType.Info);
            campaign=(CampaignDefinition)EditorGUILayout.ObjectField("Campaign",campaign,typeof(CampaignDefinition),false);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Open Game Scene")) { if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath); }
                if(GUILayout.Button("Game Outline")) EditorUtility.OpenWithDefaultApp(System.IO.Path.GetFullPath("Assets/Documentation/GameOutline.md"));
                if(GUILayout.Button("Save Content")) AssetDatabase.SaveAssets();
            }
            if(campaign==null) { if(GUILayout.Button("Create Starter Content")) campaign=PresidencyContent.CreateStarter(); return; }
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("New Card")) CreateCard(false);
                using(new EditorGUI.DisabledScope(selected==null)) if(GUILayout.Button("Duplicate Selected")) CreateCard(true);
                if(GUILayout.Button("Campaign Settings")) Selection.activeObject=campaign;
                if(GUILayout.Button("Validate Deck")) message=Validate(campaign);
                if(GUILayout.Button("Balance Preview")) message=Simulate(campaign);
                if(GUILayout.Button("Run Rule Checks")) { try { CampaignChecks.Run(); message="All campaign rule checks passed. See Console for details."; } catch(Exception e) { message=e.Message; Debug.LogException(e); } }
            }
            search=EditorGUILayout.TextField("Find card",search);
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUILayout.VerticalScope(GUILayout.Width(250)))
                {
                    listScroll=EditorGUILayout.BeginScrollView(listScroll);
                    foreach(var c in campaign.cards.Where(c=>c!=null && (c.headline??"").IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0))
                        if(GUILayout.Button(c.headline,selected==c?EditorStyles.toolbarButton:EditorStyles.miniButton,GUILayout.Height(30))) Select(c);
                    EditorGUILayout.EndScrollView();
                    GUILayout.Label(campaign.cards.Count+" cards in this campaign",EditorStyles.miniLabel);
                }
                using(new EditorGUILayout.VerticalScope())
                {
                    detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                    if(selected==null) EditorGUILayout.HelpBox("Select a card to edit its artwork, text, effects and follow-up branches.",MessageType.None);
                    else
                    {
                        if(selected.portrait!=null) { var rect=GUILayoutUtility.GetRect(160,160,GUILayout.ExpandWidth(false)); var sprite=selected.portrait; GUI.DrawTextureWithTexCoords(rect,sprite.texture,new Rect(sprite.rect.x/sprite.texture.width,sprite.rect.y/sprite.texture.height,sprite.rect.width/sprite.texture.width,sprite.rect.height/sprite.texture.height)); }
                        if(cardEditor!=null) cardEditor.OnInspectorGUI();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }
        void Select(DecisionCard card) { selected=card; if(cardEditor!=null) DestroyImmediate(cardEditor); cardEditor=UnityEditor.Editor.CreateEditor(card); }
        void CreateCard(bool duplicate)
        {
            var card=duplicate?Instantiate(selected):CreateInstance<DecisionCard>();
            card.id=Guid.NewGuid().ToString("N").Substring(0,12); if(duplicate) card.headline+=" (copy)";
            if(!duplicate) { card.headline="New policy decision"; card.left.label="Decline"; card.right.label="Approve"; }
            var path=AssetDatabase.GenerateUniqueAssetPath(PresidencyContent.Root+"/Cards/NewDecision.asset");
            AssetDatabase.CreateAsset(card,path); Undo.RecordObject(campaign,"Add decision card"); campaign.cards.Add(card); EditorUtility.SetDirty(campaign); AssetDatabase.SaveAssets(); Select(card);
        }
        public static string Validate(CampaignDefinition campaign)
        {
            var errors=new System.Collections.Generic.List<string>();
            if(campaign.cards.Count==0) errors.Add("Campaign has no cards.");
            if(campaign.startYear<2001 || campaign.startYear%4!=1) errors.Add("Start year should follow a presidential election (for example 2025).");
            if(campaign.nation!=null)
            {
                var states=campaign.nation.states;
                if(states==null || states.Length!=51) errors.Add("National simulation needs 50 states plus DC.");
                else
                {
                    if(states.Any(s=>s==null)) errors.Add("Missing state profile.");
                    else
                    {
                        if(states.Sum(s=>s.electoralVotes)!=538) errors.Add("Electoral votes must total 538.");
                        if(states.Select(s=>s.abbreviation).Distinct().Count()!=51) errors.Add("Duplicate state abbreviation.");
                        if(states.Any(s=>s.interests.x+s.interests.y+s.interests.z+s.interests.w<=0)) errors.Add("State interest weights must have a positive total.");
                    }
                }
            }
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var c in campaign.cards)
            {
                if(c==null) { errors.Add("Missing card reference."); continue; }
                if(string.IsNullOrWhiteSpace(c.id)||!ids.Add(c.id)) errors.Add(c.name+": missing or duplicate ID.");
                if(string.IsNullOrWhiteSpace(c.headline)||string.IsNullOrWhiteSpace(c.briefing)) errors.Add(c.name+": missing headline or briefing.");
                if(string.IsNullOrWhiteSpace(c.advisor)) errors.Add(c.name+": missing advisor for geometric portrait.");
                foreach(var choice in new[]{c.left,c.right})
                {
                    if(choice==null||string.IsNullOrWhiteSpace(choice.label)||string.IsNullOrWhiteSpace(choice.consequence)) errors.Add(c.name+": incomplete choice.");
                    else if(choice.institution!=InstitutionRule.None && string.IsNullOrWhiteSpace(choice.blockedConsequence)) errors.Add(c.name+": missing blocked outcome.");
                }
            }
            if(!campaign.cards.Any(c=>c!=null&&c.earliestDecision<=1)) errors.Add("No card is available on the first decision.");
            return errors.Count==0 ? $"Deck valid: {campaign.cards.Count} cards ready to play." : string.Join("\n",errors);
        }
        public static string Simulate(CampaignDefinition campaign)
        {
            int wins=0, total=0;
            for(int seed=0;seed<200;seed++)
            {
                var state=new CampaignState(campaign,seed);
                while(!state.ended && state.current!=null && state.decisions<240)
                {
                    if(state.ElectionPending) state.AcknowledgeElection();
                    var l=state.current.left; var r=state.current.right;
                    int left=Score(state.support,state.nation.CanResolve(l)?l.change:l.blockedChange), right=Score(state.support,state.nation.CanResolve(r)?r.change:r.blockedChange);
                    state.Choose(right<left); // Simple policy: keep the coalition near its midpoint.
                }
                if(!state.ended && state.decisions>=240) wins++;
                total+=state.decisions;
            }
            return $"200 simulated runs (choices favor support near 50): {wins} survived 20 years; average {total/200f:F1} decisions. This is a balancing aid, not a prediction of player results.";
        }
        static int Score(int[] values,SupportChange change)
        {
            int score=0; var deltas=change.Values;
            for(int i=0;i<4;i++) { int next=values[i]+deltas[i]; score+=(next-50)*(next-50); if(next<=0) score+=100000; }
            return score;
        }
    }
}
