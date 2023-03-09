using System;
using System.Threading.Tasks;
using Godot;

namespace Coin.Scripts;

public partial class Game : Node
{
	private GameManager? _gameManager;

	private Node? _uiRoot;

	private Node? _enemyCards;
	private ColorRect? _enemyCoin;
	private ColorRect? _coin;
	private ColorRect? _myCoin;
	private Node? _myCards;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		_uiRoot ??= GetNode<Node>("/root/Container");

		var serverPopupPrefab = GD.Load<PackedScene>("res://Scenes/server_popup.tscn");
		var serverPopup = serverPopupPrefab.Instantiate();
		var label = serverPopup.GetNode<Label>("VBoxContainer/MarginContainer/Label");
		var textEdit = serverPopup.GetNode<TextEdit>("VBoxContainer/TextEdit");
		var joinButton = serverPopup.GetNode<Button>("VBoxContainer/Button");
		joinButton.Disabled = true;
		textEdit.TextChanged += () =>
		{
			joinButton.Disabled = textEdit.Text == string.Empty;
		};
		joinButton.ButtonDown += () =>
		{
			label.Text = "Connecting...";
			joinButton.Disabled = true;
			Task.Run(async () =>
			{
				try
				{
					_gameManager = new GameManager(textEdit.Text);
					await SetupGameManager();
					serverPopup.QueueFree();
				}
				catch
				{
					label.Text = "Error. Try again.";
					textEdit.Clear();
					var timer = GetTree().CreateTimer(3);
					timer.Timeout += () => label.Text = "Enter server address:";
				}
			});
		};

		_uiRoot.CallDeferred(Node.MethodName.AddChild, serverPopup);        
	}

	private async Task SetupGameManager()
	{
		_gameManager!.ShouldMakeChoice += (_, _) =>
		{
			ShowChoicePopup();
		};

		_gameManager.BeforeMyTurn += (_, turnResult) =>
		{
			UpdateTable(false, turnResult);
		};

		_gameManager.AfterMyTurn += (_, turnResult) =>
		{
			UpdateTable(true, turnResult);
		};

		_gameManager.RoundEnded += (_, roundResult) =>
		{
			if (roundResult.IWonTheGame is not null)
			{
				ShowLabelForSeconds(roundResult.IWonTheGame.Value ? "I WIN!" : "I LOSE :(", 5);
				foreach (var card in _myCards!.GetChildren())
					card.QueueFree();
				_enemyCoin!.Color = Colors.White;
				_coin!.Color = Colors.White;
				_myCoin!.Color = Colors.White;

				ShowRejoinButton();
			}
			else
			{

				var text = $"Me: {(roundResult.IWonTheRound ? "win" : "lose")} ({roundResult.MyScore})\n" +
						   $"Enemy: {(roundResult.EnemyWonTheRound ? "win" : "lose")} ({roundResult.EnemyScore})";
				ShowLabelForSeconds(text, 5);
			}
		};

		await _gameManager.Start();
	}

	private void ShowRejoinButton()
	{
		_uiRoot ??= GetNode<Node>("/root/Container");
		var rejoinButtonPrefab = GD.Load<PackedScene>("res://Scenes/rejoin_button.tscn");

		var rejoinButton = rejoinButtonPrefab.Instantiate<Button>();
		rejoinButton.ButtonDown += async () =>
		{
			rejoinButton.Text = "Waiting...";
			await _gameManager!.Start();
			rejoinButton.QueueFree();
		};

		_uiRoot.CallDeferred(Node.MethodName.AddChild, rejoinButton);
	}

	private void ShowChoicePopup()
	{
		_uiRoot ??= GetNode<Node>("/root/Container");
		var choicePopupPrefab = GD.Load<PackedScene>("res://Scenes/choice_popup.tscn");

		var choicePopup = choicePopupPrefab.Instantiate();

		var headsButton = choicePopup.GetNode<Button>("VBoxContainer/HBoxContainer/Button");
		headsButton.ButtonDown += () => {
		{
			_gameManager!.MakeChoice(true);
			choicePopup.QueueFree();
		} };

		var tailsButton = choicePopup.GetNode<Button>("VBoxContainer/HBoxContainer/Button2");
		tailsButton.ButtonDown += () =>
		{
			_gameManager!.MakeChoice(false);
			choicePopup.QueueFree();
		};

		var closeButton = choicePopup.GetNode<Button>("CloseButton");
		closeButton.QueueFree();

		_uiRoot.CallDeferred(Node.MethodName.AddChild, choicePopup);
	}

	private void UpdateTable(bool afterMyTurn, TurnResult turnResult)
	{
		if (!afterMyTurn && turnResult.LastEnemyPlayedEffect is not null)
			ShowLabelForSeconds($"Enemy used: {turnResult.LastEnemyPlayedEffect}", 5);

		_enemyCoin ??= GetNode<ColorRect>("../VBoxContainer/Table/MarginContainer/Table/EnemyCoin/MarginContainer/ColorRect");
		_coin ??= GetNode<ColorRect>("../VBoxContainer/Table/MarginContainer/Table/Coin");
		_myCoin ??= GetNode<ColorRect>("../VBoxContainer/Table/MarginContainer/Table/MyCoin/MarginContainer/ColorRect");

		_coin.Color = turnResult.Coin ? Colors.Green : Colors.Red;
		_myCoin.Color = turnResult.MyChoice is null ? Colors.White : turnResult.MyChoice.Value ? Colors.Green : Colors.Red;
		_enemyCoin.Color = turnResult.EnemyChoice is null ? Colors.White : turnResult.EnemyChoice.Value ? Colors.Green : Colors.Red;

		var enemyCoinMarginContainer = _enemyCoin.GetParent<MarginContainer>();
		enemyCoinMarginContainer.AddThemeConstantOverride("margin_top", 0);
		enemyCoinMarginContainer.AddThemeConstantOverride("margin_bottom", 0);
		enemyCoinMarginContainer.AddThemeConstantOverride("margin_left", 0);
		enemyCoinMarginContainer.AddThemeConstantOverride("margin_right", 0);
		
		var myCoinMarginContainer = _myCoin.GetParent<MarginContainer>();
		myCoinMarginContainer.AddThemeConstantOverride("margin_top", 0);
		myCoinMarginContainer.AddThemeConstantOverride("margin_bottom", 0);
		myCoinMarginContainer.AddThemeConstantOverride("margin_left", 0);
		myCoinMarginContainer.AddThemeConstantOverride("margin_right", 0);

		if (turnResult.EnemyKnowsTheirChoice)
		{
			enemyCoinMarginContainer.AddThemeConstantOverride("margin_top", 20);
			enemyCoinMarginContainer.AddThemeConstantOverride("margin_bottom", 20);
			enemyCoinMarginContainer.AddThemeConstantOverride("margin_left", 20);
			enemyCoinMarginContainer.AddThemeConstantOverride("margin_right", 20);
		}

		if (turnResult.EnemyKnowsMyChoice)
		{
			myCoinMarginContainer.AddThemeConstantOverride("margin_top", 20);
			myCoinMarginContainer.AddThemeConstantOverride("margin_bottom", 20);
			myCoinMarginContainer.AddThemeConstantOverride("margin_left", 20);
			myCoinMarginContainer.AddThemeConstantOverride("margin_right", 20);
		}

		_myCards ??= GetNode<Node>("../VBoxContainer/MyCards/MarginContainer/ScrollContainer/HBoxContainer");

		foreach (var child in _myCards.GetChildren())
			child.QueueFree();
		
		var cardPrefab = GD.Load<PackedScene>("res://Scenes/card.tscn");
		foreach (var (cardType, count) in turnResult.MyCards)
		{
			var card = cardPrefab.Instantiate();
			var button = card.GetNode<Button>("Button");

			var mirrorHint = cardType is CardType.Mirror
				? turnResult.LastEnemyPlayedEffect is not null ? $" ({turnResult.LastEnemyPlayedEffect})" : " (Pass)"
				: string.Empty;
			button.Text = $"{cardType}{mirrorHint} ({count})";

			if (afterMyTurn)
			{
				button.Disabled = true;
			}
			else
			{
				button.ButtonDown += () =>
				{
					if (cardType is CardType.Scan or CardType.Random or CardType.Flip)
						ShowSpecifyPopup(cardType, count, button);
					else if (cardType is CardType.Mirror)
						UseCard(cardType, turnResult.LastEnemyPlayedEffect ?? CardEffectType.Pass, count, button);
					else
						UseCard(cardType, GetCorrespondingEffect(cardType), count, button);
				};
			}

			_myCards.CallDeferred(Node.MethodName.AddChild, card);
		}

		var passCard = cardPrefab.Instantiate();
		var passButton = passCard.GetNode<Button>("Button");
		passButton.Text = "Pass";
		
		if (afterMyTurn)
			passButton.Disabled = true;
		else
			passButton.ButtonDown += () => UseCard(CardType.Pass, CardEffectType.Pass, 1, passButton);
		_myCards.CallDeferred(Node.MethodName.AddChild, passCard);
	}

	private void UseCard(CardType cardType, CardEffectType effectType, int cardsThisTypeCount, Button cardButton)
	{
		_gameManager!.UseCard(cardType, effectType);
		foreach (var c in cardButton.GetParent().GetParent().GetChildren())
		{
			var b = c.GetChild<Button>(0);
			b.Disabled = true;
		}

		if (cardsThisTypeCount == 1)
			cardButton.QueueFree();
		else
			cardButton.Text = $"{cardType} ({cardsThisTypeCount - 1})";
	}

	private void ShowSpecifyPopup(CardType cardType, int cardsThisTypeCount, Button cardButton)
	{
		_myCards ??= GetNode<Node>("../VBoxContainer/MyCards/MarginContainer/ScrollContainer/HBoxContainer");
		foreach (var child in _myCards.GetChildren())
		{
			var b = child.GetChild<Button>(0);
			b.Disabled = true;
		}
		
		_uiRoot ??= GetNode<Node>("/root/Container");
		var choicePopupPrefab = GD.Load<PackedScene>("res://Scenes/choice_popup.tscn");

		var choicePopup = choicePopupPrefab.Instantiate();

		var meButton = choicePopup.GetNode<Button>("VBoxContainer/HBoxContainer/Button");
		meButton.Text = "Me";
		meButton.ButtonDown += () => {
		{
			UseCard(cardType, GetTargetEffect(cardType, true), cardsThisTypeCount, cardButton);
			choicePopup.QueueFree();
		} };

		var enemyButton = choicePopup.GetNode<Button>("VBoxContainer/HBoxContainer/Button2");
		enemyButton.Text = "Enemy";
		enemyButton.ButtonDown += () =>
		{
			UseCard(cardType, GetTargetEffect(cardType, false), cardsThisTypeCount, cardButton);
			choicePopup.QueueFree();
		};

		var closeButton = choicePopup.GetNode<Button>("CloseButton");
		closeButton.ButtonDown += () =>
		{
			choicePopup.QueueFree();
			foreach (var child in _myCards.GetChildren())
			{
				var b = child.GetChild<Button>(0);
				b.Disabled = false;
			}
		};

		_uiRoot.CallDeferred(Node.MethodName.AddChild, choicePopup);
	}

	private void ShowLabelForSeconds(string text, int timeSeconds)
	{
		_uiRoot ??= GetNode<Node>("/root/Container");
		var label = new Label();
		label.Text = text;
		label.Theme = GD.Load<Theme>("res://theme.tres");
		var timer = GetTree().CreateTimer(timeSeconds);
		timer.Timeout += () => label.QueueFree();
		_uiRoot.CallDeferred(Node.MethodName.AddChild, label);
	}

	private static CardEffectType GetCorrespondingEffect(CardType cardType)
	{
#pragma warning disable CS8509
		return cardType switch
#pragma warning restore CS8509
		{
			CardType.Swap => CardEffectType.Swap,
			CardType.Yield => CardEffectType.Yield,
		};
	}

	private static CardEffectType GetTargetEffect(CardType cardType, bool me)
	{
		// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
		return cardType switch
		{
			CardType.Random when me => CardEffectType.RandomMe,
			CardType.Random when !me => CardEffectType.RandomEnemy,
			CardType.Flip when me => CardEffectType.FlipMe,
			CardType.Flip when !me => CardEffectType.FlipEnemy,
			CardType.Scan when me => CardEffectType.ScanMe,
			CardType.Scan when !me => CardEffectType.ScanEnemy,
			_ => throw new ArgumentOutOfRangeException(nameof(cardType), cardType, null)
		};
	}
}
