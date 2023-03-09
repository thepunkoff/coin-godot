namespace Coin.Scripts;

public class RoundResult
{
    public int MyScore { get; set; }

    public int EnemyScore { get; set; }

    public bool IWonTheRound { get; set; }

    public bool EnemyWonTheRound { get; set; }

    public bool? IWonTheGame { get; set; }
}