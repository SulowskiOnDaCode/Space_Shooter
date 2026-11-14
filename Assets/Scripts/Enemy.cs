using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>Asteroid: drifts down the screen while slowly tumbling in 3D.</summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] private float minSpeed = 3f;
    [SerializeField] private float maxSpeed = 6f;
    [Tooltip("Fastest tumble in degrees per second.")]
    [SerializeField] private float maxRotationSpeed = 30f;
    [Tooltip("Slowest tumble in degrees per second.")]
    [SerializeField] private float minRotationSpeed = 10f;
    [SerializeField] private Vector2 minMaxScale = new Vector2(2f, 7f);

    private float speed;
    private Vector3 tumbleAxis;
    private float tumbleSpeed;
    private float scale;
    private float LiveTime = 20f;
    private bool destroyed;

    public void SetSpeedAndPosition()
    {
        // Speed
        speed = Random.Range(minSpeed, maxSpeed);

        // Position
        var position = Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0f, 1f), 1, 0));
        position.y += 2;
        position.z = 0;
        transform.position = position;

        // Rotation: random starting orientation, then a steady tumble around one random 3D axis.
        // A single axis reads as a solid object spinning; per-axis Euler steps just look wobbly.
        transform.rotation = Random.rotationUniform;
        tumbleAxis = Random.onUnitSphere;
        tumbleSpeed = Random.Range(minRotationSpeed, maxRotationSpeed) * (Random.value < 0.5f ? -1f : 1f);

        // Scale
        scale = Random.Range(minMaxScale.x, minMaxScale.y);
        transform.localScale = Vector3.one * scale;

        // Lifetime (restarts if the asteroid is recycled to the top of the screen).
        CancelInvoke(nameof(DestroyAsteroid));
        Invoke(nameof(DestroyAsteroid), LiveTime);
    }

    void Start()
    {
        AsteroidModels.Apply(gameObject); // random rock from Asteroids.fbx with the lava material
        SetSpeedAndPosition();
    }

    void Update()
    {
        Move();
    }

    private void Move()
    {
        float amtToMove = speed * Time.deltaTime;
        transform.Translate(Vector3.down * amtToMove, Space.World);
        transform.Rotate(tumbleAxis, tumbleSpeed * Time.deltaTime, Space.World);

        // Remove once it has drifted well below the view, so the spawner can send new ones in.
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic &&
            transform.position.y < cam.transform.position.y - cam.orthographicSize - scale - 2f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerProjectile") && !destroyed)
        {
            destroyed = true;
            PowerUp.TryDropFromAsteroid(transform.position);
            Destroy(gameObject);
        }
    }

    private void DestroyAsteroid()
    {
        Destroy(gameObject);
    }
}
