using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System;
using System.ClientModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AstrolonUI.ViewModels.Workflow.Common;

public static class AgentServices
{
    public static async Task WorkAsync(IAIProvider provider, ChatMedium medium, CancellationToken ct)
    {
        switch (provider)
        {
            case IOpenAIProvider openai:
                await RunOpenAIAsync(
                    openai,
                    medium,
                    ct,
                    provider as IChatCallBackProvider,
                    provider);
                break;
        }
    }

    public static async Task RunOpenAIAsync(IOpenAIProvider provider, ChatMedium medium, CancellationToken ct, IChatCallBackProvider? chatCallBack = null, object? callbackSender = null)
    {
        await RunAgentAsync(
            agent: await BuildAgentAsync(provider, medium),
            medium: medium,
            ct: ct,
            chatCallBack: chatCallBack,
            callbackSender: callbackSender ?? provider);
    }

    public static async Task RunAgentAsync(ChatClientAgent agent, ChatMedium medium, CancellationToken ct, IChatCallBackProvider? chatCallBack = null, object? callbackSender = null)
    {
        switch (medium.AllowStreaming)
        {
            case true:
                await AskStreamingAsync(agent, medium, ct, chatCallBack, callbackSender);
                break;
            case false:
                await AskAsync(agent, medium, ct, chatCallBack, callbackSender);
                break;
        }
    }

    public static async Task AskAsync(ChatClientAgent agent, ChatMedium medium, CancellationToken ct, IChatCallBackProvider? chatCallBack = null, object? callbackSender = null)
    {
        try
        {
            var response = await agent.RunAsync(
                message: medium.Message,
                session: medium.Session,
                cancellationToken: ct);
            chatCallBack?.OnSyncResponse?.Invoke(callbackSender, response?.Text ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[ LOG ] Agent Chat Canceled");
            chatCallBack?.OnSyncResponse?.Invoke(callbackSender, "_Canceled._");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ ERROR ] {ex.Message}");
            chatCallBack?.OnSyncResponse?.Invoke(callbackSender, $"_Error: {ex.Message}_");
        }
    }

    public static async Task AskStreamingAsync(ChatClientAgent agent, ChatMedium medium, CancellationToken ct, IChatCallBackProvider? chatCallBack = null, object? callbackSender = null)
    {
        try
        {
            await foreach (var chunk in agent.RunStreamingAsync(
                message: medium.Message,
                session: medium.Session,
                cancellationToken: ct))
            {
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    chatCallBack?.OnStreamChunk?.Invoke(callbackSender, chunk.Text);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[ LOG ] Agent Chat Canceled");
            chatCallBack?.OnSyncResponse?.Invoke(callbackSender, "_Canceled._");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ ERROR ] {ex.Message}");
            chatCallBack?.OnSyncResponse?.Invoke(callbackSender, $"_Error: {ex.Message}_");
        }
    }

    public static async Task<ChatClientAgent> BuildAgentAsync(IOpenAIProvider provider, ChatMedium medium)
    {
        var apiKey = Environment.GetEnvironmentVariable(provider.EnvironmentVariableName) ??
            throw new ArgumentNullException($"Environment variable not set: [{provider.EnvironmentVariableName}]");

        var agent = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(provider.Endpoint)
            })
            .GetChatClient(provider.Model)
            .AsIChatClient()
            .AsAIAgent(
            instructions: medium.Prompt,
            tools: medium.Tools);

        return agent;
    }
}
