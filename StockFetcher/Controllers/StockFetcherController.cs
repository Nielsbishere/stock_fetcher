using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace StockFetcher.Controllers {

	[ApiController]
	[Route("[controller]")]
	public class StockFetcherController : ControllerBase {

		private readonly ILogger<StockFetcherController> _logger;

		public StockFetcherController(ILogger<StockFetcherController> logger) {
			_logger = logger;
		}

		[HttpGet]
		public async Task<string> Get(string inputTickers) {

			string[] tickers = inputTickers.Split(',');

			HttpClient client = new HttpClient();

			//User agent is required or yahoo will (correctly assume) that we're a bot.
			//We're totally not a bot actually, don't even worry about it.
			//All your data are belong to us.

			client.DefaultRequestHeaders.Add(
				"User-Agent", "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/6.0;)"
			);

			List<Task<HttpResponseMessage>> responses = new List<Task<HttpResponseMessage>>();

			foreach(string ticker in tickers)
				responses.Add(client.GetAsync(
					$"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?region=US&lang=en-US&includePrePost=false&interval=2m&useYfid=true&range=1d&corsDomain=finance.yahoo.com&.tsrc=finance"
				));

			string outputCsv = "";
			int it = 0;

			Dictionary<string, float> conversions = new Dictionary<string, float>();

			foreach(Task<HttpResponseMessage> response in responses) {

				HttpResponseMessage res = await response;
				string jsonStr = await res.Content.ReadAsStringAsync();
				JsonNode? obj = JsonNode.Parse(jsonStr);

				try {

					JsonNode? meta = obj["chart"]["result"][0]["meta"];
					string currency = meta["currency"].AsValue().GetValue<string>();

					float v = meta["regularMarketPrice"].AsValue().GetValue<float>();

					if (tickers[it].EndsWith("=X"))
						conversions[tickers[it] == "EUR=X" ? "USD" : tickers[it].Replace("EUR", "").Replace("=X", "")] = v;

					float multiplier = 1;

					if (currency != "EUR")
						multiplier = conversions[currency];

					string total = "" + v * multiplier;
					total = total.Replace('.', ',');		//European currencies use ,

					outputCsv += tickers[it] + ";" + total + "\n";

				} catch (Exception e) {
					outputCsv += tickers[it] + ";ERROR" + e.Message + "\n";
				}

				++it;
			}

			return outputCsv;
		}
	}
}
