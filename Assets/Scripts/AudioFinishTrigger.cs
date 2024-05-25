using UnityEngine;
using System.Collections;
public class AudioFinishTrigger : MonoBehaviour
{
    AudioSource audioSource;
    public Animator anim;
    public string triggerName = "prompt_done";

    void Awake() {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            Debug.LogError("Audio source not found");
        if (anim == null)
            Debug.LogError("Animator not found");
    }
    void OnEnable() {
        audioSource.Play();
        StartCoroutine(LateTrigger(audioSource.clip.length));
    }

    private IEnumerator LateTrigger(float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetTrigger(triggerName);
    }

}
