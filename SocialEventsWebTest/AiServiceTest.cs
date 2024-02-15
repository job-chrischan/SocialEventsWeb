namespace SocialEventsWebTest
{

    [TestClass]
    public class AiServiceTests
    {
        private AiService _aiService;


        [TestInitialize]
        public void Setup()
        {
            ///TODO: Update file path before testing
            string rootpath = "C:/Users/Chris/source/repos/SocialEventServices/SocialEventServices/Services/OpenNlpModels/";

            _aiService = new AiService(rootpath + "en-ner-location.bin", rootpath + "opennlp-en-ud-ewt-sentence-1.0-1.9.3.bin", rootpath + "Sentiment.bin", rootpath + "opennlp-en-ud-ewt-tokens-1.0-1.9.3.bin");
        }


        [TestMethod]
        public async Task AskAsync_ReturnsResponse()
        {
            // Arrange
            string prompt = "When asked the question \"Would you like help in finding social events to add to your calendar?\", and the response was \"Sure\", is that a yes, no or a confused response to the question? Answer fewer than 5 words.";

            // Act
            var response = await _aiService.AskAsync(prompt);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(response));
            Assert.IsTrue(response.Contains("Yes")); // Adjust this based on the expected response

            // You can add more assertions based on the expected behavior of the method
        }


        [TestMethod]
        public void GetInterests_ShouldReturnTwo()
        {
            // Arrange
            string documentText = "Based on the provided response, the following comma separated categories most closely match the response: Business, Music.";

            // Act
            string[] interests = _aiService.GetInterests(documentText);
            // Assert
            CollectionAssert.AreEqual(new string[] { "Business", "Music" }, interests);
        }

        [TestMethod]
        public void GetInterests_ShouldReturnOne()
        {
            // Arrange
            string documentText = "The closest matching categories for the response \"Cars, Business, Women, Liverpool\" would be Business.";

            // Act
            string[] interests = _aiService.GetInterests(documentText);

            // Assert
            CollectionAssert.AreEqual(new string[] { "Business" }, interests);
        }

        [TestMethod]
        public void GetLocation_NoMentionOfPlace()
        {
            // Arrange
            string documentText = "I would like to eat turkey at Christmas on my china plates because I am hungary.";

            // Act
            string location = _aiService.GetLocation(documentText);

            // Assert
            Assert.AreEqual("none", location);
        }

        [TestMethod]
        public void GetLocation_ShouldReturnOneName()
        {
            // Arrange
            string documentText = "Maidstone, Kent";

            // Act
            string location = _aiService.GetLocation(documentText);

            // Assert
            Assert.AreEqual("Maidstone", location);
        }

        [TestMethod]
        public void GetLocation_ShouldReturnTwoNames()
        {
            // Arrange
            string documentText = "The Big Apple,New York";

            // Act
            string location = _aiService.GetLocation(documentText);

            // Assert
            Assert.AreEqual("none", location);
        }


        [TestMethod]
        public void GetSentiment_ShouldReturnPositive()
        {
            // Arrange
            string documentText = "Yeah";

            // Act
            string sentiment = _aiService.GetSentiment(documentText);

            // Assert
            Assert.AreEqual("Positive", sentiment);
        }

        [TestMethod]
        public void GetSentiment_ShouldReturnNegative()
        {
            // Arrange
            string documentText = "No need, thanks";

            // Act
            string sentiment = _aiService.GetSentiment(documentText);

            // Assert
            Assert.AreEqual("Negative", sentiment);
        }

        [TestMethod]
        public void GetSentiment_ShouldReturnConfused()
        {
            // Arrange
            string documentText = "I don't know what to think.";

            // Act
            string sentiment = _aiService.GetSentiment(documentText);

            // Assert
            Assert.AreEqual("Confused", sentiment);
        }

        [TestMethod]
        public void GetSentiment_ShouldReturnCancel()
        {
            // Arrange
            string documentText = "Stop";

            // Act
            string sentiment = _aiService.GetSentiment(documentText);

            // Assert
            Assert.AreEqual("Cancel", sentiment);
        }
    }
}