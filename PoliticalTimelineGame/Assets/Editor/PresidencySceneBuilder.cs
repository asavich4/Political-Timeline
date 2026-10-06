using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PoliticalTimeline.Editor
{
    public static class PresidencySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Presidency.unity";
        static readonly Color Ink = new Color(.13f,.18f,.19f);
        static readonly Color Paper = new Color(.96f,.93f,.83f);
        static readonly Color Gold = new Color(.70f,.56f,.31f);
        static TMP_FontAsset font;
        static Material sharpText;

        [MenuItem("Political Timeline/Create Starter Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild starter scene?", "This replaces the Presidency scene layout. Your card assets and original Main scene are preserved.", "Rebuild", "Cancel")) return;
            var campaign = PresidencyContent.CreateStarter();
            EnsureMeterSprite();
            if(campaign.nation==null)
            {
                campaign.nation=AssetDatabase.LoadAssetAtPath<NationalDefinition>("Assets/Content/Presidency/Nation.asset");
                if(campaign.nation==null) { campaign.nation=ScriptableObject.CreateInstance<NationalDefinition>(); AssetDatabase.CreateAsset(campaign.nation,"Assets/Content/Presidency/Nation.asset"); }
                EditorUtility.SetDirty(campaign);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            font = CrispFont();
            sharpText=AssetDatabase.LoadAssetAtPath<Material>("Assets/Content/Presidency/SharpText.mat");
            if(sharpText==null)
            {
                sharpText=new Material(font.material) { name="Sharp UI Text" };
                AssetDatabase.CreateAsset(sharpText,"Assets/Content/Presidency/SharpText.mat");
            }
            sharpText.CopyPropertiesFromMaterial(font.material);
            sharpText.SetFloat("_Sharpness",.75f);
            sharpText.SetFloat("_PerspectiveFilter",0);
            sharpText.SetFloat("_OutlineSoftness",0);
            sharpText.DisableKeyword("UNDERLAY_ON"); sharpText.DisableKeyword("UNDERLAY_INNER");
            EditorUtility.SetDirty(sharpText);
            var cam = new GameObject("Main Camera", typeof(Camera)); cam.tag = "MainCamera";
            cam.GetComponent<Camera>().backgroundColor = Ink; cam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            var canvasObject = new GameObject("Presidency • Portrait", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect=true;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390,844); scaler.matchWidthOrHeight = 0;
            var game = canvasObject.AddComponent<PresidencyGame>(); game.campaign = campaign;
            var bg = Panel(canvas.transform,"Background",0,0,390,844,Ink);
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            game.background=bg;
            var safe=Rect(canvas.transform,"Safe Area",0,0,390,844);
            safe.anchorMin=Vector2.zero; safe.anchorMax=Vector2.one; safe.offsetMin=safe.offsetMax=Vector2.zero;
            var board=Rect(safe,"Portrait Layout • 390 x 640",0,0,390,640);
            board.anchorMin=board.anchorMax=board.pivot=new Vector2(.5f,.5f); board.anchoredPosition=Vector2.zero;
            var layout=canvasObject.AddComponent<PortraitLayout>(); layout.safeArea=safe; layout.content=board;
            Transform root=board;
            // One screen, one decision. All secondary information lives behind the meters.
            var meters=Rect(root,"Support • tap for details",20,0,350,70);
            game.powerButtons=new Button[4];
            game.powerIcons=new Image[4];
            game.supportFills = new Image[4];
            game.supportChanges = new TMP_Text[4];
            string[] names = { "Workers", "Middle class", "Economy", "Elites" };
            for(int i=0;i<4;i++)
            {
                float x=i*90;
                var hit=Panel(meters,names[i]+" • tap for details",x,0,80,70,Color.clear);
                var button=hit.gameObject.AddComponent<Button>(); button.targetGraphic=hit;
                game.powerButtons[i]=button;
                var badge=Panel(hit.transform,"Icon background",12,0,56,46,Paper);
                badge.raycastTarget=false;
                var icon=Panel(badge.transform,names[i]+" Image",2,0,52,46,Color.white);
                string[] files={"Workers","MiddleClass","Economy","Elites"};
                icon.sprite=ImportSprite("Assets/Content/Presidency/PowerImages/"+files[i]+".png",false);
                icon.preserveAspect=true; icon.raycastTarget=false; game.powerIcons[i]=icon;
                Panel(meters,"Track "+i,x,47,80,9,new Color(.32f,.37f,.36f)).raycastTarget=false;
                var fill=Panel(meters,"Support "+i,x,47,80,9,Gold);
                fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Content/Presidency/Meter.png");
                fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillAmount=.5f; fill.raycastTarget=false;
                game.supportFills[i]=fill;
                game.supportChanges[i]=Label(meters,"Change "+i,x,57,80,22,"",18,Paper,TextAnchor.MiddleCenter);
            }
            game.helpButton=game.powerButtons[0];
            game.monthLabel=Label(root,"Month",20,82,350,22,"January 2025",16,Paper,TextAnchor.MiddleCenter);
            game.briefing=Label(root,"Question",20,106,350,108,campaign.cards[0].briefing,28,Paper,TextAnchor.MiddleCenter);
            game.briefing.fontStyle=FontStyles.Bold;
            var card=Panel(root,"Decision Card",20,218,350,350,Color.clear);
            card.rectTransform.pivot=new Vector2(.5f,.5f);
            card.rectTransform.anchoredPosition+=new Vector2(175,-175);
            game.cardTransform=card.rectTransform; card.gameObject.AddComponent<CardDrag>().game=game;
            // Retain the legacy sprite slot for authored content; flat art is layered above it.
            var portrait=Panel(card.transform,"Character",0,0,350,350,Color.white);
            portrait.sprite=campaign.cards[0].portrait; portrait.preserveAspect=false; portrait.raycastTarget=false; game.portrait=portrait;
            var stamp=Panel(card.transform,"Choice • revealed while holding",0,0,350,116,new Color(.06f,.09f,.10f,.94f));
            stamp.raycastTarget=false;
            game.choiceOverlay=stamp.gameObject.AddComponent<CanvasGroup>();
            game.choiceOverlay.alpha=0; game.choiceOverlay.blocksRaycasts=false; game.choiceOverlay.interactable=false;
            game.choiceLabel=Label(stamp.transform,"Held Choice",20,12,310,88,"",28,Color.white,TextAnchor.MiddleCenter);
            game.choiceLabel.fontStyle=FontStyles.Bold;
            game.commitIndicator=Panel(stamp.transform,"Release Threshold",0,110,350,6,Gold);
            game.commitIndicator.raycastTarget=false;
            game.restartButton=Choice(root,"New Administration",20,476,350,64,"Try again",out _); game.restartButton.gameObject.SetActive(false);
            var navigation=new Button[3];
            navigation[0]=Choice(root,"Map",20,582,106,54,"Map",out _);
            navigation[1]=Choice(root,"Congress",136,582,118,54,"Congress",out _);
            navigation[2]=Choice(root,"Court",264,582,106,54,"Court",out _);
            foreach(var button in navigation) button.GetComponentInChildren<TMP_Text>().fontSize=18;
            var help=Panel(root,"Details",0,0,390,640,Ink); game.helpPanel=help.gameObject;
            game.helpTitle=Label(help.transform,"Help Heading",20,26,350,44,"Keys of power",28,Paper,TextAnchor.MiddleCenter);
            game.helpText=Label(help.transform,"Help Text",28,105,334,402,"Keep every group between 0 and 100.\n\nHold left or right, then release.",23,Paper,TextAnchor.UpperLeft);
            game.closeHelpButton=Choice(help.transform,"Close Help",20,552,350,76,"Back to the card",out _);
            help.gameObject.SetActive(false);
            game.nationalPanels=BuildNationalPanels(root,campaign,navigation);
            game.BuildEditablePresentation();
            game.nationalPanels.PreviewForEditing(1,campaign);
            game.PreviewCardForEditing(campaign.cards[0]);
            new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;
            PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneOnly;
            PlayerSettings.defaultScreenWidth=390; PlayerSettings.defaultScreenHeight=844;
            Canvas.ForceUpdateCanvases(); layout.Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath,true) };
            foreach(var existing in EditorBuildSettings.scenes) if(existing.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(existing.path,false));
            EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Political Timeline: portrait iPhone scene created.");
        }

        static NationalPanels BuildNationalPanels(Transform root,CampaignDefinition campaign,Button[] navigation)
        {
            var panel=Panel(root,"National View",0,0,390,640,Ink);
            var view=panel.gameObject.AddComponent<NationalPanels>(); view.background=panel; view.navigation=navigation;
            view.title=Label(panel.transform,"Title",20,12,298,44,"Election map",28,Paper);
            view.subtitle=Label(panel.transform,"Date",20,61,350,48,"January 2025",17,Paper);
            view.closeButton=Choice(panel.transform,"Close",322,10,48,48,"X",out _);
            view.footnote=Label(panel.transform,"Simulation Note",20,522,350,28,"Fictional game simulation",14,Paper,TextAnchor.MiddleCenter);
            view.actionButton=Choice(panel.transform,"Continue or Nominate",20,568,350,64,"Continue",out view.actionLabel);
            var map=Rect(panel.transform,"States",0,0,390,520); view.mapGroup=map.gameObject;
            view.mapTiles=new Image[campaign.nation.states.Length]; view.mapButtons=new Button[campaign.nation.states.Length];
            var placement=JsonUtility.FromJson<MapPlacement>(File.ReadAllText("Assets/Content/Presidency/Map/Placement.json"));
            const float mapWidth=350, mapHeight=219;
            for(int i=0;i<campaign.nation.states.Length;i++)
            {
                var state=campaign.nation.states[i];
                var shape=System.Array.Find(placement.states,p=>p.name==state.stateName);
                if(shape==null) throw new System.Exception("Missing geographic outline for "+state.stateName);
                var tile=Panel(map,state.stateName,20+shape.x*mapWidth,119+shape.y*mapHeight,shape.width*mapWidth,shape.height*mapHeight,Color.white);
                tile.sprite=ImportSprite("Assets/Content/Presidency/Map/"+state.stateName+".png",true);
                tile.alphaHitTestMinimumThreshold=.2f;
                var button=tile.gameObject.AddComponent<Button>(); button.targetGraphic=tile;
                view.mapButtons[i]=button; view.mapTiles[i]=tile;
            }
            var borders=Panel(map,"State boundaries",20,119,mapWidth,mapHeight,Color.white);
            borders.sprite=ImportSprite("Assets/Content/Presidency/Map/Borders.png",false); borders.raycastTarget=false;
            // Northeast states and DC remain easy to select on a phone.
            string[] smallStates={"DC","DE","RI","CT","NJ","MA"};
            view.stateShortcuts=new Button[smallStates.Length]; view.shortcutStates=new int[smallStates.Length];
            for(int i=0;i<smallStates.Length;i++)
            {
                view.shortcutStates[i]=System.Array.FindIndex(campaign.nation.states,s=>s.abbreviation==smallStates[i]);
                view.stateShortcuts[i]=Choice(map,smallStates[i]+" shortcut",20+i*59,342,55,38,smallStates[i],out var smallLabel);
                smallLabel.fontSize=16;
            }
            view.mapSummary=Label(map,"Vote Totals",20,385,350,46,"",24,Paper,TextAnchor.MiddleCenter);
            view.mapDetail=Label(map,"State Detail",24,437,342,80,"",18,Paper,TextAnchor.MiddleCenter);
            var congress=Rect(panel.transform,"Chambers",0,0,390,520); view.congressGroup=congress.gameObject;
            view.houseText=Label(congress,"House Seats",24,117,342,68,"",26,Paper);
            Panel(congress,"House Opposition",24,193,342,14,new Color(.73f,.30f,.22f));
            view.houseFill=Panel(congress,"House Coalition",24,193,342,14,new Color(.12f,.43f,.48f));
            view.senateText=Label(congress,"Senate Seats",24,232,342,68,"",26,Paper);
            Panel(congress,"Senate Opposition",24,307,342,14,new Color(.73f,.30f,.22f));
            view.senateFill=Panel(congress,"Senate Coalition",24,307,342,14,new Color(.12f,.43f,.48f));
            foreach(var fill in new[]{view.houseFill,view.senateFill}) { fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Content/Presidency/Meter.png"); fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; }
            view.congressNote=Label(congress,"Balance of Power",24,355,342,164,"",19,Paper);
            var court=Rect(panel.transform,"Justices",0,0,390,520); view.courtGroup=court.gameObject;
            view.courtTiles=new Image[9]; view.courtLabels=new TMP_Text[9];
            for(int i=0;i<9;i++)
            {
                var tile=Panel(court,"Justice "+(i+1),24+(i%3)*116,125+(i/3)*82,110,72,Gold);
                view.courtTiles[i]=tile;
                view.courtLabels[i]=Label(tile.transform,"Seat",4,8,102,56,"Seat "+(i+1),16,Color.white,TextAnchor.MiddleCenter);
            }
            view.courtSummary=Label(court,"Appointments",24,397,342,120,"",20,Paper,TextAnchor.MiddleCenter);
            view.actionButton.transform.SetAsLastSibling(); view.closeButton.transform.SetAsLastSibling();
            panel.gameObject.SetActive(false); return view;
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        [System.Serializable] class MapPlacement { public MapShape[] states; }
        [System.Serializable] class MapShape { public string name; public float x,y,width,height; }
        static Sprite ImportSprite(string path,bool readable)
        {
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null) throw new System.Exception("Missing artwork: "+path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=true; importer.isReadable=readable; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Trilinear;
            importer.maxTextureSize=2048; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static TMP_FontAsset CrispFont()
        {
            const string path="Assets/Content/Presidency/PresidencyFont.asset";
            var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(asset!=null) return asset;
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            asset=TMP_FontAsset.CreateFontAsset(source,120,12,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
            asset.name="Presidency • high resolution";
            string characters="";
            for(int i=32;i<127;i++) characters+=(char)i;
            characters+="•·—–‘’“”…";
            if(!asset.TryAddCharacters(characters,out string missing)) throw new System.Exception("Missing font glyphs: "+missing);
            AssetDatabase.CreateAsset(asset,path);
            AssetDatabase.AddObjectToAsset(asset.material,asset);
            foreach(var atlas in asset.atlasTextures) { atlas.filterMode=FilterMode.Bilinear; AssetDatabase.AddObjectToAsset(atlas,asset); }
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
            return asset;
        }
        static void EnsureMeterSprite()
        {
            const string path="Assets/Content/Presidency/Meter.png";
            if(!File.Exists(path))
            {
                var texture=new Texture2D(2,2);
                texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white}); texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType!=TextureImporterType.Sprite)
            {
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false; importer.filterMode=FilterMode.Point; importer.SaveAndReimport();
            }
        }
        static Image Panel(Transform p,string n,float x,float y,float w,float h,Color color)
        { var r=Rect(p,n,x,y,w,h); var image=r.gameObject.AddComponent<Image>(); image.color=color; return image; }
        static TMP_Text Label(Transform p,string n,float x,float y,float w,float h,string value,int size,Color color,TextAnchor alignment=TextAnchor.UpperLeft)
        { var r=Rect(p,n,x,y,w,h); var text=r.gameObject.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSharedMaterial=sharpText; text.text=value; text.fontSize=size; text.color=color; text.alignment=Align(alignment); text.enableAutoSizing=false; text.richText=true; text.raycastTarget=false; return text; }
        static TextAlignmentOptions Align(TextAnchor alignment)
        {
            switch(alignment)
            {
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                default: return TextAlignmentOptions.TopLeft;
            }
        }
        static Button Choice(Transform p,string n,float x,float y,float w,float h,string value,out TMP_Text label)
        {
            var panel=Panel(p,n,x,y,w,h,new Color(.26f,.32f,.31f)); var button=panel.gameObject.AddComponent<Button>(); button.targetGraphic=panel;
            var colors=button.colors; colors.highlightedColor=new Color(1,.9f,.65f); colors.pressedColor=new Color(.7f,.7f,.6f); button.colors=colors;
            label=Label(panel.transform,"Label",10,6,w-20,h-12,value,23,Paper,TextAnchor.MiddleCenter); return button;
        }
    }
}
