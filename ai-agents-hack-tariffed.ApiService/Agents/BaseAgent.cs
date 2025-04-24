using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Projects;
using System.ClientModel;
using System.Text.Json;
using ai_agents_hack_tariffed.ApiService.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ai_agents_hack_tariffed.ApiService.Agents
{


    public abstract class BaseAgent(AIProjectClient client, string modelName) : IAsyncDisposable
    {
        protected AIProjectClient Client { get; } = client;
        protected abstract string AgentName { get; }
        protected string ModelName { get; } = modelName;
        protected AgentsClient? agentClient;
        protected Agent? agent;
        protected AgentThread? thread;

        protected abstract string InstructionsFileName { get; }

        private readonly JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        const int maxCompletionTokens = 4096;
        const int maxPromptTokens = 10240;
        const float temperature = 0.1f;
        const float topP = 0.1f;

        private bool disposeAgent = true;

        public virtual IEnumerable<ToolDefinition> InitializeTools() => [];

        private IEnumerable<ToolDefinition> SetupTools() => [
            new FunctionToolDefinition(
            name: nameof(HtsDatabaseTool.GetCountryTariffRateInfo),
            description: "This function is used to get tariff rates and country information in the HTS database",
            parameters: BinaryData.FromObjectAsJson(new {
                Type = "object",
                Properties = new {
                    Query = new {
                        Type = "string",
                        Description = "The input should be the country representing a major importer of goods and commodities. The query result will be returned as a JSON object."
                    }
                },
                Required = new [] { "query" }
            },
            new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        )
        ];

        public async Task RunAsync()
        {
            await Console.Out.WriteLineAsync("Creating agent...");
            agentClient = Client.GetAgentsClient();

            await InitializeAsync(agentClient);

            IEnumerable<ToolDefinition> tools = SetupTools();
            ToolResources? toolResources = InitializeResources();

            string instructions = CreateInstructions();

            agent = await agentClient.CreateAgentAsync(
                model: ModelName,
                name: AgentName,
                instructions: instructions,
                tools: tools,
                temperature: temperature,
                topP: topP,
                toolResources: toolResources
            );

            await Console.Out.WriteLineAsync($"{AgentName} created with ID: {agent.Id}");
            await Console.Out.WriteLineAsync("{AgentName} ({agent.Id}) Creating thread...");

            thread = await agentClient.CreateThreadAsync();
            await Console.Out.WriteLineAsync($"{AgentName} ({agent.Id}) Thread created with ID: {thread.Id}");
        }

        public async Task<ApiResponse> GetResponseAsync(string prompt)
        {
            await Console.Out.WriteLineAsync($"API Request: {prompt}");
            var response = new ApiResponse
            {
                Success = false
            };

            if (agentClient == null || agent == null || thread == null)
            {
                response.Error = "Agent request was made before initialized";
                return response;
            }

            if (string.IsNullOrEmpty(prompt) || string.IsNullOrWhiteSpace(prompt))
            {
                response.Error = "Request was empty";
                return response;
            }

            try
            {
                _ = await agentClient.CreateMessageAsync(
                    threadId: thread.Id,
                    role: MessageRole.User,
                    content: prompt
                );

                AsyncCollectionResult<StreamingUpdate> streamingUpdate = agentClient.CreateRunStreamingAsync(
                threadId: thread.Id,
                assistantId: agent.Id,
                maxCompletionTokens: maxCompletionTokens,
                maxPromptTokens: maxPromptTokens,
                temperature: temperature,
                topP: topP
            );

                var message = new StringBuilder();

                await foreach (StreamingUpdate update in streamingUpdate)
                {
                    message.Append(await HandleStreamingUpdateAsync(update));
                }

                //Build response
                response.Message = message.ToString().Contains("Error:") ? string.Empty : message.ToString();
                response.Error = message.ToString().Contains("Error:") ? message.ToString() : string.Empty;
                response.Success = !message.ToString().Contains("Error:");

                return response;

            }
            catch (Exception ex)
            {
                response.Error = $"Error: {ex.Message}";
                return response;
            }
        }

        protected virtual ToolResources? InitializeResources() => null;

        protected virtual string CreateInstructions()
        {
            string instructions = File.ReadAllText(InstructionsFileName);

            return instructions;
        }

        protected virtual Task InitializeAsync(AgentsClient agentClient) => Task.CompletedTask;

        private async Task<string> HandleStreamingUpdateAsync(StreamingUpdate update)
        {
            var message = string.Empty;

            switch (update.UpdateKind)
            {
                case StreamingUpdateReason.RunRequiresAction:
                    // The run requires an action from the application, such as a tool output submission.
                    // This is where the application can handle the action.
                    RequiredActionUpdate requiredActionUpdate = (RequiredActionUpdate)update;
                    await HandleActionAsync(requiredActionUpdate);
                    break;

                case StreamingUpdateReason.MessageUpdated:
                    // The agent has a response to the user, potentially requiring some user input
                    // or further action. This comes as a stream of message content updates.
                    MessageContentUpdate messageContentUpdate = (MessageContentUpdate)update;
                    message = messageContentUpdate.Text;
                    break;

                case StreamingUpdateReason.MessageCompleted:
                    MessageStatusUpdate messageStatusUpdate = (MessageStatusUpdate)update;
                    ThreadMessage tm = messageStatusUpdate.Value;

                    var contentItems = tm.ContentItems;

                    foreach (MessageContent contentItem in contentItems)
                    {
                        if (contentItem is MessageImageFileContent imageContent)
                        {
                            await DownloadImageFileContentAsync(imageContent);
                        }
                    }
                    break;

                case StreamingUpdateReason.RunCompleted:
                    break;

                case StreamingUpdateReason.RunFailed:
                    // The run failed, so we can print the error message.
                    RunUpdate runFailedUpdate = (RunUpdate)update;

                    if (runFailedUpdate.Value.LastError.Code == "rate_limit_exceeded")
                    {
                        return runFailedUpdate.Value.LastError.Message;
                    }

                    message = $"Error: {runFailedUpdate.Value.LastError.Message} (code: {runFailedUpdate.Value.LastError.Code})";
                    break;
            }

            return message;
        }

        private async Task DownloadImageFileContentAsync(MessageImageFileContent imageContent)
        {
            if (agentClient is null)
            {
                return;
            }

            //Utils.LogGreen($"Getting file with ID: {imageContent.FileId}");

            //BinaryData fileContent = await agentClient.GetFileContentAsync(imageContent.FileId);
            //string directory = Path.Combine(SharedPath, "files");
            //if (!Directory.Exists(directory))
            //{
            //    Directory.CreateDirectory(directory);
            //}

            //string filePath = Path.Combine(directory, imageContent.FileId + ".png");
            //await File.WriteAllBytesAsync(filePath, fileContent.ToArray());

            Utils.LogGreen($"File save to ");
        }

        protected virtual AsyncCollectionResult<StreamingUpdate> HandleRequiredAction(RequiredActionUpdate requiredActionUpdate) =>
            throw new NotImplementedException();

        private async Task HandleActionAsync(RequiredActionUpdate requiredActionUpdate)
        {
            if (agentClient is null)
            {
                return;
            }

            //AsyncCollectionResult<StreamingUpdate> toolOutputUpdate;
            //if (requiredActionUpdate.FunctionName != nameof(HtsDatabaseTool.GetCountryTariffRateInfo))
            //{
            //    toolOutputUpdate = HandleRequiredAction(requiredActionUpdate);
            //}
            //else
            //{
            //    GetCountryTariffRateInfoArgs args = JsonSerializer.Deserialize<GetCountryTariffRateInfoArgs>(requiredActionUpdate.FunctionArguments, options) ?? throw new InvalidOperationException("Failed to parse JSON object.");
            //    string result = await HtsDatabaseTool.(args.country);
            //    toolOutputUpdate = agentClient.SubmitToolOutputsToStreamAsync(
            //        requiredActionUpdate.Value,
            //        new List<ToolOutput>([new ToolOutput(requiredActionUpdate.ToolCallId, result)])
            //    );
            //}

            //await foreach (StreamingUpdate toolUpdate in toolOutputUpdate)
            //{
            //    await HandleStreamingUpdateAsync(toolUpdate);
            //}
        }

        public async ValueTask DisposeAsync()
        {
            //SalesData.Dispose();

            if (!disposeAgent)
            {
                return;
            }

            if (agentClient is not null)
            {
                if (thread is not null)
                {
                    await agentClient.DeleteThreadAsync(thread.Id);
                }

                if (agent is not null)
                {
                    await agentClient.DeleteAgentAsync(agent.Id);
                }
            }
        }

        record GetCountryTariffRateInfoArgs(string country);
    }
}

