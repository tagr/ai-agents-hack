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
2. A custom database representing the Harmonized Tariff Schedule and potential rates of duty for all countries.
3. A web application to set the agents to work and return the answers to those questions.

### Author
Andy Merhaut (GitHub: [@tagr](https://github.com/tagr))

### Technologies Applied
* [Azure AI Agent Service](https://learn.microsoft.com/en-us/azure/ai-services/agents/overview)
* [Grounding with Bing Search](https://learn.microsoft.com/en-us/azure/ai-services/agents/how-to/tools/bing-grounding?tabs=python&pivots=overview)
* [.NET 9 Framework](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)
* [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
* [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
