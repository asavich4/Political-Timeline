# Political Timeline

A portrait card-swiping game about the hidden leadership behind a political party. Presidents come and go; your party survives in government or opposition. The four keys of power are **Workers, Middle class, Economy and Elites**, shown as illustrated hard-hat, house, rising-coins and briefcase images.

## Play and preview

Choose **Political Timeline > Preview iPhone Portrait**, then press Play. The preview uses the phone's portrait aspect ratio at the available Game-view resolution. **Political Timeline > Fix Game View Sharpness** disables low-resolution aspect ratios and resets zoom; it also works during Play mode. Shift+Space returns from the maximized Game view.

Hold a card left or right to reveal the choice. Release beyond the gold threshold line to choose. Return to the center to cancel. Arrow keys and A/D support the same hold-and-release interaction. On a committed swipe, the card flies off in the chosen direction for 0.30 seconds. Meters animate and show numeric changes for 1.15 seconds, then the consequence card waits indefinitely. Swipe it left or right (or hold and release an arrow key / A or D) to confirm it has been read. This second swipe does not change support or advance the month. It also comes before election night and game-over screens. The next card enters over 0.25 seconds after acknowledgement. Input is locked during this sequence; restarting cancels it. One completed choice advances **one calendar month**, beginning in January 2025. Opening a panel or canceling a swipe does not advance time.

The cards use flat geometric advisor portraits, muted colors and a paper caption. The short question stays above the card; the caption identifies the speaker and shows the previous decision outcome. Hold an institutional choice to see its voting or court requirement. All text uses a dedicated 2048-pixel font atlas sampled at 120 points and a sharp TextMesh Pro material. Entering Play mode in the Presidency scene automatically applies the native-resolution portrait Game view settings. Tap a power image to see the group's meaning and current support. A top meter reaching zero ends the party campaign. Full meters do not end play. There is no term limit or automatic victory date.

## Map, Congress and Supreme Court

Four buttons at the bottom open separate views so the main card stays uncluttered:

- **Map:** geographic outlines of all 50 states and DC, with Alaska and Hawaii inset. Teal favors your coalition, red favors the opposition, and gray means a close race. Tap a state to reveal its electoral votes and projected support bar, with a 50% marker. Support is the two-coalition vote share implied by the simulated margin (50 + margin / 2). Election-night selections hide the bar until that state reports, then use its recorded election margin. Larger shortcuts below the map select DC, DE, RI, CT, NJ and MA. These are fictional game projections, not real polling or party affiliations.
- **Congress:** separate semicircle diagrams show all 435 House seats and 100 Senate seats, with one dot per seat. Teal dots belong to your coalition; red dots belong to the opposition. The House needs 218 seats for a majority; the simplified Senate confirmation rule needs 51. Composition changes at November elections. Bills marked Congress need 218 House seats, 51 Senate seats and your party in the presidency. Without both majorities, the blocked outcome and support changes apply.
- **Court:** nine fictional seats marked aligned, independent, opposed or vacant. A retirement creates a vacancy every 18 months if one is not already open. With your party in the presidency and at least 51 allied senators, the nomination button confirms an aligned replacement. Nominations do not consume an extra month. Judicial review cards need five aligned justices to uphold a policy; otherwise the blocked outcome applies. This is an intentionally simplified fictional rule. A vacancy-only event also lets the player nominate through a monthly card.

**Policies:** a read-only ledger lists enacted policies, descriptions and enactment dates, with three entries per page. Twenty durable policies can be enacted through successful event choices. Failed votes do not enact laws; repeat enactments do not duplicate them. Laws survive election losses and only a successful repeal event removes them. School meals has a repeal event available while enacted.

The party starts with 210 House seats and 48 senators. State leans are strengthened by 15%, support sensitivity is reduced to 0.55, and electoral resistance subtracts four margin points. These editable fictional difficulty settings make a neutral coalition less competitive. State leans and support weights are intentionally fictional and editable. Economic support affects state projections alongside workers, the middle class and elites. The old Security meter has been migrated to Economy without losing the authored effect values.

## Election calendar

- Every even-numbered year switches the main background to warm white, with dark text. Power illustrations keep their original colors.
- Midterms take place every two years; presidential contests take place every four years.
- The party continues indefinitely through successive presidents and election cycles.
- Elections automatically pause decisions after the swipe consequence and open election night. States begin gray and report over time, with safer contests first and close races last. Electoral totals build toward 270 in presidential years; House and Senate totals build in midterms. The reveal uses the already-computed result and never rerolls it. Uncalled states hide results until they report. Skip to final results completes the count immediately. **View Congress** shows the new chambers, and **Continue** returns to the cards (including after a defeat).
- Controlling the presidency requires at least 270 electoral votes. Falling short, including a tie, moves the party into opposition without ending play. Organizing and candidate-selection events help it rebuild. A later election can return it to power.
- At every election, all House seats are recalculated and one third of the modeled Senate seats are contested. State margins derive from their starting lean and the player's four support levels. Results are deterministic for the same support and configuration.

The state electoral weights use the [National Archives' 2024/2028 allocations](https://www.archives.gov/electoral-college/allocation). For this prototype, those weights stay fixed in later years and all states use statewide winner-take-all results, including Maine and Nebraska. House districts, individual Senate races, real court appointments and contingent elections are simplified game systems.

## Add content in Unity

Open **Political Timeline > Content Workshop**. New Card and Duplicate Selected add real ScriptableObject cards to the campaign. Write a short question (around 50 characters), two choice labels (around 20 characters), consequences, four support changes and an optional follow-up branch. Advisor names select geometric character variants; CONGRESS and THE COURT categories add institutional backgrounds. Legacy portrait Sprites remain stored but are no longer displayed. Set a choice institution rule and its blocked consequence/support changes for conditional outcomes. Set the card condition to CourtVacancy or DividedCongress to restrict when it can appear. The starter deck now contains 38 cards, including opposition organizing and an event-driven policy repeal.

Follow-ups override ordinary selection rules. Otherwise, earliest decision, weight and once-per-run control draws. Cards can repeat across months, but immediate repeats are avoided when alternatives exist. **Validate Deck** checks content and national settings. **Run Rule Checks** exercises the calendar, support limits, elections, congressional totals, court appointments and deck behavior. **Balance Preview** runs 200 simulated campaigns, acknowledging election reports as it goes.

Choose **Political Timeline > National Simulation Settings** to edit state leans, interest weights, electoral weights, incumbent advantage and the court retirement interval. The legacy column/row fields are unused by the geographic map. Interest vector X = Workers, Y = Middle class, Z = Economy, W = Elites. Campaign Settings controls the inauguration year, starting support and national asset. Start years should follow presidential elections, such as 2025 or 2029. Changes take effect on a new run.

The four transparent power PNGs are in `Assets/Content/Presidency/PowerImages`; replace these files to change the illustrations, then rebuild the starter scene. The map uses [us-atlas](https://github.com/topojson/us-atlas) v3 projected Census Bureau 2017 boundaries. Its ISC license, original TopoJSON and placement data are retained in `Assets/Content/Presidency/Map`. `Tools/build_state_map.py` rebuilds the tintable state sprites with Python and Pillow. No network connection is needed to play.

## Status and scene maintenance

This is an editable game foundation with 38 cards. Progress currently resets when Play mode ends. State and institutional systems are fictional abstractions, not a forecast. Physical iPhone testing and a signed iOS build remain to be done.

The original Main scene is preserved. **Create Starter Scene** rebuilds the Presidency layout after confirmation while preserving content assets. Save custom layouts under a different scene name before rebuilding.
