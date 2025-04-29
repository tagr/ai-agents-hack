[![Azure AI Agents Hackathon](https://img.shields.io/badge/Azure-AI--Agents--Hackathon-512BD4?style=for-the-badge&logo=microsoft)](https://microsoft.github.io/AI_Agents_Hackathon/)
# AI Agents Hackathon: TARIFFED!

### Project Goal
> The Harmonized Tariff Schedule of the United States (HTS) sets out the tariff rates and statistical categories for all merchandise imported into the United States. 
> Source: United States International Trade Commission - [Harmonized Tariff Schedule](https://hts.usitc.gov/)

International trade and import laws can be bewildering, especially when considering tariffs' impact on day-to-day lives.

This problem, and the dizzying pace of news about trade wars and supply chain disruptions inspired me to apply [Azure AI Agent Service](https://learn.microsoft.com/en-us/azure/ai-services/agents/) to pore over the corpora of tariff schedules to see how these policy changes affect us and the things we use every day. Specifically, I set out to build:

1. Several AI agents using `gpt-4o` to process questions like:
  * *Which countries are the primary importers of goods into the United States?*
  * *What is the potential average rate of duty if tariff policy changes?*
  * *What are some substitute goods that are produced domestically?*
2. A custom SQL database representing the Harmonized Tariff Schedule and potential rates of duty for all countries.
3. A web application to set the agents to work and return the answers to those questions.

### Author
Andy Merhaut (GitHub: [@tagr](https://github.com/tagr))

### Project Video
* TODO

### Samples
![sample1](https://github.com/user-attachments/assets/ade3694e-86df-4417-8a4e-40d8a1917ffa)
*Figure 1. Search results for __silk__*

![sample2](https://github.com/user-attachments/assets/e5226c19-2153-4b90-a598-8bfa30892b2c)
*Figure 2. Search results for __cardamom__*

![sample3](https://github.com/user-attachments/assets/1c5c0c67-97eb-4de2-98d8-d19bde15dd38)
*Figure 3. Substitute recommendations including HTS numbers and Wikipedia links from Bing integration*

![Animation](https://github.com/user-attachments/assets/eaf30a0a-35d5-494f-a832-54e9e13acc84)
*Figure 4. Key stat animation*

### Architecture
![agent-diagram](https://github.com/user-attachments/assets/e1ace6e5-a742-4cf8-a1fe-ded9c5204d77)
*Figure 1. Azure AI Agent Service featuring Grounding with Bing and function tools*

![overall-diagram](https://github.com/user-attachments/assets/1d7a552d-3275-409b-8455-050313007693)
*Figure 2. Overall system diagram with agents and .NET Aspire Blazor web application*

### Technologies Applied
* 🤖 [Azure AI Agent Service](https://learn.microsoft.com/en-us/azure/ai-services/agents/overview)
* 👀 [Grounding with Bing Search](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/bing-grounding?tabs=python&pivots=overview)
* 🟣 [.NET 9 Framework](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)
* 🟣 [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
* 🟣 [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
* 🟣 SQL Server 2022
* 🟣 Visual Studio 2022
* 🟣 [Azure CLI 2.71](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?pivots=msi)
* 🐋 [Docker Desktop 4.40](https://docs.docker.com/desktop/release-notes/#4400)

### Prerequisites
* An active Azure subscription
* A Resource Group to host Azure AI Services
* A project created in Azure [AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/create-projects?tabs=ai-studio)
  * Keep the project connection string for use in the following setup procedure
* Grounding with Bing Search resource provisioned in the resource group and [connected](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/bing-grounding?tabs=python&pivots=overview) to the Azure AI project
  * Also keep the connection name for this handy 

### Setup
* Update `ai-agents-hack-tariffed.AppHost/Properties/launchSettings.json` and modify the `BING_GROUNDING_CONNECTION_NAME` value with your value.
* Update `ai-agents-hack-tariffed.ApiService\appsettings.json` and modify the `AiAgentService` value with the AI Foundry project connection string

> **⚠️ IMPORTANT ⚠️**
> 
> The first time you run the solution, `AppHost` project bootstraps the `ai-agent-hackathon` SQL Server database and migration script `init.sql`.
> 
> Then the `ApiService` starts a background process (instance of `ScopedProcessingService`), which executes stored procedure from that SQL script, `[spSeedHts]`
> The procedure creates tables and populates them, which takes some time (10+ minutes).
>
> I recommend logging into the database using the *ConnectionStrings__ai-agent-hackathon*
> environment variable as "Additional Connection Properties" in SQL Server Management Studio and running a `SELECT` statement on one of the tables every few minutes, or
> until the `ApiService` logs show an entry, "✅ SQL initialization complete."

### Acknowledgements
* ♥️ My family (again)
* 🤖 Aaron Powell's livestream [Build your code-first app with Azure AI Agent Service (.NET)](https://developer.microsoft.com/en-us/reactor/events/25370/). These examples were critical to my understanding of the Azure AI Agent Service
