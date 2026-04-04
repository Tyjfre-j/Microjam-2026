using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class uwinscreen : MonoBehaviour

{

    public Animator clip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
                    // Wait a frame to ensure the animator has started
            
            float duration = clip.GetCurrentAnimatorStateInfo(0).length;


    }

    // Update is called once per frame
    void Update()
    {
        playclip();     
    }


    public IEnumerator playclip()
    {
        yield return null; 
            
        float duration = clip.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(duration);
        SceneManager.LoadScene("CutScene"); 
    }



}
