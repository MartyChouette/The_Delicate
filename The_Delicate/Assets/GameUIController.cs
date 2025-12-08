using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmotionBank
{
    public class GameUIController : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text centerText;
        public TMP_Text healthText;
        public GameObject gameOverPanel;
        public Button restartButton;
        public Button quitButton;

        // Helper to flash "BEGIN"
        private float _beginTimer = 0f;

        private void Start()
        {
            gameOverPanel.SetActive(false);
            restartButton.onClick.AddListener(() => GameSessionManager.Instance.RestartGameServerRpc());
            quitButton.onClick.AddListener(() => GameSessionManager.Instance.QuitGameServerRpc());
        }

        private void Update()
        {
            if (GameSessionManager.Instance == null) return;
            var gm = GameSessionManager.Instance;

            // Health UI
            if (gm.currentState.Value == GameState.Gameplay)
                healthText.text = $"Integrity: {Mathf.CeilToInt(gm.currentBoxHealth.Value)}%";
            else
                healthText.text = "";

            // State UI
            switch (gm.currentState.Value)
            {
                case GameState.Lobby:
                    centerText.text = "WAITING FOR PLAYERS...";
                    centerText.color = Color.white;
                    break;

                case GameState.CountdownToSelection:
                    centerText.text = Mathf.CeilToInt(gm.countdownTimer.Value).ToString();
                    centerText.color = Color.yellow;
                    break;

                case GameState.Selection:
                    centerText.text = "SELECTION";
                    centerText.color = Color.cyan;
                    break;

                case GameState.CountdownToGameplay:
                    centerText.text = Mathf.CeilToInt(gm.countdownTimer.Value).ToString();
                    centerText.color = Color.yellow;
                    break;

                case GameState.Gameplay:
                    // Show BEGIN for 2 seconds, then hide
                    if (_beginTimer < 2.0f)
                    {
                        _beginTimer += Time.deltaTime;
                        centerText.text = "BEGIN";
                        centerText.color = Color.green;
                    }
                    else
                    {
                        centerText.text = "";
                    }
                    break;

                case GameState.GameOver:
                    centerText.text = gm.didWin.Value ? "YOU WIN" : "FAILURE";
                    centerText.color = gm.didWin.Value ? Color.green : Color.red;
                    if (!gameOverPanel.activeSelf) gameOverPanel.SetActive(true);
                    break;
            }
        }
    }
}