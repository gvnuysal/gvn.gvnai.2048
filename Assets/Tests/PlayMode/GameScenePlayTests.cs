using System.Collections;
using System.Linq;
using Game2048.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game2048.Tests
{
    /// <summary>Loads Game.unity and drives it with virtual keyboard/mouse input.</summary>
    public class GameScenePlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameManager _manager;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsScoreStore.BestKey);
            base.TearDown();
        }

        private IEnumerator LoadGame()
        {
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            _manager = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_manager, "GameManager missing in scene");
        }

        private static int VisibleTiles() =>
            Object.FindObjectsByType<TileView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

        private static int NonZero(BoardModel board) => board.ToArray().Cast<int>().Count(v => v != 0);

        private static IEnumerator WaitAnimations() => new WaitForSecondsRealtime(0.5f);

        private static GameObject Overlay() =>
            Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "ResultOverlay").gameObject;

        [UnityTest]
        public IEnumerator Start_ShowsTwoTiles_ScoreZero()
        {
            yield return LoadGame();
            yield return WaitAnimations();

            Assert.AreEqual(2, NonZero(_manager.Session.Board));
            Assert.AreEqual(2, VisibleTiles());
            Assert.AreEqual(0, _manager.Session.Score);
            Assert.IsFalse(Overlay().activeSelf);
        }

        [UnityTest]
        public IEnumerator ArrowKey_MergesAndSpawns_ViewMatchesModel()
        {
            yield return LoadGame();
            var cells = new int[4, 4];
            cells[0, 0] = 2;
            cells[0, 2] = 2;
            _manager.LoadPosition(cells);

            Press(_keyboard.leftArrowKey);
            yield return null;
            Release(_keyboard.leftArrowKey);
            yield return WaitAnimations();

            Assert.AreEqual(4, _manager.Session.Score);
            Assert.AreEqual(4, _manager.Session.Board[0, 0]);
            Assert.AreEqual(2, NonZero(_manager.Session.Board));
            Assert.AreEqual(2, VisibleTiles());
        }

        [UnityTest]
        public IEnumerator WasdKey_Moves()
        {
            yield return LoadGame();
            var cells = new int[4, 4];
            cells[3, 3] = 8;
            _manager.LoadPosition(cells);

            Press(_keyboard.wKey);
            yield return null;
            Release(_keyboard.wKey);
            yield return WaitAnimations();

            Assert.AreEqual(8, _manager.Session.Board[0, 3]);
        }

        [UnityTest]
        public IEnumerator MouseSwipeRight_Moves()
        {
            yield return LoadGame();
            var cells = new int[4, 4];
            cells[1, 0] = 16;
            _manager.LoadPosition(cells);

            Set(_mouse.position, new Vector2(200f, 400f));
            yield return null;
            Press(_mouse.leftButton);
            yield return null;
            Set(_mouse.position, new Vector2(450f, 410f));
            yield return null;
            Release(_mouse.leftButton);
            yield return WaitAnimations();

            Assert.AreEqual(16, _manager.Session.Board[1, 3]);
            Assert.AreEqual(2, VisibleTiles());
        }

        [UnityTest]
        public IEnumerator GameOver_ShowsOverlay_BlocksInput_RestartResets()
        {
            yield return LoadGame();
            _manager.LoadPosition(new[,]
            {
                { 2, 4, 2, 4 },
                { 4, 2, 4, 2 },
                { 2, 4, 2, 4 },
                { 8, 16, 4, 4 }
            }, score: 50);

            Press(_keyboard.leftArrowKey);
            yield return null;
            Release(_keyboard.leftArrowKey);
            yield return WaitAnimations();

            Assert.AreEqual(GameState.Lost, _manager.Session.State);
            var overlay = Overlay();
            Assert.IsTrue(overlay.activeSelf);
            Assert.AreEqual("Game Over", overlay.GetComponentInChildren<TMP_Text>().text);

            int score = _manager.Session.Score;
            Press(_keyboard.upArrowKey);
            yield return null;
            Release(_keyboard.upArrowKey);
            yield return null;
            Assert.AreEqual(score, _manager.Session.Score);

            _manager.Restart();
            yield return WaitAnimations();
            Assert.IsFalse(overlay.activeSelf);
            Assert.AreEqual(0, _manager.Session.Score);
            Assert.AreEqual(2, VisibleTiles());
        }

        [UnityTest]
        public IEnumerator Win_ShowsOverlay_ContinueHidesIt()
        {
            yield return LoadGame();
            var cells = new int[4, 4];
            cells[0, 0] = 1024;
            cells[0, 1] = 1024;
            _manager.LoadPosition(cells);

            Press(_keyboard.leftArrowKey);
            yield return null;
            Release(_keyboard.leftArrowKey);
            yield return WaitAnimations();

            var overlay = Overlay();
            Assert.AreEqual(GameState.Won, _manager.Session.State);
            Assert.IsTrue(overlay.activeSelf);
            Assert.AreEqual("You Win!", overlay.GetComponentInChildren<TMP_Text>().text);

            _manager.Continue();
            yield return null;
            Assert.IsFalse(overlay.activeSelf);
            Assert.AreEqual(GameState.PlayingAfterWin, _manager.Session.State);
        }

        [UnityTest]
        public IEnumerator Undo_RestoresPreviousBoard()
        {
            yield return LoadGame();
            var cells = new int[4, 4];
            cells[2, 1] = 4;
            cells[2, 2] = 4;
            _manager.LoadPosition(cells);

            Press(_keyboard.rightArrowKey);
            yield return null;
            Release(_keyboard.rightArrowKey);
            yield return WaitAnimations();
            Assert.AreEqual(8, _manager.Session.Score);

            _manager.Undo();
            yield return null;
            Assert.AreEqual(0, _manager.Session.Score);
            CollectionAssert.AreEqual(cells, _manager.Session.Board.ToArray());
            Assert.AreEqual(2, VisibleTiles());
        }

        [UnityTest]
        public IEnumerator RapidInput_DuringAnimation_KeepsViewInSync()
        {
            yield return LoadGame();
            var cells = new int[4, 4];
            cells[0, 0] = 2;
            cells[0, 1] = 2;
            cells[1, 0] = 4;
            _manager.LoadPosition(cells);

            foreach (var key in new[] { _keyboard.rightArrowKey, _keyboard.downArrowKey, _keyboard.leftArrowKey, _keyboard.upArrowKey })
            {
                Press(key);
                yield return null;
                Release(key);
                yield return null;
            }
            yield return WaitAnimations();

            Assert.AreEqual(NonZero(_manager.Session.Board), VisibleTiles());
        }
    }
}
