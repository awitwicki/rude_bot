using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeAI.Core;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging;
using RudeBot.Services.UserProfileService;

namespace RudeBot.Services.Ai;

/// <summary>
/// Дає моделі читальний доступ до даних ОДНОГО чату. chatId не є параметром жодної
/// функції, тож попросити дані іншого чату модель не може навіть теоретично.
/// Живе рівно один запит: разом з ним живе і лічильник викликів.
/// </summary>
public class ChatRagTool : IFunctionTool
{
    public const string GetUserProfile = "get_user_profile";
    public const string GetUserMessages = "get_user_messages";
    public const string GetUserStats = "get_user_stats";
    public const string SearchMessages = "search_messages";

    public const int MaxTextLength = 300;
    public const int DefaultUserMessages = 20;
    public const int MaxUserMessages = 50;
    public const int DefaultSearchResults = 10;
    public const int MaxSearchResults = 30;

    private static readonly string[] FunctionNames =
    {
        GetUserProfile, GetUserMessages, GetUserStats, SearchMessages,
    };

    private readonly long _chatId;
    private readonly IReadOnlyList<RosterEntry> _roster;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger _logger;
    private readonly int _callBudget;

    private int _callsMade;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ChatRagTool(
        long chatId,
        IReadOnlyList<RosterEntry> roster,
        IChatMessageRepository chatMessageRepository,
        IUserProfileService userProfileService,
        ILogger logger,
        int callBudget = 6)
    {
        _chatId = chatId;
        _roster = roster;
        _chatMessageRepository = chatMessageRepository;
        _userProfileService = userProfileService;
        _logger = logger;
        _callBudget = callBudget;
    }

    public bool IsContainFunction(string name) => FunctionNames.Contains(name);

    public Tool AsTool() => new()
    {
        FunctionDeclarations = new List<FunctionDeclaration>
        {
            new()
            {
                Name = GetUserProfile,
                Description = "Що ти пам'ятаєш про учасника чату: характер, звички, улюблені теми. "
                            + "Виклич, коли питання стосується конкретної людини.",
                Parameters = ObjectSchema(
                    ("user_name", SchemaType.STRING, "Ім'я або нік учасника так, як його згадують у чаті.", true)),
            },
            new()
            {
                Name = GetUserMessages,
                Description = "Останні повідомлення конкретного учасника, новіші спочатку. "
                            + "Виклич, коли треба саме те, що людина писала, а не переказ.",
                Parameters = ObjectSchema(
                    ("user_name", SchemaType.STRING, "Ім'я або нік учасника.", true),
                    ("count", SchemaType.INTEGER, $"Скільки повідомлень повернути, від 1 до {MaxUserMessages}. За замовчуванням {DefaultUserMessages}.", false)),
            },
            new()
            {
                Name = GetUserStats,
                Description = "Статистика учасника в цьому чаті: карма, попередження, кількість повідомлень і матюків.",
                Parameters = ObjectSchema(
                    ("user_name", SchemaType.STRING, "Ім'я або нік учасника.", true)),
            },
            new()
            {
                Name = SearchMessages,
                Description = "Пошук по тексту всіх повідомлень чату. Виклич, коли питання про тему чи подію, а не про людину.",
                Parameters = ObjectSchema(
                    ("query", SchemaType.STRING, "Слово або фраза для пошуку.", true),
                    ("count", SchemaType.INTEGER, $"Скільки результатів повернути, від 1 до {MaxSearchResults}. За замовчуванням {DefaultSearchResults}.", false)),
            },
        },
    };

    public async Task<FunctionResponse> CallAsync(FunctionCall functionCall, CancellationToken cancellationToken = default)
    {
        // Google_GenerativeAI dispatches усі функції одного ходу моделі ПАРАЛЕЛЬНО (Task.Run + Task.WhenAll),
        // а IChatMessageRepository/IUserProfileService тут ділять один InstancePerLifetimeScope DataContext.
        // Без цього гейту паралельні виклики або впадуть на "A second operation was started on this context
        // instance...", або зроблять read-check-increment бюджету неатомарним.
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var response = new FunctionResponse(functionCall.Name) { Id = functionCall.Id };

            try
            {
                if (_callsMade >= _callBudget)
                {
                    _logger.LogWarning("ChatRagTool call budget exhausted for chat {ChatId}", _chatId);
                    return WithPayload(response, new { error = "call_budget_exceeded" });
                }

                _callsMade++;

                object payload = functionCall.Name switch
                {
                    GetUserProfile => await HandleGetUserProfileAsync(functionCall.Args),
                    GetUserMessages => await HandleGetUserMessagesAsync(functionCall.Args),
                    GetUserStats => HandleGetUserStats(functionCall.Args),
                    SearchMessages => await HandleSearchMessagesAsync(functionCall.Args),
                    _ => new { error = "unknown_function" },
                };

                return WithPayload(response, payload);
            }
            catch (Exception ex)
            {
                // Виняток посеред авто-циклу бібліотеки вбив би всю відповідь, тому все ловимо тут.
                _logger.LogError(ex, "ChatRagTool {Function} failed for chat {ChatId}", functionCall.Name, _chatId);
                return WithPayload(response, new { error = "internal" });
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<object> HandleGetUserProfileAsync(JsonNode? args)
    {
        if (!TryResolve(args, out var entry, out var error)) return error;

        var profile = await _userProfileService.GetAsync(_chatId, entry.UserId);

        if (profile is null || string.IsNullOrWhiteSpace(profile.Profile))
        {
            return new { error = "no_profile", user_name = entry.DisplayName };
        }

        return new
        {
            user_name = entry.DisplayName,
            profile = profile.Profile,
            updated_at = FormatDate(profile.UpdatedAt),
        };
    }

    private object HandleGetUserStats(JsonNode? args)
    {
        if (!TryResolve(args, out var entry, out var error)) return error;

        return new
        {
            user_name = entry.DisplayName,
            karma = entry.Karma,
            warns = entry.Warns,
            total_messages = entry.TotalMessages,
            total_bad_words = entry.TotalBadWords,
        };
    }

    private async Task<object> HandleGetUserMessagesAsync(JsonNode? args)
    {
        if (!TryResolve(args, out var entry, out var error)) return error;

        var count = Math.Clamp(GetInt(args, "count", DefaultUserMessages), 1, MaxUserMessages);
        var rows = await _chatMessageRepository.GetLastNByUserAsync(_chatId, entry.UserId, count);

        return new
        {
            user_name = entry.DisplayName,
            messages = rows.Select(m => new
            {
                text = Truncate(m.Text),
                created_at = FormatDate(m.CreatedAt),
            }).ToList(),
        };
    }

    private async Task<object> HandleSearchMessagesAsync(JsonNode? args)
    {
        var query = GetString(args, "query").Trim();

        if (query.Length == 0)
        {
            return new { error = "empty_query" };
        }

        var count = Math.Clamp(GetInt(args, "count", DefaultSearchResults), 1, MaxSearchResults);
        var rows = await _chatMessageRepository.SearchAsync(_chatId, query, count);

        return new
        {
            query,
            messages = rows.Select(m => new
            {
                user_name = _roster.FirstOrDefault(r => r.UserId == m.UserId)?.DisplayName ?? m.UserName,
                text = Truncate(m.Text),
                created_at = FormatDate(m.CreatedAt),
            }).ToList(),
        };
    }

    private bool TryResolve(JsonNode? args, out RosterEntry entry, out object error)
    {
        var resolution = UserResolver.Resolve(_roster, GetString(args, "user_name"));

        if (resolution.Entry is not null)
        {
            entry = resolution.Entry;
            error = null!;
            return true;
        }

        entry = null!;

        // Навмисно if/else, а не тернарник: гілки повертають РІЗНІ анонімні типи,
        // спільного типу для них компілятор не виведе.
        if (resolution.IsAmbiguous)
        {
            error = new { error = "ambiguous", candidates = resolution.Candidates };
        }
        else
        {
            error = new { error = "not_found", known_users = _roster.Select(r => r.DisplayName).ToList() };
        }

        return false;
    }

    private static FunctionResponse WithPayload(FunctionResponse response, object payload)
    {
        response.Response = JsonSerializer.SerializeToNode(payload) ?? new JsonObject();
        return response;
    }

    private static Schema ObjectSchema(params (string Name, SchemaType Type, string Description, bool Required)[] properties) =>
        new()
        {
            Type = SchemaType.OBJECT.ToString(),
            Properties = properties.ToDictionary(
                p => p.Name,
                p => new Schema { Type = p.Type.ToString(), Description = p.Description }),
            Required = properties.Where(p => p.Required).Select(p => p.Name).ToList(),
        };

    // ToString(), а не GetValue<string>(): модель цілком може прислати число там, де очікується рядок.
    private static string GetString(JsonNode? args, string name) => args?[name]?.ToString() ?? "";

    private static int GetInt(JsonNode? args, string name, int fallback)
    {
        var node = args?[name];

        if (node is null) return fallback;

        return int.TryParse(node.ToString(), out var value) ? value : fallback;
    }

    private static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    private static string Truncate(string text) =>
        text.Length <= MaxTextLength ? text : text[..MaxTextLength] + "…";
}
