using UnityEngine;

/// <summary>
/// Contrôleur de joueur TPS avec déplacements réalistes.
/// Compatible clavier (flèches / ZQSD-WASD selon la disposition) et manette,
/// grâce aux axes "Horizontal" et "Vertical" du système d'Input classique de Unity,
/// déjà configurés par défaut pour lire ces trois méthodes de contrôle.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("La caméra utilisée pour orienter les déplacements (généralement la Main Camera ou une caméra Cinemachine).")]
    [SerializeField] private Transform cameraTransform;

    [Header("Vitesse de déplacement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [Tooltip("Temps (en secondes) pour atteindre la vitesse max depuis l'arrêt.")]
    [SerializeField] private float accelerationTime = 0.15f;
    [Tooltip("Temps (en secondes) pour s'arrêter depuis la vitesse max.")]
    [SerializeField] private float decelerationTime = 0.1f;

    [Header("Rotation")]
    [Tooltip("Vitesse à laquelle le personnage s'oriente vers la direction du déplacement (en degrés/seconde).")]
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Gravité et saut")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;
    [Tooltip("Petite valeur de gravité appliquée en continu au sol pour garder le CharacterController 'collé' au sol.")]
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController controller;
    private Vector3 currentVelocity;      // vitesse horizontale actuelle (lissée)
    private float verticalVelocity;       // vitesse verticale (gravité / saut)
    private float currentSpeedRef;        // référence interne pour SmoothDamp

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Si aucune caméra n'est assignée dans l'Inspector, on prend la caméra principale par défaut.
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleGravityAndJump();
    }

    private void HandleMovement()
    {
        // Lecture des axes : couvre flèches directionnelles, WASD/ZQSD (selon disposition clavier) et manette (stick gauche).
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(inputX, 0f, inputZ);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f); // évite une vitesse diagonale plus rapide

        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetAxisRaw("Fire3") != 0f; // Fire3 = souvent bouton "stick press" ou bumper selon config manette
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        // Oriente l'input par rapport à la direction de la caméra (déplacement relatif caméra, standard en TPS).
        Vector3 moveDirection = Vector3.zero;
        if (inputDirection.sqrMagnitude > 0.001f && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * inputDirection.z + camRight * inputDirection.x).normalized;
        }

        Vector3 targetVelocity = moveDirection * targetSpeed;

        // Lissage réaliste : accélération et décélération progressives plutôt qu'un mouvement instantané.
        float smoothTime = (targetVelocity.magnitude > currentVelocity.magnitude) ? accelerationTime : decelerationTime;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(smoothTime, 0.0001f)));

        // Rotation fluide du personnage vers la direction du déplacement.
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleGravityAndJump()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            // Petite valeur négative constante pour garder le personnage "collé" au sol
            // (évite les micro-chutes qui déclenchent isGrounded = false à chaque frame).
            verticalVelocity = groundedGravity;
        }

        // Saut : touche Espace au clavier, ou bouton Sud (A/Croix) sur la plupart des manettes via "Jump" (à configurer dans Input Manager si besoin).
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}