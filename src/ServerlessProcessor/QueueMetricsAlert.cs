using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ServerlessProcessor
{
    public static class QueueMetricsAlert
    {
        [FunctionName("QueueMetricsAlert")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "QueueMetricsAlert")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
           
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();            
            log.LogInformation(requestBody);
           
            return new OkResult();
        }
    }
}
