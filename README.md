# Order of the Slice

A 2D arcade game for Windows and Android. Swipe airborne ingredients in the order shown on the recipe, avoid the wrong ingredients and bombs, and build a combo that charges Fever.

## Team

- **Bar Israelov** — core gameplay, technical architecture, slicing/input, object pooling, and builds.
- **Linoy Kasuker** — recipes and progression, UI/UX, visual and audio presentation, playtesting, and balancing.
- Both — programming, game design, Android integration, documentation, and QA.

## Requirements

- Unity **6000.3.20f1**
- Platforms: Windows and Android

## Open and play

1. Clone the repository.
2. Open the project in Unity 6000.3.20f1.
3. Open `Assets/Scenes/MainMenu.unity`.
4. Press Play and choose Play from the main menu.

No additional setup is required.

## Controls

- **Windows:** hold the left mouse button and swipe to slice. Click UI buttons.
- **Android:** hold one finger and swipe to slice. Tap UI buttons.
- **Pause:** Esc or the Pause button during play. Android Back does the same.
- **Resume:** Resume, or Android Back, from Pause. A pause during the countdown restarts the countdown from 3.
- **Game Over:** after the short lockout, Restart or Main Menu. Android Back returns to the Main Menu.
- **Quit:** available from the Main Menu.

## Build

Enabled scenes, in order:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/Gameplay.unity`

Android player settings already in the project:

- IL2CPP
- ARM64 only
- Landscape
- Package `com.israelovkasuker.orderoftheslice`

## Design

The design document is [Docs/GDD.md](Docs/GDD.md).

## Course patterns

- **Object pooling** — `PoolManager`, `SlicePresentation`, `ScoreFeedback`
- **Coroutines** — `GameManager`, `SpawnDirector`, `RecipeManager`
- **Singleton** — `AudioManager` is the only object kept across scenes
- **ScriptableObjects** — `GameConfig`, `RecipeDefinition`
- **Observer / events** — `GameManager`, `RecipeManager`, `ScoreFeverSystem`
- **State machine** — `GameState`
- **PlayerPrefs** — `SaveService` stores only the best score

## Credits and licences

Ingredient, bomb, kitchen, and application-icon art were made with AI-assisted image generation and team direction.

UI and interface sounds are selected Kenney CC0 pieces. Slice impacts are Independent.nu CC0. The bomb clip is Listener’s CC0 dynamite effect. Music is Fupi’s CC0 Empacotatron. Full names and URLs are in the in-game Credits panel.

Slice and launch feel referenced from the public-domain [Brackeys Fruit Ninja Replica](https://github.com/Brackeys/Fruit-Ninja-Replica). The implementation was rewritten for this project’s Input System, pooling, and architecture. No Brackeys assets are included.
