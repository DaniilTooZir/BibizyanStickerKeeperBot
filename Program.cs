using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

using DotNetEnv;

using StickerKeeperBot.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Env.Load(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));

string token = Env.GetString("TELEGRAM_BOT_TOKEN");
string supabaseUrl = Env.GetString("SUPABASE_URL");
string supabaseKey = Env.GetString("SUPABASE_KEY");

var bot = new BotService(token, supabaseUrl, supabaseKey);
bot.Start();

