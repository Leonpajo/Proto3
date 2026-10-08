using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// ONE of these in the scene (on the Canvas, next to DialogueUI).
/// The hand is a 3D model that is attached to the player's camera during the slap.
/// Call SlapHand.Instance.Play(...) to get slapped.
public class SlapHand : MonoBehaviour
{
    public static SlapHand Instance { get; private set; }
    public static bool IsPlaying { get; private set; }

    [Header("References")]
    [Tooltip("The 3D hand model. Can be a prefab from the Project window OR an object in the scene.")]
    public Transform hand;
    [Tooltip("Leave empty to use Camera.main.")]
    public Camera targetCamera;
    [Tooltip("Optional: full-screen red UI Image (alpha 0, Raycast Target OFF).")]
    public Image flash;
    public AudioSource audioSource;     // optional
    public AudioClip slapSound;         // optional

    [Header("Motion (LOCAL to the camera: X right, Y up, Z forward)")]
    public Vector3 startPos = new Vector3(1.5f, -0.3f, 0.8f);   // outside the view
    public Vector3 startEuler = new Vector3(0f, 0f, 0f);
    public Vector3 hitPos = new Vector3(0.1f, -0.05f, 0.6f);    // in front of your face
    public Vector3 hitEuler = new Vector3(0f, 0f, 0f);
    public float swingTime = 0.12f;     // small = fast = more painful
    public float holdTime = 0.15f;
    public float retreatTime = 0.35f;

    [Header("Impact")]
    public float flashAlpha = 0.5f;
    public float flashFadeTime = 0.4f;
    public bool shakeCamera = true;
    public float shakeStrength = 0.15f;
    public float shakeTime = 0.25f;

    Transform handInstance;
    Camera cam;
    Action pendingDone;
    Vector3 cameraRestPosition;
    bool shaking;
    bool ownsHand;
    Coroutine flashRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        IsPlaying = false;
        // If the hand is an object in the scene, hide it until the slap.
        if (hand != null && hand.gameObject.scene.IsValid()) hand.gameObject.SetActive(false);
        if (flash != null) { flash.raycastTarget = false; SetFlash(0f); }
    }

    /// onHit = called at the moment of impact. onDone = called when the hand has left.
    public void Play(Action onHit = null, Action onDone = null)
    {
        if (IsPlaying) return;
        if (!isActiveAndEnabled) { onDone?.Invoke(); return; }

        if (!Setup())
        {
            // Nothing to show, but don't break the dialogue flow.
            onHit?.Invoke();
            onDone?.Invoke();
            return;
        }
        pendingDone = onDone;
        IsPlaying = true;
        StartCoroutine(SlapRoutine(onHit));
    }

    bool Setup()
    {
        if (hand == null)
        {
            Debug.LogWarning("SlapHand: 'Hand' is not assigned.");
            return false;
        }

        cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("SlapHand: no camera found (tag your camera MainCamera or assign Target Camera).");
            return false;
        }

        if (handInstance == null)
        {
            // Prefab asset -> make a copy. Scene object -> use it directly.
            ownsHand = !hand.gameObject.scene.IsValid();
            handInstance = ownsHand ? Instantiate(hand) : hand;
        }
        if (handInstance.parent != cam.transform) handInstance.SetParent(cam.transform, false);
        return true;
    }

    IEnumerator SlapRoutine(Action onHit)
    {
        IsPlaying = true;
        handInstance.gameObject.SetActive(true);
        handInstance.localPosition = startPos;
        handInstance.localRotation = Quaternion.Euler(startEuler);

        // 1) Swing in (ease-in so it accelerates into the hit)
        for (float t = 0; t < swingTime; t += Time.unscaledDeltaTime)
        {
            float k = t / swingTime; k *= k;
            handInstance.localPosition = Vector3.LerpUnclamped(startPos, hitPos, k);
            handInstance.localRotation = Quaternion.Euler(Vector3.LerpUnclamped(startEuler, hitEuler, k));
            yield return null;
        }
        handInstance.localPosition = hitPos;
        handInstance.localRotation = Quaternion.Euler(hitEuler);

        // 2) IMPACT
        if (audioSource != null && slapSound != null) audioSource.PlayOneShot(slapSound);
        if (flash != null) flashRoutine = StartCoroutine(FlashRoutine());
        if (shakeCamera) StartCoroutine(ShakeRoutine(cam.transform));
        onHit?.Invoke();

        yield return new WaitForSecondsRealtime(Mathf.Max(0, holdTime));

        // 3) Retreat (ease-out)
        for (float t = 0; t < retreatTime; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / retreatTime, 2f);
            handInstance.localPosition = Vector3.Lerp(hitPos, startPos, k);
            handInstance.localRotation = Quaternion.Euler(Vector3.Lerp(hitEuler, startEuler, k));
            yield return null;
        }

        handInstance.gameObject.SetActive(false);
        // Wait for the camera and flash to settle before returning to gameplay.
        while (shaking) yield return null;
        if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; }
        SetFlash(0);
        IsPlaying = false;
        Action done = pendingDone;
        pendingDone = null;
        done?.Invoke();
    }

    IEnumerator FlashRoutine()
    {
        for (float t = 0; t < flashFadeTime; t += Time.unscaledDeltaTime)
        {
            SetFlash(Mathf.Lerp(flashAlpha, 0f, t / flashFadeTime));
            yield return null;
        }
        SetFlash(0f);
    }

    void SetFlash(float a)
    {
        if (flash == null) return;
        Color c = flash.color; c.a = a; flash.color = c;
    }

    IEnumerator ShakeRoutine(Transform camT)
    {
        cameraRestPosition = camT.localPosition;
        Vector3 original = cameraRestPosition;
        shaking = true;
        for (float t = 0; camT != null && t < shakeTime; t += Time.unscaledDeltaTime)
        {
            float fade = 1f - t / shakeTime;
            camT.localPosition = original + (Vector3)UnityEngine.Random.insideUnitCircle * shakeStrength * fade;
            yield return null;
        }
        if (camT != null) camT.localPosition = original;
        shaking = false;
    }

    void OnDisable()
    {
        if (Instance != this) return;
        StopAllCoroutines();
        if (shaking && cam != null) cam.transform.localPosition = cameraRestPosition;
        shaking = false;
        if (handInstance != null) handInstance.gameObject.SetActive(false);
        if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; }
        SetFlash(0);
        IsPlaying = false;
        Action done = pendingDone;
        pendingDone = null;
        done?.Invoke();
    }

    void OnDestroy()
    {
        if (ownsHand && handInstance != null) Destroy(handInstance.gameObject);
        if (Instance == this) { Instance = null; IsPlaying = false; }
    }
}