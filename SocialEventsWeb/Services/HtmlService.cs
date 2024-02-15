using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models.CallRecords;
using System.Net;

namespace SocialEventsWeb.Services
{
    public class HtmlService
    {
        //private const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3";
        //www.valto.co.uk doesn't like older browser huh?
        private const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";


        /// <summary>
        /// Method to extract the content of a web page given the url.
        /// </summary>
        /// <param name="url">The website to extract content from</param>
        /// <returns>string with the content of the web page</returns>
        /// <exception cref="ArgumentNullException">No url was provided.</exception>
        /// <exception cref="WebException">The contents of the web page was empty.</exception>
        public static string FetchWebPage(string url)
        {
            return Task.Run(() => AsyncFetchWebPage(url).GetAwaiter().GetResult()).Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<string> AsyncFetchWebPage(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            //Errors are thrown up the stack to be handled
            string htmlContent = string.Empty;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", USERAGENT);
                htmlContent = await httpClient.GetStringAsync(url);
            }

            if (string.IsNullOrEmpty(htmlContent))
            {
                throw new WebException("Unable to fetch web page content. Webpage was empty for:" + url);
            }

            return htmlContent;
        }

        private static async Task<string> FetchWebPageContent(string url, string userAgent)
        {
            using (var httpClient = new HttpClient())
            {
                if (userAgent.Length > 0) httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
                return await httpClient.GetStringAsync(url);
            }
        }
    }
}
