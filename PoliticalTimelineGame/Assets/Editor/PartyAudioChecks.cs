using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    [InitializeOnLoad]
    public static class PartyAudioChecks
    {
        const string Key="PartyAudioChecks.Running";
        static double began;
        static int stage;
        static PartyAudioChecks()
        {
            if(!SessionState.GetBool(Key,false)) return;
            began=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
        }
        // Batch invocation intentionally omits -quit: exit after runtime checks finish.
        public static void BatchPlayCheck()
        {
            PartyAudioSetup.BatchInstall();
            SessionState.SetBool(Key,true);
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool ok,string text) { if(!ok) throw new Exception(text); }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-began>60) throw new Exception("Audio runtime check timed out.");
                if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-began<3) return;
                var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>();
                if(game==null || game.State==null) return;
                var audio=game.partyAudio;
                if(stage==0)
                {
                    Check(audio.musicSource.isPlaying && audio.musicSource.loop,"Looping music starts on the menu");
                    Check(audio.musicSource.volume>0,"Music fades in");
                    game.frontMenu.panel.SetActive(false);
                    game.Decide(true);
                    Check(audio.cardSource.isPlaying,"Committed decision plays a swipe sound");
                    int count=(int)typeof(PartyAudio).GetField("nextSwipe",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio);
                    game.Decide(true);
                    Check(count==(int)typeof(PartyAudio).GetField("nextSwipe",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio),"Blocked repeat input does not play another sound");
                    game.AdvanceTransition(2);
                    Check(game.AwaitingAcknowledgement,"Consequence waits for confirmation");
                    audio.cardSource.Stop(); game.AcknowledgeOutcome(false);
                    Check(audio.cardSource.isPlaying,"Consequence confirmation plays paper sound");
                    audio.cardSource.Stop(); audio.muteCards=true; audio.PlaySwipe(true);
                    Check(!audio.cardSource.isPlaying,"Card mute works");
                    audio.muteCards=false; audio.muteMusic=true;
                    stage=1; return;
                }
                Check(audio.musicSource.volume==0,"Music mute works without restarting loop");
                audio.muteMusic=false;
                float position=audio.musicSource.time;
                game.UseCampaign(CampaignState.Restore(game.campaign,game.State.Export()),false);
                game.frontMenu.ShowStart();
                Check(audio.musicSource.isPlaying && audio.musicSource.time>=position,"Save/load and menus preserve music playback");
                File.AppendAllText("Library/AudioValidation.txt","\nPASS: Play Mode music startup/fade, committed swipe, duplicate-input suppression, confirmation swipe, independent mutes, uninterrupted music across load/menu.");
                End(0);
            }
            catch(Exception e) { Debug.LogException(e); End(1); }
        }
        static void End(int status)
        {
            SessionState.SetBool(Key,false); EditorApplication.update-=Tick;
            Debug.Log(status==0?"AUDIO PLAY CHECKS PASSED":"AUDIO PLAY CHECKS FAILED");
            EditorApplication.Exit(status);
        }
    }
}
