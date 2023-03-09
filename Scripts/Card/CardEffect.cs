using Godot;

namespace Coin.Scripts.Card;

public partial class CardEffect : Resource
{
	[Export]
	public CardEffectType Type { get; set; }

	[Export]
	public string DisplayName { get; set; }

	public CardEffect() : this(CardEffectType.Pass, "Pass")
	{
	}

	public CardEffect(CardEffectType type, string displayName)
	{
		Type = type;
		DisplayName = displayName;
	}
}
