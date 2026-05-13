using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SynapseEnqueteManagement.BL.Ai;
using SynapseEnqueteManagement.BL.AiSupport;

namespace SynapseEnqueteManagement.BL.AiQuestionSupport;

public sealed class AiQuestionSupportService(
    ILogger<AiQuestionSupportService> logger)
    : ChatClientAiSupportBase<QuestionSupportChatRequest, QuestionSupportChatReply>(logger),
      IAiQuestionSupportService
{
    protected override List<ChatMessage> BuildMessages(QuestionSupportChatRequest request, ResolvedPlatformAiConfig platformConfig)
    {
        var config   = platformConfig.QuestionSupport;
        var messages = new List<ChatMessage> { new(ChatRole.System, config.SystemPrompt) };

        if (request.IsFirstTurn)
        {
            var questionsBlock = request.ExistingQuestions.Count > 0
                ? string.Join("\n", request.ExistingQuestions.Select((q, i) => $"{i + 1}. {q}"))
                : "(nog geen vragen aangemaakt)";

            var ctx = config.ContextPrompt
                .Replace("{project_name}",        request.ProjectName,        StringComparison.OrdinalIgnoreCase)
                .Replace("{project_description}", request.ProjectDescription, StringComparison.OrdinalIgnoreCase)
                .Replace("{existing_questions}",  questionsBlock,             StringComparison.OrdinalIgnoreCase);

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
}