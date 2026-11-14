using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameLogic : MonoBehaviour
{
    
    [SerializeField] private TMPro.TextMeshProUGUI scoreUI;
    [SerializeField] private TMPro.TextMeshProUGUI livesUI;
    
    [SerializeField] private GameObject asteroid;
    
    public static int score;
    public static int lives;
    private int bestAtStart;
    [SerializeField] private GameObject losingCanvas;
    [SerializeField] private GameObject escMenu;

    private void Start()
    {
        //SpawnAsteroids();
        bestAtStart = Highscores.Best;
        HullBar.Create(livesUI);
        SpaceBackground.Attach(Camera.main);
        DynamicPostFX.Attach();
        MenuExtras.Setup(this, escMenu, losingCanvas);
    }

    private void Update()
    {
        scoreUI.text = "Score: " + score + "   Best: " + Mathf.Max(bestAtStart, score);
        livesUI.text = "Lives: " + lives;

         if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || GetStartInput())
        {
            // Call a method to activate the menu
            ActivateMenu();
            AudioListener.volume = 0;
        }
    }

    public static void SetLivesAndScore(float hp)
    {
        lives = (int) hp;
        score = 0;
    }
    
    //private void SpawnAsteroids()
  //  {
    //    for (int i = 0; i < 5; i++)
      //  {
        //    var position = Camera.main.ViewportToWorldPoint(new Vector3(Random.Range(0f, 1f), 1, 0));
           // Instantiate(asteroid, position, Quaternion.identity);
        //}
    //}


    public static bool HandleLiveDecrease()
    {
        lives--;

        if (lives <= 0)
        {
            //SceneManager.LoadScene(3);
            return false;
        }

        return true;
    }
    
    public static bool HandleScoreIncrease()
    {
        score += 100;
        
        if (score == 50000)
        {
            Highscores.Submit(score); // victory: save the run
            SceneManager.LoadScene(2);
            return false;
        }
        
        return true;
    }
    public void Restart()
    {
        SceneFlow.RestartGame();
    }
    void ActivateMenu()
    {
        if (escMenu != null)
        {
            escMenu.SetActive(true);
            Time.timeScale = 0;
        }
        else
        {
            Debug.LogWarning("ESC_Menu not assigned in the inspector!");
        }
    }
    public void ResumeGame()
    {
        Time.timeScale = 1;
        AudioListener.volume = 1;
    }
    public void ExitGame()
    {
        SceneFlow.LoadMainMenu();
    }

    public void GoToMainMenu()
    {
        SceneFlow.LoadMainMenu();
    }

    public void QuitGame()
    {
        SceneFlow.QuitGame();
    }
     public static bool GetStartInput()
    {
        return TwinStickMovement.isUsingMenu;
    }

}
