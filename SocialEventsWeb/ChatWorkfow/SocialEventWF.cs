using com.sun.tools.@internal.xjc.reader.gbind;
using SocialEventsWeb.Models;
using SocialEventsWeb.Services;

namespace SocialEventsWeb.ChatWorkfow
{
    public class SocialEventWF
    {
        /// <summary>
        /// All the variables act like memory to the context of the conversation.
        /// </summary>
        public List<Models.ChatMessage> Messages { get; set; }

        //Prototype - one one location please
        public string SearchLocation { get; set; }
        public string SearchInterests { get; set; }
        public List<OutlookEvent> ExistingAppointments { get; set; }
        public List<OutlookEvent> FoundEvents { get; set; }
        public List<OutlookEvent> InterestedEvents { get; set; }
        public bool _approvedToBook;

        public Stages _stage = Stages.JustStarted;
        private string _response;
        //private bool

        /// <summary>
        /// Enumeration representing the different stages of this workflow.
        /// The flags represent a mix of different stages of compleness and readiness
        /// </summary>
        [Flags]
        public enum Stages
        {
            ///<summary>User wants to exit the process</summary>
            ///<summary>Getting started</summary>
            WhatsToStop = 1,
            JustStarted = 2,
            ///<summary>Ready to proceed with searching on Events</summary>
            HelpOnSearch = 4,
            HasInterest = 8,
            HasLocations = 16,
            ///<summary>Ready to proceed with picking events</summary>
            SearchEvents = 32,
            SelectedEvents = 64,
            ///<summary>Ready to proceed with processing the Outlook Calendar</summary>
            HelpOnCalendar = 128,
            HasCalendarRead = 256,
            ConfirmCalendarWrite = 512,
            ///<summary>All done</summary>
            Completed = 512
        }
        public SocialEventWF()
        {
            Messages = new List<ChatMessage>();
            Messages.Add(new ChatMessage("Robot", "Welcome to the social events manager, would you like help with finding events to add to your calendar?"));
            SearchLocation = string.Empty;
            SearchInterests = string.Empty;
            ExistingAppointments = new List<OutlookEvent>();
            FoundEvents = new List<OutlookEvent>();
            InterestedEvents = new List<OutlookEvent>();

            _stage = Stages.JustStarted;
        }

        /// <summary>
        /// Readonly property representing the stages of the workflow completeness and readiness
        /// </summary>
        public Stages WorkFlowStage
        {
            get { return _stage; }
        }
        private string StageInitiation()
        {
            _stage |= Stages.JustStarted;
            return "Welcome to the social events manager, would you like help with finding events to add to your calendar?";
        }

        private string StageGetStarted(AiService aiService, string userResponse)
        {
            string interpretation = aiService.GetSentiment(userResponse);
            string response = string.Empty;
            switch (interpretation)
            {
                case "Positive":
                    _stage |= Stages.HelpOnSearch;  //Start Help on search
                    _stage &= ~Stages.JustStarted; //Remove start
                    response += "Great!\n";
                    if (SearchLocation == string.Empty || SearchInterests.Length == 0)
                    {
                        response += "Let's get you started!\nPlease let me know what events may be of interest to you?";
                    }
                    break;
                case "Cancel":
                case "Negative":
                    _stage = Stages.WhatsToStop;
                    response += "If you do change your mind, then do feel free to come back any time.";
                    break;
                case "Neutral":
                case "Confused":
                    response += "The expected response is a yes or no. Please answer \"yes\" if you want to proceed with this process, otherwise \"no\" would exit the process.";
                    break;
            }
            return response;
        }
        private string StageGatherSearch(AiService aiService, string userResponse)
        {

            string response = string.Empty;

            //This workflow only allows you to set the interests once in the workflow.  It is a prototype.
            if ((_stage & Stages.HasInterest) != Stages.HasInterest)
            {
                //Could be a response with a list of interests
                var interests = aiService.GetInterests(userResponse);
                if (interests != null && interests.Length > 0)
                {
                    //Got some interests already
                    _stage |= Stages.HasInterest;
                    SearchInterests = string.Join(", ", interests);
                    response += response.Length > 0 ? "<br>" : "";
                    response += $"The closest match to that is {SearchInterests}.";
                }
            }
            else
            {
                response += "Please enter the categories of events you may find interesting.";
            }

            //This workflow only allows you to set the location once in the workflow.  It is a prototype.
            if ((_stage & Stages.HasLocations) != Stages.HasLocations)
            {
                var location = aiService.GetLocation(userResponse);
                if (location != null && location.Length > 0 && location !="none")
                {
                    //Got some interests
                    _stage |= Stages.HasLocations;
                    SearchLocation = location;
                    response += response.Length > 0 ? "<br>" : "";
                    response += $"The closest match is {SearchLocation}.";
                }
            }
            
            if (Stages.HasInterest == (_stage & Stages.HasInterest) && (_stage & Stages.HasLocations) != Stages.HasLocations)
            {
                //Show another message as the interests are already present
                response += response.Length > 0 ? "<br>" : "";
                response += "Please enter the location around where I shall search for events.";
            }

            var phaseCompleteness = Stages.HelpOnSearch | Stages.HasLocations | Stages.HasInterest;

            //Are all the parts of this stage done?
            if (phaseCompleteness == (_stage & phaseCompleteness))
            {
                _stage |= Stages.SearchEvents; //Add event search and selection phase
                _stage &= ~Stages.HelpOnSearch; //Remove Search


                //Search for Events
                var b = new BriteEventService();
                var foundEvents =  b.FindEvents(SearchInterests, SearchLocation);
                if (foundEvents.EventCalendars.Count > 0)
                {
                    response += "The following events were found, are you interested in any of them?<hr><table class=\"table  table-sm table-striped\"><tbody>";
                    for (int i = 0; i < foundEvents.EventCalendars.Count; i++)
                    {
                        string index = (i + 1).ToString();
                        response += "<tr>";
                        response += "<td>" + index.ToString() + "</td><td>" + foundEvents.EventCalendars[i].subject + "</td><td>" + foundEvents.EventCalendars[i].body.content + "</td>";
                        response += "<tr>";
                    }
                    response += "</tbody></table>";
                    return response;
                }
                //Found nothing, carry on searching
                response += "No events found, would you try to search again on different interests and locations?";

            }


            //if (_stage  Stages.HelpOnSearch | Stages.HasLocations | Stages.HasInterest)
            //        Permissions userPermissions = Permissions.Read | Permissions.Write;

            //if (_stage == Stages.WantsHelp)
            //    interpretation = new OpenNlpService().GetLocation(userResponse);
            //if (interpretation.Length > 0)
            //{
            //    //Location selected / changed
            //    SearchLocation = interpretation;
            //}
            //else
            //{
            //    response += "No location was mentioned, expecting If you do change your mind, then do feel free to come back any time.";
            //}

            //response = "Please enter your areas of interest or if you are not sure, I could provide a list of categories.";
            return response;
            //FindEvents();
        }
        private string StagePickEvents(AiService aiService, string userResponse)
        {
            //string response = string.Empty;

            ////Try to see if the user has picked the events
            //var interpretation = aiService.GetChosenEvents(userResponse);

            //if (InterestedEvents != null && InterestedEvents.Count > 0)
            //{
            //    //User is affirming that the selected events are good to go.
            //    response += "Great! Would you like help to add these to add these to your Outlook Calendar?";
            //}
            //else
            //{
            //    response = "Would you like to make another selection?";
            //}


            ////Try to get the picked events
            //var pickedEvents = aiService.GetChosenEvents(userResponse, EventBriteEvent[] listEvents);
            //if ((_stage | Stages.SelectedEvents) == Stages.SelectedEvents)
            //{
            //    if (pickedEvents.Count > 0)
            //    {
            //        string eventList = string.Empty;
            //        int i = 0;
            //        foreach (var pickedEvent in pickedEvents)
            //        {
            //            i++;
            //            if (eventList.Length > 0) { eventList += "\n"; }
            //            eventList += i.ToString() + "." + pickedEvent.name;
            //        }
            //        response += "You picked the folowing:\n" + eventList + "Is this correct?";
            //        InterestedEvents = pickedEvents;
            //        _stage = Stages.SelectedEvents;
            //    }
                return null;
        }


        private string StageConfirmCalendar(AiService aiService, string userResponse)
        {
            string response = string.Empty;
            return response;
        }

        /// <summary>
        /// Method that handles the generation of the responses back to the user.
        /// Works like a giant IF statement to figure out where the user is in the "workflow"
        /// And responds accordingly.
        /// <param name="userResponse">The language response by the user.</param>
        /// <returns>Returns the chat response by the machine to the user.</returns>
        /// </summary>
        public string PostResponse(string userResponse)
        {

            var aiService = new AiService();

            //Figure out the stage, and try to process that piece.
            switch (_stage & (Stages.JustStarted | Stages.HelpOnSearch | Stages.SearchEvents | Stages.HelpOnCalendar))
            {
                case Stages.JustStarted:
                    //Check response for "Welcome to the social events manager, would you like help with finding events to add to your calendar?"
                    _response = StageGetStarted(aiService, userResponse);
                    return _response;
                    break;
                case Stages.HelpOnSearch:

                    //check response for "Please enter your areas of interest or if you are not sure, I could provide a list of categories.";
                    _response = StageGatherSearch(aiService, userResponse);
                    return _response;
                    break;
                case Stages.SearchEvents:

                    _response = StagePickEvents(aiService, userResponse);
                    return _response;

                    //CheckCalendar();
                    break;
                case Stages.HelpOnCalendar:
                    _response = StageConfirmCalendar(aiService, userResponse);
                    return _response;

                    //Exit();
                    break;
                default:
                    _response = "I didn't understand that, could you re-phrase that please?";
                    return _response;
                    //StartConversation();
                    break;
            }
            return null;
        }
    }
}
