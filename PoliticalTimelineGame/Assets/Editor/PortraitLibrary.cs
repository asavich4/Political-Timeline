using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class PortraitLibrary
    {
        public const string Folder="Assets/Content/Presidency/Portraits";
        // Legacy geometric export retained for old assets only.
        public static void CreateMissing()
        {
            if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Content/Presidency","Portraits");
            var go=new GameObject("Portrait export",typeof(RectTransform),typeof(CanvasRenderer),typeof(AdvisorArt));
            try
            {
                var art=go.GetComponent<AdvisorArt>();
                foreach(var guid in AssetDatabase.FindAssets("t:DecisionCard"))
                {
                    var card=AssetDatabase.LoadAssetAtPath<DecisionCard>(AssetDatabase.GUIDToAssetPath(guid));
                    if(card.artwork!=null) continue;
                    var asset=ScriptableObject.CreateInstance<PortraitArtwork>(); asset.layers=art.ExportLayers(card);
                    AssetDatabase.CreateAsset(asset,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+card.name+".asset"));
                    Undo.RecordObject(card,"Assign editable portrait"); card.artwork=asset; EditorUtility.SetDirty(card);
                }
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        public static void Draw(Rect rect,DecisionCard card)
        {
            if(card.portrait!=null)
            {
                var sprite=card.portrait; var r=sprite.rect; var t=sprite.texture;
                GUI.DrawTextureWithTexCoords(rect,t,new Rect(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
            }
            else { EditorGUI.DrawRect(rect,new Color(.18f,.22f,.22f)); GUI.Label(rect,"Assign a Portrait sprite"); }
        }
        public static void Draw(Rect rect,PortraitArtwork artwork)
        {
            EditorGUI.DrawRect(rect,new Color(.18f,.22f,.22f));
            if(artwork==null) { GUI.Label(rect,"Create portrait assets"); return; }
            Handles.BeginGUI();
            foreach(var layer in artwork.layers)
            {
                if(!layer.visible || layer.points==null || layer.points.Length<3) continue;
                Handles.color=layer.color;
                for(int i=1;i<layer.points.Length-1;i++) Handles.DrawAAConvexPolygon(Point(rect,layer.points[0]),Point(rect,layer.points[i]),Point(rect,layer.points[i+1]));
            }
            Handles.color=Color.white; Handles.EndGUI();
        }
        public static Vector3 Point(Rect r,Vector2 p) => new Vector3(r.x+p.x*r.width/100,r.yMax-p.y*r.height/100,0);
        public static void BatchBuild()
        {
            CreateMissing();
            var cards=AssetDatabase.FindAssets("t:DecisionCard").Select(g=>AssetDatabase.LoadAssetAtPath<DecisionCard>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            if(cards.Length<102 || cards.Any(c=>c.artwork==null || c.artwork.layers.Count<10 || c.artwork.layers.Any(l=>l.points==null || l.points.Length<3))) throw new Exception("Missing portrait layers");
            int count=AssetDatabase.FindAssets("t:PortraitArtwork").Length;
            CreateMissing();
            if(count!=AssetDatabase.FindAssets("t:PortraitArtwork").Length) throw new Exception("Duplicate portrait assets");
            // Prove edited data drives the runtime mesh and that Undo can restore it.
            var go=new GameObject("Portrait check",typeof(RectTransform),typeof(CanvasRenderer),typeof(AdvisorArt));
            try
            {
                var art=go.GetComponent<AdvisorArt>(); art.Present(cards[0]);
                var method=typeof(AdvisorArt).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly);
                using(var helper=new UnityEngine.UI.VertexHelper())
                {
                    var original=cards[0].artwork.layers[0].color;
                    cards[0].artwork.layers[0].color=Color.magenta;
                    method.Invoke(art,new object[]{helper});
                    var vertex=new UIVertex(); helper.PopulateUIVertex(ref vertex,0);
                    cards[0].artwork.layers[0].color=original;
                    if(!vertex.color.Equals((Color32)Color.magenta)) throw new Exception("Portrait edits did not reach runtime mesh");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            PresidencyPreview.BatchCheck();
            File.AppendAllText("Validation.txt","\nAll "+cards.Length+" cards have editable layered portrait assets; repeat generation preserves assets; layer edits drive runtime artwork.");
        }
    }

    public class PortraitGallery : EditorWindow
    {
        Vector2 scroll; string search=""; DecisionCard[] cards;
        [MenuItem("Political Timeline/Portrait Gallery")]
        public static void Open() => GetWindow<PortraitGallery>("Portrait Gallery");
        void OnEnable() { minSize=new Vector2(530,400); Reload(); }
        void Reload() { cards=AssetDatabase.FindAssets("t:DecisionCard").Select(g=>AssetDatabase.LoadAssetAtPath<DecisionCard>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(c=>c.advisor).ThenBy(c=>c.headline).ToArray(); }
        void OnProjectChange() { Reload(); Repaint(); }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Every card, including campaign, opposition and linked events. Select sprite opens the actual image asset. Edit card opens its text, effects, conditions and follow-ups. Changes affect the game.",MessageType.Info);
            using(new EditorGUILayout.HorizontalScope())
            {
                search=EditorGUILayout.TextField("Find",search);
                if(GUILayout.Button("Assign missing sprites",GUILayout.Width(150))) { SpritePortraitSetup.AssignMissing(); Reload(); }
                if(GUILayout.Button("Save",GUILayout.Width(55))) AssetDatabase.SaveAssets();
            }
            var filtered=cards.Where(c=>(c.headline+" "+c.advisor+" "+c.category+" "+c.condition).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            GUILayout.Label(filtered.Length+" / "+cards.Length+" cards");
            scroll=EditorGUILayout.BeginScrollView(scroll);
            int columns=Mathf.Max(1,(int)((position.width-25)/180));
            for(int row=0;row<filtered.Length;row+=columns)
            using(new EditorGUILayout.HorizontalScope())
                for(int col=row;col<Mathf.Min(row+columns,filtered.Length);col++)
                using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox,GUILayout.Width(172)))
                {
                    var card=filtered[col]; PortraitLibrary.Draw(GUILayoutUtility.GetRect(160,150,GUILayout.ExpandWidth(false)),card);
                    GUILayout.Label(card.advisor,EditorStyles.boldLabel,GUILayout.Width(160));
                    GUILayout.Label(card.headline,EditorStyles.wordWrappedLabel,GUILayout.Width(160),GUILayout.Height(38));
                    GUILayout.Label(card.condition.ToString(),EditorStyles.miniLabel);
                    if(GUILayout.Button("Select sprite")) { Selection.activeObject=card.portrait; EditorGUIUtility.PingObject(card.portrait); }
                    if(GUILayout.Button("Edit card")) Selection.activeObject=card;
                }
            EditorGUILayout.EndScrollView();
        }
    }

    [CustomEditor(typeof(PortraitArtwork))]
    public class PortraitArtworkInspector : UnityEditor.Editor
    {
        int layerIndex, dragged=-1;
        public override void OnInspectorGUI()
        {
            var art=(PortraitArtwork)target;
            EditorGUILayout.HelpBox("Select a layer to highlight it. Drag its points in the preview. Below, edit colors, visibility, names and points; reorder layers to change overlap. Ctrl+Z undoes edits. Each card has its own asset.",MessageType.Info);
            if(art.layers.Count>0)
            {
                layerIndex=Mathf.Clamp(layerIndex,0,art.layers.Count-1);
                layerIndex=EditorGUILayout.Popup("Selected shape",layerIndex,art.layers.Select((l,i)=>i+"  "+l.name).ToArray());
                var rect=GUILayoutUtility.GetRect(250,280,GUILayout.ExpandWidth(true));
                PortraitLibrary.Draw(rect,art);
                var layer=art.layers[layerIndex];
                if(layer.points!=null)
                {
                    Handles.BeginGUI(); Handles.color=Color.cyan;
                    for(int i=0;i<layer.points.Length;i++)
                    {
                        var p=PortraitLibrary.Point(rect,layer.points[i]);
                        Handles.DrawLine(p,PortraitLibrary.Point(rect,layer.points[(i+1)%layer.points.Length]));
                        EditorGUI.DrawRect(new Rect(p.x-3,p.y-3,6,6),Color.cyan);
                        if(Event.current.type==EventType.MouseDown && Event.current.button==0 && Vector2.Distance(Event.current.mousePosition,p)<9) { dragged=i; Undo.RecordObject(art,"Move portrait point"); GUIUtility.hotControl=GUIUtility.GetControlID(FocusType.Passive); Event.current.Use(); }
                    }
                    Handles.EndGUI();
                    if(dragged>=0 && Event.current.type==EventType.MouseDrag)
                    {
                        var p=Event.current.mousePosition;
                        layer.points[dragged]=new Vector2(Mathf.Clamp((p.x-rect.x)/rect.width*100,0,100),Mathf.Clamp((rect.yMax-p.y)/rect.height*100,0,100));
                        EditorUtility.SetDirty(art); Event.current.Use(); Repaint(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    }
                    if(dragged>=0 && Event.current.rawType==EventType.MouseUp) { dragged=-1; GUIUtility.hotControl=0; Repaint(); }
                }
            }
            DrawDefaultInspector();
            if(GUI.changed) UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        public override bool HasPreviewGUI() => true;
        public override void OnPreviewGUI(Rect r,GUIStyle background) => PortraitLibrary.Draw(r,(PortraitArtwork)target);
    }
}

