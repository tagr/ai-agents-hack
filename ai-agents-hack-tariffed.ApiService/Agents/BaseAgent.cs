using ai_agents_hack_tariffed.ApiService.Data;
using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public abstract class BaseAgent(IAgentParameters parameters, TariffRateDb context) : IAsyncDisposable
    {
        public StringBuilder OutputBuilder { get; } = new();
        protected DbContext? context = context;
        protected AIProjectClient Client { get; } = parameters.ProjectClient;
        protected string ModelName { get; } = parameters.ApiDeploymentName;
        protected AgentsClient? agentClient;
        protected Agent? agent;
        protected AgentThread? thread;

        const int maxCompletionTokens = 20000;
        const int maxPromptTokens = 10240;
        const float temperature = 0.1f;
        const float topP = 0.3f;

        private readonly bool disposeAgent = true;

        public virtual IEnumerable<ToolDefinition> InitializeTools() => [];

        public async Task RunAsync(string agentName, string instructionFilePath, IEnumerable<ToolDefinition> tools, string entities = "")
        {
            this.OutputBuilder.Clear();

            await Console.Out.WriteLineAsync("Creating agent...");
            agentClient = Client.GetAgentsClient();

            await InitializeAsync(agentClient);

            string instructions = entities == string.Empty 
                ? await CreateInstructions(instructionFilePath)
                : await CreateInstructions(instructionFilePath, entities);

            agent = await agentClient.CreateAgentAsync(
                model: ModelName,
                name: agentName,
                instructions: instructions,
                tools: tools,
                temperature: temperature,
                topP: topP
            );

            await Console.Out.WriteLineAsync($"{agentName} created with ID: {agent.Id}");
        }

        public async Task GetResponseAsync(string prompt, string errorContains = "Error:", [CallerMemberName] string callerName = "")
        {
            await Console.Out.WriteLineAsync($"API Request: {prompt}");

            this.thread = await agentClient.CreateThreadAsync();

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

            await foreach (StreamingUpdate update in streamingUpdate)
            {
                string? m = await HandleStreamingUpdateAsync(update);
                OutputBuilder.Append(m);
                if (m.Length > 0) break;
            }
        }

        protected virtual ToolResources? InitializeResources() => null;

        protected async virtual Task<string> CreateInstructions(string path, string entities = "'Hts','Special','TariffRate'")
        {
            string instructions = await File.ReadAllTextAsync(path);

            if (context is null)
            {
                return instructions;
            }

            string schema = await HtsDatabaseTool.GetTariffRateDbSchema(entities, context);
            instructions = instructions.Replace("{database_schema_string}", schema);

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

                case StreamingUpdateReason.MessageCompleted:
                    MessageStatusUpdate messageStatusUpdate = (MessageStatusUpdate)update;
                    ThreadMessage tm = messageStatusUpdate.Value;

                    var contentItems = tm.ContentItems;

                    foreach (MessageContent contentItem in contentItems)
                    {
                        if (contentItem is MessageTextContent content)
                        {
                            var text = content.Text;
                            message = text;
                            break;
                        }
                    }
                    break;
            }

            return message;
        }

        protected virtual AsyncCollectionResult<StreamingUpdate> HandleRequiredAction(RequiredActionUpdate requiredActionUpdate) =>
            throw new NotImplementedException();

        private async Task HandleActionAsync(RequiredActionUpdate requiredActionUpdate)
        {
            AsyncCollectionResult<StreamingUpdate> toolUpdate;

            if (requiredActionUpdate.FunctionName != nameof(HtsDatabaseTool.Query) &&
                requiredActionUpdate.FunctionName != nameof(HtsDatabaseTool.QueryAll))
            {

                toolUpdate = HandleRequiredAction(requiredActionUpdate);
            }
            else if (requiredActionUpdate.FunctionName == nameof(HtsDatabaseTool.Query))
            {
                TariffDatabaseArgs args =
                    JsonConvert.DeserializeObject<TariffDatabaseArgs>(requiredActionUpdate.FunctionArguments)
                    ?? throw new InvalidOperationException("failed to parse json object.");

                string result = await HtsDatabaseTool.Query(args.query, this.context);
                toolUpdate = agentClient.SubmitToolOutputsToStreamAsync(
                    requiredActionUpdate.Value,
                    new List<ToolOutput>([new ToolOutput(requiredActionUpdate.ToolCallId, result)])
                );
            }
            else if (requiredActionUpdate.FunctionName == nameof(HtsDatabaseTool.QueryAll))
            {
                TariffDatabaseArgs args =
                    JsonConvert.DeserializeObject<TariffDatabaseArgs>(requiredActionUpdate.FunctionArguments)
                    ?? throw new InvalidOperationException("failed to parse json object.");

                string result = await HtsDatabaseTool.QueryAll(args.query, this.context);
                toolUpdate = agentClient.SubmitToolOutputsToStreamAsync(
                    requiredActionUpdate.Value,
                    new List<ToolOutput>([new ToolOutput(requiredActionUpdate.ToolCallId, result)])
                );
            }
            else
            {
                toolUpdate = HandleRequiredAction(requiredActionUpdate);
            }

            await foreach (StreamingUpdate update in toolUpdate)
            {
                OutputBuilder.Append(await HandleStreamingUpdateAsync(update));
            }
        }
        public async ValueTask DisposeAsync()
        {
            if (!disposeAgent)
            {
                return;
            }

            try
            {
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
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Exception: {ex.Message}");
            } 
        }

        record TariffDatabaseArgs(string query);
    }
}