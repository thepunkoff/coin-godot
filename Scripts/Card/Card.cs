using Godot;

namespace Coin.Scripts.Card;

public partial class Card : Resource
{
	[Export]
	public CardType Type { get; set; }

	[Export]
	public Godot.Collections.Array<CardEffect> Effects { get; set; }

	public Card() : this(CardType.Pass, new Godot.Collections.Array<CardEffect> { new() { Type = CardEffectType.Pass, DisplayName = "Pass" } })
	{
	}

	public Card(CardType type, Godot.Collections.Array<CardEffect> effects)
	{
		Type = type;
		Effects = effects;
	}
}
