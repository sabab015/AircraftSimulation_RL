using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Aircraft
{
    public class GameoverUIController : MonoBehaviour
    {
        public TextMeshProUGUI placeText; //Text to display finish place (e.g. 2nd place)

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void OnEnable()
        {
            if(GameManager.Instance != null &&
                GameManager.Instance.GameState == GameState.Gameover)
            {
                //Gets the place and update the text
                string place = raceManager.GetAgentPlace(raceManager.FollowAgent);
                this.placeText.text = place + "Place";
            }
        }
        /// <summary>
        /// Loads the MainMenu Scene
        /// </summary>
        public void MainMenuButtonClicked()
        {
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
        }

    }

}
