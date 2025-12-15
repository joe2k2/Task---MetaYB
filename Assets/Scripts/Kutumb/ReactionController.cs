using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReactionController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Reaction Buttons")]
    [SerializeField] private Button playReactionButton;
    [SerializeField] private Button playDialogueButton;

    [Header("Reaction Settings")]
    [SerializeField] private string smileTriggerName;
    [SerializeField] private string sadTriggerName;

    [Header("Dialogue Playback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogueAudioClip;
    [SerializeField] private string idleStateName = "Idle";

    private bool isPlaying = false;

    private void Start()
    {
        playReactionButton.onClick.AddListener(PlayReactionSequence);
        playDialogueButton.onClick.AddListener(PlayRecordedDialogue);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetButtonsInteractable(true);
    }

    public void PlayReactionSequence()
    {
        if (isPlaying)
            return;

        StartCoroutine(ReactionSequence());
    }

    public void PlayRecordedDialogue()
    {
        if (isPlaying)
        {
            Debug.LogWarning("Already playing dialogue!");
            return;
        }

        if (dialogueAudioClip == null)
        {
            Debug.LogError("Dialogue audio clip not assigned!");
            return;
        }

        StartCoroutine(PlayDialogueSequence());
    }

    private IEnumerator PlayDialogueSequence()
    {
        isPlaying = true;
        SetButtonsInteractable(false);

        Debug.Log($"<color=cyan>Playing dialogue: {dialogueAudioClip.name}</color>");

        animator.SetBool("IsTalking", true);
        animator.Play(idleStateName, 0);

        audioSource.PlayOneShot(dialogueAudioClip);

        yield return new WaitForSeconds(dialogueAudioClip.length);

        animator.SetBool("IsTalking", false);

        isPlaying = false;
        SetButtonsInteractable(true);

        Debug.Log("<color=green>✓ Dialogue playback complete!</color>");
    }

    private IEnumerator ReactionSequence()
    {
        isPlaying = true;
        SetButtonsInteractable(false);

        animator.SetTrigger(smileTriggerName);
        yield return WaitForAnimation("Smile");

        animator.SetTrigger(sadTriggerName);
        yield return WaitForAnimation("Sad");

        animator.SetTrigger(smileTriggerName);
        yield return WaitForAnimation("Smile");

        animator.SetTrigger(sadTriggerName);
        yield return WaitForAnimation("Sad");

        animator.Play(idleStateName, 0);

        yield return new WaitForSeconds(0.2f);

        isPlaying = false;
        SetButtonsInteractable(true);

        Debug.Log("<color=green>✓ Reaction sequence complete! Returned to Idle.</color>");
    }

    private IEnumerator WaitForAnimation(string stateName)
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            yield return null;
        }

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (playReactionButton != null)
        {
            playReactionButton.interactable = interactable;
        }

        if (playDialogueButton != null)
        {
            playDialogueButton.interactable = interactable;
        }
    }
}
