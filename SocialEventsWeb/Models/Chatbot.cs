using Microsoft.Graph.Models.IdentityGovernance;
using SocialEventsWeb.ChatWorkfow;

namespace SocialEventsWeb.Models
{
    [Serializable]
    public class Chatbot
    {
        public SocialEventWF Workflow;

        public Chatbot()
        {
            Workflow= new SocialEventWF();
        }

    }
}
