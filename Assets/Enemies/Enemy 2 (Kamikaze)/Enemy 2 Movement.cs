using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Enemy2Movement : MonoBehaviour
{
    [SerializeField] private float enemy2speed = 10f;
    [SerializeField] public float distanceToExplode = 1f;

    [SerializeField] public GameObject explosionEnemy2;

    private Rigidbody rb;
    public Transform player;
    private Vector3 movement;
    

    [SerializeField] private int Health = 5;
    private int currentHealth;
    

    [Tooltip("When further away than this, the kamikaze speeds up to catch up.")]
    [SerializeField] private float catchUpDistance = 14f;
    [Tooltip("Top speed when far away (reached 10 units beyond catchUpDistance).")]
    [SerializeField] private float catchUpSpeed = 15f;

    private void Start()
    {
        currentHealth = Health;

        rb = this.GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth on screen between physics steps

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - rb.position;
        toPlayer.z = 0f;
        float distance = toPlayer.magnitude;

        if (distance <= distanceToExplode)
        {
            var transform1 = transform;
            Instantiate(explosionEnemy2, transform1.position, transform1.rotation);
            Destroy(gameObject);
            return;
        }

        movement = toPlayer / distance;
        float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
        rb.MoveRotation(Quaternion.AngleAxis(angle, Vector3.forward));

        // Cruise speed close by, smoothly faster when far away (no more jumping at 100).
        float catchUp = Mathf.InverseLerp(catchUpDistance, catchUpDistance + 10f, distance);
        float speed = Mathf.Lerp(enemy2speed, catchUpSpeed, catchUp);
        rb.MovePosition(rb.position + movement * (speed * Time.fixedDeltaTime));

        if (distance > 1000f) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerProjectile"))
        {
            TakeDamage(1);
        }
    }

    private void TakeDamage(int damage)
    {
        SoundManager.soundManager.PlayHitMarkerSound();
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            DestroyEnemy();
        }
    }

    private void DestroyEnemy()
    {
        Instantiate(explosionEnemy2, transform.position, Quaternion.identity);
        Destroy(gameObject);
        SoundManager.soundManager.PlayExplosionSound();
         SoundManager.soundManager.PlayKillConfirmedSound();
    }
    private void EnemyTooFarAway()
    {
        float DistanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (DistanceToPlayer > 1000f)
        {
            Destroy(gameObject);
        }
    }
}

