using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiSupport;
using SynapseEnqueteManagement.BL.AiUsage;
using SynapseEnqueteManagement.BL.Domain.Ai.Entities;

namespace SynapseEnqueteManagement.BL.AiQuestionSupport;

public sealed class AiQuestionSupportService(
    ILogger<AiQuestionSupportService> logger,
    IAiUsageManager usageManager)
    : ChatClientAiSupportBase<QuestionSupportChatRequest, QuestionSupportChatReply>(logger, usageManager),
      IAiQuestionSupportService
{
    protected override AiServiceKind Service => AiServiceKind.QuestionSupport;
    protected override List<ChatMessage> BuildMessages(QuestionSupportChatRequest request, ResolvedPlatformAiConfig platformConfig)
    {
        var config           = platformConfig.QuestionSupport;
        var chatLanguage     = string.IsNullOrWhiteSpace(request.ChatLanguage)     ? "en" : request.ChatLanguage;
        var questionLanguage = string.IsNullOrWhiteSpace(request.QuestionLanguage) ? chatLanguage : request.QuestionLanguage;
        var chatLangName     = GetLanguageName(chatLanguage);
        var questionLangName = GetLanguageName(questionLanguage);

        var systemPrompt = config.SystemPrompt
            .Replace("{chat_language}",     chatLanguage,     StringComparison.OrdinalIgnoreCase)
            .Replace("{question_language}", questionLanguage, StringComparison.OrdinalIgnoreCase);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.System,
                $"You MUST write all chat replies to the admin in {chatLangName}. " +
                $"Any suggested or rewritten survey question text MUST be written in {questionLangName}, " +
                "even when that differs from the chat language. Do not mix languages within a single sentence."),
        };

        if (request.IsFirstTurn)
        {
            var questionsBlock = request.ExistingQuestions.Count > 0
                ? string.Join("\n", request.ExistingQuestions.Select((q, i) => $"{i + 1}. {q}"))
                : "(none)";

            var ctx = config.ContextPrompt
                .Replace("{project_name}",        request.ProjectName,        StringComparison.OrdinalIgnoreCase)
                .Replace("{project_description}", request.ProjectDescription, StringComparison.OrdinalIgnoreCase)
                .Replace("{existing_questions}",  questionsBlock,             StringComparison.OrdinalIgnoreCase)
                .Replace("{chat_language}",       chatLanguage,               StringComparison.OrdinalIgnoreCase)
                .Replace("{question_language}",   questionLanguage,           StringComparison.OrdinalIgnoreCase);

            messages.Add(new ChatMessage(ChatRole.System, ctx));
        }

        foreach (var turn in request.History)
        {
            var role = turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User;
            messages.Add(new ChatMessage(role, turn.Content));
        }

        if (!string.IsNullOrWhiteSpace(request.UserMessage))
            messages.Add(new ChatMessage(ChatRole.User, request.UserMessage));
        else if (request.IsFirstTurn)
            messages.Add(new ChatMessage(ChatRole.User, "start"));

        return messages;
    }

    protected override QuestionSupportChatReply ParseRawReply(string raw, QuestionSupportChatRequest _, ResolvedPlatformAiConfig platformConfig)
    {
        var cleaned = StripCodeFences(raw);
        var json    = ExtractJson(cleaned);

        try
        {
            var aiReply = JsonSerializer.Deserialize<QuestionSupportAiReply>(json, JsonOpts);

            if (aiReply is null || string.IsNullOrWhiteSpace(aiReply.Message))
                return FallbackReply(platformConfig);

            return new QuestionSupportChatReply { AssistantMessage = aiReply.Message };
        }
        catch (JsonException)
        {
            return new QuestionSupportChatReply { AssistantMessage = raw };
        }
    }

    protected override QuestionSupportChatReply FallbackReply(ResolvedPlatformAiConfig platformConfig) =>
        new() { AssistantMessage = platformConfig.QuestionSupport.FallbackMessage };

    private static string GetLanguageName(string langCode)
    {
        try { return System.Globalization.CultureInfo.GetCultureInfo(langCode).EnglishName; }
        catch { return langCode; }
    }
}