using Microsoft.AspNetCore.Mvc;
using SocialEventsWeb.Models;

namespace SocialEventsWeb.Controllers
{
    public abstract class BaseController : Controller
    {

        protected void Flash(string message, string debug = null)
        {
            var alerts = TempData.ContainsKey(Alert.AlertKey) ?
                (List<Alert>)TempData[Alert.AlertKey] :
                new List<Alert>();

            alerts.Add(new Alert
            {
                Message = message,
                Debug = debug
            });

            TempData[Alert.AlertKey] = alerts;
        }
    }
}
