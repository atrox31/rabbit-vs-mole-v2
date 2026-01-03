using RabbitVsMole.GameData;

namespace RabbitVsMole
{
    /// <summary>
    /// Utility class for evaluating win conditions based on game mode and player stats.
    /// Pure logic with no state - all methods are static.
    /// </summary>
    public static class WinConditionEvaluator
    {
        public enum Winner
        {
            None,
            Rabbit,
            Mole,
            Both
        }

        public struct WinResult
        {
            public Winner winner;

            public WinResult(Winner winner)
            {
                this.winner = winner;
            }
        }

        public static WinResult GetWinner(PlayerType playerType) =>
            playerType switch
            {
                PlayerType.Rabbit => new WinResult(Winner.Rabbit),
                PlayerType.Mole => new WinResult(Winner.Mole),
                _ => throw new System.NotImplementedException(),
            };

        /// <summary>
        /// Evaluates winner when time runs out based on game mode and carrot counts.
        /// </summary>
        public static WinResult EvaluateByTime(GameModeData gameMode, int rabbitCarrots, int moleCarrots)
        {
            if (gameMode == null)
            {
                DebugHelper.LogWarning(null, "WinConditionEvaluator.EvaluateByTime: gameMode is null");
                return new WinResult(Winner.None);
            }

            return gameMode.winCondition switch
            {
                GameModeWinCondition.TimeLimit => new WinResult(GetLeader(rabbitCarrots, moleCarrots)),
                GameModeWinCondition.Rivalry => new WinResult(Winner.Mole),
                _ => new WinResult(Winner.None)
            };
        }

        /// <summary>
        /// Evaluates winner when carrot goal is reached based on game mode and carrot counts.
        /// </summary>
        public static WinResult EvaluateByGoal(GameModeData gameMode, int rabbitCarrots, int moleCarrots)
        {
            if (gameMode == null)
            {
                DebugHelper.LogWarning(null, "WinConditionEvaluator.EvaluateByGoal: gameMode is null");
                return new WinResult(Winner.None);
            }

            return gameMode.winCondition switch
            {
                GameModeWinCondition.CarrotCollection => new WinResult(GetLeader(rabbitCarrots, moleCarrots)),
                GameModeWinCondition.Rivalry => new WinResult(GetLeader(rabbitCarrots, moleCarrots)),
                GameModeWinCondition.Cooperation => new WinResult(Winner.Both),
                _ => new WinResult(Winner.None)
            };
        }

        /// <summary>
        /// Determines which player is leading based on carrot count.
        /// </summary>
        private static Winner GetLeader(int rabbitCarrots, int moleCarrots)
        {
            int comparison = rabbitCarrots.CompareTo(moleCarrots);
            
            // comparison > 0 means Rabbit has more
            // comparison < 0 means Mole has more
            // comparison == 0 means equal
            return comparison > 0 ? Winner.Rabbit : (comparison < 0 ? Winner.Mole : Winner.Both);
        }
    }
}
