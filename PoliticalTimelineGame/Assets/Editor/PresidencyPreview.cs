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
            StoryChecks.Run(game.campaign);
            ElectoralChecks.Run(game.campaign);
            game.Initialize();
            Check(game.GetComponentsInChildren<PowerIcon>().Length==4,"Four geometric power symbols");
            foreach(var icon in game.powerIcons) Check(!icon.enabled,"Legacy power image hidden");
            Check(game.briefing.font.atlasWidth==2048,"High-resolution font atlas");
            string[] powerNames={"Workers","Middle class","Economy","Elites"};
            for(int i=0;i<4;i++)
            {
                game.powerButtons[i].onClick.Invoke();
                Check(game.helpTitle.text==powerNames[i] && game.helpText.text.Contains("50 / 100"),"Correct power details");
                game.closeHelpButton.onClick.Invoke();
            }
            Check(game.portrait.enabled && game.portrait.sprite!=null && game.GetComponentInChildren<AdvisorArt>()==null && game.briefing.text!="", "Image sprite card presentation");
            Check(game.choiceOverlay.alpha==0,"Choices hidden at rest");
            Check(game.briefing.fontSharedMaterial.GetFloat("_Sharpness")>.3f,"Sharp text material applied");
            Capture("PresidencyPreview.png",390,844,new Rect(0,34,390,763));
            Capture("PresidencyCompactPreview.png",375,667,new Rect(0,0,375,647));
            Capture("PresidencyTallPreview.png",430,932,new Rect(0,34,430,839));
            Capture("PresidencyRetinaPreview.png",1170,2532,new Rect(0,102,1170,2289));
            foreach(string id in new[]{"rail_vote","court_nominee","water","harvest"})
            {
                game.State.current=game.campaign.cards.Find(c=>c.id==id); game.Refresh();
                Capture(id+"Preview.png",390,844,new Rect(0,34,390,763));
            }
            foreach(string id in new[]{"grid_start","care_start","nursery_start","presslaw_start","soil_start","ballot_start"})
            {
                game.State.current=game.campaign.cards.Find(c=>c.id==id); game.Refresh();
                Capture(id+"Preview.png",390,844,new Rect(0,34,390,763));
            }
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
            Check(game.IsTransitioning,"Release starts animation");
            Vector2 released=game.cardTransform.anchoredPosition;
            game.Decide(false); game.nationalPanels.navigation[0].onClick.Invoke(); game.ShowPower(0);
            Check(game.Decisions==1 && !game.nationalPanels.IsOpen && !game.helpPanel.activeSelf,"Animation blocks repeat decisions and panels");
            game.AdvanceTransition(.15f);
            Check(game.cardTransform.anchoredPosition.x>released.x,"Card travels toward committed side");
            Capture("SwipeDeparturePreview.png",390,844,new Rect(0,34,390,763));
            game.AdvanceTransition(.45f);
            Check(!game.cardTransform.gameObject.activeSelf && game.IsTransitioning,"Consequence pause hides departing card");
            var outcome=game.transform.Find("Safe Area/Portrait Layout • 390 x 640/Decision outcome/Outcome").GetComponent<TMP_Text>();
            Check(outcome.text.StartsWith(game.State.lastResult),"Consequence displays resolved outcome");
            Capture("SwipeOutcomePreview.png",390,844,new Rect(0,34,390,763));
            game.AdvanceTransition(2);
            Check(game.AwaitingAcknowledgement,"Consequence waits for acknowledgement");
            game.AdvanceTransition(60); Check(game.AwaitingAcknowledgement && game.Decisions==1,"Reading never advances on a timer");
            Swipe(game,new Vector2(25,0)); Check(game.AwaitingAcknowledgement,"Short acknowledgement swipe cancels");
            Swipe(game,new Vector2(100,180)); Check(game.AwaitingAcknowledgement,"Vertical acknowledgement swipe cancels");
            Swipe(game,new Vector2(-110,0)); Check(!game.AwaitingAcknowledgement && game.Decisions==1,"Second swipe acknowledges without a second decision");
            game.AdvanceTransition(2);
            Check(!game.IsTransitioning && game.cardTransform.gameObject.activeSelf,"New card finishes entering");
            for(int i=0;i<4;i++) Check(Mathf.Abs(game.supportFills[i].fillAmount-game.State.support[i]/100f)<.001f,"Animated meters reach exact support");
            game.restartButton.onClick.Invoke(); Check(game.Decisions==0,"Restart");
            Swipe(game,new Vector2(-110,0)); Check(game.Decisions==1,"Left swipe commits");
            released=game.cardTransform.anchoredPosition; game.AdvanceTransition(.15f);
            Check(game.cardTransform.anchoredPosition.x<released.x,"Left card travels left");
            game.Restart(); game.helpButton.onClick.Invoke(); Check(game.helpPanel.activeSelf,"Help opens");
            Check(!game.IsTransitioning,"Restart cancels animation");
            Swipe(game,new Vector2(110,0)); Check(game.Decisions==0,"Help blocks swipes");
            game.closeHelpButton.onClick.Invoke(); Swipe(game,new Vector2(110,0)); Check(game.Decisions==1,"Help closes and play resumes");
            Check(PlayerSettings.defaultInterfaceOrientation==UIOrientation.Portrait,"Portrait orientation lock");
            CheckNationalViews(game);
            game.Restart(); game.State.decisions=17; game.State.current=game.campaign.cards.Find(c=>c.id=="factory_tour"); game.Refresh();
            var campaignDrag=Hold(game,new Vector2(-45,0));
            Check(game.cardTransform.Find("Paper caption/Last decision").GetComponent<TMP_Text>().text.Contains("PA"),"Campaign preview names target states");
            Capture("CampaignCardPreview.png",390,844,new Rect(0,34,390,763)); game.EndDrag(campaignDrag);
            game.Decide(false); game.AdvanceTransition(.6f);
            Capture("CampaignOutcomePreview.png",390,844,new Rect(0,34,390,763));
            game.AdvanceTransition(2); Swipe(game,new Vector2(110,0)); game.AdvanceTransition(2);
            game.nationalPanels.Open(0,false);
            int pennsylvania=System.Array.FindIndex(game.State.nation.states,s=>s.abbreviation=="PA");
            game.nationalPanels.mapButtons[pennsylvania].onClick.Invoke();
            Check(Mathf.Abs(game.State.nation.VoterSupportBonus(pennsylvania)-4)<.001f,"Campaign effect remains after reading acknowledgement");
            Capture("CampaignMapPreview.png",390,844,new Rect(0,34,390,763)); game.nationalPanels.Close();
            game.State.nation.HoldsPresidency=false; game.State.current=game.campaign.cards.Find(c=>c.id=="opp_spending"); game.Refresh();
            Capture("OppositionCardPreview.png",390,844,new Rect(0,34,390,763));
            game.Restart(); game.State.current=game.campaign.cards.Find(c=>c.id=="harvest"); game.State.support[0]=1; game.Refresh();
            game.Decide(true); Check(game.State.ended && game.IsTransitioning,"Fatal decision still animates");
            game.AdvanceTransition(2); Check(game.AwaitingAcknowledgement,"Fatal consequence waits for reading");
            Swipe(game,new Vector2(110,0)); game.AdvanceTransition(2);
            Check(!game.IsTransitioning && game.restartButton.gameObject.activeSelf,"Fatal consequence leads to restart after acknowledgement");
            File.AppendAllText("Validation.txt","\nSDF text, full-card artwork, safe areas and text fit verified at 375x667, 390x844, 430x932 and 1170x2532.\nDirectional hold previews, delayed commit, release threshold, canceled gestures, restart and help checks passed.\nPhysical iPhone testing and an iOS build have not been performed.");
            File.AppendAllText("Validation.txt","\nCampaign-season gating, state voter changes, bounded decay, opposition weighting/blocking, expanded policy ledger, and four geometric power icons verified.");
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
            Check(game.nationalPanels.StateSupportFill.gameObject.activeInHierarchy,"State selection shows support bar");
            float expected=(50+game.State.nation.Margin(4,game.State.support)/2)/100;
            Check(Mathf.Abs(game.nationalPanels.StateSupportFill.fillAmount-expected)<.001f,"State support matches projected margin");
            Capture("StateMapPreview.png",390,844,new Rect(0,34,390,763));
            game.nationalPanels.closeButton.onClick.Invoke();
            game.nationalPanels.navigation[1].onClick.Invoke();
            Check(game.nationalPanels.HouseChart.Total==435 && game.nationalPanels.HouseChart.Allied==game.State.nation.houseSeats,"House chart shows all seats and exact coalition");
            Check(game.nationalPanels.SenateChart.Total==100 && game.nationalPanels.SenateChart.Allied==game.State.nation.SenateSeats,"Senate chart shows all seats and exact coalition");
            Capture("CongressPreview.png",390,844,new Rect(0,34,390,763));
            Check(game.nationalPanels.HouseChart.canvasRenderer!=null && game.nationalPanels.SenateChart.canvasRenderer!=null,"Seat charts have renderers");
            Capture("CongressCompactPreview.png",375,667,new Rect(0,0,375,647));
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
                    Check(!game.nationalPanels.IsOpen && game.IsTransitioning,"Election waits for swipe consequence");
                    game.AdvanceTransition(2);
                    Check(game.AwaitingAcknowledgement && !game.nationalPanels.IsOpen,"Election waits for acknowledgement");
                    Swipe(game,new Vector2(110,0)); game.AdvanceTransition(2);
                    Check(game.State.ElectionPending && game.nationalPanels.IsOpen,"November automatically opens election results");
                }
                else { game.State.Choose(false); game.Refresh(); }
                Check(!game.State.ended,"Stable test administration survives");
                if(month==12)
                {
                    Check(game.background.color.r>.95f && game.briefing.color.r<.2f,"Election year white theme and dark text");
                    Check(game.monthLabel.text.Contains("Midterm election year"),"Midterm year label");
                    Capture("ElectionYearPreview.png",390,844,new Rect(0,34,390,763));
                }
                if(month==36)
                {
                    Check(game.monthLabel.text.Contains("Presidential election year"),"Presidential year label");
                    Capture("PresidentialYearPreview.png",390,844,new Rect(0,34,390,763));
                    Capture("PresidentialYearCompactPreview.png",375,667,new Rect(0,0,375,647));
                }
                if(month==18)
                {
                    game.nationalPanels.navigation[2].onClick.Invoke();
                    Check(!game.nationalPanels.actionButton.interactable,"Minority Senate blocks nomination");
                    game.State.nation.Elect(2026,new[]{90,90,90,90});
                    game.nationalPanels.Open(2,false);
                    game.nationalPanels.actionButton.onClick.Invoke();
                    Check(game.State.nation.Vacancy<0,"Nomination fills vacancy");
                    game.nationalPanels.closeButton.onClick.Invoke();
                }
                if(game.State.ElectionPending)
                {
                    game.nationalPanels.OpenElection();
                    Check(game.nationalPanels.Night.Count==0,"Election begins with no reported states");
                    game.nationalPanels.mapButtons[4].onClick.Invoke();
                    Check(game.nationalPanels.mapDetail.text.Contains("Awaiting returns"),"Uncalled state does not leak final result");
                    game.nationalPanels.AdvanceElectionNight(5);
                    Check(game.nationalPanels.Night.Count>0 && !game.nationalPanels.Night.Complete,"Partial returns arrive over time");
                    if(month==47) Capture("ElectionNightPreview.png",390,844,new Rect(0,34,390,763));
                    else Capture("MidtermNightPreview.png",390,844,new Rect(0,34,390,763));
                    int before=game.Decisions; game.Decide(true); Check(game.Decisions==before,"Results block decisions");
                    if(month==47) game.nationalPanels.actionButton.onClick.Invoke();
                    else game.nationalPanels.AdvanceElectionNight(100);
                    Check(game.nationalPanels.Night.Complete && game.State.ElectionPending,"Skip or natural completion preserves pending election");
                    if(month==47) Capture("ElectionResultsPreview.png",390,844,new Rect(0,34,390,763));
                    game.nationalPanels.actionButton.onClick.Invoke();
                    Check(game.nationalPanels.congressGroup.activeSelf,"Results continue to Congress");
                    if(month==47) Capture("ElectionCongressPreview.png",390,844,new Rect(0,34,390,763));
                    game.nationalPanels.actionButton.onClick.Invoke();
                    Check(!game.State.ElectionPending && !game.nationalPanels.IsOpen,"Results return to cards");
                }
            }
            File.AppendAllText("Validation.txt","\nSwipe departure, indefinite reading pause, acknowledgement without another decision, animated meters, entry and input lock verified.\nHouse and Senate seat counts and selected-state support bar verified.\nProgressive presidential and midterm returns, uncalled-state privacy, skip and natural completion verified.\nMap, state selection, Congress, court nominations, annual theme and automatic November result flow verified.");
            game.Restart(); game.nationalPanels.navigation[3].onClick.Invoke();
            Check(game.nationalPanels.title.text=="Enacted policies" && game.State.Policies.Count==0,"Policies opens read-only empty ledger");
            Capture("PoliciesEmptyPreview.png",390,844,new Rect(0,34,390,763)); game.nationalPanels.Close();
            game.State.nation.Elect(2026,new[]{90,90,90,90});
            foreach(string id in new[]{"rail_vote","school_lunch","drug_prices","flood_vote"}) { game.State.current=game.campaign.cards.Find(c=>c.id==id); game.State.Choose(false); }
            game.Refresh(); game.nationalPanels.navigation[3].onClick.Invoke(); Check(game.State.Policies.Count==4,"Successful policy events populate ledger");
            Capture("PoliciesPreview.png",390,844,new Rect(0,34,390,763));
            game.nationalPanels.actionButton.onClick.Invoke(); Check(game.State.Policies.Count==4,"Policy paging cannot enact laws");
            Capture("PoliciesPageTwoPreview.png",375,667,new Rect(0,0,375,647));
            game.nationalPanels.Close();
            while(game.Decisions<47)
            {
                if(game.State.ElectionPending) game.State.AcknowledgeElection();
                for(int i=0;i<4;i++) game.State.support[i]=30;
                game.State.Choose(false);
            }
            Check(!game.State.nation.HoldsPresidency && !game.State.ended,"Lost presidency leaves party alive");
            int survivingPolicies=game.State.Policies.Count;
            game.nationalPanels.OpenElection(); game.nationalPanels.AdvanceElectionNight(100);
            game.nationalPanels.actionButton.onClick.Invoke(); game.nationalPanels.actionButton.onClick.Invoke();
            Check(!game.State.ElectionPending && !game.nationalPanels.IsOpen && !game.State.ended,"Lost election returns to playable party");
            Check(game.monthLabel.text.Contains("Opposition") && game.State.Policies.Count==survivingPolicies,"Opposition status and policies persist");
            Capture("OppositionPreview.png",390,844,new Rect(0,34,390,763));
            File.AppendAllText("Validation.txt","\nParty survives electoral defeat, returns to play in opposition, and retains enacted policies. Event-only enactment/repeal and read-only policy paging verified. Campaign rule checks cover twenty years, loss and regain of power, zero-only failure and harder elections.");
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
                Fits(game.monthLabel,game.monthLabel.text,"Election/date heading");
                var policyList=game.nationalPanels.transform.Find("Policies/Policy list").GetComponent<TMP_Text>();
                if(policyList.gameObject.activeInHierarchy) Fits(policyList,policyList.text,"Policy page");
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
                    var caption=game.cardTransform.Find("Paper caption/Last decision").GetComponent<TMP_Text>();
                    var advisor=game.cardTransform.Find("Paper caption/Advisor").GetComponent<TMP_Text>();
                    Fits(advisor,card.advisor+"  /  "+card.category,card.id);
                    foreach(var choice in new[]{card.left,card.right})
                    {
                        Fits(caption,choice.consequence,card.id);
                        Fits(game.cardTransform.parent.Find("Decision outcome/Outcome").GetComponent<TMP_Text>(),choice.consequence,card.id);
                        if(choice.institution!=InstitutionRule.None) Fits(caption,choice.blockedConsequence,card.id);
                    }
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
