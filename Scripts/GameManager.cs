using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Coin.Shared;
using Godot;

namespace Coin.Scripts;

public class GameManager
{
	private readonly CoinClient _client;

	public event EventHandler ShouldMakeChoice;

	public event EventHandler<TurnResult> AfterMyTurn;

	public event EventHandler<TurnResult> BeforeMyTurn;

	public event EventHandler<RoundResult> RoundEnded;

	public GameManager(string serverAddress)
	{
		_client = new CoinClient(serverAddress);
	}

	public async Task Start()
	{
		GD.Print("Connecting to server...");
		if (!await _client.Connect().ConfigureAwait(false))
		{
			GD.PrintErr("Error connecting to server.");
			return;
		}
		
		GD.Print("Connected to server. Waiting for the other player to join...");
		await WaitForTheOtherPlayer().ConfigureAwait(false);
		GD.Print("Players ready.");

		ShouldMakeChoice?.Invoke(this, EventArgs.Empty);
	}

	public async void MakeChoice(bool choice)
	{
		var res = await _client.SetChoice(choice);
		if (res is not null)
			throw new Exception(res);

		var (_, gameData) = await WaitMyTurn(_client, false);
		BeforeMyTurn?.Invoke(this, gameData);
	}

	public async void UseCard(CardType card, CardEffectType effect)
	{
		var res = await _client.UseCard(card, effect);
		if (res is not null)
			throw new Exception(res);

		var jDoc = await _client.GetStateJson();
		var gameData = GetGameData(jDoc);
		if (gameData is null)
		{
			var roundResult = await GetRoundResult(_client);
			RoundEnded?.Invoke(this, roundResult);

			if (roundResult.IWonTheGame is null)
				ShouldMakeChoice?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			AfterMyTurn?.Invoke(this, gameData);

			var (roundEnded, newGameData) = await WaitMyTurn(_client, false);
			if (!roundEnded)
			{
				BeforeMyTurn?.Invoke(this, newGameData);
			}
			else
			{
				var roundResult = await GetRoundResult(_client);
				RoundEnded?.Invoke(this, roundResult);

				if (roundResult.IWonTheGame is null)
					ShouldMakeChoice?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	private async Task WaitForTheOtherPlayer()
	{
		var printed = false;
		while (true)
		{
			var jDoc = await _client.GetStateJson().ConfigureAwait(false);

			if (!jDoc.RootElement.TryGetProperty("state", out var stateProp))
				throw new InvalidDataException();

			var stateString = stateProp.GetString();
			if (stateString is null)
				throw new InvalidDataException();
			
			if (!Enum.TryParse<GameStateType>(stateString, out var state))
				throw new InvalidDataException();

			if (state is not (GameStateType.WaitingForPlayers or GameStateType.WaitingForSides))
				throw new InvalidDataException();

			if (state == GameStateType.WaitingForSides)
				break;

			if (!printed)
			{
				GD.Print("Waiting for the other player to join...");
				printed = true;
			}
			await Task.Delay(1000).ConfigureAwait(false);
		}
	}

	private static async Task<(bool RoundEnded, TurnResult? GameData)> WaitMyTurn(CoinClient coinClient, bool coinKnown)
	{
		var printedFirst = false;
		var printedSecond = false;
		bool? coin = null;
		while (true)
		{
			var jDoc = await coinClient.GetStateJson();

			if (!jDoc.RootElement.TryGetProperty("state", out var stateProp))
				throw new InvalidDataException();

			var stateString = stateProp.GetString();
			if (stateString is null)
				throw new InvalidDataException();

			if (!Enum.TryParse<GameStateType>(stateString, out var state))
				throw new InvalidDataException();

			if (state is not (GameStateType.WaitingForSides or GameStateType.WaitingForCards or GameStateType.RoundEnded))
				throw new InvalidDataException();

			if (state is GameStateType.RoundEnded)
				return (true, null);
			
			if (state is GameStateType.WaitingForSides)
			{
				if (!printedFirst)
				{
					GD.Print("Waiting for the other player to make a choice...", ConsoleColor.Green);
					printedFirst = true;
				}

				await Task.Delay(1000);
				continue;
			}

			if (coin is null)
			{
				if (!jDoc.RootElement.TryGetProperty("coin", out var coinProp))
					throw new InvalidDataException();

				coin = coinProp.GetBoolean();
			}

			if (!jDoc.RootElement.TryGetProperty("turn", out var turnProp))
				throw new InvalidDataException();

			var turn = turnProp.GetString();
			if (turn is null or not ("me" or "enemy"))
				throw new InvalidDataException();

			if (turn == "me")
			{
				var gameData = GetGameData(jDoc);
				return (false, gameData);
			}

			if (!printedSecond)
			{
				if (!coinKnown)
					GD.Print($"The coin is: {(coin.Value ? "Heads" : "Tails")}.", ConsoleColor.Yellow);

				GD.Print("Waiting for the other player to use a card...", ConsoleColor.Green);
				printedSecond = true;
			}

			await Task.Delay(1000);
		}
	}

	private static TurnResult? GetGameData(JsonDocument jDoc)
	{
		if (!jDoc.RootElement.TryGetProperty("state", out var stateProp))
			throw new InvalidDataException();

		var stateString = stateProp.GetString();
		if (stateString is null)
			throw new InvalidDataException();

		if (!Enum.TryParse<GameStateType>(stateString, out var state))
			throw new InvalidDataException();

		if (state is GameStateType.RoundEnded)
			return null;
		
		if (state is not GameStateType.WaitingForCards)
			throw new InvalidDataException();

		if (!jDoc.RootElement.TryGetProperty("meSideKnownToEnemy", out var meSideKnownProp))
			throw new InvalidDataException();

		if (!jDoc.RootElement.TryGetProperty("enemySideKnownToEnemy", out var enemySideKnownProp))
			throw new InvalidDataException();

		if (!jDoc.RootElement.TryGetProperty("coin", out var coinProp))
			throw new InvalidDataException();
		var coin = coinProp.GetBoolean();
		
		bool? enemyChoice = null;
		if (jDoc.RootElement.TryGetProperty("enemySide", out var enemySideProp))
			enemyChoice = enemySideProp.GetBoolean();
		
		bool? myChoice = null;
		if (jDoc.RootElement.TryGetProperty("meSide", out var mySideProp))
			myChoice = mySideProp.GetBoolean();

		var meChoiceKnown = meSideKnownProp.GetBoolean();
		var enemyChoiceKnown = enemySideKnownProp.GetBoolean();

		if (!jDoc.RootElement.TryGetProperty("lastEnemyPlayedCard", out var lastEnemyPlayedEffectProp))
			throw new InvalidDataException();
		var lastEnemyPlayedEffectString = lastEnemyPlayedEffectProp.GetString();
		if (lastEnemyPlayedEffectString is null)
			throw new InvalidDataException();

		CardEffectType? lastEnemyPlayedEffect;
		if (lastEnemyPlayedEffectString == string.Empty)
		{
			lastEnemyPlayedEffect = null;
		}
		else
		{
			if (!Enum.TryParse<CardEffectType>(lastEnemyPlayedEffectString, out var lastEnemyPlayedEffectFromEnum))
				throw new InvalidDataException();

			lastEnemyPlayedEffect = lastEnemyPlayedEffectFromEnum;
		}

		return new TurnResult(coin, GetCardsFromJson(jDoc), meChoiceKnown, enemyChoiceKnown, enemyChoice, myChoice, lastEnemyPlayedEffect);
	}

	private static IReadOnlyDictionary<CardType, int> GetCardsFromJson(JsonDocument jDoc)
	{
		if (!jDoc.RootElement.TryGetProperty("meCards", out var meCardsProp))
			throw new InvalidDataException();

		var cardsDic = new Dictionary<CardType, int>();
		foreach (var cardProp in meCardsProp.EnumerateObject())
		{
			if (!Enum.TryParse<CardType>(cardProp.Name, out var card))
				throw new InvalidDataException();

			if (!cardProp.Value.TryGetInt32(out var count))
				throw new InvalidDataException();

			cardsDic.Add(card, count);
		}

		return cardsDic;
	}

	private static async Task<RoundResult> GetRoundResult(CoinClient coinClient)
	{
		var jDoc = await coinClient.GetStateJson();

		if (!jDoc.RootElement.TryGetProperty("state", out var stateProp))
			throw new InvalidDataException();

		var stateString = stateProp.GetString();
		if (stateString is null)
			throw new InvalidDataException();

		if (!Enum.TryParse<GameStateType>(stateString, out var state))
			throw new InvalidDataException();

		if (state is not GameStateType.RoundEnded)
			throw new InvalidDataException();

		if (!jDoc.RootElement.TryGetProperty("meScore", out var meScoreProp))
			throw new InvalidDataException();

		if (!jDoc.RootElement.TryGetProperty("enemyScore", out var enemyScoreProp))
			throw new InvalidDataException();
		
		if (!jDoc.RootElement.TryGetProperty("meWon", out var meWonProp))
			throw new InvalidDataException();

		if (!jDoc.RootElement.TryGetProperty("enemyWon", out var enemyWonProp))
			throw new InvalidDataException();

		bool? iAmTheWinner = null;
		if (jDoc.RootElement.TryGetProperty("iAmTheWinner", out var iAmTheWinnerProp))
			iAmTheWinner = iAmTheWinnerProp.GetBoolean();

		var meScore = meScoreProp.GetInt32();
		var enemyScore = enemyScoreProp.GetInt32();
		var meWon = meWonProp.GetBoolean();
		var enemyWon = enemyWonProp.GetBoolean();

		return new RoundResult
		{
			MyScore = meScore,
			EnemyScore = enemyScore,
			IWonTheRound = meWon,
			EnemyWonTheRound = enemyWon,
			IWonTheGame = iAmTheWinner,
		};
	}
}
