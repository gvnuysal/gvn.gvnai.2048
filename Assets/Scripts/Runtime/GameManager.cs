using Game2048.Core;
using UnityEngine;

namespace Game2048
{
    /// <summary>
    /// Wires input, rules and views: new game, valid moves, spawning, score and win/lose flow.
    /// The move is always resolved in <see cref="GameSession"/> first; views update afterwards.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameUI ui;
        [SerializeField] private InputHandler input;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            if (Application.isMobilePlatform) Application.targetFrameRate = 60;

            Session = new GameSession(new SystemRandom(), new PlayerPrefsScoreStore());
            input.DirectionPressed += OnDirection;
            ui.RestartClicked += Restart;
            ui.UndoClicked += Undo;
            ui.ContinueClicked += Continue;
        }

        private void Start()
        {
            boardView.Bind(Session.Board);
            Restart();
        }

        private void OnDestroy()
        {
            if (input != null) input.DirectionPressed -= OnDirection;
            if (ui == null) return;
            ui.RestartClicked -= Restart;
            ui.UndoClicked -= Undo;
            ui.ContinueClicked -= Continue;
        }

        private void OnDirection(Direction direction)
        {
            if (!Session.AcceptsInput) return;

            // Snap the previous animation before the model changes again.
            boardView.FinishAnimations();
            var turn = Session.TryMove(direction);
            if (turn == null) return;

            boardView.Animate(turn);
            ui.Refresh(Session);
        }

        public void Restart()
        {
            boardView.FinishAnimations();
            Session.NewGame();
            boardView.Render();
            ui.Refresh(Session);
        }

        public void Undo()
        {
            boardView.FinishAnimations();
            if (!Session.Undo()) return;
            boardView.Render();
            ui.Refresh(Session);
        }

        /// <summary>Loads a specific position (tests / debugging).</summary>
        public void LoadPosition(int[,] cells, int score = 0)
        {
            boardView.FinishAnimations();
            Session.Load(cells, score);
            boardView.Render();
            ui.Refresh(Session);
        }

        public void Continue()
        {
            if (!Session.Continue()) return;
            ui.Refresh(Session);
        }
    }
}
