using ai_agents_hack_tariffed.ApiService.Data;
using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Grpc.Core;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

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


        private bool disposeAgent = true;

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
            //var response = new ApiResponse
            //{
            //    Success = false
            //};

            //if (agentClient == null || agent == null)
            //{
            //    response.Error = "Agent request was made before initialized";
            //    return response;
            //}

            //if (string.IsNullOrEmpty(prompt) || string.IsNullOrWhiteSpace(prompt))
            //{
            //    response.Error = "Request was empty";
            //    return response;
            //}


                this.thread = await agentClient.CreateThreadAsync();

                var threadMessage = await agentClient.CreateMessageAsync(
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


                var m = string.Empty;
                
                await foreach (StreamingUpdate update in streamingUpdate)
                {
                    m = await HandleStreamingUpdateAsync(update);
                    OutputBuilder.Append(m);

                    if (m.Length > 0) break;
            }

                //Build response
                //response.Message = message.ToString().Contains(errorContains, StringComparison.InvariantCultureIgnoreCase)
                //    ? string.Empty
                //    : message.ToString();
                //response.Error = string.IsNullOrEmpty(message.ToString().Trim())
                //    ? $"Response from agent was empty. Caller: {callerName}"
                //    : message.ToString().Contains(errorContains, StringComparison.InvariantCultureIgnoreCase)
                //        ? message.ToString()
                //        : string.Empty;
                //response.Success = !string.IsNullOrEmpty(message.ToString().Trim())
                //    && !message.ToString().Contains(errorContains, StringComparison.InvariantCultureIgnoreCase);

                //var concat = message.ToString();

                //response.Message = concat.Contains("Error:") ? string.Empty : concat;
                //response.Error = concat.Contains("Error:") ? concat : string.Empty;
                //response.Success = !concat.Contains("Error:") && !string.IsNullOrEmpty(concat);
                //response.ThreadId = thread.Id;

                //return response;

            //}
        }

        protected virtual ToolResources? InitializeResources() => null;

        protected async virtual Task<string> CreateInstructions(string path, string entities = "'Hts','Special','TariffRate'")
        {
            string instructions = File.ReadAllText(path);

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

                //case StreamingUpdateReason.MessageUpdated:
                //    // The agent has a response to the user, potentially requiring some user input
                //    // or further action. This comes as a stream of message content updates.
                //    MessageContentUpdate messageContentUpdate = (MessageContentUpdate)update;
                //    return messageContentUpdate.Text;

                case StreamingUpdateReason.MessageCompleted:
                    MessageStatusUpdate messageStatusUpdate = (MessageStatusUpdate)update;
                    ThreadMessage tm = messageStatusUpdate.Value;

                    var contentItems = tm.ContentItems;

                    foreach (MessageContent contentItem in contentItems)
                    {
                        if (contentItem is MessageTextContent c1)
                        {
                            var ct = c1.Text;
                            message = ct;
                            break;
                        }
                    }
                    break;

                    //case StreamingUpdateReason.RunCompleted:
                    //    await Console.Out.WriteLineAsync();
                    //    break;

                    //case StreamingUpdateReason.RunFailed:
                    //    // The run failed, so we can print the error message.
                    //    RunUpdate runFailedUpdate = (RunUpdate)update;

                    //    if (runFailedUpdate.Value.LastError.Code == "rate_limit_exceeded")
                    //    {
                    //        return runFailedUpdate.Value.LastError.Message;
                    //    }

                    //    message = $"Error: {runFailedUpdate.Value.LastError.Message} (code: {runFailedUpdate.Value.LastError.Code})";
                    //    break;
            }

            return message;
        }

        private async Task DownloadImageFileContentAsync(MessageImageFileContent imageContent)
        {
            if (agentClient is null)
            {
                return;
            }

            Utils.LogGreen($"Getting file with ID: {imageContent.FileId}");

            BinaryData fileContent = await agentClient.GetFileContentAsync(imageContent.FileId);
            string directory = Path.Combine("files");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, imageContent.FileId + ".png");
            await File.WriteAllBytesAsync(filePath, fileContent.ToArray());

            Utils.LogGreen($"File save to {Path.GetFullPath(filePath)}");
        }

        protected virtual AsyncCollectionResult<StreamingUpdate> HandleRequiredAction(RequiredActionUpdate requiredActionUpdate) =>
            throw new NotImplementedException();

        private async Task HandleActionAsync(RequiredActionUpdate requiredActionUpdate)
        {
            //if (agentClient is null)
            //{
            //    return;
            //}

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

