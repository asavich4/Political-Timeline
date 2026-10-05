using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace PoliticalTimeline.Editor
{
    public static class PresidencyPreview
    {
        public static void BatchCheck()
        {
            PresidencySetup.BatchSetup();
            Check(IPhonePreview.EnsureSize()>=0,"Portrait Game view preset");
            IPhonePreview.ValidatePreviewApi();
            var game=Object.FindFirstObjectByType<PresidencyGame>();
            game.Initialize();
            Check(game.powerIcons.Length==4,"Four illustrated power images");
            foreach(var icon in game.powerIcons) Check(icon.sprite!=null,"Power image sprite assigned");
            Check(game.briefing.font.atlasWidth==2048,"High-resolution font atlas");
            string[] powerNames={"Workers","Middle class","Economy","Elites"};
            for(int i=0;i<4;i++)
            {
                game.powerButtons[i].onClick.Invoke();
                Check(game.helpTitle.text==powerNames[i] && game.helpText.text.Contains("50 / 100"),"Correct power details");
                game.closeHelpButton.onClick.Invoke();
            }
            Check(game.portrait.sprite!=null && game.briefing.text!="", "Card presentation");
            Check(game.choiceOverlay.alpha==0,"Choices hidden at rest");
            Check(game.briefing.fontSharedMaterial.GetFloat("_Sharpness")>.3f,"Sharp text material applied");
            Capture("PresidencyPreview.png",390,844,new Rect(0,34,390,763));
            Capture("PresidencyCompactPreview.png",375,667,new Rect(0,0,375,647));
            Capture("PresidencyTallPreview.png",430,932,new Rect(0,34,430,839));
            Capture("PresidencyRetinaPreview.png",1170,2532,new Rect(0,102,1170,2289));
            var current=game.campaign.cards.Find(c=>c.briefing==game.briefing.text);
            var drag=Hold(game,new Vector2(-45,0));
            Check(game.choiceLabel.text==current.left.label && game.choiceOverlay.alpha>0,"Left hold reveals left choice");
            Check(game.Decisions==0,"Holding does not commit");
            Capture("PresidencyLeftPreview.png",390,844,new Rect(0,34,390,763));
            game.EndDrag(drag); Check(game.Decisions==0 && game.choiceOverlay.alpha==0,"Short hold cancels and hides overlay");
            drag=Hold(game,new Vector2(45,0));
            Check(game.choiceLabel.text==current.right.label && game.choiceOverlay.alpha>0,"Right hold reveals right choice");
            Capture("PresidencyRightPreview.png",390,844,new Rect(0,34,390,763));
            game.EndDrag(drag);
            drag=Hold(game,new Vector2(110,0));
            var parent=(RectTransform)game.cardTransform.parent;
            drag.position=RectTransformUtility.WorldToScreenPoint(null,parent.TransformPoint(new Vector2(-110,0)));
            game.Drag(drag);
            Check(game.choiceLabel.text==current.left.label && game.Decisions==0,"Changing direction replaces the choice");
            drag.position=RectTransformUtility.WorldToScreenPoint(null,parent.TransformPoint(Vector3.zero));
            game.Drag(drag); game.EndDrag(drag);
            Check(game.choiceOverlay.alpha==0 && game.Decisions==0,"Returning to center cancels");
            Swipe(game,new Vector2(20,0)); Check(game.Decisions==0,"Short drag cancels");
            Swipe(game,new Vector2(100,180)); Check(game.Decisions==0,"Vertical gesture cancels");
            drag=Hold(game,new Vector2(110,0));
            Check(game.commitIndicator.enabled && game.Decisions==0,"Threshold indicates ready without early commit");
            game.EndDrag(drag); Check(game.Decisions==1 && game.choiceOverlay.alpha==0,"Right release commits and clears overlay");
            game.restartButton.onClick.Invoke(); Check(game.Decisions==0,"Restart");
            Swipe(game,new Vector2(-110,0)); Check(game.Decisions==1,"Left swipe commits");
            game.Restart(); game.helpButton.onClick.Invoke(); Check(game.helpPanel.activeSelf,"Help opens");
            Swipe(game,new Vector2(110,0)); Check(game.Decisions==0,"Help blocks swipes");
            game.closeHelpButton.onClick.Invoke(); Swipe(game,new Vector2(110,0)); Check(game.Decisions==1,"Help closes and play resumes");
            Check(PlayerSettings.defaultInterfaceOrientation==UIOrientation.Portrait,"Portrait orientation lock");
            CheckNationalViews(game);
            File.AppendAllText("Validation.txt","\nSDF text, full-card artwork, safe areas and text fit verified at 375x667, 390x844, 430x932 and 1170x2532.\nDirectional hold previews, delayed commit, release threshold, canceled gestures, restart and help checks passed.\nPhysical iPhone testing and an iOS build have not been performed.");
        }

        static void CheckNationalViews(PresidencyGame game)
        {
            game.Restart();
            game.nationalPanels.navigation[0].onClick.Invoke();
            foreach(var tile in game.nationalPanels.mapTiles)
                Check(tile.sprite!=null && tile.alphaHitTestMinimumThreshold>0,"Geographic state sprite and shape hit test");
            for(int i=0;i<game.nationalPanels.stateShortcuts.Length;i++)
            {
                game.nationalPanels.stateShortcuts[i].onClick.Invoke();
                Check(game.nationalPanels.mapDetail.text.Contains(game.State.nation.states[game.nationalPanels.shortcutStates[i]].stateName),"Small state shortcut selects correct state");
            }
            Check(game.nationalPanels.mapGroup.activeSelf && game.nationalPanels.mapTiles.Length==51,"State map opens");
            game.nationalPanels.mapButtons[4].onClick.Invoke();
            Check(game.nationalPanels.mapDetail.text.Contains("California"),"State selection");
            Capture("StateMapPreview.png",390,844,new Rect(0,34,390,763));
            game.nationalPanels.closeButton.onClick.Invoke();
            game.nationalPanels.navigation[1].onClick.Invoke();
            Capture("CongressPreview.png",390,844,new Rect(0,34,390,763));
            game.nationalPanels.closeButton.onClick.Invoke();
            game.nationalPanels.navigation[2].onClick.Invoke();
            Capture("CourtPreview.png",390,844,new Rect(0,34,390,763));
            game.nationalPanels.closeButton.onClick.Invoke();
            for(int month=1;month<=47;month++)
            {
                for(int key=0;key<4;key++) game.State.support[key]=70;
                if(month==47)
                {
                    game.Decide(false);
                    Check(game.State.ElectionPending && game.nationalPanels.IsOpen,"November automatically opens election results");
                }
                else { game.State.Choose(false); game.Refresh(); }
                Check(!game.State.ended,"Stable test administration survives");
                if(month==12)
                {
                    Check(game.background.color.r>.95f && game.briefing.color.r<.2f,"Election year white theme and dark text");
                    Capture("ElectionYearPreview.png",390,844,new Rect(0,34,390,763));
                }
                if(month==18)
                {
                    game.nationalPanels.navigation[2].onClick.Invoke();
                    Check(game.nationalPanels.actionButton.interactable,"Nomination available");
                    game.nationalPanels.actionButton.onClick.Invoke();
                    Check(game.State.nation.Vacancy<0,"Nomination fills vacancy");
                    game.nationalPanels.closeButton.onClick.Invoke();
                }
                if(game.State.ElectionPending)
                {
                    game.nationalPanels.OpenElection();
                    if(month==47) Capture("ElectionResultsPreview.png",390,844,new Rect(0,34,390,763));
                    int before=game.Decisions; game.Decide(true); Check(game.Decisions==before,"Results block decisions");
                    game.nationalPanels.actionButton.onClick.Invoke();
                    Check(game.nationalPanels.congressGroup.activeSelf,"Results continue to Congress");
                    if(month==47) Capture("ElectionCongressPreview.png",390,844,new Rect(0,34,390,763));
                    game.nationalPanels.actionButton.onClick.Invoke();
                    Check(!game.State.ElectionPending && !game.nationalPanels.IsOpen,"Results return to cards");
                }
            }
            File.AppendAllText("Validation.txt","\nMap, state selection, Congress, court nominations, annual theme and automatic November result flow verified.");
        }

        static PointerEventData Hold(PresidencyGame game,Vector2 localDelta)
        {
            var parent=(RectTransform)game.cardTransform.parent;
            Vector2 origin=RectTransformUtility.WorldToScreenPoint(null,parent.TransformPoint(Vector3.zero));
            Vector2 end=RectTransformUtility.WorldToScreenPoint(null,parent.TransformPoint(localDelta));
            var drag=new PointerEventData(EventSystem.current) { position=origin };
            game.BeginDrag(drag); drag.position=end; game.Drag(drag); return drag;
        }
        static void Swipe(PresidencyGame game,Vector2 delta) { var drag=Hold(game,delta); game.EndDrag(drag); }
        static void Check(bool success,string message) { if(!success) throw new System.Exception("Portrait check failed: "+message); }

        static void Capture(string path,int width,int height,Rect safePixels)
        {
            var game=Object.FindFirstObjectByType<PresidencyGame>();
            var canvas=game.GetComponent<Canvas>();
            var layout=game.GetComponent<PortraitLayout>();
            var camera=Camera.main;
            var texture=new RenderTexture(width,height,24);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            float previousScale=canvas.scaleFactor;
            Vector2 previousMin=layout.safeArea.anchorMin, previousMax=layout.safeArea.anchorMax;
            Vector3 previousBoardScale=layout.content.localScale;
            var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            var qualityPipeline=QualitySettings.renderPipeline;
            try
            {
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;
                QualitySettings.renderPipeline=null;
                camera.orthographic=true; camera.orthographicSize=height/2f; camera.transform.position=new Vector3(0,0,-10);
                camera.targetTexture=texture; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                canvas.scaleFactor=width/390f;
                Canvas.ForceUpdateCanvases(); layout.Apply(safePixels,new Vector2(width,height)); Canvas.ForceUpdateCanvases();
                foreach(var label in game.GetComponentsInChildren<TMP_Text>()) label.ForceMeshUpdate();
                camera.Render(); RenderTexture.active=texture;
                image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
                var corners=new Vector3[4]; layout.content.GetWorldCorners(corners);
                foreach(var point in corners)
                {
                    var pixel=camera.WorldToScreenPoint(point);
                    Check(pixel.x>=safePixels.xMin-1 && pixel.x<=safePixels.xMax+1 && pixel.y>=safePixels.yMin-1 && pixel.y<=safePixels.yMax+1,"Board fits safe area at "+width+"x"+height);
                }
                Check(game.portrait.rectTransform.rect.size==game.cardTransform.rect.size,"Portrait fills card");
                Check(game.briefing.fontSize * layout.content.localScale.x * canvas.scaleFactor >= 25,"Question remains large");
                Check(game.briefing.font!=null && game.briefing.font.atlasTexture!=null,"SDF font available");
                foreach(var card in game.campaign.cards)
                {
                    Fits(game.briefing,card.briefing,card.id);
                    Fits(game.choiceLabel,card.left.label,card.id);
                    Fits(game.choiceLabel,card.right.label,card.id);
                }
            }
            finally
            {
                canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null;
                camera.targetTexture=null; RenderTexture.active=previous;
                canvas.scaleFactor=previousScale;
                layout.safeArea.anchorMin=previousMin; layout.safeArea.anchorMax=previousMax;
                layout.content.localScale=previousBoardScale;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=qualityPipeline;
                Object.DestroyImmediate(image); texture.Release(); Object.DestroyImmediate(texture);
            }
        }
        static void Fits(TMP_Text text,string value,string id)
        {
            Vector2 needed=text.GetPreferredValues(value,text.rectTransform.rect.width,Mathf.Infinity);
            Check(needed.y<=text.rectTransform.rect.height+1,id+" / "+text.name+" fits ("+needed.y+")");
        }
    }
}
