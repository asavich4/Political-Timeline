using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class CastEventContent
    {
        [MenuItem("Political Timeline/Characters/Update Events to New Cast")]
        public static void Install()
        {
            var deck=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset");
            Ensure(deck); AssetDatabase.SaveAssets();
        }

        public static void Ensure(CampaignDefinition deck)
        {
            var rewrites=Rows.Trim().Split('\n').Select(r=>r.Trim().Split('|')).ToDictionary(r=>r[0]);
            foreach(var card in deck.cards)
            {
                if(card==null || card.castRevision>=1) continue;
                Undo.RecordObject(card,"Update event cast");
                if(rewrites.TryGetValue(card.id,out var row))
                {
                    card.advisor=row[1]; card.headline=card.briefing=row[2];
                    if(row.Length==7)
                    {
                        card.left.label=row[3]; card.left.consequence=row[4];
                        card.right.label=row[5]; card.right.consequence=row[6];
                    }
                }
                var portrait=CharacterCastEditor.ForAdvisor(card.advisor);
                // Authors can create a new speaker name and explicitly choose a cast sprite.
                if(portrait==null && card.portrait!=null && AssetDatabase.GetAssetPath(card.portrait).StartsWith(CharacterCastEditor.Folder+"/",StringComparison.Ordinal)) portrait=card.portrait;
                if(portrait==null) throw new Exception("No cast sprite for "+card.id+": "+card.advisor);
                card.portrait=portrait; card.usePortraitSprite=true; card.artwork=null;
                // One-time content migration. Later edits in the Inspector survive setup/reimport.
                card.castRevision=1; EditorUtility.SetDirty(card);
            }
        }

        public static void BatchCheck()
        {
            Install();
            var deck=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset");
            var before=deck.cards.Select(JsonUtility.ToJson).ToArray();
            Ensure(deck);
            if(!before.SequenceEqual(deck.cards.Select(JsonUtility.ToJson))) throw new Exception("Cast migration is not idempotent.");
            // Simulate a subsequent author edit: setup must never overwrite it.
            var edited=deck.cards[0]; string text=edited.briefing;
            edited.briefing="Author's custom dialogue"; Ensure(deck);
            if(edited.briefing!="Author's custom dialogue") throw new Exception("Cast migration overwrote an author edit.");
            edited.briefing=text;
            var custom=ScriptableObject.CreateInstance<DecisionCard>();
            custom.id="author-cast-test"; custom.advisor="A custom speaker"; custom.portrait=CharacterCastEditor.Sprite("NormalGuy");
            deck.cards.Add(custom); Ensure(deck);
            if(custom.portrait!=CharacterCastEditor.Sprite("NormalGuy")) throw new Exception("Custom speaker lost its assigned cast sprite.");
            deck.cards.Remove(custom); UnityEngine.Object.DestroyImmediate(custom);
            CharacterCastEditor.BatchInstallAndCheck();
            ExpansionChecks.BatchCheck();
            deck=AssetDatabase.LoadAssetAtPath<CampaignDefinition>(PresidencyContent.Root+"/FirstAdministration.asset");
            var used=deck.cards.Select(c=>Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(c.portrait))).Distinct().ToArray();
            foreach(var card in deck.cards)
                if(!AssetDatabase.GetAssetPath(card.portrait).StartsWith(CharacterCastEditor.Folder+"/",StringComparison.Ordinal)) throw new Exception("Old-style art returned: "+card.id);
            if(CharacterCastEditor.Names.Where(n=>n!="MaskedFigure").Except(used).Any()) throw new Exception("Unused cast: "+string.Join(", ",CharacterCastEditor.Names.Where(n=>n!="MaskedFigure").Except(used)));
            File.WriteAllText("FullCastValidation.txt", "PASS: all "+deck.cards.Count+" cards use the new cast; all "+used.Length+" characters appear in the deck. Idempotent migration and later author edits verified. Full expansion, policy, linked-story, election, save-slot and card layout checks passed.");
        }

        // All modern leaders and diplomatic scenarios here are fictional. Existing choice
        // mechanics, policy IDs, prerequisites and follow-up links remain on the assets.
        const string Rows=@"
health|Community Nurse|Our rural hospital is closing. Help us keep it open?
housing|Homeless Man|I lost my flat. Will your party build affordable homes?
protest|Cop|The avenue is packed. Talk to protesters or clear it?
cyber|Tech Guy|Hospital computers are locked. We need a response.
surveillance|Spy|More surveillance would help us. Will you back it?
leaked_memo|Spy|Your private memo is public. Shall we find the leak?
defense|Head of NATO|Our allies need ships. Back the over-budget fleet?
trade|Head of China|Our trade offer cuts prices. What about your workers?
energy|Arab Leader|Our oil exports cost more. How will your party respond?
diplomacy|UN Diplomat|A rival wants peace talks. Will your party engage?
exp_h_audit|Angry General|My procurement bill looks wrong. Fund an audit?
exp_g_broadchurch|Head of Russia|I offer a summit. Public talks or private meetings?|Insist on public talks|Voters hear the arguments. Your allies dislike the messy public disputes.|Meet behind closed doors|Negotiators speak freely. Critics suspect a secret bargain.
exp_g_foodbank|Religious Leader|Our food bank needs help. Please leave party logos out.
exp_g_coolingcenter|Homeless Man|There is nowhere cool to sleep. Lend the charity space?
exp_g_accessramp|Secret Service|The hall has stairs. We need an accessible entrance.
exp_g_donoraccess|Wealthy Donor|Dinner with me? I have several very profitable ideas.
exp_f_cake|Wealthy Donor|My cake says Happy Bribeday. Surely a bakery error?
exp_f_printer|Party Intern|The printer ate your manifesto. I saved the stapler.
exp_f_replyall|Party Intern|I replied all. Nine hundred people replied back.
exp_g_receipts|Party Intern|This expense claim has a cocktail napkin, no receipts.
exp_c_opinion|News Lady|Two hundred pages of legalese. How do we explain it?
exp_h_readbill|Old Senator|Nine hundred pages? My spectacles have resigned.
exp_h_whip|Congress Head|Three members may skip our vote. How hard do I push?
exp_h_stopgap|Congress Head|I can keep the lights on if you back this stopgap.
exp_law_CampaignDisclosure|Congress Head|Show voters our major donors. Will you back a law?
exp_f_mug|Party Intern|Our mugs say World's Best Opposition. Excellent timing.
exp_f_hat|Wife|Four hundred hats. Wrong party color. Your plan?
exp_f_chili|Wife|The chili contest is a faction war. Fetch a spoon.
exp_f_mutebutton|Online Influencer|Your leader was on mute. I have the whole video.
exp_f_teleprompter|Party Deputy|My script says insert inspiring story. Any ideas?
exp_f_dance|Cult Leader|My followers invented a party dance. Join the circle?|Try it once|Volunteers cheer. Your knees reject the movement's founding principles.|Let volunteers lead|The dance spreads. The cult leader calls it grassroots recruitment.
exp_f_census|Alien Ambassador|Your branch counted my houseplant as a member.|Correct the total|The membership roll is fixed. The plant requests an appeal.|Audit every branch|The audit finds two alien ferns and a photocopier with voting rights.
exp_f_mascotpoll|Ghost of Washington|I poll above your candidate. Must I haunt a poster?|Put him on posters|The posters sell. Reporters ask about the ghost's very old tax plan.|Keep a serious tone|Donors relax. Washington starts an unofficial spectral fan club.
exp_c_steps|Feminist Activist|Our supporters want a peaceful courthouse rally.
exp_g_seniors|Old Senator|An evening meeting? At my age that is tomorrow.
exp_repeal_WealthTax|Wealthy Donor|This wealth tax is terribly personal. Repeal it?
exp_repeal_CampaignDisclosure|Wealthy Donor|Must everyone know I paid? Scrap donor disclosure.
exp_repeal_RightToRepair|Tech Guy|Our devices, our repair shops. Reverse that law?
exp_repeal_SurveillancePowers|Feminist Activist|Expanded spying chills protest. End these powers.
exp_repeal_WorkerSafety|Wealthy Donor|Inspections cost my companies money. End the rule?
";
    }
}
