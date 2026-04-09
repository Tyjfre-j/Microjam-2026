using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Using New Input System

public class FaceSwitcher : MonoBehaviour
{
    [Header("Hierarchy Links")]
    public GameObject player;      
    public Transform cubeHolder;   
    public Transform spawnPoint;   
    public GameObject menuPanel;   
    public Image flashOverlay;     

    private bool isMenuOpen = false;

    void Update()
    {
        // OPEN/CLOSE with TAB Key
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }
    }

    public void OpenMenu()
    {
        isMenuOpen = true;
        menuPanel.SetActive(true);
        
        // Persona Style: Slow down time for a cool effect
        Time.timeScale = 0.1f; 
        
        // Unlock Mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        menuPanel.SetActive(false);
        Time.timeScale = 1.0f;

       
    }

    // --- YOUR HARD CODED BUTTONS ---
    public void Go_Normal() => StartCoroutine(SnapSequence(new Vector3(0, 0, 0)));
    public void Go_Right()  => StartCoroutine(SnapSequence(new Vector3(0, 90, 0)));
    public void Go_Back()   => StartCoroutine(SnapSequence(new Vector3(0, 180, 0)));
    public void Go_Left()   => StartCoroutine(SnapSequence(new Vector3(0, -90, 0)));
    public void Go_Up()     => StartCoroutine(SnapSequence(new Vector3(90, 180, 0)));
    public void Go_Down()   => StartCoroutine(SnapSequence(new Vector3(-90, 180, 0)));

    IEnumerator SnapSequence(Vector3 targetRotation)
    {
        if (player == null || cubeHolder == null || spawnPoint == null || flashOverlay == null) yield break;

        player.SetActive(false);
        flashOverlay.canvasRenderer.SetAlpha(1.0f);
        cubeHolder.eulerAngles = targetRotation;
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;
        player.transform.SetParent(null); 

        yield return new WaitForSecondsRealtime(0.1f); 

        player.SetActive(true);
        
        // Close menu and restore time after the snap
        CloseMenu(); 
        flashOverlay.CrossFadeAlpha(0, 0.3f, true);
    }
}