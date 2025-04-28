using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public abstract class BaseAgent(AIProjectClient client, string modelName) : IAsyncDisposable
    {
        protected DbContext? context;
        protected AIProjectClient Client { get; } = client;
        protected abstract string Schema { get; }
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

        private async Task<IEnumerable<ToolDefinition>> SetupTools(AIProjectClient client)
        {
            ConnectionResponse bingConnection = await client.GetConnectionsClient()
                .GetConnectionAsync(Environment.GetEnvironmentVariable("BING_GROUNDING_CONNECTION_NAME"));
            var connectionId = bingConnection.Id;

            ToolConnectionList connectionList = new()
            {
                ConnectionList = { new ToolConnection(connectionId) }
            };
            BingGroundingToolDefinition bingGroundingTool = new(connectionList);

            return [
            new FunctionToolDefinition(
                name: nameof(HtsDatabaseTool.Query),
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
            ),
            new FunctionToolDefinition(
                name: nameof(HtsDatabaseTool.QueryAll),
                description: "This function should query all trade agreements and HTS goods.",
                parameters: BinaryData.FromObjectAsJson(new {
                    Type = "object",
                    Properties = new {
                        Query = new {
                            Type = "string",
                            Description = "The input should be a well-formed T-SQL query selecting all agreements and Harmonized Tariff Schedules. The query result will be returned as a JSON object.."
                        }
                    },
                    Required = new [] { "query" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ),
            bingGroundingTool
        ];
        }

        public async Task RunAsync(DbContext context)
        {
            this.context = context;
            await Console.Out.WriteLineAsync("Creating agent...");
            agentClient = Client.GetAgentsClient();

            await InitializeAsync(agentClient);
         


            IEnumerable<ToolDefinition> tools = await SetupTools(Client);
            ToolResources? toolResources = InitializeResources();

            string instructions = await CreateInstructions();

            agent = await agentClient.CreateAgentAsync(
                model: ModelName,
                name: AgentName,
                instructions: instructions,
                tools: tools,
                temperature: temperature,
                topP: topP,
                toolResources: toolResources,
                metadata: new Dictionary<string, string>
                {
                    { "x-ms-enable-preview", "true" }
                }
            );

            await Console.Out.WriteLineAsync($"{AgentName} created with ID: {agent.Id}");
            await Console.Out.WriteLineAsync("{AgentName} ({agent.Id}) Creating thread...");

            thread = await agentClient.CreateThreadAsync();
            await Console.Out.WriteLineAsync($"{AgentName} ({agent.Id}) Thread created with ID: {thread.Id}");
        }

        public async Task<ApiResponse> GetResponseAsync(string prompt, string errorContains = "Error:", [CallerMemberName] string callerName = "")
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
                //maxCompletionTokens: maxCompletionTokens,
                //maxPromptTokens: maxPromptTokens,
                temperature: temperature,
                topP: topP
            );

                var message = new StringBuilder();

                await foreach (StreamingUpdate update in streamingUpdate)
                {
                    message.Append(await HandleStreamingUpdateAsync(update));
                }

                //Build response
                response.Message = message.ToString().Contains(errorContains, StringComparison.InvariantCultureIgnoreCase)
                    ? string.Empty
                    : message.ToString();
                response.Error = string.IsNullOrEmpty(message.ToString())
                    ? $"Response from agent was empty. Caller: {callerName}"
                    : message.ToString().Contains(errorContains, StringComparison.InvariantCultureIgnoreCase)
                        ? message.ToString()
                        : string.Empty;
                response.Success = !string.IsNullOrEmpty(message.ToString())
                    && !message.ToString().Contains(errorContains, StringComparison.InvariantCultureIgnoreCase);

                return response;

            }
            catch (Exception ex)
            {
                response.Error = $"Error: {ex.Message}";
                return response;
            }
        }

        protected virtual ToolResources? InitializeResources() => null;

        protected async virtual Task<string> CreateInstructions()
        {
            string instructions = File.ReadAllText(InstructionsFileName);

            if (context is null)
            {
                return instructions;
            }

            string schema = await HtsDatabaseTool.GetTariffRateDbSchema(Schema, context);
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

                case StreamingUpdateReason.MessageUpdated:
                    // The agent has a response to the user, potentially requiring some user input
                    // or further action. This comes as a stream of message content updates.
                    MessageContentUpdate messageContentUpdate = (MessageContentUpdate)update;
                    message = messageContentUpdate.Text;
                    break;

                case StreamingUpdateReason.MessageCompleted:
                    MessageStatusUpdate messageStatusUpdate = (MessageStatusUpdate)update;
                    ThreadMessage tm = messageStatusUpdate.Value;

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

        protected virtual AsyncCollectionResult<StreamingUpdate> HandleRequiredAction(RequiredActionUpdate requiredActionUpdate) =>
            throw new NotImplementedException();

        private async Task HandleActionAsync(RequiredActionUpdate requiredActionUpdate)
        {
            if (agentClient is null)
            {
                return;
            }

            AsyncCollectionResult<StreamingUpdate> toolUpdate;
            if (requiredActionUpdate.FunctionName == nameof(HtsDatabaseTool.Query))
            {
                TariffDatabaseArgs args =
                    JsonConvert.DeserializeObject<TariffDatabaseArgs>(requiredActionUpdate.FunctionArguments)
                    ?? throw new InvalidOperationException("failed to parse json object.");

                string result = await HtsDatabaseTool.Query(args.query, context);
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

                string result = await HtsDatabaseTool.QueryAll(args.query, context);
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
                await HandleStreamingUpdateAsync(update);
            }
        }

        public async ValueTask DisposeAsync()
        {
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

        record TariffDatabaseArgs(string query);
    }
}

