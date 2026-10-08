# Political Timeline

A portrait card-swiping game about the hidden leadership behind a political party. Presidents come and go; your party survives in government or opposition. The four keys of power are **Workers, Middle class, Economy and Elites**, shown as flat geometric hammer, home, growth-bar and crown symbols. The symbols use cream ink in ordinary years and dark ink in election years.

## Play and preview

Choose **Political Timeline > Preview iPhone Portrait**, then press Play. The preview uses the phone's portrait aspect ratio at the available Game-view resolution. **Political Timeline > Fix Game View Sharpness** disables low-resolution aspect ratios and resets zoom; it also works during Play mode. Shift+Space returns from the maximized Game view.

Hold a card left or right to reveal the choice. Release beyond the gold threshold line to choose. Return to the center to cancel. Arrow keys and A/D support the same hold-and-release interaction. On a committed swipe, the card flies off in the chosen direction for 0.30 seconds. Meters animate and show numeric changes for 1.15 seconds, then the consequence card waits indefinitely. Swipe it left or right (or hold and release an arrow key / A or D) to confirm it has been read. This second swipe does not change support or advance the month. It also comes before election night and game-over screens. The next card enters over 0.25 seconds after acknowledgement. Input is locked during this sequence; restarting cancels it. One completed choice advances **one calendar month**, beginning in January 2025. Opening a panel or canceling a swipe does not advance time.

The cards use illustrated PNG character sprites, muted colors and a paper caption. The short question stays above the card; the caption identifies the speaker and shows the previous decision outcome. Hold an institutional choice to see its voting or court requirement. All text uses a dedicated 2048-pixel font atlas sampled at 120 points and a sharp TextMesh Pro material. Entering Play mode in the Presidency scene automatically applies the native-resolution portrait Game view settings. Tap a power image to see the group's meaning and current support. A top meter reaching zero ends the party campaign. Full meters do not end play. There is no term limit or automatic victory date.

## Map, Congress and Supreme Court

Four buttons at the bottom open separate views so the main card stays uncluttered:

- **Map:** geographic outlines of all 50 states and DC, with Alaska and Hawaii inset. Blue represents Democrats, red represents Republicans, and gray means a close race. Tap a state to reveal its electoral votes and projected support bar, with a 50% marker. Support is the two-coalition vote share implied by the simulated margin (50 + margin / 2). Election-night selections hide the bar until that state reports, then use its recorded election margin. Larger shortcuts below the map select DC, DE, RI, CT, NJ and MA. These are fictional game projections, not real polling or party affiliations.
- **Congress:** separate semicircle diagrams show all 435 House seats and 100 Senate seats, with one dot per seat. Blue dots represent Democrats and red dots represent Republicans; the seat counts refer to your chosen party. The House needs 218 seats for a majority; the simplified Senate confirmation rule needs 51. Composition changes at November elections. Bills marked Congress need 218 House seats, 51 Senate seats and your party in the presidency. Without both majorities, the blocked outcome and support changes apply.
- **Court:** nine fictional seats marked aligned, independent, opposed or vacant. A retirement creates a vacancy every 18 months if one is not already open. With your party in the presidency and at least 51 allied senators, the nomination button confirms an aligned replacement. Nominations do not consume an extra month. Judicial review cards need five aligned justices to uphold a policy; otherwise the blocked outcome applies. This is an intentionally simplified fictional rule. A vacancy-only event also lets the player nominate through a monthly card.

**Policies:** a read-only ledger lists enacted policies, descriptions and enactment dates, with three entries per page. Fifty-two durable policies can be enacted through successful event choices. Failed votes do not enact laws; repeat enactments do not duplicate them. Laws survive election losses and only a successful repeal event removes them. School meals has a repeal event available while enacted.

The party starts with 210 House seats and 48 senators. State leans are strengthened by 15%, support sensitivity is reduced to 0.55, and electoral resistance subtracts four margin points. These editable fictional difficulty settings make a neutral coalition less competitive. State leans and support weights are intentionally fictional and editable. Economic support affects state projections alongside workers, the middle class and elites. The old Security meter has been migrated to Economy without losing the authored effect values.

## Election calendar

- Every even-numbered year switches the main background to warm white, with dark text. The top date area explicitly reads Midterm election year or Presidential election year. Power symbols switch to dark ink.
- Midterms take place every two years; presidential contests take place every four years.
- The party continues indefinitely through successive presidents and election cycles.
- Elections automatically pause decisions after the swipe consequence and open election night. States begin gray and report over time, with safer contests first and close races last. Electoral totals build toward 270 in presidential years; House and Senate totals build in midterms. The reveal uses the already-computed result and never rerolls it. Uncalled states hide results until they report. Skip to final results completes the count immediately. **View Congress** shows the new chambers, and **Continue** returns to the cards (including after a defeat).
- Controlling the presidency requires at least 270 electoral votes. Falling short, including a tie, moves the party into opposition without ending play. Organizing and candidate-selection events help it rebuild. A later election can return it to power.
- At every election, all House seats are recalculated and one third of the modeled Senate seats are contested. State margins derive from their starting lean and the player's four support levels. Results are deterministic for the same support and configuration.

The state electoral weights use the [National Archives' 2024/2028 allocations](https://www.archives.gov/electoral-college/allocation). For this prototype, those weights stay fixed in later years and all states use statewide winner-take-all results, including Maine and Nebraska. House districts, individual Senate races, real court appointments and contingent elections are simplified game systems.

## Add content in Unity

### Preview and edit before Play

**Political Timeline > Portrait Gallery** displays the actual artwork for every card, including cards restricted to campaigns, opposition and follow-up stories. Search by advisor, headline, category or condition. **Select sprite** opens the image asset; **Edit card** opens event text, choices, effects and availability rules.

All 182 cards now use normal Unity Sprite references. Six new characters are sliced from `Assets/Art/CharacterSprites/PolicyCharacters.png`: Judge, CampaignOrganizer, Farmer, Nurse, Teacher and CongressLeader. Ten existing character sprites are reused from `Assets/Sprites`. Expand the new sheet in the Project browser to see its six sprites.

Assign any Sprite to **Portrait** in the card Inspector. The gallery, scene preview and game all use that same image. Edit source PNGs in an image editor; Unity reimports changes. Adjust slice rectangles in the Sprite Editor. **Assign Missing Character Sprites** fills empty slots without replacing assigned images.

The Presidency scene now saves the card artwork, four power symbols, consequence panel, Congress seat diagrams, state support bar and Policies panel as real scene objects. Starting the game reuses these objects.

1. Open `Assets/Scenes/Presidency.unity` and select **Presidency • Portrait** in the Hierarchy.
2. In its Inspector, use **Preview card**, **Preview consequence**, **Map**, **Congress**, **Court** or **Policies** to show the screen you want to edit. These previews use sample data; gameplay supplies the actual values.
3. Select a card under `Assets/Content/Presidency/Cards` and click **Preview this card in the open scene**. Edit its briefing, advisor, category, design and choices on the card asset.
4. Drag your chosen image sprite into **Portrait** in the card Inspector. Click **Select portrait sprite** to locate the source image in the Project browser.
5. Select **Geometric power symbol** under a top meter to change its symbol or size. Under **National View > Chambers**, select **House hemicycle** or **Senate hemicycle** to change seat colors and dot size. Move and resize UI objects with their RectTransforms.

Make and save layout changes outside Play mode; Unity discards scene changes made during Play. Game text and live values come from card assets and game state. Power icon colors still follow the election-year theme.

**Political Timeline > Update Editable Scene** upgrades an older open scene without rebuilding its existing layout. The current scene is already upgraded. Inspector preview buttons expose panels that normally start inactive in the Hierarchy.

Open **Political Timeline > Content Workshop**. New Card and Duplicate Selected add real ScriptableObject cards to the campaign. Write a short question (around 50 characters), two choice labels (around 20 characters), consequences, four support changes and an optional follow-up branch. Assign a Portrait sprite directly; advisor and category only control the text labels. Set a choice institution rule and its blocked consequence/support changes for conditional outcomes. Set the card condition to CourtVacancy or DividedCongress to restrict when it can appear. The starter deck now contains 182 cards, including opposition organizing and an event-driven policy repeal.

Six four-card storylines cover backup power, paid leave, childcare, press protection, soil restoration and polling access. Each opening offers a policy path or a local response. Passing a bill unlocks its policy aftermath; a failed vote or opposition presidency leads to the local response. Follow-up-only chapters never enter random draws. Stories resume after election night. Eight new laws accompany these stories and eight standalone events. Their portraits use the shared PNG character collection.

Follow-ups override ordinary selection rules. A choice has separate successful and blocked follow-up slots. Otherwise, earliest decision, weight and once-per-run control draws. Cards can repeat across months, but immediate repeats are avoided when alternatives exist. **Validate Deck** checks content and national settings. **Run Rule Checks** exercises the calendar, support limits, elections, congressional totals, court appointments and deck behavior. **Balance Preview** runs 200 simulated campaigns, acknowledging election reports as it goes.

Choose **Political Timeline > National Simulation Settings** to edit state leans, interest weights, electoral weights, incumbent advantage and the court retirement interval. The legacy column/row fields are unused by the geographic map. Interest vector X = Workers, Y = Middle class, Z = Economy, W = Elites. Campaign Settings controls the inauguration year, starting support and national asset. Start years should follow presidential elections, such as 2025 or 2029. Changes take effect on a new run.

The power symbols are drawn as crisp UI geometry by PowerIcon. The legacy PNG slots remain for scene compatibility and are hidden during play. The map uses [us-atlas](https://github.com/topojson/us-atlas) v3 projected Census Bureau 2017 boundaries. Its ISC license, original TopoJSON and placement data are retained in `Assets/Content/Presidency/Map`. `Tools/build_state_map.py` rebuilds the tintable state sprites with Python and Pillow. No network connection is needed to play.

## Status and scene maintenance

This is an editable game foundation with 182 cards. Three local save slots preserve progress across Play sessions and app restarts. State and institutional systems are fictional abstractions, not a forecast. Physical iPhone testing and a signed iOS build remain to be done.

The original Main scene is preserved. **Create Starter Scene** rebuilds the Presidency layout after confirmation while preserving content assets. Save custom layouts under a different scene name before rebuilding.

## Campaigning and opposition

Twelve campaign-only cards appear from January through November of even-numbered years. Both choices target named states; successful choices add 3–4 percentage points of local voter support, in addition to their national-meter costs. Each local support point adds two margin points in the two-party model. Outreach bonuses are capped at plus or minus 12 support points and retain 97% of their value each month; the new choice is applied after that decay and before November results. The map and election tallies use these local bonuses. Swipe previews name targets, consequence cards report the actual added support, and selected states show their current outreach bonus.

Eight new opposition-only cards cover four legislative blocking fights and four voter-rebuilding efforts. Out of power, opposition-card draw weights are tripled and ordinary federal enactment cards leave the random pool. Existing linked stories still resolve their blocked branches. Blocking a government bill needs either 218 House seats or 41 senators in this simplified game; a failed block grants no local voter gain and records no success. Congress displays the number of successful blocks. A compromise or organizing choice can still rebuild votes when the party lacks blocking seats.

Twelve new governing-policy proposals add transit, renter protection, food safety, disaster insurance, cybersecurity, small-business credit, veterans care, water conservation, public records, wage enforcement, rural clinics and mental-health access.


## Start menu, party choice and saves

Play opens the start menu. Each of three slots offers New Game or Continue. Starting in an occupied slot opens a party-selection screen with an explicit Replace & Start button; Back cancels without changing the existing save. Choose Democrat or Republican before starting. The selected team is shown on the in-game Save & Menu button.

Party choice determines which side of the fictional state lean each player starts on, and blue/red affiliation in the map, Congress and support bars. Both parties use the same event deck and support rules. The state leans are fictional balancing data, not actual polling or an ideological model.

Progress autosaves after a decision, consequence acknowledgement, election acknowledgement and court nomination. Save & Menu saves the active slot and returns to the slot list. Backgrounding or quitting also saves. Saves include the current event, next-draw random sequence, seen one-time events, four support meters, elapsed months, party, policies and their enactment dates, House and individual Senate seats, court seats, state outreach, control of the presidency, unread consequence and election results. Election reporting resumes from the saved number of reported states.

Files live in Unity's persistentDataPath/CampaignSaves as slot-1.json through slot-3.json. Writes use a temporary file and preserve the previous save as a .bak file. Invalid or unsupported saves are marked unavailable and cannot be loaded; they are not silently replaced. Saves are local to this device, not cloud-synced.

The menu is saved in the Presidency scene under Portrait Layout > Start Menu. Select Presidency • Portrait and use Preview start menu or Preview party selection in the Inspector to edit their objects before Play. Preview card returns to the game view. All labels, colors, positions and button sizes are normal scene UI objects.
## Everyday politics expansion

80 new, editable card assets are under Assets/Content/Presidency/Cards/Expansion: 24 general community/party events, 16 funny events, 12 new-law proposals, 12 repeal fights, eight Congress events and eight court events. The deck now has 182 cards and 52 possible policies. General events have draw weight 2; funny events have weight 1. Most work in either government or opposition.

Policy proposals require government and congressional passage. The new proposals stop appearing once their law is enacted. Repeals only appear with their corresponding law active and your party in government; a failed repeal leaves the law intact. Four court-defense cards require an active policy: privacy, clean air, press protection or polling access. Five aligned justices uphold it under the existing simplified court model; defeat removes it from the ledger even if your party is in opposition. Settlement choices leave the policy enacted. These are game abstractions, not legal guidance.

New PNG portraits are Clerk.png, Ranger.png and Goose.png in Assets/Art/CharacterSprites. Existing portraits fill the other roles. The portrait gallery shows all of them. Content Workshop searches now include category, advisor and ID; search exp_ for the expansion, or PARTY NONSENSE for the comedy cards. Each asset exposes text, effects, portraits, required/excluded policy and source notes in the Inspector.

Historical inspiration is marked in each relevant card's Historical Note and Source Url fields. Open historical source in the Inspector opens its reference. The choices are fictional modern situations, not verbatim historical accounts:

- Bank queues: the 1931-33 banking panic and March 1933 bank holiday. [Federal Reserve History](https://www.federalreservehistory.org/essays/banking-panics-1931-33).
- Deposit insurance: creation of US deposit insurance in 1933. [FDIC history](https://www.fdic.gov/history/1930-1939).
- Burning river: the 1969 Cuyahoga River fire and environmental debate. [EPA history](https://www.epa.gov/sciencematters/putting-out-fire-50-years-science-protect-americas-water).
- Press protections: the 1971 Pentagon Papers. [National Archives](https://www.archives.gov/research/pentagon-papers).

Political Timeline > Add Expansion Cards installs any missing expansion assets and adds them to the campaign without rewriting existing ones. Manual Inspector edits are preserved by repeated installation. Existing policy enum values and card IDs are preserved for saved games.