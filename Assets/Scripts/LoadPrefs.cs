using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

public class LoadPrefs : MonoBehaviour
{
    [Header("General Setting")]
    [SerializeField] private bool canUse = false;
    [SerializeField] private MenuController menuController;

    [Header("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private GameObject videoPlayerObject; 
    private VideoPlayer videoPlayer;





    private void Awake()
    {
        if (canUse)
        {
            // Not assigned in the scene: find it (a fresh install has no saved volume and needs it).
            if (menuController == null) menuController = FindAnyObjectByType<MenuController>();

            if(PlayerPrefs.HasKey("masterVolume"))
            {
                // Saved as 0..1 (AudioListener volume); the slider and label use 0..100.
                float localVolume = PlayerPrefs.GetFloat("masterVolume");
                volumeTextValue.text = Mathf.RoundToInt(localVolume * 100f).ToString();
                volumeSlider.value = localVolume * 100f;
                AudioListener.volume = localVolume;

                if (videoPlayerObject != null)
                {
                    videoPlayer = videoPlayerObject.GetComponent<VideoPlayer>();
                    if (videoPlayer != null) videoPlayer.SetDirectAudioVolume(0, localVolume);
                }
            }
            else if (menuController != null)
            {
                menuController.ResetButton("Audio");
            }
        }
        
    }

     private void LoadPlayerClass()
    {
        // Check if PlayerPrefs has the saved player class
        if (PlayerPrefs.HasKey("playerClass"))
        {
            // Retrieve the saved player class name
            string className = PlayerPrefs.GetString("playerClass");

            // Create an instance of the player class based on the class name
            Class loadedClass = CreateClassInstance(className);

            // Set the loaded player class in TwinStickMovement
            TwinStickMovement.setPlayerClass(loadedClass);
        }
    }

    private Class CreateClassInstance(string className)
    {
        // Add conditions to check for different class names and instantiate the corresponding class
        switch (className)
        {
            case "Wraith":
                return new Wraith();
            case "Assault":
                return new Assault();
            case "Tank":
                return new Tank();
            default:
                // Default to a specific class if the saved class name is not recognized
                return new Assault();
        }
    }
}
