using System;
using Game2048.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game2048
{
    /// <summary>Top bar (score, best, undo, restart) and the result overlay.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button undoButton;

        [Header("Result overlay")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private Image overlayBackground;
        [SerializeField] private TMP_Text overlayTitle;
        [SerializeField] private Button overlayContinueButton;
        [SerializeField] private Button overlayRestartButton;

        public event Action RestartClicked;
        public event Action UndoClicked;
        public event Action ContinueClicked;

        private void Awake()
        {
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            overlayRestartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            undoButton.onClick.AddListener(() => UndoClicked?.Invoke());
            overlayContinueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
        }

        public void Refresh(GameSession session)
        {
            scoreText.text = session.Score.ToString();
            bestText.text = session.Best.ToString();
            undoButton.interactable = session.CanUndo;

            switch (session.State)
            {
                case GameState.Won:
                    ShowOverlay("You Win!", TileColors.OverlayWon, TileColors.LightText, canContinue: true);
                    break;
                case GameState.Lost:
                    ShowOverlay("Game Over", TileColors.OverlayLost, TileColors.DarkText, canContinue: false);
                    break;
                default:
                    overlay.SetActive(false);
                    break;
            }
        }

        private void ShowOverlay(string title, Color background, Color textColor, bool canContinue)
        {
            overlayTitle.text = title;
            overlayTitle.color = textColor;
            overlayBackground.color = background;
            overlayContinueButton.gameObject.SetActive(canContinue);
            overlay.SetActive(true);
        }
    }
}
