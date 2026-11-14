using UnityEngine;

/// <summary>
/// Basic enemy: turns towards the player and follows at a steady cruise speed, keeping
/// StopDistance away so it can shoot. When it is far behind (e.g. just spawned off-screen) it
/// smoothly speeds up to farAwaySpeed instead of jumping. Rigidbody interpolation keeps the
/// physics-step movement smooth on screen.
/// </summary>
public class Enemy1Movement : MonoBehaviour
{
   public Transform Player;
   private Rigidbody rb;

   [Tooltip("Normal follow speed.")]
   [SerializeField] private float moveSpeed = 5.5f;
   [Tooltip("Keeps this distance to the player (slows down smoothly before it).")]
   [SerializeField] private float StopDistance = 7f;
   [Tooltip("From this distance on it starts to speed up to catch up.")]
   [SerializeField] private float farAwayDistance = 12f;
   [Tooltip("Top speed when far away (reached 10 units beyond farAwayDistance).")]
   [SerializeField] private float farAwaySpeed = 14f;

   private void Start()
   {
       GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
       if (playerObject != null) Player = playerObject.transform;
       rb = GetComponent<Rigidbody>();
       rb.interpolation = RigidbodyInterpolation.Interpolate;
   }

   private void FixedUpdate()
   {
       if (Player == null) return;

       Vector3 toPlayer = Player.position - rb.position;
       toPlayer.z = 0f;
       float distance = toPlayer.magnitude;
       if (distance < 0.001f) return;
       Vector3 direction = toPlayer / distance;

       // Face the player.
       float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
       rb.MoveRotation(Quaternion.AngleAxis(angle, Vector3.forward));

       // Cruise, ramp up smoothly when far away, ease out before the stop distance.
       float catchUp = Mathf.InverseLerp(farAwayDistance, farAwayDistance + 10f, distance);
       float speed = Mathf.Lerp(moveSpeed, farAwaySpeed, catchUp);
       speed *= Mathf.Clamp01((distance - StopDistance) / 1.5f);

       rb.MovePosition(rb.position + direction * (speed * Time.fixedDeltaTime));

       if (distance > 1000f) Destroy(gameObject);
   }
}
