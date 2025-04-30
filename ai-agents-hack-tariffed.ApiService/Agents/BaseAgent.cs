using ai_agents_hack_tariffed.ApiService.Data;
using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ClientModel;
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

        /// <summary>
        /// Executes the process of creating and initializing an agent with the specified configuration and tools.
        /// </summary>
        /// <remarks>This method clears the current output, initializes the agent client, and creates an
        /// agent with the specified  configuration. The agent's instructions are generated based on the provided
        /// instruction file and optional entities.</remarks>
        /// <param name="agentName">The name to assign to the agent being created.</param>
        /// <param name="instructionFilePath">The file path to the instruction file that contains the agent's operational guidelines.</param>
        /// <param name="tools">The tools available to the agent.</param>
        /// <param name="entities">An optional string representing additional entities to include in the agent's instructions.  If not provided
        /// or empty, the instructions will be generated without additional entities.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Sends a prompt to the API, processes the response asynchronously, and appends the result to the output
        /// builder.
        /// </summary>
        /// <remarks>This method initiates an API request, creates a thread and message, and streams the
        /// response updates. The response is processed incrementally, and the first meaningful result is appended to
        /// the output builder.</remarks>
        /// <param name="prompt">The input prompt to send to the API. This represents the user's query or request.</param>
        /// <returns></returns>
        public async Task GetResponseAsync(string prompt)
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

        /// <summary>
        /// Handles the specified required action asynchronously, performing the appropriate operation based on the
        /// provided function name and arguments.
        /// </summary>
        /// <remarks>This method determines the appropriate action to take based on the <see
        /// cref="RequiredActionUpdate.FunctionName"/>. It supports specific operations such as querying the database
        /// using <see cref="HtsDatabaseTool.Query"/> or <see cref="HtsDatabaseTool.QueryAll"/>, as well as handling
        /// other required actions through a general handler.  If the function name corresponds to a database query, the
        /// method deserializes the function arguments into <see cref="TariffDatabaseArgs"/> and performs the query. The
        /// results are then submitted to a streaming client for further processing. For other function names, the
        /// method delegates the action to a general handler.  Any updates resulting from the action are processed
        /// asynchronously and appended to the output builder.
        /// </remarks>
        /// <param name="requiredActionUpdate">An object containing details about the required action, including the function name, arguments, and other
        /// contextual information.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the required action has been
        /// processed and any resulting updates have been handled.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the function arguments cannot be deserialized into a valid <see cref="TariffDatabaseArgs"/>
        /// object.</exception>
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

        /// <summary>
        /// Represents the arguments required to query the tariff database.
        /// </summary>
        /// <param name="query">The query string used to search the tariff database. This value cannot be null or empty.</param>
        record TariffDatabaseArgs(string query);
    }
}