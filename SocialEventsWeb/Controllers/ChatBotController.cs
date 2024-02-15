using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocialEventsWeb.ChatWorkfow;
using SocialEventsWeb.Models;
using System.IO;
using System.Text;

namespace SocialEventsWeb.Controllers
{
    public class ChatBotController : BaseController
    {
        private readonly ILogger<ChatBotController> _logger;

        public ChatBotController(ILogger<ChatBotController> logger)
        {
            _logger = logger;
        }

        public Chatbot chatBot;

        public IActionResult Index()
        {
            if (chatBot == null) chatBot = new Chatbot();

            //string serializedObject = Serialize(chatBot);

            //HttpContext.Session.SetString("KeyForObject", serializedObject);

            return View("ChatBot",chatBot);
        }

        private string Serialize(Chatbot chatBot)
        {
            string serializedObject = string.Empty;
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, new UTF8Encoding(false)))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(sw, chatBot);
                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                // Create a StreamReader to read the data from the MemoryStream
                using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
                {
                    // Read the entire content of the MemoryStream
                    return reader.ReadToEnd();
                }
            }
            return null;
        }
        private Chatbot Deserialize(string serializedObject)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(serializedObject)))
            using (var sr = new StreamReader(ms, new UTF8Encoding(false)))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (Chatbot)serializer.Deserialize(sr, typeof(Chatbot));
            }
        }



        [HttpPost]
        public IActionResult SendMessage([FromBody] object message)
        {
            // Process the received message (you can perform any necessary logic here)
            if (HttpContext.Session.TryGetValue("KeyForObject", out byte[] value))
            {
                // The key exists in the session
                chatBot = Deserialize(Encoding.UTF8.GetString(value));
            }
            else
            {
                chatBot = new Chatbot();
            }

            // Parse the JSON string to JObject
            JObject jobject = JObject.Parse(message.ToString());
            // Access the "message" property and get its value
            string messageValue = (string)jobject["message"];


            // Add the server's response to the chatbox content
            chatBot.Workflow.Messages.Add(new Models.ChatMessage("User", messageValue));

            var response = chatBot.Workflow.PostResponse(messageValue);
            chatBot.Workflow.Messages.Add(new Models.ChatMessage("Robot", response));
            var chatboxContent = $@"<div class=""d-flex flex-row justify-content-start mb-4""><img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='32' height='32' fill='currentColor' class='bi bi-chat-left-dots-fill' viewBox='0 0 16 16'%3E%3Cpath d='M0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H4.414a1 1 0 0 0-.707.293L.854 15.146A.5.5 0 0 1 0 14.793zm5 4a1 1 0 1 0-2 0 1 1 0 0 0 2 0m4 0a1 1 0 1 0-2 0 1 1 0 0 0 2 0m3 1a1 1 0 1 0 0-2 1 1 0 0 0 0 2'/%3E%3C/svg%3E"" alt=""Chat Icon""><div class=""p-3 ms-3"" style=""border-radius: 15px; background-color: rgba(57, 192, 237,.2);""><p class=""small mb-0"">{response}</p></div></div>";


            string serializedObject = Serialize(chatBot);
            HttpContext.Session.SetString("KeyForObject", serializedObject);


            // Return the response
            return Ok(chatboxContent);
        }





    }
}