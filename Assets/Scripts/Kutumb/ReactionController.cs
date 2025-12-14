using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReactionController : MonoBehaviour
{
    public Animator animator;

    private bool isPlaying = false;

    [SerializeField] private Button playReactionButton;
    [SerializeField] private string smileTriggerName;
    [SerializeField] private string sadTriggerName;
    private void Start()
    {
        playReactionButton.onClick.AddListener(PlayReactionSequence);
    }
    public void PlayReactionSequence()
    {
        if (isPlaying)
            return;

        StartCoroutine(ReactionSequence());
    }

    private IEnumerator ReactionSequence()
    {
        isPlaying = true;

        animator.SetTrigger(smileTriggerName);
        yield return WaitForAnimation("Smile");

        animator.SetTrigger(sadTriggerName);
        yield return WaitForAnimation("Sad");

        animator.SetTrigger(smileTriggerName);
        yield return WaitForAnimation("Smile");

        animator.SetTrigger(sadTriggerName);
        yield return WaitForAnimation("Sad");

        isPlaying = false;
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
}
