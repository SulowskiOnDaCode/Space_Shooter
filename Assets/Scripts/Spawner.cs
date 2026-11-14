using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Endless, ramping waves.
///  - Each wave spawns more ships than the last (see <see cref="ShipsForWave"/>), so difficulty
///    climbs quickly, but there are never more than <see cref="maxShips"/> ships alive at once.
///  - Kamikazes join from wave <see cref="kamikazeFromWave"/>, heavy gunships from
///    <see cref="heavyFromWave"/>, and their share grows over time.
///  - The next wave starts when the current one is cleared, or after <see cref="waveTimeout"/>
///    seconds (topping up to the cap) so the pressure never stops.
///  - Ships appear just outside the camera view around the player, then fly in.
///
/// Ships are counted by their scripts, not tags (the old version looked for an "Enemy2" tag that
/// isn't defined in the project, which threw an error and stopped the waves after wave 2).
/// </summary>
public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject Asteroid;
    [SerializeField] private GameObject Enemy1;
    [SerializeField] private GameObject Enemy2;
    [SerializeField] private GameObject Enemy3;

    [SerializeField] private float AsteroidSpawnHeight; // kept for the existing scene reference (asteroids place themselves)

    [Header("Difficulty")]
    [Tooltip("Hard cap on ships alive at the same time.")]
    [SerializeField] private int maxShips = 11;
    [SerializeField] private int firstWaveShips = 2;
    [Tooltip("Extra ships added every wave.")]
    [SerializeField] private float shipsAddedPerWave = 1.5f;
    [SerializeField] private int kamikazeFromWave = 3;
    [SerializeField] private int heavyFromWave = 5;
    [Tooltip("Max heavy gunships alive at once (they're tough).")]
    [SerializeField] private int maxHeavies = 3;

    [Header("Asteroids (continuous, independent of ship waves)")]
    [Tooltip("Asteroids kept on screen at the start.")]
    [SerializeField] private int baseAsteroids = 16;
    [Tooltip("Extra asteroids allowed per wave reached.")]
    [SerializeField] private float asteroidsAddedPerWave = 1f;
    [SerializeField] private int maxAsteroids = 32;
    [Tooltip("Seconds between asteroid spawns while below the target count (randomised ±40%).")]
    [SerializeField] private float asteroidInterval = 0.3f;

    [Header("Timing")]
    [Tooltip("Seconds before the next wave arrives even if ships are still alive.")]
    [SerializeField] private float waveTimeout = 25f;
    [SerializeField] private float pauseBetweenWaves = 2f;
    [Tooltip("Delay between individual ship spawns inside a wave.")]
    [SerializeField] private float spawnStagger = 0.2f;

    private GameObject player;
    private Camera cam;
    public int CurrentWave { get; private set; }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        cam = Camera.main;
        // Enemies placed directly in the scene get the glow outline too.
        foreach (var e in FindObjectsByType<Enemy1DamageSystem>()) EnemyGlow.Apply(e.gameObject);
        foreach (var e in FindObjectsByType<Enemy2Movement>()) EnemyGlow.Apply(e.gameObject);
        foreach (var e in FindObjectsByType<Enemy3>()) EnemyGlow.Apply(e.gameObject);
        StartCoroutine(SpawnWaves());
        StartCoroutine(AsteroidField());
    }

    /// <summary>Keeps a steady stream of asteroids going the whole game, denser in later waves.</summary>
    private IEnumerator AsteroidField()
    {
        while (true)
        {
            while (TimeFreeze.Active) yield return null; // nothing new appears while time is frozen
            int target = Mathf.Min(maxAsteroids,
                Mathf.FloorToInt(baseAsteroids + Mathf.Max(0, CurrentWave - 1) * asteroidsAddedPerWave));

            if (Asteroid != null && Count<Enemy>() < target)
                Instantiate(Asteroid, Vector3.zero, Quaternion.identity); // Enemy.cs places it above the view

            yield return new WaitForSeconds(asteroidInterval * Random.Range(0.6f, 1.4f));
        }
    }

    /// <summary>Wave 1: 2 ships, 2: 4, 3: 5, 4: 7, 5: 8, 6: 10, 7+: 11 (the cap).</summary>
    public int ShipsForWave(int wave)
    {
        return Mathf.Min(maxShips, Mathf.FloorToInt(firstWaveShips + (wave - 1) * shipsAddedPerWave + 0.5f));
    }

    private IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(1f);

        for (int wave = 1; ; wave++)
        {
            CurrentWave = wave;

            // Ships (asteroids run separately in AsteroidField), topped up to the cap
            int toSpawn = Mathf.Min(ShipsForWave(wave), maxShips - CountShips());
            for (int i = 0; i < toSpawn; i++)
            {
                while (TimeFreeze.Active) yield return null;
                if (CountShips() >= maxShips) break;
                SpawnShip(PickShip(wave));
                yield return new WaitForSeconds(spawnStagger);
            }

            // Wait for the wave to be cleared, or time out (but only spawn more if there's room).
            float waveStart = Time.time;
            yield return new WaitUntil(() => !TimeFreeze.Active && (
                CountShips() == 0 ||
                (Time.time - waveStart > waveTimeout && CountShips() < maxShips)));

            yield return new WaitForSeconds(pauseBetweenWaves);
        }
    }

    private GameObject PickShip(int wave)
    {
        float basic = 1f;
        float kamikaze = wave >= kamikazeFromWave ? Mathf.Min(1f, 0.35f + 0.1f * (wave - kamikazeFromWave)) : 0f;
        float heavy = wave >= heavyFromWave && Count<Enemy3>() < maxHeavies
            ? Mathf.Min(0.6f, 0.2f + 0.05f * (wave - heavyFromWave))
            : 0f;

        float roll = Random.value * (basic + kamikaze + heavy);
        if (roll < heavy) return Enemy3;
        if (roll < heavy + kamikaze) return Enemy2;
        return Enemy1;
    }

    private void SpawnShip(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject ship = Instantiate(prefab, OffscreenPoint(), Quaternion.identity);
        EnemyGlow.Apply(ship); // subtle coral outline so ships read against the dark background
    }

    /// <summary>A random point just outside the camera view, centred on the player.</summary>
    private Vector3 OffscreenPoint()
    {
        Vector3 centre = player != null ? player.transform.position : transform.position;
        float halfH = CameraView.HalfHeight(cam);
        float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);
        float radius = Mathf.Sqrt(halfW * halfW + halfH * halfH) + 1.5f;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(radius, radius + 5f);
        return new Vector3(centre.x + Mathf.Cos(angle) * dist, centre.y + Mathf.Sin(angle) * dist, 0f);
    }

    private static int CountShips()
    {
        return Count<Enemy1DamageSystem>() + Count<Enemy2Movement>() + Count<Enemy3>();
    }

    private static int Count<T>() where T : Object
    {
        return FindObjectsByType<T>().Length;
    }
}
