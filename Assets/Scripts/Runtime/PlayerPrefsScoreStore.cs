using Game2048.Core;
using UnityEngine;

namespace Game2048
{
    public sealed class PlayerPrefsScoreStore : IScoreStore
    {
        public const string BestKey = "game2048.best";

        public int LoadBest() => PlayerPrefs.GetInt(BestKey, 0);

        public void SaveBest(int best)
        {
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }
    }
}
