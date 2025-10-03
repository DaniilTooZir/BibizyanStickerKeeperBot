using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

using StickerKeeperBot.Services;
using StickerKeeperBot.Data;

namespace StickerKeeperBot.Services
{
    public class BotService
    {
        private readonly ITelegramBotClient _bot;
        private readonly StickerService _stickerService;

        public BotService(string token, string supabaseUrl, string supabaseKey)
        {
            _bot = new TelegramBotClient(token);

            var db = new StickerDb(supabaseUrl, supabaseKey);
            db.InitAsync().Wait();

            _stickerService = new StickerService(db);
        }

        public void Start()
        {
            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);
            _bot.SetMyCommands(new[]
            {
                new Telegram.Bot.Types.BotCommand
                {
                    Command = "/menu",
                    Description = "Вызвать меню"
                },
                new Telegram.Bot.Types.BotCommand
                {
                    Command = "/add",
                    Description = "Добавить стикор"
                },
                new Telegram.Bot.Types.BotCommand
                {
                    Command = "/search",
                    Description = "Поиск стикоров"
                }
            }).Wait();
            Console.WriteLine("Бот запущен. Нажми Enter для выхода...");
            Console.ReadLine();
            cts.Cancel();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Type == UpdateType.Message && update.Message != null)
                await HandleMessage(bot, update.Message, ct);
            else if (update.Type == UpdateType.InlineQuery && update.InlineQuery != null)
                await HandleInlineQuery(bot, update.InlineQuery, ct);
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                await HandleCallbackQuery(bot, update.CallbackQuery, ct);
        }

        private async Task HandleMessage(ITelegramBotClient bot, Message message, CancellationToken ct)
        {
            if (message.Text == null) return;
            if (message.Text != null && message.Text.StartsWith("/add") && message.ReplyToMessage?.Sticker != null)
            {
                string command = message.Text.Length > 5 ? message.Text.Substring(5).Trim() : "";
                string[] parts = command.Split('|', 2);
                if (parts.Length < 2)
                {
                    await bot.SendMessage(
                        chatId: message.Chat,
                        text: "Используй: /add <название> | <категория> (ответом на стикер)",
                        cancellationToken: ct);
                    return;
                }

                string name = parts[0].Trim();
                string category = parts[1].Trim();
                string fileId = message.ReplyToMessage.Sticker.FileId;
                await _stickerService.AddSticker(name, fileId, category);
                await bot.SendMessage(
                    chatId: message.Chat,
                    text: $"Стикер \"{name}\" добавлен в категорию \"{category}\"",
                    cancellationToken: ct);
            }
            else if (message.Text == "/menu")
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Добавить стикер", "menu_add"),
                        InlineKeyboardButton.WithCallbackData("Поиск", "menu_search"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("GitHub", "https://github.com/DaniilTooZir/BibizyanStickerKeeperBot.git")
                    }
                });
                var replyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Добавить стикер", "Поиск" },
                    new KeyboardButton[] { "Категории" }
                })
                {
                    ResizeKeyboard = true
                };

                await bot.SendMessage(
                    chatId: message.Chat,
                    text: "Выберите действие:",
                    replyMarkup: replyKeyboard,
                    cancellationToken: ct);

                await bot.SendMessage(
                    chatId: message.Chat,
                    text: "Выберите действия",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: ct);
            }
            else if (message.Text == "Добавить стикер")
            {
                await bot.SendMessage(
                    chatId: message.Chat,
                    text: "Чтобы добавить стикер, используй команду: /add <название> | <категория> ответом на стикер",
                    cancellationToken: ct);
            }
            else if (message.Text == "Поиск")
            {
                await bot.SendMessage(
                    chatId: message.Chat,
                    text: "Напиши слово для поиска, или используй inline-запрос: @ИмяБота <ключевое слово>",
                    cancellationToken: ct);
            }
            else if (message.Text == "Категории")
            {
                await bot.SendMessage(
                    chatId: message.Chat,
                    text: "Список категорий будет здесь скоро ",
                    cancellationToken: ct);
            }
        }

        private async Task HandleInlineQuery(ITelegramBotClient bot, InlineQuery query, CancellationToken ct)
        {
            var results = new List<InlineQueryResult>();
            try
            {
                if (string.IsNullOrWhiteSpace(query.Query))
                {
                    var categories = await _stickerService.GetCategories();
                    const int perCategory = 3;
                    foreach (var category in categories)
                    {
                        var stickersInCat = await _stickerService.GetStickerByCategory(category);
                        if (stickersInCat == null || !stickersInCat.Any())
                            continue;
                        foreach (var s in stickersInCat.Take(perCategory))
                        {
                            results.Add(new InlineQueryResultCachedSticker(
                                id: $"{s.Id}",
                                stickerFileId: s.FileId
                            ));
                        }
                    }
                    if (!results.Any())
                    {
                        results.Add(new InlineQueryResultArticle(
                            id: "empty",
                            title: "Стикеры не найдены",
                            inputMessageContent: new InputTextMessageContent("В хранилище пока нет стикеров.")
                        ));
                    }
                }
                else
                {
                    var stickersByCategory = await _stickerService.GetStickerByCategory(query.Query);
                    if (stickersByCategory != null && stickersByCategory.Any())
                    {
                        foreach (var s in stickersByCategory)
                        {
                            results.Add(new InlineQueryResultCachedSticker(
                                id: s.Id.ToString(),
                                stickerFileId: s.FileId
                            ));
                        }
                    }
                    else
                    {
                        var stickers = await _stickerService.SearchStickers(query.Query);
                        foreach (var s in stickers)
                        {
                            results.Add(new InlineQueryResultCachedSticker(
                                id: s.Id.ToString(),
                                stickerFileId: s.FileId
                            ));
                        }
                    }
                    if (!results.Any())
                    {
                        results.Add(new InlineQueryResultArticle(
                            id: "notfound",
                            title: "Ничего не найдено",
                            inputMessageContent: new InputTextMessageContent("Ничего не найдено по запросу.")
                        ));
                    }
                }
                await bot.AnswerInlineQuery(
                    inlineQueryId: query.Id,
                    results: results,
                    isPersonal: true,
                    cacheTime: 10,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HandleInlineQuery error: {ex.Message}");
                try
                {
                    await bot.AnswerInlineQuery(inlineQueryId: query.Id, results: new List<InlineQueryResult>(), isPersonal: true, cacheTime: 1, cancellationToken: ct);
                }
                catch {}
            }
        }

        private async Task HandleCallbackQuery(ITelegramBotClient bot, CallbackQuery query, CancellationToken ct)
        {
            if (query.Data == "menu_add")
            {
                await bot.AnswerCallbackQuery(query.Id, "Чтобы добавить стикер, используй команду: /add <название> | <категория>", cancellationToken: ct);
            }
            else if (query.Data == "menu_search")
            {
                await bot.AnswerCallbackQuery(query.Id, "Используй inline-запрос: напиши @ИмяБота <ключевое слово>", cancellationToken: ct);
            }
        }
        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
