// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;
using UnityEngine.UI;

namespace SpaceInvaders
{
    /// <summary>
    /// Manages the in-game UI elements (start screen, score, health).
    /// Attach to the Canvas GameObject so it can locate child Text components.
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        private Text _startText;
        private Text _scoreText;
        private Text _healthText;

        private void Awake()
        {
            FindUIElements();
        }

        private void FindUIElements()
        {
            var startGo = GameObject.Find("StartText");
            if (startGo != null) _startText = startGo.GetComponent<Text>();

            var scoreGo = GameObject.Find("ScoreText");
            if (scoreGo != null) _scoreText = scoreGo.GetComponent<Text>();

            var healthGo = GameObject.Find("HealthText");
            if (healthGo != null) _healthText = healthGo.GetComponent<Text>();
        }

        public void ShowStartScreen()
        {
            if (_startText != null) _startText.gameObject.SetActive(true);
            if (_scoreText != null) _scoreText.gameObject.SetActive(false);
            if (_healthText != null) _healthText.gameObject.SetActive(false);
        }

        public void HideStartScreen()
        {
            if (_startText != null) _startText.gameObject.SetActive(false);
            if (_scoreText != null)
            {
                _scoreText.gameObject.SetActive(true);
            }
            if (_healthText != null)
            {
                _healthText.gameObject.SetActive(true);
            }
        }

        public void UpdateScore(int score)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
        }

        public void UpdateHealth(int health)
        {
            if (_healthText != null)
                _healthText.text = $"HP: {health}";
        }
    }
}
