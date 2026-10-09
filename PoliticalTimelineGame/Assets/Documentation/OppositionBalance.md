# Elections, opposition and policy records

- FirstAdministration's **Decision Impact** is 1.75: all successful and blocked card meter changes are multiplied, rounded away from zero, then clamped to 0–100. Individual cards remain editable.
- National meter influence on a state's margin now has diminishing returns and a six-point maximum. Campaign outreach contributes 0.75 margin points per organizing point and still decays monthly. The map and election results use the same calculation.
- Nation's **Incumbent Fatigue** penalizes the governing party and helps the challenger. Electoral Resistance and Support Sensitivity remain adjustable. High national meters alone no longer flip virtually every state.
- Losing the presidency continues the same party campaign. Opposition draws focus on campaigning, organizing, fundraising and blocking government bills. Campaign events can appear year-round while out of office. Only a depleted top meter ends the run.
- Six rival policy proposals have enactment and repeal variants. A successful congressional blockade preserves the law's existing status; a failed blockade or choosing to campaign against the bill lets the rival change the law. Blocking requires the House or 41 senators.
- Four rebuilding events offer ways to recover campaign resources. The 16 new cards are regular assets under Content/Presidency/Cards, with `rival_` and `rebuild_` prefixes and existing cast sprites.
- Card choices expose **Rival Enact Policy** and **Rival Repeal Policy**. These only change laws while the player is in opposition. Policy changes still happen through events.
- The Policy record lists newest changes first, three per page. Democrats are blue; Republicans are red. Each entry names the actor, date and action, plus the law's current status. The Policies button gains an asterisk until the record is opened.
- Policy changes are announced in the consequence card, which requires another swipe. Ownership, history and unread status are saved. Older saves attribute existing player-enacted laws to the selected party.

Validation: `PoliticalTimeline.Editor.BalanceChecks.BatchCheck` runs campaign, branching-story and electoral checks, plus effect scaling, both parties' election bounds, opposition draws, successful/failed obstruction, repeal, legacy-save migration, deterministic reload and rendered policy/consequence screens. Run against a separate test project because this editor check creates missing content and opens the gameplay scene.
