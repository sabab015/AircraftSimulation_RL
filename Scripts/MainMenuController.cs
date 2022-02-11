using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;


namespace Aircraft
{
    public class MainMenuController : MonoBehaviour
    {
        public List<string> levels; //list of scenes name that can be loaded
        public TMP_Dropdown levelDropdown; //Dropdown for selecting level
        public TMP_Dropdown difficultyDropdown; //Dropdown for selecting game difficulty

        private string selectedLevel;
        private GameDifficulty selectedDifficulty;

        /// <summary>
        /// Automatically fill down the dropdown lists
        /// </summary>
        private void Start()
        {
            Debug.Assert(levels.Count > 0, "No levels available");
            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(levels);
            selectedLevel = levels[0];

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(Enum.GetNames(typeof(GameDifficulty)).ToList());
            selectedDifficulty = GameDifficulty.Normal;
        }
        public void SetLevel(int levelIndex)
        {
            selectedLevel = levels[levelIndex];
        }

        public void SetDifficulty(int difficultyIndex)
        {
            selectedDifficulty = (GameDifficulty)difficultyIndex;
        }

        /// <summary>
        /// Start The chosen level
        /// </summary>
        public void StartButtonClicked()
        {
            GameManager.Instance.GameDifficulty = selectedDifficulty; //set game difficulty

            GameManager.Instance.LoadLevel(selectedLevel, GameState.Preparing); //Load the level in preparing mode
        }


        /// <summary>
        /// Quit the game
        /// </summary>
        public void QuitButtonClicked()
        {

            Application.Quit();
        }
    }

}
