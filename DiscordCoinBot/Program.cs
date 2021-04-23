using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;

namespace DiscordCoin
{
    class CoinData
    {
        public string symbol = "";
        public string price = "";
    }

    class Program
    {
        CoinData coin;
        HttpClient httpClient;
        DiscordSocketClient client; 
        CommandService commands;   
        string ticker;
        List<string> tickers = new List<string>();
        List<string> removeTickers = new List<string>();

        static void Main(string[] args)
        {
            new Program().BotMain().GetAwaiter().GetResult();   
        }

        public async Task BotMain()
        {
            ticker = File.ReadAllText("Ticker.txt");

            tickers.AddRange(ticker.Split(','));

            foreach (string str in tickers)
                if (!str.Contains("USDT"))
                    removeTickers.Add(str);

            for (int i = 0; i < removeTickers.Count; i++)
            {
                tickers.Remove(removeTickers[i]);
            }

            client = new DiscordSocketClient(new DiscordSocketConfig()
            {  
                LogLevel = LogSeverity.Verbose                 
            });
            commands = new CommandService(new CommandServiceConfig()      
            {
                LogLevel = LogSeverity.Verbose                          
            });


            client.Log += OnClientLogReceived;
            commands.Log += OnClientLogReceived;

            await client.LoginAsync(TokenType.Bot, "YOURTOKEN"); 
            await client.StartAsync();                      

            client.MessageReceived += OnClientMessage;        

            await Task.Delay(-1);   
        }

        private async Task OnClientMessage(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message == null) return;

            int pos = 0;

            if (!(message.HasCharPrefix('!', ref pos) ||
             message.HasMentionPrefix(client.CurrentUser, ref pos)) ||
              message.Author.IsBot)
                return;

            var context = new SocketCommandContext(client, message);       

            if (tickers.Contains(message.Content.Replace("!", "").ToUpper()))   
            {
                httpClient = new HttpClient();
                var webRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.binance.com/api/v3/ticker/price?symbol=" + message.Content.Replace("!", "").ToUpper());

                var response = await httpClient.SendAsync(webRequest);

                var reader = response.Content.ReadAsStringAsync().Result;
                coin = JsonConvert.DeserializeObject<CoinData>(reader);

                await context.Channel.SendMessageAsync("현재 " + message.Content.Replace("!", "").ToUpper() + "의 가격 : " + double.Parse(coin.price) + "$");
            }
        }

        private Task OnClientLogReceived(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}