using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Projectile : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    
    [SerializeField] private float projectileSpeed;
    
    private void Awake()
    {
        BulletStyle.ApplyPlayer(gameObject); // sleek white bolt + trail
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move()
    {
        float speedFactor = 1f;

        if (TimeFreeze.Active)
        {
            // Time stop: coast to a halt just ahead of the ship and hang there, hitbox off,
            // so shots pile up and only connect when time resumes.
            if (!hitboxOff) SetHitbox(false);
            frozenFor += Time.deltaTime;
            speedFactor = Mathf.Clamp01(1f - frozenFor / FreezeCoastTime);
            if (speedFactor <= 0f) return;
        }
        else if (hitboxOff)
        {
            // Time resumed: re-enabling the collider makes bullets already inside a target register the hit.
            SetHitbox(true);
            frozenFor = 0f;
        }

        if (homing && !TimeFreeze.Active) SteerTowardsTarget();

        float amtToMove = projectileSpeed * Time.deltaTime * speedFactor;
        transform.Translate(Vector3.up * amtToMove);
    }

    // ------------------------------------------------------------------ time freeze

    private const float FreezeCoastTime = 0.18f;
    private float frozenFor;
    private bool hitboxOff;
    private Collider[] hitboxes;

    private void SetHitbox(bool on)
    {
        if (hitboxes == null) hitboxes = GetComponentsInChildren<Collider>();
        foreach (var c in hitboxes) if (c != null) c.enabled = on;
        hitboxOff = !on;
    }

    // ------------------------------------------------------------------ homing (Assault dash buff)

    [Header("Homing")]
    [Tooltip("Degrees per second a homing bullet can turn. Low = subtle curve.")]
    [SerializeField] private float homingTurnRate = 220f;
    [Tooltip("How far ahead a homing bullet looks for enemies.")]
    [SerializeField] private float homingRange = 9f;

    private bool homing;
    private Transform homingTarget;
    private float retargetTimer;

    /// <summary>Makes this bullet gently curve towards the nearest enemy in front of it.</summary>
    public void EnableHoming()
    {
        homing = true;
        var style = GetComponent<BulletStyle>();
        if (style != null) style.SetColor(BulletStyle.HomingColor); // homing shots glow cyan
    }

    private void SteerTowardsTarget()
    {
        retargetTimer -= Time.deltaTime;
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy || retargetTimer <= 0f)
        {
            homingTarget = HomingTargets.FindBest(transform.position, transform.up, homingRange);
            retargetTimer = 0.15f;
        }
        if (homingTarget == null) return;

        Vector2 toTarget = homingTarget.position - transform.position;
        float desired = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
        float current = transform.eulerAngles.z;
        float angle = Mathf.MoveTowardsAngle(current, desired, homingTurnRate * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        //other.GetComponent<Enemy>().SetSpeedAndPosition();
        
        bool isMissingScore = GameLogic.HandleScoreIncrease();

        if (isMissingScore)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            SoundManager.soundManager.PlayExplosionSound();
        }
    }
}
