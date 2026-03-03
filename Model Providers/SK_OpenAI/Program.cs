using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SK_OpenAI
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

            string modelId = config["OpenAI_Cre:modelId"];
            string apiKey = config["OpenAI_Cre:apiKey"];

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(modelId, apiKey);
            Kernel kernel = builder.Build();

            var history = new ChatHistory();

            // Get the chat completion service from the kernel
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Define the settings for the OpenAI prompt execution
            OpenAIPromptExecutionSettings settings = new()
            {
                ChatSystemPrompt = "You are a helpful assistant that provides concise and accurate answers to user questions.",
                Temperature = 0.9,
                MaxTokens = 100,
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

                //// Get the chat response from the assistant without ChatHistory
                //var response = await chatCompletionService.GetChatMessageContentAsync(prompt, settings);

                // Get the chat response from the assistant with ChatHistory
                history.AddUserMessage(prompt);
                var response = await chatCompletionService.GetChatMessageContentAsync(history, settings);
                history.Add(response);

                Console.WriteLine($"Assistant: {response.Content}");

                var reducerMessage = await reducer.ReduceAsync(history);
                if (reducerMessage is not null)
                {
                    history = new(reducerMessage);
                }
            }
        }
    }
}

//goto www.openai.com => API Platform => Create Project <=
// Created Project => Configuration => API keys => to get your API key and endpoint, 
//then add them to your appsettings.json or user secrets with the following structure:
