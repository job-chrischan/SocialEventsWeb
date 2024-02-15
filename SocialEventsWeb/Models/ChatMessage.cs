using com.sun.xml.@internal.ws.client;
using Microsoft.AspNetCore.Mvc;

namespace SocialEventsWeb.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Text { get; set; }


        public ChatMessage(string sender, string text) {

            Sender = sender;
            Text = text;
        }
    }

}


