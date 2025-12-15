#if UNITY_EDITOR
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Features.LipSync;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class ConvaiAudioRecorder : MonoBehaviour
{
    [Header("Convai Setup")]
    [SerializeField] private ConvaiNPC convaiNPC;
    [SerializeField] private ConvaiLipSync convaiLipSync;

    [Header("Dialogue to Generate")]
    [TextArea(3, 10)]
    [SerializeField] private string dialogueText = "Hello! I'm happy to see you today!";

    [Header("Output Settings")]
    [SerializeField] private string outputFileName = "Dialogue_Audio";
    [SerializeField] private string audioOutputFolder = "Assets/Audio/Dialogue/";
    [SerializeField] private string animationOutputFolder = "Assets/Animations/LipSync/";

    [Header("Multi-Part Recording Settings")]
    [SerializeField] private float silenceTimeoutSeconds = 2f;

    [Header("Recording Targets (Auto-detected from ConvaiLipSync)")]
    [SerializeField] private SkinnedMeshRenderer headMesh;
    [SerializeField] private SkinnedMeshRenderer teethMesh;
    [SerializeField] private SkinnedMeshRenderer tongueMesh;
    [SerializeField] private Transform jawBone;
    [SerializeField] private Transform tongueBone;

    [Header("Recording Info")]
    [SerializeField] private int blendShapesRecorded = 0;
    [SerializeField] private int bonesRecorded = 0;
    [SerializeField] private int audioChunksRecorded = 0;

    private List<AudioClip> recordedClips = new List<AudioClip>();
    private GameObjectRecorder animationRecorder;
    private bool isRecording = false;

    private void Awake()
    {
        if (convaiNPC == null)
        {
            convaiNPC = GetComponent<ConvaiNPC>();
        }

        if (convaiLipSync == null)
        {
            convaiLipSync = GetComponent<ConvaiLipSync>();
        }

        DetectTargetsFromConvaiLipSync();
    }

    private void DetectTargetsFromConvaiLipSync()
    {
        if (convaiLipSync == null)
        {
            Debug.LogWarning("ConvaiLipSync not found. Trying manual detection...");
            ManualDetection();
            return;
        }

        var facialData = convaiLipSync.FacialExpressionData;

        if (facialData.Head.Renderer != null)
        {
            headMesh = facialData.Head.Renderer;
        }

        if (facialData.Teeth.Renderer != null)
        {
            teethMesh = facialData.Teeth.Renderer;
        }

        if (facialData.Tongue.Renderer != null)
        {
            tongueMesh = facialData.Tongue.Renderer;
        }

        if (facialData.JawBone != null)
        {
            jawBone = facialData.JawBone.transform;
        }

        if (facialData.TongueBone != null)
        {
            tongueBone = facialData.TongueBone.transform;
        }
    }

    private void ManualDetection()
    {
        headMesh = transform.Find("CC_Base_Body")?.GetComponent<SkinnedMeshRenderer>();
        teethMesh = transform.Find("CC_Base_Teeth")?.GetComponent<SkinnedMeshRenderer>();
        tongueMesh = transform.Find("CC_Base_Tongue")?.GetComponent<SkinnedMeshRenderer>();
        jawBone = FindInChildren(transform, "CC_Base_JawRoot");
        tongueBone = FindInChildren(transform, "CC_Base_Tongue01");
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

    [ContextMenu("Generate and Save Audio + Lip Sync")]
    public void GenerateAndSaveAudio()
    {
        if (convaiNPC == null)
        {
            Debug.LogError("ConvaiNPC not assigned!");
            return;
        }

        if (string.IsNullOrEmpty(dialogueText))
        {
            Debug.LogError("Dialogue text is empty!");
            return;
        }

        StartCoroutine(RecordConvaiAudioAndLipSync());
    }

    private IEnumerator RecordConvaiAudioAndLipSync()
    {
        isRecording = true;
        blendShapesRecorded = 0;
        bonesRecorded = 0;
        audioChunksRecorded = 0;
        recordedClips.Clear();

        Debug.Log("<color=cyan>Starting multi-part audio + lip sync recording...</color>");

        AudioSource audioSource = convaiNPC.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = convaiNPC.gameObject.AddComponent<AudioSource>();
        }

        animationRecorder = new GameObjectRecorder(gameObject);

        if (headMesh != null)
        {
            animationRecorder.BindComponentsOfType<SkinnedMeshRenderer>(headMesh.gameObject, false);
            blendShapesRecorded += headMesh.sharedMesh.blendShapeCount;
        }

        if (teethMesh != null && teethMesh.sharedMesh.blendShapeCount > 0)
        {
            animationRecorder.BindComponentsOfType<SkinnedMeshRenderer>(teethMesh.gameObject, false);
            blendShapesRecorded += teethMesh.sharedMesh.blendShapeCount;
        }

        if (tongueMesh != null && tongueMesh.sharedMesh.blendShapeCount > 0)
        {
            animationRecorder.BindComponentsOfType<SkinnedMeshRenderer>(tongueMesh.gameObject, false);
            blendShapesRecorded += tongueMesh.sharedMesh.blendShapeCount;
        }

        if (jawBone != null)
        {
            animationRecorder.BindComponentsOfType<Transform>(jawBone.gameObject, false);
            bonesRecorded++;
        }

        if (tongueBone != null)
        {
            animationRecorder.BindComponentsOfType<Transform>(tongueBone.gameObject, false);
            bonesRecorded++;
        }

        Debug.Log($"<color=yellow>Recording: {blendShapesRecorded} blend shapes, {bonesRecorded} bones</color>");

        convaiNPC.SendTextDataAsync(dialogueText);

        yield return new WaitUntil(() => convaiNPC.IsCharacterTalking);
        yield return new WaitForSeconds(0.1f);

        Debug.Log("<color=green>Recording in progress (multi-part)...</color>");

        AudioClip currentClip = null;
        float silenceTimer = 0f;
        bool hasStartedTalking = false;

        while (true)
        {
            if (convaiNPC.IsCharacterTalking)
            {
                hasStartedTalking = true;
                silenceTimer = 0f;

                if (audioSource.clip != null && audioSource.clip != currentClip)
                {
                    currentClip = audioSource.clip;
                    recordedClips.Add(currentClip);
                    audioChunksRecorded++;
                    Debug.Log($"  → Captured audio chunk {audioChunksRecorded}: {currentClip.name}");
                }

                animationRecorder.TakeSnapshot(Time.deltaTime);
            }
            else if (hasStartedTalking)
            {
                silenceTimer += Time.deltaTime;

                if (silenceTimer >= silenceTimeoutSeconds)
                {
                    Debug.Log($"<color=yellow>Silence detected for {silenceTimeoutSeconds}s - stopping recording</color>");
                    break;
                }

                animationRecorder.TakeSnapshot(Time.deltaTime);
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        if (recordedClips.Count > 0)
        {
            AudioClip combinedClip = CombineAudioClips(recordedClips);
            SaveAudioClip(combinedClip, outputFileName);
            Debug.Log($"✓ Combined {audioChunksRecorded} audio chunks and saved to {audioOutputFolder}{outputFileName}.wav");
        }
        else
        {
            Debug.LogError("Failed to capture any audio clips!");
        }

        SaveAnimationClip(outputFileName);

        animationRecorder = null;
        isRecording = false;

        Debug.Log($"<color=green>✓ Recording complete! Full multi-part audio and lip sync saved.</color>");
    }

    private AudioClip CombineAudioClips(List<AudioClip> clips)
    {
        if (clips.Count == 1)
        {
            return clips[0];
        }

        int totalSamples = 0;
        int frequency = clips[0].frequency;
        int channels = clips[0].channels;

        foreach (AudioClip clip in clips)
        {
            totalSamples += clip.samples;
        }

        AudioClip combinedClip = AudioClip.Create("CombinedDialogue", totalSamples, channels, frequency, false);
        float[] combinedData = new float[totalSamples * channels];

        int offset = 0;
        foreach (AudioClip clip in clips)
        {
            float[] clipData = new float[clip.samples * channels];
            clip.GetData(clipData, 0);
            clipData.CopyTo(combinedData, offset);
            offset += clipData.Length;
        }

        combinedClip.SetData(combinedData, 0);
        return combinedClip;
    }

    private void SaveAudioClip(AudioClip clip, string fileName)
    {
        if (!Directory.Exists(audioOutputFolder))
        {
            Directory.CreateDirectory(audioOutputFolder);
        }

        string filepath = Path.Combine(audioOutputFolder, fileName + ".wav");
        SavWav.Save(filepath, clip);
        AssetDatabase.Refresh();
    }

    private void SaveAnimationClip(string fileName)
    {
        if (!Directory.Exists(animationOutputFolder))
        {
            Directory.CreateDirectory(animationOutputFolder);
        }

        string clipPath = animationOutputFolder + fileName + "_LipSync.anim";
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60;
        clip.legacy = false;

        animationRecorder.SaveToClip(clip);

        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"✓ Lip sync animation saved to: {clipPath}");
        Debug.Log($"  - {blendShapesRecorded} blend shapes animated");
        Debug.Log($"  - {bonesRecorded} bones animated");
        Debug.Log($"  - {audioChunksRecorded} audio chunks combined");
    }
}

public static class SavWav
{
    public static void Save(string filepath, AudioClip clip)
    {
        using (FileStream fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, clip);
            WriteHeader(fileStream, clip);
        }
    }

    private static FileStream CreateEmpty(string filepath)
    {
        FileStream fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < 44; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = System.BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = System.BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        ushort one = 1;
        byte[] audioFormat = System.BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        byte[] numChannels = System.BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        byte[] sampleRate = System.BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = System.BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        ushort blockAlign = (ushort)(channels * 2);
        fileStream.Write(System.BitConverter.GetBytes(blockAlign), 0, 2);

        ushort bps = 16;
        byte[] bitsPerSample = System.BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        byte[] subChunk2 = System.BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
#endif
