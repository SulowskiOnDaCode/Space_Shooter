using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class TwinStickMovement : MonoBehaviour
{
    private enum PlayerState
    {
        Vulnerable,
        Invincible
    }
    
    public static Class playerClass;

    [SerializeField] private GameObject assaultModel;
    [SerializeField] private GameObject tankModel;
    [SerializeField] private GameObject wraithModel;
    [SerializeField] private GameObject shieldModel;
    [SerializeField] private GameObject explosionPrefab;
    [Tooltip("Big explosion spawned when the player loses their last life. Falls back to explosionPrefab if empty.")]
    [SerializeField] private GameObject deathExplosionPrefab;
    [SerializeField] private GameObject losingCanvas;
    
    [SerializeField] private AbilityBar abilityBar;
    
    [SerializeField] private float playerSpeed;
    [SerializeField] private float controllerDeadzone = 0.1f;
    [SerializeField] private float gamepadRotationSmoothing = 1000f;
    
    [SerializeField] private bool isGamepad;
    
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletModel;
    [SerializeField] private float CurrentPlayerSpeed;
    [SerializeField] private float pitchChangeSpeed = 0.1f;
    
    private GameObject playerModel;
    private Renderer modelRenderer;
    private Renderer shieldRenderer;
    
    private PlayerState playerState = PlayerState.Vulnerable;
    
    private float shootingCooldown;
    private float abilityCooldown;
    
    private float dashSpeed = 20f;
    private float dashTime = 0.25f;

    private float shieldTime = 5f;

    private float teleportDistance = 10f;

    [Header("Ability buffs")]
    [Tooltip("Wraith: fire-rate multiplier after teleporting.")]
    [SerializeField] private float wraithFireRateMultiplier = 3f;
    [Tooltip("Tank: fire-rate multiplier while the shield is up.")]
    [SerializeField] private float tankFireRateMultiplier = 2f;
    [Tooltip("Seconds the Wraith rapid fire / Assault homing bullets last after the ability.")]
    [SerializeField] private float abilityBuffDuration = 4f;

    private float rapidFireUntil = -1f;
    private float homingUntil = -1f;

    [Header("Banking tilt")]
    [Tooltip("Max degrees the ship rolls when strafing left/right.")]
    [SerializeField] private float maxBankAngle = 20f;
    [Tooltip("How quickly the ship rolls into / out of the tilt.")]
    [SerializeField] private float bankSmoothing = 8f;
    [Tooltip("Flip if the ship tilts the wrong way.")]
    [SerializeField] private bool invertBank;

    private Quaternion modelBaseRotation;
    private float currentBank;

    private float ABILITY_COOLDOWN;
    private float SHOOTING_COOLDOWN;
    
    private CharacterController controller;
    
    private Vector2 movement;
    private Vector2 aim;
    private Vector2 move;
    
    private bool isShooting;
    private bool isUsingAbility;
    private bool doesAbility;
    private bool isInvincible;
    public static bool isUsingMenu;
    
    private PlayerControls playerControls;
    private PlayerInput playerInput;

    private Camera cam;
    private const float INVINCIBILITY_DURATION = 3f;
    private const float BLINKING_DURATION = 0.2f;
    
    private static readonly WaitForSeconds waitForInvinibilityTime = new WaitForSeconds(INVINCIBILITY_DURATION);
    private static readonly WaitForSeconds waitForBlinkingTime = new WaitForSeconds(BLINKING_DURATION);
    
    public static void setPlayerClass(Class playerClass) {
            TwinStickMovement.playerClass = playerClass;
    }
    
    private void Start()
    {
        cam = Camera.main;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();

        cam = Camera.main;

        if (playerClass == null) playerClass = new Assault();
        playerSpeed = playerClass.movementSpeed;
        SHOOTING_COOLDOWN = playerClass.shootingCooldown;
        ABILITY_COOLDOWN = playerClass.ability.cooldown;
        
        SetModel();
        IgnoreOwnColliders();
        ShieldGlow.Apply(shieldModel);
        GameLogic.SetLivesAndScore(playerClass.hp);
        abilityBar.SetMaxProgress(ABILITY_COOLDOWN);
    }

    private void SetModel()
    {
        if (playerClass.GetType() == typeof(Assault))
        {
            playerModel = assaultModel;
            assaultModel.SetActive(true);
            tankModel.SetActive(false);
            wraithModel.SetActive(false);
        }
        else if (playerClass.GetType() == typeof(Tank))
        {
            playerModel = tankModel;
            assaultModel.SetActive(false);
            tankModel.SetActive(true);
            wraithModel.SetActive(false);
        }
        else if (playerClass.GetType() == typeof(Wraith))
        {
            playerModel = wraithModel;
            assaultModel.SetActive(false);
            tankModel.SetActive(false);
            wraithModel.SetActive(true);
        }
        
        modelRenderer = playerModel.GetComponent<Renderer>();
        modelBaseRotation = playerModel.transform.localRotation;
    }

    /// <summary>
    /// Rolls the ship model around its nose-to-tail axis based on how fast it is moving sideways
    /// relative to where it is facing, so strafing left/right gives a small 3D tilt.
    /// </summary>
    private void HandleBanking()
    {
        if (playerModel == null) return;

        // Sideways component of movement relative to the ship's heading (-1 = left, 1 = right).
        float lateral = Vector2.Dot(move, (Vector2)transform.right);
        float target = -lateral * maxBankAngle * (invertBank ? -1f : 1f);

        currentBank = Mathf.Lerp(currentBank, target, 1f - Mathf.Exp(-bankSmoothing * Time.deltaTime));

        // Parent's local up is the ship's forward direction in this top-down setup.
        playerModel.transform.localRotation = Quaternion.AngleAxis(currentBank, Vector3.up) * modelBaseRotation;
    }

    /// <summary>
    /// The ship models carry their own (non-trigger) colliders. Without this the CharacterController
    /// collides with its own model, which makes the ship jitter when moving into it (e.g. pressing
    /// down while the model is rotated so its collider sticks out below the controller).
    /// Unity resets ignore pairs when a collider is deactivated, so call this again after re-enabling.
    /// </summary>
    private void IgnoreOwnColliders()
    {
        if (controller == null) return;
        foreach (var col in GetComponentsInChildren<Collider>(false))
        {
            if (col != controller && col.enabled) Physics.IgnoreCollision(controller, col, true);
        }
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        shootingCooldown -= Time.deltaTime;
        abilityCooldown -= Time.deltaTime;
        abilityBar.SetProgress(ABILITY_COOLDOWN - abilityCooldown);

        GetPlayerSpeed(); 
        // Adjust audio pitch based on player speed
        AdjustAudioPitch(CurrentPlayerSpeed);
        HandleBanking(); // also runs during the dash so the tilt stays smooth

        if (doesAbility) return;
        HandleInput();
        HandleMovement();
        HandleRotation();
        HandleShooting();
        HandleAbility();
    }

    private void FixedUpdate()
    {
        if (doesAbility) return;
    }

    private void HandleInput()
    {
        movement = playerControls.Controls.Movement.ReadValue<Vector2>();
        aim = playerControls.Controls.Aim.ReadValue<Vector2>();
        isShooting = playerControls.Controls.Shooting.ReadValue<float>() > 0.1f;
        isUsingAbility = playerControls.Controls.Ability.ReadValue<float>() > 0.1f;
        isUsingMenu = playerControls.Controls.Menu.ReadValue<float>() > 0.1f;
    }

    private void HandleMovement()
    {
        move = new Vector2(movement.x, movement.y).normalized;
        controller.Move(move * (Time.deltaTime * playerSpeed));
    }

    private void HandleRotation()
    {
        if (isGamepad)
        {
            // Gamepad Rotation
            
            if (Mathf.Abs(aim.x) > controllerDeadzone || Mathf.Abs(aim.y) > controllerDeadzone)
            {
                Vector3 playerDirection = Vector3.right * aim.x + Vector3.up * aim.y;
                
                if (playerDirection.sqrMagnitude > 0.0f)
                {
                    float angle = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg - 90f;
                    Quaternion newRotation = Quaternion.Euler(0, 0, angle);
                    
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, gamepadRotationSmoothing * Time.deltaTime);
                }
            }
        }
        else
        {
            // Mouse Rotation
            
            // Aim in screen space: compare the mouse with the ship's *on-screen* position, both from
            // the same camera state. The camera follows with smoothing and updates after the ship,
            // so mixing the old camera with the new ship position made the aim angle wobble every
            // frame (visible jitter, worst when moving sideways to the aim direction).
            Vector3 shipOnScreen = cam.WorldToScreenPoint(transform.position);
            Vector2 lookDir = new Vector2(aim.x - shipOnScreen.x, aim.y - shipOnScreen.y);
            if (lookDir.sqrMagnitude < 4f) return; // mouse right on the ship: keep the current heading
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void HandleShooting()
    {
        if (isShooting && shootingCooldown <= 0)
        {
            GameObject bullet = Instantiate(bulletModel, firePoint.position, firePoint.rotation);
            IgnoreBulletCollisions(bullet);
            if (Time.time < homingUntil)
            {
                var projectile = bullet.GetComponent<Projectile>();
                if (projectile != null) projectile.EnableHoming();
            }
            SoundManager.soundManager.PlayFiringSound();
            shootingCooldown = SHOOTING_COOLDOWN / FireRateMultiplier();
        }
    }

    /// <summary>
    /// Bullets have solid colliders and spawn right at the nose. Without this, the CharacterController
    /// bumps into its own bullets when flying in the shooting direction, which makes the ship jitter.
    /// </summary>
    private void IgnoreBulletCollisions(GameObject bullet)
    {
        foreach (var bulletCol in bullet.GetComponentsInChildren<Collider>())
        {
            if (controller != null) Physics.IgnoreCollision(controller, bulletCol, true);
            foreach (var shipCol in GetComponentsInChildren<Collider>(false))
                if (shipCol != controller && shipCol.enabled) Physics.IgnoreCollision(shipCol, bulletCol, true);
        }
    }

    private float FireRateMultiplier()
    {
        float multiplier = 1f;
        if (Time.time < rapidFireUntil) multiplier = Mathf.Max(multiplier, wraithFireRateMultiplier);
        if (shieldModel != null && shieldModel.activeSelf) multiplier = Mathf.Max(multiplier, tankFireRateMultiplier);
        return multiplier;
    }

    private void HandleAbility()
    {
        if (isUsingAbility && abilityCooldown <= 0) // move != Vector2.zero
        {
            if (playerClass.GetType() == typeof(Assault))
            {
                SoundManager.soundManager.PlayAbility1Sound();
                StartCoroutine(Dash());
            } else if (playerClass.GetType() == typeof(Tank))
            {
                SoundManager.soundManager.PlayAbility2Sound();
                StartCoroutine(Shield());
            }
            else
            {
                SoundManager.soundManager.PlayAbility3Sound();
                StartCoroutine(Teleport());
            }
            abilityCooldown = ABILITY_COOLDOWN;
        }
        /*
        abilityCooldown -= Time.deltaTime;
        
        if (isUsingAbility && abilityCooldown <= 0)
        {
            playerClass.ability.Activate(playerModel, playerControls);
            abilityCooldown = playerClass.ability.cooldown;
        }
        */
    }

    public void OnDeviceChange(PlayerInput input)
    {
        isGamepad = input.currentControlScheme.Equals("Gamepad");
    }

    public void GetPlayerSpeed()
    {
    Vector2 move = new Vector2(movement.x, movement.y);
    CurrentPlayerSpeed = (move.magnitude / Time.deltaTime) / 100; // Calculate speed by dividing magnitude by time
    //return CurrentPlayerSpeed;
    }

    private void AdjustAudioPitch(float targetSpeed)
{
    AudioSource audioSource = GetComponent<AudioSource>();

    // Calculate the target pitch based on the target speed
    float targetPitch = Mathf.Clamp(targetSpeed, 1f, 1.2f);

    // Smoothly interpolate towards the target pitch
    float smoothedPitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * pitchChangeSpeed);

    audioSource.pitch = smoothedPitch;
}
    
    private void OnTriggerEnter(Collider other)
    {
        Enemy collideWith = other.GetComponent<Enemy>();
        
        if (playerState == PlayerState.Vulnerable)
        {
            if (collideWith != null) other.GetComponent<Enemy>().SetSpeedAndPosition();

            bool hasLivesLeft = GameLogic.HandleLiveDecrease();

            if (hasLivesLeft)
            {
                SoundManager.soundManager.PlayExplosionSound();
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                StartCoroutine(HandleHit());
            }
            if (!hasLivesLeft)
            {
                Highscores.Submit(GameLogic.score); // save the run before the death screen shows it
                losingCanvas.SetActive(true);
                StartCoroutine(GradualTimeScaleChange(0.1f, 1f, 1f));
                Instantiate(deathExplosionPrefab != null ? deathExplosionPrefab : explosionPrefab, transform.position, Quaternion.identity);
                SoundManager.soundManager.PlayExplosionSound();
                playerModel.SetActive(false);
            }
        }
    }
    
    private IEnumerator HandleHit()
    {
        playerState = PlayerState.Invincible;
        float endTime = Time.time + INVINCIBILITY_DURATION;
        bool b = false;
        while (Time.time <= endTime)
        {
            playerModel.SetActive(b);
            if (b) IgnoreOwnColliders();
            b = !b;
            yield return waitForBlinkingTime;
        }
        playerModel.SetActive(true);
        IgnoreOwnColliders();
        playerState = PlayerState.Vulnerable;
    }

    private IEnumerator Shield()
    {
        playerState = PlayerState.Invincible;
        shieldModel.SetActive(true);

        yield return new WaitForSeconds(shieldTime);

        shieldModel.SetActive(false);
        playerState = PlayerState.Vulnerable;
    }

    private IEnumerator Dash()
    {
        float startTime = Time.time;
        doesAbility = true;

        while (Time.time <= startTime + dashTime)
        {
            if (move == Vector2.zero) move = Vector2.up;
            controller.Move(move * (dashSpeed * Time.deltaTime));
            yield return null;
        }

        doesAbility = false;
        homingUntil = Time.time + abilityBuffDuration; // Assault: homing bullets after the dash
    }

    private IEnumerator Teleport()
    {
        if (move == Vector2.zero) move = Vector2.up;
        Vector3 position = transform.position;
        position = new Vector3(position.x + move.x * teleportDistance, position.y + move.y * teleportDistance,
            position.z);
        transform.position = position;
        Physics.SyncTransforms();
        rapidFireUntil = Time.time + abilityBuffDuration; // Wraith: rapid fire after teleporting

        yield return null;
    }
    private IEnumerator GradualTimeScaleChange(float targetTimeScale, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);

        float currentTime = 0f;
        float initialTimeScale = Time.timeScale;

        while (currentTime < duration)
        {
            // Don't override a paused game (ESC menu sets timeScale to 0).
            if (Time.timeScale == 0f) { yield return null; continue; }
            currentTime += Time.deltaTime;
            Time.timeScale = Mathf.Lerp(initialTimeScale, targetTimeScale, currentTime / duration);
            yield return null;
        }

        Time.timeScale = targetTimeScale; // Ensure that the target timescale is set precisely
    }
   
}
