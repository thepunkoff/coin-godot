using System.Collections.Generic;

namespace Coin.Scripts;

public class TurnResult
{
    public bool Coin { get; }
    public IReadOnlyDictionary<CardType, int> MyCards {get; }
    public bool EnemyKnowsMyChoice {get; }
    public bool EnemyKnowsTheirChoice {get; }
    public bool? EnemyChoice {get; }
    public bool? MyChoice {get; }
    public CardEffectType? LastEnemyPlayedEffect {get; }

    public TurnResult(bool coin, IReadOnlyDictionary<CardType, int> myCards, bool enemyKnowsMyChoice, bool enemyKnowsTheirChoice, bool? enemyChoice, bool? myChoice, CardEffectType? lastEnemyPlayedEffect)
    {
        Coin = coin;
        MyCards = myCards;
        EnemyKnowsMyChoice = enemyKnowsMyChoice;
        EnemyKnowsTheirChoice = enemyKnowsTheirChoice;
        EnemyChoice = enemyChoice;
        MyChoice = myChoice;
        LastEnemyPlayedEffect = lastEnemyPlayedEffect;
    }
}