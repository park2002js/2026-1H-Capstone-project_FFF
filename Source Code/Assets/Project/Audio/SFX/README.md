# SFX Source Notes

Prototype SFX are copied into `Assets/Project/Resources/Audio/SFX` using the runtime sound ID as the filename.

Sources:
- Kenney Casino Audio: https://kenney.nl/assets/casino-audio
- Kenney Interface Sounds: https://kenney.nl/assets/interface-sounds
- Kenney RPG Audio: https://kenney.nl/assets/rpg-audio

License:
- Creative Commons CC0. Credit to `Kenney.nl` is appreciated but not required by the source packs.

Replacement workflow:
- Replace any `Resources/Audio/SFX/{sound_id}.ogg` file with a new clip using the same filename.
- Keep the sound ID in `SoundIds.cs` and `SoundCatalog.asset` unchanged.
