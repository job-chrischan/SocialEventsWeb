using System.Text.Json;
using com.sun.tools.javadoc;
using com.sun.xml.@internal.bind.v2.runtime;
using HtmlAgilityPack;
using javax.security.auth;
using SocialEventsWeb.Models;
using System.Threading.Tasks;
using System.Security.Policy;

namespace SocialEventsWeb.Services
{
    public class BriteEventService
    {
        private List<EventBriteEvent>? _eventBriteEvents;

        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public EventCalendar FindEvents(string Interests, string Location)
        {
            string startDate = DateTime.Today.ToString("yyyy-MM-dd");
            string endDate = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd");


            EventCalendar found = new EventCalendar();
            string[] Interest = Interests.Split(',');
            for (int i = 0; i < Interest.Length; i++)
            { 
                string url = $"https://www.eventbrite.co.uk/d/united-kingdom--{Location}/{Interests[i]}--events/?page=2&start_date={startDate}&end_date={endDate}";
                if (i == 0)
                {
                    found = ProcessWebSearch(url);
                }
                else
                {
                    var newFound = ProcessWebSearch(url);
                    foreach (var item in newFound.EventCalendars)
                    { 
                        found.EventCalendars.Add(item);
                    }
                }

            }
            return found;
        }

        /// <summary>
        /// Method to convert the BriteEvent html page to an Outlook Calendar with events.
        /// It's complex, because it looks up the web page, extracts the contents, and parses it.
        /// </summary>
        /// <param name="url">The web page to process</param>
        /// <returns>An Outlook version of the BriteEvent calendar</returns>
        public EventCalendar ProcessWebSearch(string url)
        {
            string html = HtmlService.FetchWebPage(url);
            _eventBriteEvents = ParseJson(ParseHtml(html));
            return ToOutlookEvent();
        }

        private string ParseHtml(string html)
        {
            string serverDataValue = ExtractValue(html, "window.__SERVER_DATA__");
            Console.Write(serverDataValue);
            if (serverDataValue != null)  return serverDataValue;
            return null;
        }

        private List<EventBriteEvent> ParseJson(string json)
        {

            BriteDiscoveryData discoveryData = JsonSerializer.Deserialize<BriteDiscoveryData>(json: json, options: _options);

            if (discoveryData != null && discoveryData.jsonld != null)
            {
                List<EventBriteEvent> eventBriteEvents = discoveryData.jsonld.ToList();
                if (eventBriteEvents != null) { return eventBriteEvents; }

            }
            return null;
        }

        private string ExtractValue(string html, string valueName)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find the script tag containing window.__SERVER_DATA__
            var scriptNode = doc.DocumentNode.SelectSingleNode("//script[contains(text(),'" + valueName + "')]");

            if (scriptNode != null)
            {

                // Extract the value using a regular expression or other parsing logic
                string regSearch = valueName + @"\s*=\s*(.*?)(?=;|$)";
                regSearch = valueName + @" = ({.*?});";
                var match = System.Text.RegularExpressions.Regex.Match(scriptNode.InnerText, regSearch);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Method to convert the BriteEvent to an Outlook type class for serialization later.
        /// </summary>
        /// <returns>An Outlook version of the BriteEvent calendar</returns>
        private EventCalendar ToOutlookEvent()
        {
            EventCalendar calendar = new EventCalendar();
            var outlookEvents = calendar.EventCalendars;
            for (int i = 0; i < _eventBriteEvents.Count - 1; i++)
            {
                var eventBriteEvent = _eventBriteEvents[i];
                //Create the description in the Outlook format
                string combinedDescription = string.Empty;
                if (eventBriteEvent.description != null && eventBriteEvent.url != null && eventBriteEvent.description.Length > 0 && eventBriteEvent.url.Length > 0)
                {
                    //head
                    combinedDescription = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head><body>";
                    //description text
                    if (eventBriteEvent.description.Length > 0)
                    {
                        combinedDescription += "<div class=\"elementToProof\" style=\"font-family:Aptos,Aptos_EmbeddedFont,Aptos_MSFontService,Calibri,Helvetica,sans-serif; font-size:12pt; color:rgb(0,0,0)\">" + eventBriteEvent.description + "</div>";
                    }
                    //url
                    if (eventBriteEvent.url.Length > 0)
                    {
                        combinedDescription += "<div class=\"elementToProof\" style=\"font-family:Aptos,Aptos_EmbeddedFont,Aptos_MSFontService,Calibri,Helvetica,sans-serif; font-size:12pt; color:rgb(0,0,0)\"><a href=\"" + eventBriteEvent.url + "\" class=\"OWAAutoLink ContentPasted0\" target=\"_blank\">Link</a><br></div>";
                    }
                    //end
                    combinedDescription += "</body></html>";
                }

                try
                {
                    //Copy different parts of the structure
                    var outlookEvent = new OutlookEvent
                    {
                        subject = eventBriteEvent.name,
                        body = new Body
                        {
                            contentType = "html",
                            content = combinedDescription
                        },
                        start = (DateTime)eventBriteEvent.startDate,
                        end = (DateTime)eventBriteEvent.endDate,
                        location = new Models.Location
                        {
                            displayName = eventBriteEvent.location.name,
                            address = new Address
                            {
                                postalCode = eventBriteEvent.location.postalCode
                            },
                            coordinates = new Coordinates
                            {
                                latitude = eventBriteEvent.location.geo.latitude,
                                longitude = eventBriteEvent.location.geo.longitude
                            }
                        }
                    };
                    outlookEvents.Add(outlookEvent);
                }
                catch (Exception e)
                {
                    e = null;
                }
                
            }

            return calendar == null ? new Models.EventCalendar() : calendar;
        }


        //BriteEvent Model - used internally
        //Would be mapped to the Outlook Version to be used with Graph
        private class BriteDiscoveryData
        {
            public int page_count { get; set; }
            public int page_number { get; set; }
            public EventBriteEvent[] jsonld { get; set; }
        }

        private class EventBriteEvent //: SocialEvent
        {
            public string? name { get; set; }
            public DateTime? startDate { get; set; }
            public DateTime? endDate { get; set; }
            public string url { get; set; }
            public string description { get; set; }
            public Location? location { get; set; }
        }
        private class Location
        {
            public string? name { get; set; }
            public string? postalCode { get; set; }
            public Geo? geo { get; set; }
        }

        private class Geo
        {
            public string? latitude { get; set; }
            public string? longitude { get; set; }

        }
    }
}