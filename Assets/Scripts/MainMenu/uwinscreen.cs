using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class uwinscreen : MonoBehaviour

{

    public Animator clip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string menuSceneName = "MainMenu";
    [Tooltip("Return to the menu automatically after the clip. Off when the WinScreenUI overlay offers buttons.")]
    public bool autoReturn = true;

    public float ClipLength => clip != null ? clip.GetCurrentAnimatorStateInfo(0).length : 3f;

    void Start()
    {
        StartCoroutine(playclip());
    }

    public IEnumerator playclip()
    {
        // Wait a frame so the animator has entered its first state
        yield return null;

        float duration = clip != null ? clip.GetCurrentAnimatorStateInfo(0).length : 3f;
        yield return new WaitForSeconds(duration);
        if (autoReturn) SceneManager.LoadScene(menuSceneName);
    }



}
