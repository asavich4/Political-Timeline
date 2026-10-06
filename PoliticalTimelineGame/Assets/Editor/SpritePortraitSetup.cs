using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class SpritePortraitSetup
    {
        public const string Sheet="Assets/Art/CharacterSprites/PolicyCharacters.png";
        static readonly string[] Names={"Judge","CampaignOrganizer","Farmer","Nurse","Teacher","CongressLeader"};
        static Sprite Existing(string stamp) => AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/ChatGPT Image Jul 2, 2025, "+stamp+".png").OfType<Sprite>().First();
        public static void ConfigureSheet()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Sheet);
            if(importer==null) throw new Exception("Missing new portrait sheet.");
            if(AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().Count()==6) return;
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048; importer.filterMode=FilterMode.Bilinear;
            importer.spritePixelsPerUnit=100;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
            int w=texture.width/3,h=texture.height/2;
#pragma warning disable 0618
            importer.spritesheet=Names.Select((n,i)=>new SpriteMetaData {name=n,rect=new Rect((i%3)*w,(1-i/3)*h,w,h),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();
#pragma warning restore 0618
            importer.SaveAndReimport();
        }
        [MenuItem("Political Timeline/Assign Missing Character Sprites")]
        public static void AssignMissing() => Assign(false);
        public static void Assign(bool replace)
        {
            ConfigureSheet();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToDictionary(s=>s.name);
            foreach(var guid in AssetDatabase.FindAssets("t:DecisionCard"))
            {
                var card=AssetDatabase.LoadAssetAtPath<DecisionCard>(AssetDatabase.GUIDToAssetPath(guid));
                if(!replace && card.usePortraitSprite && card.portrait!=null) continue;
                Sprite selected;
                switch(card.advisor)
                {
                    case "Chief Justice": case "Solicitor General": selected=sprites["Judge"]; break;
                    case "House Speaker": case "Senate Leader": case "Committee Chair": selected=sprites["CongressLeader"]; break;
                    case "Campaign Chair": case "Campaign Organizer": case "Field Organizer": case "Party Organizer": case "Union Apprentice": selected=sprites["CampaignOrganizer"]; break;
                    case "Agriculture Secretary": case "Farm Cooperative": selected=sprites["Farmer"]; break;
                    case "Community Nurse": case "Health Secretary": selected=sprites["Nurse"]; break;
                    case "School Principal": case "Education Secretary": selected=sprites["Teacher"]; break;
                    case "Grid Engineer": case "Labor Secretary": selected=Existing("10_00_00 AM"); break;
                    case "Science Adviser": selected=Existing("11_25_22 AM"); break;
                    case "Network Engineer": selected=Existing("11_23_14 AM"); break;
                    case "Security Adviser": selected=Existing("09_43_29 AM"); break;
                    case "Secretary of State": selected=Existing("01_42_19 PM"); break;
                    case "Press Secretary": case "Investigative Reporter": selected=Existing("11_05_07 AM"); break;
                    case "Treasury Secretary": selected=Existing("01_34_38 PM"); break;
                    case "Opposition Leader": selected=Existing("01_31_49 PM"); break;
                    case "Policy Director": selected=Existing("01_39_05 PM"); break;
                    default: selected=Existing("01_32_56 PM"); break;
                }
                Undo.RecordObject(card,"Assign character sprite");
                card.portrait=selected; card.usePortraitSprite=true; card.artwork=null; EditorUtility.SetDirty(card);
            }
            AssetDatabase.SaveAssets();
        }
        public static void BatchMigrate()
        {
            Assign(true);
            SaveSceneOnly();
            PresidencyPreview.BatchCheck();
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>();
            foreach(var card in game.campaign.cards)
            {
                if(card.portrait==null || !card.usePortraitSprite || card.artwork!=null) throw new Exception("Card still uses geometric art: "+card.name);
                game.Restart(); game.State.current=card; game.Refresh();
                if(!game.portrait.enabled || game.portrait.sprite!=card.portrait) throw new Exception("Wrong sprite in play: "+card.name);
            }
            File.AppendAllText("Validation.txt","\nAll 102 cards display assigned image sprites in play. Six named atlas sprites and ten existing character sprites replace geometric portraits.");
        }
        public static void SaveSceneOnly()
        {
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            EditablePresentationEditor.UpgradeCurrent();
            var game=UnityEngine.Object.FindFirstObjectByType<PresidencyGame>();
            game.PreviewCardForEditing(game.campaign.cards[0]);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            File.Copy(PresidencySceneBuilder.ScenePath,"Library/SpriteSceneValidated.unity",true);
        }
    }
}

