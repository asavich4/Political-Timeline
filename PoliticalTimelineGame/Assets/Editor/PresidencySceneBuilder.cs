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

        [MenuItem("Political Timeline/Create Starter Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild starter scene?", "This replaces the Presidency scene layout. Your card assets and original Main scene are preserved.", "Rebuild", "Cancel")) return;
            var campaign = PresidencyContent.CreateStarter();
            EnsureMeterSprite();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var cam = new GameObject("Main Camera", typeof(Camera)); cam.tag = "MainCamera";
            cam.GetComponent<Camera>().backgroundColor = Ink; cam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            var canvasObject = new GameObject("Presidency • Portrait", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect=true;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390,844); scaler.matchWidthOrHeight = 0;
            var game = canvasObject.AddComponent<PresidencyGame>(); game.campaign = campaign;
            var bg = Panel(canvas.transform,"Background",0,0,390,844,Ink);
            bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            var safe=Rect(canvas.transform,"Safe Area",0,0,390,844);
            safe.anchorMin=Vector2.zero; safe.anchorMax=Vector2.one; safe.offsetMin=safe.offsetMax=Vector2.zero;
            var board=Rect(safe,"Portrait Layout • 390 x 640",0,0,390,640);
            board.anchorMin=board.anchorMax=board.pivot=new Vector2(.5f,.5f); board.anchoredPosition=Vector2.zero;
            var layout=canvasObject.AddComponent<PortraitLayout>(); layout.safeArea=safe; layout.content=board;
            Transform root=board;
            // One screen, one decision. All secondary information lives behind the meters.
            var meters=Rect(root,"Support • tap for details",20,0,350,70);
            var meterButton=meters.gameObject.AddComponent<Button>();
            var meterHit=meters.gameObject.AddComponent<Image>(); meterHit.color=Color.clear;
            meterButton.targetGraphic=meterHit; game.helpButton=meterButton;
            game.supportFills = new Image[4];
            game.supportChanges = new TMP_Text[4];
            string[] names = { "Workers", "Middle\nclass", "Security", "Elites" };
            for(int i=0;i<4;i++)
            {
                float x=i*90;
                Label(meters,"Group "+i,x,0,80,40,names[i],16,Paper,TextAnchor.MiddleCenter);
                Panel(meters,"Track "+i,x,47,80,9,new Color(.32f,.37f,.36f)).raycastTarget=false;
                var fill=Panel(meters,"Support "+i,x,47,80,9,Gold);
                fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Content/Presidency/Meter.png");
                fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillAmount=.5f; fill.raycastTarget=false;
                game.supportFills[i]=fill;
                game.supportChanges[i]=Label(meters,"Change "+i,x,57,80,22,"",18,Paper,TextAnchor.MiddleCenter);
            }
            game.briefing=Label(root,"Question",20,88,350,140,campaign.cards[0].briefing,28,Paper,TextAnchor.MiddleCenter);
            game.briefing.fontStyle=FontStyles.Bold;
            var card=Panel(root,"Decision Card",20,246,350,350,Color.clear);
            card.rectTransform.pivot=new Vector2(.5f,.5f);
            card.rectTransform.anchoredPosition+=new Vector2(175,-175);
            game.cardTransform=card.rectTransform; card.gameObject.AddComponent<CardDrag>().game=game;
            // Square portraits fill the card edge to edge. No cream frame or text area.
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
            game.restartButton=Choice(root,"New Administration",20,550,350,76,"Try again",out _); game.restartButton.gameObject.SetActive(false);
            var help=Panel(root,"Details",0,0,390,640,Ink); game.helpPanel=help.gameObject;
            Label(help.transform,"Help Heading",20,26,350,44,"Keep the balance",28,Paper,TextAnchor.MiddleCenter);
            game.helpText=Label(help.transform,"Help Text",28,105,334,402,"Keep every group between 0 and 100.\n\nHold left or right, then release.",23,Paper,TextAnchor.UpperLeft);
            game.closeHelpButton=Choice(help.transform,"Close Help",20,552,350,76,"Back to the card",out _);
            help.gameObject.SetActive(false);
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

        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
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
        { var r=Rect(p,n,x,y,w,h); var text=r.gameObject.AddComponent<TextMeshProUGUI>(); text.font=font; text.text=value; text.fontSize=size; text.color=color; text.alignment=Align(alignment); text.enableAutoSizing=false; text.richText=true; text.raycastTarget=false; return text; }
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
