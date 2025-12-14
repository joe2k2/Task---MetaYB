using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Features.LipSync.Visemes;
using Convai.Scripts.Runtime.Features.LipSync.Models;

[System.Serializable]
public class VisemeData
{
    public float sil;
    public float pp;
    public float ff;
    public float th;
    public float dd;
    public float kk;
    public float ch;
    public float ss;
    public float nn;
    public float rr;
    public float aa;
    public float e;
    public float ih;
    public float oh;
    public float ou;
}

public class AudioDrivenLipSync : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private SkinnedMeshRenderer headMesh;
    [SerializeField] private Transform jawBone;
    [SerializeField] private Transform tongueBone;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clip")]
    [SerializeField] private AudioClip dialogueClip;

    [Header("Convai Viseme Assets")]
    [SerializeField] private VisemeEffectorsList visemeEffectorsList;
    [SerializeField] private VisemeBoneEffectorList jawBoneEffectors;
    [SerializeField] private VisemeBoneEffectorList tongueBoneEffectors;

    [Header("Lip Sync Settings")]
    [SerializeField] private float sensitivity = 100f;
    [SerializeField] private float weightBlendingPower = 0.5f;
    [SerializeField] private float weightMultiplier = 100f;
    [SerializeField] private int sampleSize = 256;
    [SerializeField] private float updateRate = 0.01f;

    private float[] audioSamples;
    private bool isTalking = false;
    private Coroutine lipSyncCoroutine;
    private VisemeData currentViseme;

    private void Awake()
    {
        InitializeReferences();
        audioSamples = new float[sampleSize];
        currentViseme = new VisemeData();
    }

    private void InitializeReferences()
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
            jawBone = FindInChildren(transform, "CC_Base_JawRoot");
        }

        if (tongueBone == null)
        {
            tongueBone = FindInChildren(transform, "CC_Base_Tongue01");
        }
    }

    private Transform FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }
        return null;
    }

    [ContextMenu("Play Dialogue")]
    public void PlayDialogue()
    {
        if (dialogueClip != null)
        {
            PlayDialogue(dialogueClip);
        }
        else
        {
            Debug.LogError("No dialogue clip assigned!");
        }
    }

    public void PlayDialogue(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Audio clip is null!");
            return;
        }

        if (visemeEffectorsList == null || jawBoneEffectors == null || tongueBoneEffectors == null)
        {
            Debug.LogError("Viseme assets not assigned! Please assign the Convai ScriptableObjects.");
            return;
        }

        StopDialogue();

        audioSource.clip = clip;
        audioSource.Play();
        isTalking = true;

        lipSyncCoroutine = StartCoroutine(LipSyncRoutine(clip.length));
    }

    [ContextMenu("Stop Dialogue")]
    public void StopDialogue()
    {
        if (lipSyncCoroutine != null)
        {
            StopCoroutine(lipSyncCoroutine);
            lipSyncCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isTalking = false;
        ResetMouth();
    }

    private IEnumerator LipSyncRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && audioSource.isPlaying)
        {
            audioSource.GetOutputData(audioSamples, 0);

            float totalAmplitude = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                totalAmplitude += Mathf.Abs(audioSamples[i]);
            }

            float averageAmplitude = totalAmplitude / audioSamples.Length;
            float intensity = Mathf.Clamp01(averageAmplitude * sensitivity);

            currentViseme = GenerateVisemeFromAudio(intensity);

            UpdateBlendShapes();
            UpdateBones();

            elapsed += updateRate;
            yield return new WaitForSeconds(updateRate);
        }

        yield return new WaitForSeconds(0.2f);
        StopDialogue();
    }

    private VisemeData GenerateVisemeFromAudio(float intensity)
    {
        VisemeData viseme = new VisemeData
        {
            sil = intensity < 0.05f ? 1f : 0f,
            pp = intensity > 0.05f ? intensity * 0.3f : 0f,
            ff = intensity > 0.1f ? intensity * 0.4f : 0f,
            th = intensity > 0.15f ? intensity * 0.5f : 0f,
            dd = intensity > 0.1f ? intensity * 0.6f : 0f,
            kk = intensity > 0.2f ? intensity * 0.7f : 0f,
            ch = intensity > 0.2f ? intensity * 0.6f : 0f,
            ss = intensity > 0.15f ? intensity * 0.5f : 0f,
            nn = intensity > 0.15f ? intensity * 0.6f : 0f,
            rr = intensity > 0.1f ? intensity * 0.4f : 0f,
            aa = intensity > 0.3f ? intensity * 1.2f : 0f,
            e = intensity > 0.2f ? intensity * 0.8f : 0f,
            ih = intensity > 0.15f ? intensity * 0.7f : 0f,
            oh = intensity > 0.25f ? intensity * 1.0f : 0f,
            ou = intensity > 0.3f ? intensity * 0.9f : 0f
        };

        return viseme;
    }

    private void UpdateBlendShapes()
    {
        if (headMesh == null || currentViseme == null || visemeEffectorsList == null) return;

        Dictionary<int, float> finalWeights = new Dictionary<int, float>();

        ApplyVisemeToBlendShapes(visemeEffectorsList.sil, currentViseme.sil, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.pp, currentViseme.pp, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.ff, currentViseme.ff, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.th, currentViseme.th, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.dd, currentViseme.dd, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.kk, currentViseme.kk, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.ch, currentViseme.ch, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.ss, currentViseme.ss, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.nn, currentViseme.nn, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.rr, currentViseme.rr, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.aa, currentViseme.aa, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.e, currentViseme.e, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.ih, currentViseme.ih, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.oh, currentViseme.oh, ref finalWeights);
        ApplyVisemeToBlendShapes(visemeEffectorsList.ou, currentViseme.ou, ref finalWeights);

        foreach (var kvp in finalWeights)
        {
            int index = kvp.Key;
            float targetWeight = kvp.Value * weightMultiplier;

            if (index >= 0 && index < headMesh.sharedMesh.blendShapeCount)
            {
                float currentWeight = headMesh.GetBlendShapeWeight(index);
                float newWeight = Mathf.Lerp(currentWeight, targetWeight, weightBlendingPower);
                headMesh.SetBlendShapeWeight(index, newWeight);
            }
        }
    }

    private void ApplyVisemeToBlendShapes(List<BlendShapesIndexEffector> blendShapes, float visemeValue, ref Dictionary<int, float> weights)
    {
        if (blendShapes == null) return;

        foreach (var bs in blendShapes)
        {
            if (weights.ContainsKey(bs.index))
            {
                weights[bs.index] += visemeValue * bs.effectPercentage;
            }
            else
            {
                weights[bs.index] = visemeValue * bs.effectPercentage;
            }
        }
    }

    private void UpdateBones()
    {
        if (currentViseme == null) return;

        if (jawBone != null && jawBoneEffectors != null)
        {
            float jawEffect = CalculateBoneEffect(jawBoneEffectors);
            float jawRotation = -90f - (jawEffect * 30f);
            jawBone.localEulerAngles = new Vector3(0f, 0f, jawRotation);
        }

        if (tongueBone != null && tongueBoneEffectors != null)
        {
            float tongueEffect = CalculateBoneEffect(tongueBoneEffectors);
            float tongueRotation = (tongueEffect * 80f) - 5f;
            tongueBone.localEulerAngles = new Vector3(0f, 0f, tongueRotation);
        }
    }

    private float CalculateBoneEffect(VisemeBoneEffectorList effectors)
    {
        if (effectors == null || effectors.Total == 0f) return 0f;

        float effect =
            effectors.sil * currentViseme.sil +
            effectors.pp * currentViseme.pp +
            effectors.ff * currentViseme.ff +
            effectors.th * currentViseme.th +
            effectors.dd * currentViseme.dd +
            effectors.kk * currentViseme.kk +
            effectors.ch * currentViseme.ch +
            effectors.ss * currentViseme.ss +
            effectors.nn * currentViseme.nn +
            effectors.rr * currentViseme.rr +
            effectors.aa * currentViseme.aa +
            effectors.e * currentViseme.e +
            effectors.ih * currentViseme.ih +
            effectors.oh * currentViseme.oh +
            effectors.ou * currentViseme.ou;

        return effect / effectors.Total;
    }

    private void ResetMouth()
    {
        currentViseme = new VisemeData();

        if (headMesh != null)
        {
            for (int i = 0; i < headMesh.sharedMesh.blendShapeCount; i++)
            {
                headMesh.SetBlendShapeWeight(i, 0f);
            }
        }

        if (jawBone != null)
        {
            jawBone.localEulerAngles = new Vector3(0f, 0f, -90f);
        }

        if (tongueBone != null)
        {
            tongueBone.localEulerAngles = new Vector3(0f, 0f, -5f);
        }
    }

    public bool IsTalking()
    {
        return isTalking;
    }
}
