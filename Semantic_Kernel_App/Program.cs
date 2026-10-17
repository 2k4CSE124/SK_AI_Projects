using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;

namespace Semantic_Kernel_App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Build and get configuration from appsettings.json, environment variables, and user secrets
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>()
                .Build();

            string modelId = config["Ollama_Cre:modelId"];
            string endpoint = config["Ollama_Cre:endpoint"];
            //string apiKey = config["Ollama_Cre:apiKey"];

            //var chatCompletionService=new OllamaApiClient(new Uri(endpoint), modelId).AsChatCompletionService();

            var builder = Kernel.CreateBuilder();
            var ollama = new OllamaApiClient(new Uri(endpoint), modelId);

            builder.AddOllamaChatCompletion(modelId, new Uri(endpoint));
            var kernel = builder.Build();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Create Chat History
            var history = new ChatHistory(systemMessage: "You are a helpful assistant that provides concise and accurate answers to user questions.");

            // Define the settings for the OpenAI prompt execution
            OllamaPromptExecutionSettings settings = new()
            {
                Temperature = 0.9f
            };

            var reducer = new ChatHistoryTruncationReducer(targetCount: 10);
            //var reducer = new ChatHistorySummarizationReducer(chatCompletionService, 2, 2);

            while (true)
            {
                // Get user input
                Console.Write("\n Enter your prompt: ");
                string prompt = Console.ReadLine();

                // Exit the loop if the user input is empty or whitespace
                if (string.IsNullOrWhiteSpace(prompt))
                    break;

                string fullMessage = "";
                OpenAI.Chat.ChatTokenUsage usage = null;

                history.AddUserMessage(prompt);
                // Get streaming response from chat completion service
                await foreach (StreamingChatMessageContent responseChunk in chatCompletionService.GetStreamingChatMessageContentsAsync(history, settings))
                {
                    // Print response to console
                    Console.Write(responseChunk.Content);
                    fullMessage += responseChunk.Content;
                }
                // Add response to chat history
                history.AddAssistantMessage(fullMessage);

                // Reduce chat history if necessary
                var reduceMessages = await reducer.ReduceAsync(history);
                if (reduceMessages is not null)
                    history = new(reduceMessages);

            }
        }
    }
}
