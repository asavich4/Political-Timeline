using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PoliticalTimeline.Editor
{
    public static class PresidencyPreview
    {
        public static void BatchCheck()
        {
            PresidencySetup.BatchSetup();
            Check(IPhonePreview.EnsureSize()>=0,"Portrait Game view preset");
            var game=Object.FindFirstObjectByType<PresidencyGame>();
            game.Initialize();
            Check(game.portrait.sprite!=null && game.briefing.text!="", "Card presentation");
            Capture("PresidencyPreview.png",390,844,new Rect(0,34,390,763));
            Capture("PresidencyCompactPreview.png",375,667,new Rect(0,0,375,647));
            Capture("PresidencyTallPreview.png",430,932,new Rect(0,34,430,839));
            game.leftButton.onClick.Invoke(); Check(game.Decisions==1,"Left choice");
            game.restartButton.onClick.Invoke(); Check(game.Decisions==0,"Restart");
            Swipe(game,new Vector2(20,0)); Check(game.Decisions==0,"Short drag cancels");
            Swipe(game,new Vector2(100,180)); Check(game.Decisions==0,"Vertical gesture cancels");
            Swipe(game,new Vector2(110,0)); Check(game.Decisions==1,"Right swipe");
            game.Restart(); Swipe(game,new Vector2(-110,0)); Check(game.Decisions==1,"Left swipe");
            game.Restart(); game.rightButton.onClick.Invoke(); Check(game.Decisions==1,"Right choice");
            game.Restart(); game.helpButton.onClick.Invoke();
            Check(game.helpPanel.activeSelf,"Help opens");
            game.leftButton.onClick.Invoke(); Swipe(game,new Vector2(110,0)); Check(game.Decisions==0,"Help blocks gameplay");
            game.closeHelpButton.onClick.Invoke(); game.rightButton.onClick.Invoke(); Check(game.Decisions==1,"Help closes and play resumes");
            Check(PlayerSettings.defaultInterfaceOrientation==UIOrientation.Portrait && !PlayerSettings.allowedAutorotateToLandscapeLeft && !PlayerSettings.allowedAutorotateToLandscapeRight,"Portrait orientation lock");
            File.AppendAllText("Validation.txt","\nPortrait renders and safe-area checks passed at 390x844, 375x667 and 430x932.\nBoth choice buttons, restart, both swipe directions, canceled gestures and help overlay checks passed.\nPortrait orientation locked; iPhone target configured.\nPhysical iPhone testing and an iOS build have not been performed.\n"+ContentWorkshop.Simulate(game.campaign));
        }

        static void Swipe(PresidencyGame game,Vector2 localDelta)
        {
            var parent=(RectTransform)game.cardTransform.parent;
            Vector2 origin=RectTransformUtility.WorldToScreenPoint(null,parent.TransformPoint(Vector3.zero));
            Vector2 end=RectTransformUtility.WorldToScreenPoint(null,parent.TransformPoint(localDelta));
            var drag=new PointerEventData(EventSystem.current) { position=origin };
            game.BeginDrag(drag); drag.position=end; game.Drag(drag); game.EndDrag(drag);
        }
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
                camera.Render(); RenderTexture.active=texture;
                image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
                var corners=new Vector3[4]; layout.content.GetWorldCorners(corners);
                foreach(var point in corners)
                {
                    var pixel=camera.WorldToScreenPoint(point);
                    Check(pixel.x>=safePixels.xMin-1 && pixel.x<=safePixels.xMax+1 && pixel.y>=safePixels.yMin-1 && pixel.y<=safePixels.yMax+1,"Board fits safe area at "+width+"x"+height);
                }
                var buttonCorners=new Vector3[4]; game.leftButton.GetComponent<RectTransform>().GetWorldCorners(buttonCorners);
                float pixelHeight=Vector3.Distance(camera.WorldToScreenPoint(buttonCorners[0]),camera.WorldToScreenPoint(buttonCorners[1]));
                Check(pixelHeight>=44,"Choice tap target at "+width+"x"+height);
                game.helpButton.GetComponent<RectTransform>().GetWorldCorners(buttonCorners);
                pixelHeight=Vector3.Distance(camera.WorldToScreenPoint(buttonCorners[0]),camera.WorldToScreenPoint(buttonCorners[1]));
                Check(pixelHeight>=44,"Help tap target at "+width+"x"+height);
                Check(game.briefing.fontSize * layout.content.localScale.x * canvas.scaleFactor >= 25, "Question remains large on compact phones");
                VerifyCardText(game);
            }
            finally
            {
                canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null;
                camera.targetTexture=null; RenderTexture.active=previous;
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=qualityPipeline;
                Object.DestroyImmediate(image); texture.Release(); Object.DestroyImmediate(texture);
            }
        }
        static void VerifyCardText(PresidencyGame game)
        {
            // Check every authored card at the same layout, not only the randomly drawn preview.
            foreach(var card in game.campaign.cards)
            {

                Fits(game.briefing,card.briefing,card.id);
                Fits(game.leftLabel,card.left.label,card.id);
                Fits(game.rightLabel,card.right.label,card.id);
            }
        }
        static void Fits(Text text,string value,string id)
        {
            var settings=text.GetGenerationSettings(text.rectTransform.rect.size);
            if(text.resizeTextForBestFit) { settings.resizeTextForBestFit=false; settings.fontSize=text.resizeTextMinSize; }
            float needed=new TextGenerator().GetPreferredHeight(value,settings)/text.pixelsPerUnit;
            Check(needed<=text.rectTransform.rect.height+1,id+" / "+text.name+" fits ("+needed+")");
        }
    }
}
