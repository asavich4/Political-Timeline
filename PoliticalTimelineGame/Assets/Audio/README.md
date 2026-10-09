# Game audio

Original synthesized audio made for this project; no external recordings or samples.

- **BehindTheParty_Loop.wav**: 45.714-second, 84 BPM instrumental loop with quiet plucked notes and warm sustained chords. Plays through the start menu and gameplay, fading in over two seconds.
- **Card_Swipe_01–03.wav**: alternating short paper swishes on committed decision swipes.
- **Card_Confirm.wav**: lighter paper sound when dismissing the consequence card.

In the Presidency scene, select **Game Audio** under the gameplay canvas (or use **Political Timeline > Audio > Install or Select Audio**). The Party Audio component exposes the clips, Music Volume, Card Volume, fade duration and independent mute switches. Replace any clip by dragging in another audio asset. The two Audio Sources are saved as children, and the main camera has the scene's Audio Listener.

All clips are stereo/mono 44.1 kHz 16-bit WAV assets that can be previewed in Unity's audio Inspector. The music loops continuously across saves, loading, and menus; audio pauses when the application is suspended. Cancelled gestures make no sound. Left/right committed swipes have a subtle stereo direction.

The reproducible composition and synthesis source is `Tools/create_party_audio.py` at the repository root. The current music is an original starting soundtrack, not music from Reigns.

The Masked Figure sprite is reserved for later. The 22 existing Policy Director cards now use Congress Head; the editor's advisor mapping uses the same replacement.
