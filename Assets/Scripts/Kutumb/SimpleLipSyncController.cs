using System.Collections;
using UnityEngine;

public class SimpleLipSyncController : MonoBehaviour
{
    [Header("Mesh References")]
    [SerializeField] private SkinnedMeshRenderer headMesh;
    [SerializeField] private Transform jawBone;

    [Header("Blend Shape Indices - Find these in CC_Base_Body")]
    [SerializeField] private int jawOpenBlendShapeIndex = 116;
    [SerializeField] private int mouthOpenBlendShapeIndex = 114;
    [SerializeField] private int lipPuckerBlendShapeIndex = 3;

    [Header("Lip Sync Settings")]
    [SerializeField] private float talkingSpeed = 15f;
    [SerializeField] private float jawOpenAmount = 30f;
    [SerializeField] private float mouthMovementAmount = 25f;
    [SerializeField] private AnimationCurve talkingPattern = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogueClip;

    private bool isTalking = false;
    private Coroutine lipSyncCoroutine;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (headMesh == null)
        {
            headMesh = transform.Find("CC_Base_Body")?.GetComponent<SkinnedMeshRenderer>();
        }

        if (jawBone == null)
        {
            Transform boneRoot = transform.Find("CC_Base_BoneRoot");
            if (boneRoot != null)
            {
                jawBone = boneRoot.Find("CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head/CC_Base_FacialBone/CC_Base_JawRoot");
            }
        }
    }

    public void PlayDialogue()
    {
        if (dialogueClip == null)
        {
            Debug.LogError("No dialogue clip assigned!");
            return;
        }

        StartTalking(dialogueClip);
    }

    public void PlayDialogue(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("No audio clip provided!");
            return;
        }

        StartTalking(clip);
    }

    private void StartTalking(AudioClip clip)
    {
        if (isTalking)
        {
            StopTalking();
        }

        audioSource.clip = clip;
        audioSource.Play();

        lipSyncCoroutine = StartCoroutine(LipSyncRoutine(clip.length));
    }

    public void StopTalking()
    {
        if (lipSyncCoroutine != null)
        {
            StopCoroutine(lipSyncCoroutine);
            lipSyncCoroutine = null;
        }

        isTalking = false;
        ResetMouth();

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator LipSyncRoutine(float duration)
    {
        isTalking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float normalizedTime = elapsed / duration;
            float talkCycle = Mathf.Sin(elapsed * talkingSpeed);
            float curveValue = talkingPattern.Evaluate(talkCycle);

            float jawValue = Mathf.Abs(curveValue) * jawOpenAmount;
            float mouthValue = Mathf.Abs(curveValue) * mouthMovementAmount;

            if (headMesh != null)
            {
                if (jawOpenBlendShapeIndex >= 0)
                    headMesh.SetBlendShapeWeight(jawOpenBlendShapeIndex, jawValue);

                if (mouthOpenBlendShapeIndex >= 0)
                    headMesh.SetBlendShapeWeight(mouthOpenBlendShapeIndex, mouthValue);
            }

            if (jawBone != null)
            {
                float jawRotation = -jawValue * 0.3f;
                jawBone.localRotation = Quaternion.Euler(jawRotation, 0f, 0f);
            }

            yield return null;
        }

        ResetMouth();
        isTalking = false;
    }

    private void ResetMouth()
    {
        if (headMesh != null)
        {
            if (jawOpenBlendShapeIndex >= 0)
                headMesh.SetBlendShapeWeight(jawOpenBlendShapeIndex, 0f);

            if (mouthOpenBlendShapeIndex >= 0)
                headMesh.SetBlendShapeWeight(mouthOpenBlendShapeIndex, 0f);

            if (lipPuckerBlendShapeIndex >= 0)
                headMesh.SetBlendShapeWeight(lipPuckerBlendShapeIndex, 0f);
        }

        if (jawBone != null)
        {
            jawBone.localRotation = Quaternion.identity;
        }
    }
}
