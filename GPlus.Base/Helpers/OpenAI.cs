//using OpenAI.Chat;
//using System.Text.Json;

//namespace GPlus.Base.Helpers;

//internal class OpenAI
//{
//    public string Summary(object data, int charsCount = 500)
//    {
//        string apiKey = Resources.Identifiers.key;
//        var chatClient = new ChatClient(model: "gpt-4o-mini", apiKey: apiKey);

//        string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });

//        var messages = new List<ChatMessage>
//        {
//            new SystemChatMessage("You are a strict summarizer, return plain text only, no bullets, no headings."),
//            new UserChatMessage($"Summarize the following JSON in less than {charsCount} characters. If you cannot fit, prioritize the project and the single-sentence purpose.\n\n{jsonString}")
//        };
//        ChatCompletion completion = chatClient.CompleteChat(messages);
//        string raw = completion.Content[0].Text?.Trim() ?? "";
//        return raw;  
//    }
//}
