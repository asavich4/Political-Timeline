# Political Timeline

A presidency told through difficult choices. Keep workers, the middle class, security and elites in a fragile coalition, win reelection, and finish two terms.

## Play in Unity

Choose **Political Timeline > Preview iPhone Portrait**, then press Play. This opens `Assets/Scenes/Presidency.unity`, selects a 1170 × 2532 Retina Game view and maximizes that panel. Press Shift+Space over the Game view to return to the normal editor layout. Hold the card left or right to reveal that choice over the portrait. Release after the gold line appears to commit; return to the center to cancel. Short or mostly vertical drags also cancel. Hold and release arrow keys or A/D for the same preview-and-choose behavior in the editor. The original `Main.unity` remains intact.

The iPhone layout keeps the card and controls inside the screen safe area. The main screen contains four support bars, one bold question and an edge-to-edge portrait card. Choice text stays hidden until the card is held to either side. There is no title, turn counter, adviser caption, white text panel or permanent choice buttons. All labels use TextMesh Pro distance-field fonts for sharper scaling. Both questions and revealed choices use fixed 28-point text. Small +/− indicators preview support direction while dragging and briefly confirm changes afterward. Tap the support bars for rules, term and approval. Opening details pauses decision input.

The four groups begin at 50. Each decision changes their support. Reaching either 0 or 100 ends the administration: abandonment at the low end, institutional capture or an uncontrollable mandate at the high end. This is an abstract game mechanic, not a political simulation.

After 16 decisions, an average support of at least 45 wins reelection. Surviving 32 decisions completes two terms. Each card represents a major policy episode, rather than a literal day. Cards are drawn with weights and avoid immediate repetition when another eligible card exists.

## Add content without coding

1. Open **Political Timeline > Content Workshop**.
2. Choose **New Card** or **Duplicate Selected**. The card is automatically added to the current campaign.
3. Write the briefing as a single question of about 50 characters and keep choice labels around 20 characters. The question sits above the card; a choice label appears over the image while holding it left or right. Headline, adviser and category remain available for organizing content. Drag a square portrait Sprite from the existing Sprites folder onto Portrait; expand the image asset if needed to see its Sprite. Square portraits fill the card without cropping or stretching.
4. Give each choice a short label, a consequence, and changes to all four groups. Positive numbers add support; negative numbers remove it.
5. Optionally assign a Follow Up card to either choice. Follow-ups are forced narrative branches and bypass normal availability, weight and once-per-run selection rules. Avoid unintended cycles.
6. Use Earliest Decision to delay ordinary draws, Weight to change draw frequency, and Once Per Run for unique incidents.
7. Choose **Validate Deck**, then **Save Content**. Play again to load your changes.

The workshop edits real ScriptableObject assets in `Assets/Content/Presidency`. Campaign Settings exposes the deck, starting support, election threshold, decisions per term and term limit. Remove cards from that list to retire them without deleting their files. Unity's Inspector also supports direct editing and undo.

## Current foundation

- 18 playable policy cards covering jobs, infrastructure, taxation, health, security, housing, trade, education, diplomacy and public trust.
- Existing flat geometric character artwork retained and assigned to advisers.
- Editable portrait scene hierarchy: typography, full-card portraits, support bars and a directional choice overlay. Rules and current progress are available by tapping the support bars; written consequences remain in the editable card assets.
- Mouse/touch hold-and-release decisions and equivalent keyboard controls.
- Nine constituency/election/legacy endings, election defeat and deck exhaustion handling.
- Restart after a completed run; configurable campaign rules; branching follow-up cards.
- Content validation and repeatable campaign rule checks from the workshop.
- Balance Preview runs 200 simulated campaigns using choices that favor support near 50, providing a quick check on difficulty as you add cards.

## Art direction

Preserve the original simple geometric portraits, muted browns, olive, cream and charcoal. Favor recognizable silhouettes and flat shapes. New adviser art should use a square canvas with a centered bust; avoid introducing detailed backgrounds or realistic rendering. This foundation reuses the supplied artwork and adds no generated replacement portraits.

## Next milestones

1. Balance the 18-card pool through playtesting; expand to 60–100 decisions and more distinct adviser voices.
2. Add persistent story flags and authored multi-card crises, with explicit prerequisites and consequences.
3. Add sound, card transition animation, onboarding and accessibility options.
4. Add save/resume, statistics and a legacy collection.
5. Build and test on a physical iPhone, including touch input, notches, device performance and accessibility.

This is a playable game foundation, not a finished content-complete release. Progress currently resets when Play mode ends. The layout targets portrait iPhone, with portrait-only orientation and the iPhone target configured in Player Settings. Unity previews cover compact and tall screen shapes; an iOS build and physical-device testing are still needed. Building/signing the iPhone app requires a Mac with Xcode.

## Scene maintenance

**Political Timeline > Create Starter Scene** rebuilds the starter layout after confirmation and preserves card assets. It replaces changes to the generated Presidency scene, so save your custom scene under a different name before rebuilding. The new scene is first in Build Settings; the original is retained but disabled. **Run Rule Checks** exercises support limits, elections, victory, follow-ups, availability and empty decks.
