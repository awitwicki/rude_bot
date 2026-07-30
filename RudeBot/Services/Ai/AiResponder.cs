using GenerativeAI;
using GenerativeAI.Core;
using Microsoft.Extensions.Logging;
using RudeBot.Domain;
using RudeBot.Managers;
using RudeBot.Services.ChatContextService;
using RudeBot.Services.UserProfileService;

namespace RudeBot.Services.Ai;

public class AiResponder : IAiResponder
{
    private readonly IUserManager _userManager;
    private readonly IUserProfileService _userProfileService;
    private readonly IChatContextService _chatContextService;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ILogger<AiResponder> _logger;

    public AiResponder(
        IUserManager userManager,
        IUserProfileService userProfileService,
        IChatContextService chatContextService,
        IChatMessageRepository chatMessageRepository,
        ILogger<AiResponder> logger)
    {
        _userManager = userManager;
        _userProfileService = userProfileService;
        _chatContextService = chatContextService;
        _chatMessageRepository = chatMessageRepository;
        _logger = logger;
    }

    public async Task<string> RespondAsync(
        long chatId,
        long userId,
        string userName,
        string message,
        CancellationToken cancellationToken = default)
    {
        // Sequential on purpose: these three services share one request-scoped DataContext
        // (all InstancePerLifetimeScope in Program.cs). Task.WhenAll here would race concurrent
        // EF Core operations on the same context instance and throw InvalidOperationException.
        var context = await _chatContextService.GetMessagesAsync(chatId);
        var stats = await _userManager.GetAllUsersChatStats(chatId);
        var profiles = await _userProfileService.ListForChatAsync(chatId);

        var roster = RosterBuilder.Build(stats, profiles, context, Consts.BotUserId);

        var prompt = AiPromptBuilder.Build(
            userId, userName, message, context, roster, Consts.BotUserId, Consts.CreatorId);

        var googleAi = new GoogleAi(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_API_KEY")!);
        var model = googleAi.CreateGenerativeModel(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_MODEL_NAME")!);

        var tool = new ChatRagTool(chatId, roster, _chatMessageRepository, _userProfileService, _logger);

        model.AddFunctionTool(tool, functionCallingBehaviour: new FunctionCallingBehaviour
        {
            FunctionEnabled = true,
            AutoCallFunction = true,
            AutoReplyFunction = true,
            AutoHandleBadFunctionCalls = true,
        });

        var response = await model.GenerateContentAsync(prompt, cancellationToken);

        return response.Text();
    }
}
