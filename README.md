# Space Shooter

Twin-Stick-Shooter im Weltraum, gebaut mit **Unity 6** (Built-in Render Pipeline) als Einzelprojekt.

Man fliegt durch ein Asteroidenfeld, wehrt endlose, immer größere Gegnerwellen ab und sammelt Power-ups. Vor dem Start wählt man eine von drei Klassen mit eigener Fähigkeit.

## Steuerung

| Aktion | Maus & Tastatur | Gamepad |
|---|---|---|
| Bewegen | WASD | Linker Stick |
| Zielen | Maus | Rechter Stick |
| Schießen | Linksklick | RT |
| Fähigkeit | Leertaste | LT |
| Pause | Esc | Start |

## Klassen

| Klasse | Leben | Fähigkeit (8 s Abklingzeit) |
|---|---|---|
| Assault | 3 | Dash, danach 4 s zielsuchende Schüsse |
| Tank | 4 | Schild (5 s), währenddessen doppelte Feuerrate |
| Wraith | 2 | Teleport, danach 4 s dreifache Feuerrate |

## Power-ups (aus zerstörten Asteroiden)

- **Reparatur (grünes Plus):** +1 Leben, bis zu 6 („Überheilung“). Fällt öfter, wenn man beschädigt ist.
- **Zeitstopp (lila Uhr, selten):** Gegner, Asteroiden und gegnerische Schüsse frieren 4 s ein. Eigene Schüsse bleiben in der Luft stehen und treffen, sobald die Zeit weiterläuft.

## Wo die Anforderungen umgesetzt sind

| Anforderung | Umsetzung |
|---|---|
| Hauptmenü / Pausemenü | Szene `MainMenu`; Esc-Menü mit Weiter, Hauptmenü, Beenden (`GameLogic`, `MenuExtras`) |
| Audio | Soundeffekte über den `SoundManager` (Schüsse, Explosionen, Treffer, Fähigkeiten, Zeitstopp); Musik „Attack By Storm“ (Jujutsu Kaisen Season 2 OST) über den `MusicPlayer` – im Spiel und leise im Hauptmenü |
| Eigene Shader | `Assets/Resources/VFX/*.shader` (Hintergrund, Schild-Fresnel, Explosionen, Pickups, Skybox) |
| Physik | Trigger-Kollisionen für Treffer, `CharacterController` für den Spieler, Rigidbodies für Schüsse |
| Schwierigkeit | `Spawner`: jede Welle mehr Schiffe (max. 11 gleichzeitig), später Kamikaze- und schwere Schiffe, dichteres Asteroidenfeld |
| Highscores | `Highscores` (Top 5 in `PlayerPrefs`), Anzeige im HUD, Todesbildschirm und Hauptmenü |
| Dynamisches Post-Processing | `DynamicPostFX`: Treffer-Vignette + chromatische Aberration, Puls bei 1 Leben, Bloom bei Punkten, Entsättigung bei Zeitstopp und Tod |
| VFX (Shuriken) | `SpaceExplosion` (Partikelsysteme für Explosionen) |
| Singleton | `MusicPlayer.Instance` (bleibt über Szenenwechsel bestehen) und `SoundManager.Instance` (genau eine Instanz, Duplikate werden zerstört) |

## Projektstruktur

```
Assets/
  Scenes/        MainMenu, GameScene, VictoryScene, LoseScene
  Scripts/       Spielerlogik, Gegner, Spawner, Klassen, Fähigkeiten
    Gameplay/    Power-ups, Zeitstopp, Highscores, Musik, Asteroiden-Modelle
    UI/          HUD, Menü-Erweiterungen, Szenenwechsel
    VFX/         Explosionen, Hintergrund, Licht, Schüsse, Post-Processing
  Resources/     Shader, Sounds, Musik und Modelle, die per Code geladen werden
Packages/        Unity-Pakete (Input System, Cinemachine, Post Processing, ...)
ProjectSettings/ Unity-Projekteinstellungen
```

## Öffnen

Projekt mit Unity **6000.6.4f1** öffnen und die Szene `Assets/Scenes/MainMenu.unity` starten.
