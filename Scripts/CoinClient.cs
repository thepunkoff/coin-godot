using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Coin.Scripts;
using Coin.Shared;
using Microsoft.AspNetCore.WebUtilities;

namespace Coin;

public class CoinClient
{
	private readonly HttpClient _httpClient;
	private readonly Guid _clientId = Guid.NewGuid();

	public CoinClient(string address)
	{
		_httpClient = new HttpClient();
		_httpClient.BaseAddress = new Uri($"http://{address}");
	}

	public async Task<bool> Connect()
	{
		var values = new Dictionary<string, string>
		{
			["id"] = _clientId.ToString(),
		};

		var uri = QueryHelpers.AddQueryString("/join", values);

		var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);

		return response.StatusCode == HttpStatusCode.OK;
	}

	public async Task<string?> SetChoice(bool choice)
	{
		string? ret = null;

		var values = new Dictionary<string, string>
		{
			["id"] = _clientId.ToString(),
			["side"] = choice.ToString(),
		};
		var uri = QueryHelpers.AddQueryString("/side", values);

		Console.WriteLine("[Http] Sending 'side' request");
		var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
		if (response.StatusCode != HttpStatusCode.OK)
			throw new Exception(response.ToString());

		await using var stateJsonStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
		if (stateJsonStream.Length <= 0)
		{
			Console.WriteLine("[Http] 'side' request OK");
		}
		else
		{
			Console.WriteLine("[Http] 'side' request error returned by server");
			var jDoc = await JsonDocument.ParseAsync(stateJsonStream).ConfigureAwait(false);
			if (!jDoc.RootElement.TryGetProperty("error", out var errorProp))
				throw new InvalidDataException();

			ret = errorProp.GetString();
		}
		stateJsonStream.Close();

		return ret;
	}

	public async Task<string?> UseCard(CardType card, CardEffectType cardEffect)
	{
		string? ret = null;

		var values = new Dictionary<string, string>
		{
			["id"] = _clientId.ToString(),
			["card"] = card.ToString(),
			["cardEffect"] = cardEffect.ToString(),
		};
		var uri = QueryHelpers.AddQueryString("/card", values);
		
		Console.WriteLine("[Http] Sending 'card' request");
		var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
		if (response.StatusCode != HttpStatusCode.OK)
			throw new Exception(response.ToString());

		await using var stateJsonStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
		if (stateJsonStream.Length <= 0)
		{
			Console.WriteLine("[Http] 'card' request OK");
		}
		else
		{
			Console.WriteLine("[Http] 'card' request error returned by server");
			var jDoc = await JsonDocument.ParseAsync(stateJsonStream).ConfigureAwait(false);
			if (!jDoc.RootElement.TryGetProperty("error", out var errorProp))
				throw new InvalidDataException();

			ret = errorProp.GetString();
		}
		stateJsonStream.Close();

		return ret;
	}

	public Task<string?> Pass()
	{
		return UseCard(CardType.Pass, CardEffectType.Pass);
	}

	public async Task<JsonDocument> GetStateJson()
	{
		var values = new Dictionary<string, string>
		{
			["id"] = _clientId.ToString(),
		};
		var uri = QueryHelpers.AddQueryString("/render", values);

		// Console.WriteLine("[Http] Sending 'render' request");
		var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);

		if (response.StatusCode != HttpStatusCode.OK)
			throw new Exception(response.ToString());

		// Console.WriteLine("[Http] 'render' request OK");
		await using var stateJsonStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
		var jDoc = await JsonDocument.ParseAsync(stateJsonStream).ConfigureAwait(false);
		stateJsonStream.Close();

		return jDoc;
	}
}
