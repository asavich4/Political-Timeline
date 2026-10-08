using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    // Real PNG assets: no runtime portrait generation or hidden atlas slices.
    public sealed class CharacterCastImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CharacterCastEditor.Folder + "/", StringComparison.Ordinal) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return; // Preserve subsequent Inspector edits.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100;
        }
    }

    public sealed class CharacterCastEditor : EditorWindow
    {
        public const string Folder = "Assets/Art/CharacterSprites/Cast";
        public static readonly string[] Names = {
            "MaskedFigure", "AngryGeneral", "ReligiousLeader", "Protester", "EvilRichMan", "TechGuy", "CrazyScientist", "OppositionLeader",
            "UNDiplomat", "SupremeCourtJudge", "OldSenator", "WallStreetGuy", "SecretService", "BlueCollarWorker", "HomelessMan", "Cop",
            "NewsLady", "Spy", "NATOHead", "RussiaHead", "ChinaHead", "ArabLeader", "NormalGuy", "FeministActivist",
            "CongressHead", "Aliens", "CultLeader", "SeductiveIntern", "VicePresident", "Wife", "OnlineInfluencer", "GhostWashington",
            "Nurse", "Teacher", "Farmer", "Ranger", "Goose"
        };
        Vector2 scroll;
        DecisionCard targetCard;
        string search = "";

        [MenuItem("Political Timeline/Characters/Browse Cast")]
        public static void Open() => GetWindow<CharacterCastEditor>("Character Cast");

        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
        public static Sprite ForAdvisor(string advisor)
        {
            foreach (string character in Names)
                if (advisor == character || advisor == ObjectNames.NicifyVariableName(character)) return Sprite(character);
            string name;
            switch (advisor)
            {
                case "Community Nurse": case "Health Secretary": name = "Nurse"; break;
                case "School Principal": case "Education Secretary": name = "Teacher"; break;
                case "Farm Cooperative": case "Agriculture Secretary": name = "Farmer"; break;
                case "Park Ranger": name = "Ranger"; break;
                case "Party Goose": name = "Goose"; break;
                case "Congressional Clerk": case "Party Intern": name = "SeductiveIntern"; break;
                case "Head of NATO": name = "NATOHead"; break;
                case "Head of Russia": name = "RussiaHead"; break;
                case "Head of China": name = "ChinaHead"; break;
                case "Ghost of Washington": name = "GhostWashington"; break;
                case "Alien Ambassador": name = "Aliens"; break;
                case "Wealthy Donor": name = "EvilRichMan"; break;
                case "Party Deputy": name = "VicePresident"; break;
                case "Chief Justice": case "Solicitor General": name = "SupremeCourtJudge"; break;
                case "House Speaker": case "Committee Chair": name = "CongressHead"; break;
                case "Senate Leader": name = "OldSenator"; break;
                case "Opposition Leader": name = "OppositionLeader"; break;
                case "Network Engineer": name = "TechGuy"; break;
                case "Science Adviser": name = "CrazyScientist"; break;
                case "Grid Engineer": case "Labor Secretary": case "Union Apprentice": name = "BlueCollarWorker"; break;
                case "Press Secretary": case "Investigative Reporter": name = "NewsLady"; break;
                case "Treasury Secretary": name = "WallStreetGuy"; break;
                case "Security Adviser": name = "AngryGeneral"; break;
                case "Secretary of State": name = "UNDiplomat"; break;
                case "Chief of Staff": name = "VicePresident"; break;
                case "Policy Director": name = "MaskedFigure"; break;
                case "Campaign Chair": name = "NormalGuy"; break;
                case "Campaign Organizer": case "Field Organizer": case "Party Organizer": name = "Protester"; break;
                default: return null;
            }
            return Sprite(name);
        }

        [MenuItem("Political Timeline/Characters/Apply Cast to Matching Advisors")]
        public static void ApplyCast()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:DecisionCard"))
            {
                var card = AssetDatabase.LoadAssetAtPath<DecisionCard>(AssetDatabase.GUIDToAssetPath(guid));
                var sprite = ForAdvisor(card.advisor);
                if (sprite == null || (card.portrait == sprite && card.usePortraitSprite && card.artwork == null)) continue;
                Undo.RecordObject(card, "Replace character portrait");
                card.portrait = sprite; card.usePortraitSprite = true; card.artwork = null;
                EditorUtility.SetDirty(card); count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Character cast: updated " + count + " card portraits. All " + Names.Length + " sprites are available in Characters > Browse Cast.");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("YOUR CHARACTER CAST", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Names.Length + " separate PNG sprites. Click a portrait to find its source asset. Assign a card below to change its portrait. These changes are saved in the card asset and appear before Play.", MessageType.Info);
            targetCard = (DecisionCard)EditorGUILayout.ObjectField("Card to edit", targetCard, typeof(DecisionCard), false);
            search = EditorGUILayout.TextField("Find character", search);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 24) / 172));
            var filtered = Names.Where(n => ObjectNames.NicifyVariableName(n).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            for (int row = 0; row < filtered.Length; row += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = row; col < Mathf.Min(row + columns, filtered.Length); col++)
                {
                    string name = filtered[col]; var sprite = Sprite(name);
                    EditorGUILayout.BeginVertical(GUILayout.Width(164));
                    if (GUILayout.Button(sprite != null ? new GUIContent(sprite.texture) : new GUIContent("Awaiting sprite"), GUILayout.Width(160), GUILayout.Height(160)))
                    { Selection.activeObject = sprite; EditorGUIUtility.PingObject(sprite); }
                    GUILayout.Label(ObjectNames.NicifyVariableName(name), EditorStyles.boldLabel, GUILayout.Width(164));
                    using (new EditorGUI.DisabledScope(targetCard == null || sprite == null))
                    {
                        if (GUILayout.Button("Use on card", GUILayout.Width(160)))
                        {
                            Undo.RecordObject(targetCard, "Change portrait");
                            targetCard.portrait = sprite; targetCard.usePortraitSprite = true; targetCard.artwork = null;
                            EditorUtility.SetDirty(targetCard); AssetDatabase.SaveAssets();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal(); GUILayout.Space(12);
            }
            EditorGUILayout.EndScrollView();
        }

        public static void BatchInstallAndCheck()
        {
            AssetDatabase.Refresh();
            foreach (string name in Names)
            {
                string path = Folder + "/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new Exception("Missing cast PNG: " + name);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048; importer.SaveAndReimport();
                var sprite = Sprite(name);
                if (sprite == null || sprite.rect.width != sprite.rect.height) throw new Exception("Invalid cast sprite: " + name);
            }
            CastEventContent.Install();
            ApplyCast();
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindFirstObjectByType<PresidencyGame>();
            bool menuOpen = game.frontMenu.panel.activeSelf;
            game.PreviewCardForEditing(game.editorPreviewCard != null ? game.editorPreviewCard : game.campaign.cards[0]);
            game.frontMenu.panel.SetActive(menuOpen);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            File.Copy(PresidencySceneBuilder.ScenePath, "Library/CastSceneValidated.unity", true);
            game.Initialize();
            int mapped = 0;
            foreach (var card in game.campaign.cards)
            {
                var expected = ForAdvisor(card.advisor);
                if (expected != null) { mapped++; if (card.portrait != expected) throw new Exception("Wrong cast mapping: " + card.id); }
                game.Restart(); game.State.current = card; game.Refresh();
                if (!game.portrait.enabled || game.portrait.sprite != card.portrait) throw new Exception("Missing in-game portrait: " + card.id);
            }
            // Preview each of the cast assets in the real card frame without changing any saved event.
            var preview = ScriptableObject.CreateInstance<DecisionCard>();
            preview.advisor = "Character Cast"; preview.category = "ART PREVIEW";
            preview.briefing = "Every character is a separate sprite you can assign in Unity.";
            foreach (string name in Names)
            {
                preview.portrait = Sprite(name); preview.advisor = ObjectNames.NicifyVariableName(name);
                game.PreviewCardForEditing(preview);
                if (game.portrait.sprite != preview.portrait) throw new Exception("Editor preview failed: " + name);
                if (name == "MaskedFigure" || name == "SupremeCourtJudge" || name == "BlueCollarWorker" || name == "GhostWashington")
                    typeof(PresidencyPreview).GetMethod("Capture", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(null, new object[] { "Cast" + name + "Preview.png", 750, 1334, new Rect(0, 0, 750, 1294) });
            }
            UnityEngine.Object.DestroyImmediate(preview);
            File.WriteAllText("CastValidation.txt", "PASS: " + Names.Length + " named square PNGs imported as single Sprites. " + mapped + " matching cards updated; all " + game.campaign.cards.Count + " card portraits verified in play; all cast sprites verified in editor preview. Scene saved.");
        }
    }
}
