using UnityEngine;
using UnityEngine.InputSystem;
using ProjectMayhem.Audio;

/// <summary>
/// Manages player movement and input.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The force applied to the ship to make it accelerate.")]
    [SerializeField] private float acceleration = 50f;
    [Tooltip("The maximum speed the ship can reach.")]
    [SerializeField] private float maxSpeed = 10f;
    [Tooltip("The drag applied to the ship to make it decelerate.")]
    [SerializeField] private float linearDrag = 2.5f;

    [Header("Weapon Settings")]
    [Tooltip("The projectile prefab to be fired.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("The point from which projectiles are fired.")]
    [SerializeField] private Transform firePoint;
    [Tooltip("The number of shots the player can fire per second.")]
    [SerializeField] private float fireRate = 5f;

    [Header("Visuals")]
    [Tooltip("The GameObject representing the player's shield.")]
    [SerializeField] private GameObject shieldVisual;
    [Tooltip("Enable trail renderer for the player ship")]
    [SerializeField] private bool enableTrail = true;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float nextFireTime;
    private bool isFiring;
    private Health health;
    private TrailRendererController trailController;

    private float baseFireRate;
    private Coroutine rapidFireCoroutine;

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleGameStateChange;
        if (health != null)
        {
            health.OnDeath += Die;
            health.OnShieldBroken += DeactivateShieldVisual;
        }
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Gameplay)
        {
            SFX.Play(AudioEvent.PlayerSpawn);
        }
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleGameStateChange;
        if (health != null)
        {
            health.OnDeath -= Die;
            health.OnShieldBroken -= DeactivateShieldVisual;
        }
    }

    private void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject.
        // We use Awake to ensure the reference is set before any other script tries to access it.
        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = linearDrag;
        mainCamera = Camera.main;
        health = GetComponent<Health>();
        baseFireRate = fireRate;

        // Setup trail renderer for the player ship
        if (enableTrail && TrailManager.Instance != null)
        {
            trailController = TrailManager.Instance.SetupTrailForObject(gameObject, "PlayerShip");
        }

        // Ensure the shield is disabled on awake.
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    /// <summary>
    /// Called by the PlayerInput component when the Move action is triggered.
    /// </summary>
    /// <param name="value">The input value from the action.</param>
    private void HandleGameStateChange(GameState newState)
    {
        if (newState != GameState.Gameplay)
        {
            // Stop all movement and input when not in gameplay
            moveInput = Vector2.zero;
            isFiring = false;
            rb.linearVelocity = Vector2.zero;
            
            // Disable trail when not in gameplay
            if (trailController != null)
            {
                trailController.SetTrailEnabled(false);
            }
        }
        else
        {
            // Re-enable trail when entering gameplay
            if (trailController != null && enableTrail)
            {
                trailController.SetTrailEnabled(true);
            }
        }
    }

    public void OnMove(InputValue value)
    {
        if (GameStateManager.Instance.CurrentState != GameState.Gameplay) return;
        // Read the Vector2 value from the input action and normalize it.
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called by the PlayerInput component when the Look action is triggered.
    /// </summary>
    /// <param name="value">The input value from the action.</param>
    public void OnLook(InputValue value)
    {
        if (GameStateManager.Instance.CurrentState != GameState.Gameplay) return;
        lookInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called by the PlayerInput component when the Fire action is triggered.
    /// Updates the firing state based on whether the button is pressed or released.
    /// </summary>
    /// <param name="value">The input value from the action.</param>
    public void OnFire(InputValue value)
    {
        if (GameStateManager.Instance.CurrentState != GameState.Gameplay) return;
        isFiring = value.isPressed;
    }

    /// <summary>
    /// Called by the PlayerInput component when the Pause action is triggered.
    /// </summary>
    public void OnPause()
    {
        if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
        {
            GameStateManager.Instance.ChangeState(GameState.Pause);
        }
        else if (GameStateManager.Instance.CurrentState == GameState.Pause)
        {
            GameStateManager.Instance.ChangeState(GameState.Gameplay);
        }
    }

    private void FixedUpdate()
    {
        // Do not process movement/rotation if the game is not in the Gameplay state
        if (GameStateManager.Instance.CurrentState != GameState.Gameplay)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        // Apply force for movement
        rb.AddForce(moveInput * acceleration);

        // Clamp velocity to max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
        }

        // Handle rotation and firing
        HandleRotation();
        HandleFiring();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        rb.linearDamping = linearDrag;
    }
#endif

    private void Die()
    {
        SFX.Play(AudioEvent.PlayerDeath);
        // Trigger camera shake for player death
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.TriggerPlayerDeathShake();
        }
        
        // Transition to the Defeat state when the player dies.
        GameStateManager.Instance.ChangeState(GameState.Defeat);
        // Disable the player object.
        gameObject.SetActive(false);
    }

    private void HandleFiring()
    {
        if (isFiring && Time.time >= nextFireTime)
        {
            if (projectilePrefab != null && firePoint != null)
            {
                GameObject projectileGO = ObjectPoolManager.Instance.SpawnFromPool("PlayerProjectile", firePoint.position, firePoint.rotation);
                Projectile projectile = projectileGO.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.SetVelocity(firePoint.up);
                }
                SFX.Play(AudioEvent.PlayerShoot);
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    public void ActivateRapidFire(float multiplier, float duration)
    {
        // If a rapid fire power-up is already active, stop the old coroutine.
        if (rapidFireCoroutine != null)
        {
            StopCoroutine(rapidFireCoroutine);
        }

        // Start a new coroutine for the power-up effect.
        rapidFireCoroutine = StartCoroutine(RapidFireCoroutine(multiplier, duration));
        SFX.Play(AudioEvent.PowerUpActivate, transform.position);
    }

    public void ActivateShield()
    {
        if (health != null) health.ActivateShield();
        if (shieldVisual != null) shieldVisual.SetActive(true);
    }

    private void DeactivateShieldVisual()
    {
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    private System.Collections.IEnumerator RapidFireCoroutine(float multiplier, float duration)
    {
        // Apply the fire rate multiplier.
        fireRate *= multiplier;

        // Wait for the specified duration.
        yield return new WaitForSeconds(duration);

        // Revert the fire rate to the base value.
        fireRate = baseFireRate;
        rapidFireCoroutine = null;
    }

    private void HandleRotation()
    {
        Vector2 aimDirection;
        // Check if the current control scheme is Gamepad
        if (GetComponent<PlayerInput>().currentControlScheme == "Gamepad")
        {
            // Use the raw look input as the direction vector for gamepad
            aimDirection = lookInput;
        }
        else
        {
            // For Mouse, convert screen position to world position
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(lookInput);
            aimDirection = (mousePosition - (Vector2)transform.position).normalized;
        }

        // Only rotate if there is significant input
        if (aimDirection.sqrMagnitude > 0.1f)
        {
            // Calculate the angle and set the rotation of the Rigidbody
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = angle;
        }
    }
}
