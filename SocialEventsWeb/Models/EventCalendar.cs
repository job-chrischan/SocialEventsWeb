using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocialEventsWeb.ChatWorkfow;

namespace SocialEventsWeb.Models
{


    public class EventCalendar
    {

        public List<OutlookEvent> EventCalendars;

        public EventCalendar()
        {
            EventCalendars = [];
        }

        public EventCalendar(string JsonValue)
        {
            JObject jsonObject = JObject.Parse(JsonValue);

            // Extract the "value" property and deserialize it
            var events = jsonObject["value"].ToObject<List<OutlookEvent>>();

            foreach (var OutlookEvent in events)
            {
                Console.WriteLine($"Subject: {OutlookEvent.subject}");
                Console.WriteLine($"Start: {OutlookEvent.start} ({OutlookEvent.start})");
                Console.WriteLine($"End: {OutlookEvent.end} ({OutlookEvent.end})");
            }
        }

        static List<OutlookEvent> GetEventsAvailable(List<OutlookEvent> ExistingEvents, List<OutlookEvent> PotentialEvents)
        {
            //Find events in PotentialEvents that do not have date/time overlaps with the ExistingEvents
            return PotentialEvents
                .Where(evt2 => !ExistingEvents.Any(evt1 => evt2.start < evt1.end && evt2.end > evt1.start))
                .ToList();
        }

        static List<OutlookEvent> GetEventClashes(List<OutlookEvent> ExistingEvents, List<OutlookEvent> PotentialEvents)
        {
            //Find events in PotentialEvents that do not have date/time overlaps with the ExistingEvents
            return PotentialEvents
                .Where(evt2 => ExistingEvents.Any(evt1 => evt2.start < evt1.end && evt2.end > evt1.start))
                .ToList();

        }
    }

    public class OutlookEvent
    {
        public required string subject { get; set; }
        public Body body { get; set; }


        public required DateTime start { get; set; }
        public required DateTime end { get; set; }
        public Location location {  get; set; }
    }

    public class Body
    {
        public string contentType { get; set; } //html
        public string content { get; set; }
    }
    public class Location 
    { 
        public string displayName { get; set; }
        public string locationType { get; set; }
        public Address address { get; set; }
        public Coordinates coordinates { get; set; }
    }

    public class Address
    {
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string countryOrRegion { get; set; }
        public string postalCode { get; set; }
    }
    public class Coordinates
    { 
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

}