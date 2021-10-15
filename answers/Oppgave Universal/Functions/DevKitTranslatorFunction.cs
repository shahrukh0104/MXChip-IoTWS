using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;

namespace DevKitTranslator
{
    public static class DevKitTranslatorFunction
    {
        // Subscription Key of Speech Service
        const string speechSubscriptionKey = "DIN SPEECH KEY HER";

        // Region of the speech service, see https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/regions for more details.
        const string speechServiceRegion = "DIN VALGTE REGION HER";

        // Device ID
        const string deviceName = "DIN DEVICE NAVN HER";

        [FunctionName("devkit_translator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string targetLanguage = req.Query["target"];
            string sourceLanguage = req.Query["source"];

            if (string.IsNullOrEmpty(sourceLanguage))
            {
                return new BadRequestObjectResult("Please pass a 'source' on the query string to specify the from language.");
            }
            log.LogInformation($"Source language: {sourceLanguage}");
            log.LogInformation($"Target language: {targetLanguage}");

            var speechTranslation = new SpeechTranslation(speechSubscriptionKey, speechServiceRegion, log);
            string result = await speechTranslation.TranslationWithAudioStreamAsync(req.Body, sourceLanguage, targetLanguage);
            log.LogInformation("Translation result: " + result);

            // Send translation result as C2D message
            string iothubConnectionString = System.Environment.GetEnvironmentVariable("iotHubConnectionString");
            var serviceClient = ServiceClient.CreateFromConnectionString(iothubConnectionString);
            var commandMessage = new Message(Encoding.ASCII.GetBytes(result));
            await serviceClient.SendAsync(deviceName, commandMessage);

            if (result != null)
            {
                return new OkObjectResult("Translation result: " + result);
            }
            else
            {
                return new BadRequestObjectResult("Failed to translate speech.");
            }
        }
    }
}
