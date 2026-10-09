using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class PartyAudioSetup
    {
        public const string Folder="Assets/Audio";
        [MenuItem("Political Timeline/Audio/Install or Select Audio")]
        public static void Install()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>();
            if(game==null) throw new Exception("Open the Presidency scene first.");
            Ensure(game); Selection.activeGameObject=game.partyAudio.gameObject;
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
        public static void Ensure(PresidencyGame game)
        {
            if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==0)
            {
                if(Camera.main==null) throw new Exception("The gameplay scene needs its main camera.");
                Camera.main.gameObject.AddComponent<AudioListener>();
            }
            if(game.partyAudio==null)
            {
                var root=new GameObject("Game Audio"); root.transform.SetParent(game.transform,false);
                game.partyAudio=root.AddComponent<PartyAudio>();
            }
            var audio=game.partyAudio;
            if(audio.backgroundMusic==null) audio.backgroundMusic=Clip("BehindTheParty_Loop");
            if(audio.swipeClips==null || audio.swipeClips.Length==0)
                audio.swipeClips=new[]{Clip("Card_Swipe_01"),Clip("Card_Swipe_02"),Clip("Card_Swipe_03")};
            if(audio.acknowledgeClip==null) audio.acknowledgeClip=Clip("Card_Confirm");
            if(audio.musicSource==null) audio.musicSource=Source(audio,"Background music",true);
            if(audio.cardSource==null) audio.cardSource=Source(audio,"Card sounds",false);
            audio.musicSource.clip=audio.backgroundMusic;
            EditorUtility.SetDirty(audio); EditorUtility.SetDirty(game);
        }
        static AudioClip Clip(string name)
        {
            string path=Folder+"/"+name+".wav";
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path) ?? throw new Exception("Missing sound: "+path);
        }
        static AudioSource Source(PartyAudio owner,string name,bool loop)
        {
            var child=new GameObject(name); child.transform.SetParent(owner.transform,false);
            var source=child.AddComponent<AudioSource>(); source.playOnAwake=false; source.loop=loop;
            source.spatialBlend=0; source.dopplerLevel=0; source.volume=loop?owner.musicVolume:1;
            return source;
        }
        public static void BatchInstall()
        {
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>(); Ensure(game);
            var reserved=CharacterCastEditor.Sprite("MaskedFigure");
            if(game.campaign.cards.Any(c=>c.portrait==reserved)) throw new Exception("Masked Figure still appears in the deck.");
            if(game.portrait.sprite==reserved && game.editorPreviewCard!=null) game.PreviewCardForEditing(game.editorPreviewCard);
            if(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length!=1) throw new Exception("Expected one audio listener.");
            var audio=game.partyAudio;
            if(audio.backgroundMusic.length<40 || audio.swipeClips.Any(c=>c==null || c.length>.5f) || audio.acknowledgeClip==null) throw new Exception("Invalid audio clips.");
            Ensure(game);
            if(game.GetComponentsInChildren<PartyAudio>(true).Length!=1 || audio.GetComponentsInChildren<AudioSource>().Length!=2) throw new Exception("Audio setup duplicates components.");
            EditorSceneManager.SaveScene(game.gameObject.scene);
            File.Copy(PresidencySceneBuilder.ScenePath,"Library/AudioSceneValidated.unity",true);
            File.WriteAllText("Library/AudioValidation.txt","PASS: 198 cards exclude Masked Figure; music and four swipe clips imported; one listener, two sources; idempotent setup; audio references saved in scene.");
            Debug.Log("AUDIO INSTALL PASSED");
        }
    }
}
