using System.ComponentModel;
using Serilog;
using Azure.AI.OpenAI;
using java.util;
using opennlp.tools.doccat;
using opennlp.tools.sentdetect;
using opennlp.tools.util;
using opennlp.tools.namefind;
using opennlp.tools.tokenize;
using SocialEventsWeb.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


namespace SocialEventsWeb.Services
{
    public class AiService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly DocumentCategorizerME _categorizer;
        private readonly NameFinderME _locationDetector;
        private readonly SentenceDetectorME _sentenceDetector;
        private readonly TokenizerME _tokenizer;

        private const string LOCATIONMODEL = "./Services/OpenNlpModels/en-ner-location.bin";
        private const string SENTENCEMODEL = "./Services/OpenNlpModels/opennlp-en-ud-ewt-sentence-1.0-1.9.3.bin";
        private const string SENTIMENTMODEL = "./Services/OpenNlpModels/Sentiment.bin";
        private const string TOKENMODEL = "./Services/OpenNlpModels/opennlp-en-ud-ewt-tokens-1.0-1.9.3.bin";


        /// <summary>
        /// Default constructor.
        /// </summary>
        public AiService() : this(LOCATIONMODEL, SENTENCEMODEL, SENTIMENTMODEL, TOKENMODEL)
        {
        }
        /// <summary>
        /// Constructor, taking in the parameters to load the model data used for the Open NLP library, used for Testing
        /// </summary>
        /// <param name="locationModelFile">The token model file.</param>
        /// <param name="sentenceModelFile">The sentence model file.</param>
        /// <param name="sentimentModelFile">The document categorizer file for sentiment.</param>
        /// <param name="tokenModelFile">The token model file.</param>
        public AiService(string locationModelFile, string sentenceModelFile, string sentimentModelFile, string tokenModelFile)
        {        
            _apiKey = AiServiceOptions.ApiKey;
            _model = AiServiceOptions.Model;

            //_apiKey = _configuration["PrivateSettings:oa:ApiKey"];
            //_model = "gpt-3.5-turbo"; //uses model 3.5 because it is free and limitless, albeit dated.

            Log.Information("Constructor {0} called with parameters: {1},{2},{3},{4}", "AiService", locationModelFile, sentenceModelFile, sentimentModelFile, tokenModelFile);
            // Load models for sentence detection and tokenization
            var locaModel = new TokenNameFinderModel(new java.io.FileInputStream(locationModelFile));
            _locationDetector = new NameFinderME(locaModel);

            // Load models for sentence detection and tokenization
            var sentModel = new SentenceModel(new java.io.FileInputStream(sentenceModelFile));
            _sentenceDetector = new SentenceDetectorME(sentModel);

            // Load the model for document categorization
            var docModel = new DoccatModel(new java.io.FileInputStream(sentimentModelFile));
            _categorizer = new DocumentCategorizerME(docModel);

            // Load models for tokenization
            var tokenModel = new TokenizerModel(new java.io.FileInputStream(tokenModelFile));
            _tokenizer = new TokenizerME(tokenModel);
            Log.Information("Constructor {0} completed", "AiService");
        }

        /// <summary>
        /// Method to call the OpenAI and ask anything.
        /// </summary>
        /// <param name="prompt">The input statement to ask.</param>
        /// <returns>This method returns the output from OpenAI.</returns>
        public async Task<string> AskAsync(string prompt)
        {
            //string humanSays = prompt;
            //return humanSays;

            Log.Information("Method {0} called with parameter: {1}", "OpenAiService.AskAsync", prompt);

            var client = new OpenAIClient(_apiKey, new OpenAIClientOptions());
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _model,
                Messages =
                {
                    new ChatRequestUserMessage(prompt)
                }
            };

            string skynetSays = "";
            await foreach (StreamingChatCompletionsUpdate chatUpdate in client.GetChatCompletionsStreaming(chatCompletionsOptions))
            {
                //if (chatUpdate.Role.HasValue)
                //{
                //    skynetSays += ($"{chatUpdate.Role.Value.ToString().ToUpperInvariant()}: ");
                //}
                if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
                {
                    skynetSays += (chatUpdate.ContentUpdate);
                }
            }
            Log.Information("Method {0} completed response: ", "AiService.AskAsync", skynetSays);
            return skynetSays;
        }




        /// <summary>
        /// This method is used to generate the binary file with the model data based on the training data.  For the 
        /// DocumentCategorizer, this method should be re-run if the training data is altered, so that the binary file 
        /// could be updated.
        /// </summary>
        /// <returns>This method returns whether the model file has been successfully generated</returns>
        public bool CreateTrainingFile()
        {
            // Locations of training file and output model file
            string trainingFilePath = "./Data/Sentiment.tsv";

            return CreateTrainingFile(trainingFilePath, SENTIMENTMODEL);
        }


        /// <summary>
        /// This method is used to generate the binary file with the model data based on the training data.  For the 
        /// DocumentCategorizer, this method should be re-run if the training data is altered, so that the binary file 
        /// could be updated.
        /// </summary>
        /// <param name="trainingFilePath">The input training file to convert to a model file</param>
        /// <param name="modelFilePath">The output model file.</param>
        /// <returns>This method returns whether the model file has been successfully generated</returns>
        public bool CreateTrainingFile(
            [Description("The input training file to convert to a model file.")]
            string trainingFilePath,
            [Description("The output model file.")]
            string modelFilePath)
        {
            Log.Information("Method {0} called with parameters: {1}, {2}", "AiService.CreateTrainingFile", trainingFilePath, modelFilePath);

            try
            {
                // Read the training file
                ObjectStream objectStream = new PlainTextByLineStream(new MarkableFileInputStreamFactory(new java.io.File(trainingFilePath)), java.nio.charset.StandardCharsets.UTF_8);
                var sampleStream = new DocumentSampleStream(objectStream);

                // Perform the model training.
                var model = DocumentCategorizerME.train("en", sampleStream, TrainingParameters.defaultParams(), new DoccatFactory());

                // Save the model file.
                using (var modelOut = new java.io.BufferedOutputStream(new java.io.FileOutputStream(modelFilePath)))
                {
                    model.serialize(modelOut);
                }
                Log.Information("Method {0} completed", "AiService.CreateTrainingFile");
                return true;
            }
            catch (Exception e)
            {
                Log.Information("Method {0} failed: {1}", "AiService.CreateTrainingFile", e.Message);
                return false;
            }
        }



        /// <summary>
        /// This method assesses the input text and determines whether what location has been entered 
        /// the training data to determine location.  If more that one location has been entered, it would try to identify if the statement is novel, then it will refer to an 
        /// external AI to determine a result.
        /// </summary>
        /// <param name="documentText">The statement to be assessed, which may contain interested categories.</param>
        /// <param name="eventsList">The list of events to choose from.</param>
        /// <returns>All interested categories as matched up with the avilable list</returns>
        public EventCalendar GetChosenEvents(
            [Description("The statement to be assessed to find the interested category")]
            string documentText,
            [Description("The statement to be assessed to find the interested category")]
            EventCalendar eventCalendar)
        {
            Log.Information("Method {0} called with parameters: {1}", "AiService.GetEvents", documentText);

            //TODO Build own interpreter instead of using OpenAI.

            //Create string list of events 
            string eventList = string.Empty;
            for (int i = 0; i < eventCalendar.EventCalendars.Count; i++)
            {
                if (eventList.Length > 0) { eventList += ","; }
                eventList += i.ToString() + "." + eventCalendar.EventCalendars[i].subject;
            }

            //Just going to get OpenAI do this one - it's not perfect though.
            string prompt = $"When a user responds with \"{documentText}\" to a question asking them which event they wanted from choose from this list: ({eventList}). What did numbered lines did they pick, answer with just the numbers only?";
            var response = (AskAsync(prompt)).Result;

            int[] chosenEvents = ExtractNumbers(response);
            if (chosenEvents.Length > 0)
            {
                EventCalendar returnEvents = new EventCalendar();
                for (int i = 0; i < chosenEvents.Length; i++)
                {
                    returnEvents.EventCalendars.Add(eventCalendar.EventCalendars[chosenEvents[i]]);
                }
                return returnEvents;
            }

            return new EventCalendar();
        }

        private int[] ExtractNumbers(string input)
        {
            // Use regular expression to find all numbers in the string
            var matches = Regex.Matches(input, @"\d+");

            // Convert matched numbers to integers
            var numbers = new int[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                numbers[i] = int.Parse(matches[i].Value);
            }

            return numbers;
        }

        /// <summary>
        /// This method assesses the input text and determines whether what location has been entered 
        /// the training data to determine location.  If more that one location has been entered, it would try to identify if the statement is novel, then it will refer to an 
        /// external AI to determine a result.
        /// </summary>
        /// <param name="documentText">The statement to be assessed, which may contain interested categories.</param>
        /// <returns>All interested categories as matched up with the avilable list</returns>
        public string[] GetInterests(
            [Description("The statement to be assessed to find the interested category")]
            string documentText)
        {
            Log.Information("Method {0} called with parameters: {1}", "AiService.GetInterests", documentText);

            //TODO Build own interpreter instead of using OpenAI.


            //Just going to get OpenAI do this one - it's not perfect though.
            string responseCategories = "Business,Food & Drink,Health,Music,Auto,Boat & Air,Charity & Causes,Community,Family & Education,Fashion,Film & Media,Hobbies,Home & Lifestyle,Performing & Visual Arts,Government,Spirituality,School Activities,Science & Tech,Holidays,Sports & Fitness,Travel & Outdoor,Other";
            string prompt = $"When a user responds with \"{documentText}\" to a question asking them about \"Areas of interest and location?\", which of the following comma separated categories ({responseCategories}) most closely match the response, return only a comma separated list of the matching categories in your output and as concisely as possible, e.g. \"Community,Family & Education,Fashion\"?";
            var response = (AskAsync(prompt)).Result;

            //Parse the random format of the response to get the categories
            //Would use LINQ, except for the fuzzy matching on the string.
            var availableCategories = responseCategories.Split(',');
            string matchingCategories = string.Empty;
            for (int i = 0; i < availableCategories.Length; i++)
            {
                if (response.IndexOf(availableCategories[i]) != -1)
                {
                    if (matchingCategories.Length > 0) { matchingCategories += ","; }
                    matchingCategories += availableCategories[i];
                }
            }
            Log.Information("Method {0} completed", "AiService.GetInterests");

            return matchingCategories.Split(',');


        }

        /// <summary>
        /// This method assesses the input text and determines whether what location has been entered 
        /// the training data to determine location.  If more that one location has been entered, it would try to identify if the statement is novel, then it will refer to an 
        /// external AI to determine a result.
        /// </summary>
        /// <param name="documentText">The statement to be assessed, which may contain a location.</param>
        /// <returns>The first location found in the text</returns>
        public string GetLocation(
            [Description("The statement to be assessed to find a location")]
            string documentText)
        {
            Log.Information("Method {0} called with parameters: {1}", "AiService.GetLocation", documentText);
            bool ourAiIGood = false;
            if (ourAiIGood)
            {
                // Tokenize the input
                var tokens = _tokenizer.tokenize(documentText);

                //Find using the name model with locations data
                var spans = _locationDetector.find(tokens);

                foreach (var span in spans)
                {
                    //In case a location name has more than one word - Like New York
                    var locationTokens = Arrays.copyOfRange(tokens, span.getStart(), span.getEnd());
                    var location = string.Join(" ", locationTokens);
                    return location; //First one
                }
            }
            //Theoretically great, and this is an example of how one could build up one's own AI, but the locations in
            //the training file aren't UK specific.  Let's are the OpenAI.
            string prompt = $"Given the response \"{documentText}\", what place has been mentioned with the smallest geographical area - just output the name of the place, if there is any and no other explanation, and if there is no place, just output \"none\".";
            prompt = $"If someone said \"{documentText}\", please check this is place in the UK. If more than one place was said, then just answer with the place that is smaller one, but if this is not a place, then just output \"none\". Please provide just a one word answer, i.e. the name of place"; ;
            var response = (AskAsync(prompt)).Result;
            response.Trim(' ', '.', '!');
            Log.Information("Method {0} completed", "AiService.GetLocation");
            return response == null ? "" : response;

        }


        /// <summary>
        /// This method assesses the input text and determines whether it is positive or negative.  It makes use of
        /// the training data to determine the sentiment.  If  the statement is novel, then it will refer to an 
        /// external AI to determine a result.
        /// </summary>
        /// <param name="documentText">The statement to be assessed.</param>
        /// <returns>This method returns the assessed sentiment, which could be positive, negative, confused, neutral or cancel.</returns>
        public string GetSentiment(
            [Description("The statement to be assessed for positive or negative sentiment")]
            string documentText)
        {
            Log.Information("Method {0} called with parameters: {1}", "AiService.GetSentiment", documentText);
            // Break the document into sentences
            var sentences = _sentenceDetector.sentDetect(documentText);

            // Initialize category counts
            var tokens = _tokenizer.tokenize(sentences[0]);
            var categoryDistribution = _categorizer.categorize(tokens);

            // Choose the category with the highest score
            double[] results = _categorizer.categorize(tokens);
            var category = _categorizer.getBestCategory(results);

            //Check that all results are not equal (i.e. the model failed to identify a probability and all results are equally valid)
            double resultValue = -1.0;
            bool sameResult = true;
            foreach (var result in results)
            {
                if (resultValue >= 0 && resultValue == result)
                {
                    continue;
                }
                else if (resultValue < 0)
                {
                    resultValue = result;
                }
                else if (resultValue != result)
                {
                    sameResult = false;
                    break;
                }
            }
            if (sameResult)
            {
                //Result is undetermined, let's try a higher power OpenAI to resolve the problem, otherwise returned confused
                string prompt = $"When a user responds with \"{documentText}\" to a question that required a yes or no answer, was their answer a yes, no or a confused response? Answer using fewer than 5 words.";
                var response = (AskAsync(prompt)).Result;
                if (response.Contains("yes", StringComparison.CurrentCultureIgnoreCase))
                {
                    return "Positive";
                }
                else
                if (response.Contains("no", StringComparison.CurrentCultureIgnoreCase))
                {
                    return "Negative";
                }
                return "Confused";
            }
            Log.Information("Method {0} completed", "AiService.GetSentiment");
            return category;
        }

    }


    public static class AiServiceOptions
    {
        public static string ApiKey { get; private set; }
        public static string Model { get; private set; }

        public static void LoadConfiguration(IConfiguration configuration)
        {
            // Set the default value or load from configuration
            ApiKey = configuration["OpenAiApi"] == null ? configuration["Private:oa:ApiKey"] : configuration["OpenAiApi"];
            Model = configuration["Model"] == null ? configuration["Private:oa:Model"] : configuration["Model"];
        }
    }


}
