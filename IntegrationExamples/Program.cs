using System.Dynamic;
using System.Net;
using System.Text;
using IdentityModel.Client; //https://github.com/IdentityModel
using Newtonsoft.Json;


namespace IntegrationExamples
{
    internal class DitioIntegrationExamples
    {
        private static readonly string apiBaseUrl = "https://ditio-api-test.azurewebsites.net";
        private static readonly string companyId = "";

        private static async Task Main(string[] args)
        {
            //await TestTaskIntegration();
            await TestMachineIntegration();
        }

        private static async Task TestTaskIntegration()
        {
            /*
                Testing task integration
            */
            dynamic newTask = new ExpandoObject();
            newTask.externalProjectNumber = "3210105";
            newTask.externalId = "3210105-0120-999";
            newTask.externalDim01 = "3210";
            newTask.name = "TEST TEST";
            newTask.active = true;
            dynamic? createdTask = await PostTask(newTask);

            if (createdTask == null)
            {
                //Task exists from before or something..
                return;
            }

            var ditioTaskId = (string)createdTask.id;
            //GET
            dynamic? taskReceivedFromDitio = await GetTaskByExternalIds((string)createdTask.externalProjectNumber, (string)createdTask.externalId);
            //PUT
            taskReceivedFromDitio.name = taskReceivedFromDitio.name + " (Modified task name)";
            dynamic? updatedTask = await PutTask(taskReceivedFromDitio);
            //DELETE
            dynamic? deletedTask = await DeleteTask(ditioTaskId);
        }

        private static async Task TestMachineIntegration()
        {
            /*
                Testing machine integration
            */
            dynamic newMachine = new ExpandoObject();
            newMachine.companyId = companyId;
            newMachine.machineNumber = "44444";
            newMachine.typeId = "LOKR";
            newMachine.name = "Lokomotiv ABCD";
            newMachine.buildYear = "1998";
            newMachine.department = "3210";
            newMachine.active = true;

            dynamic? createdMachine = await PostMachine(newMachine);

            if (createdMachine == null)
            {
                //Task exists from before or something..
                return;
            }

            var ditioMachineId = (string)createdMachine.id;
            //GET
            dynamic? machineReceivedFromDitio = await GetMachineByResourceNumber((string)createdMachine.machineNumber);
            //PUT
            machineReceivedFromDitio.name = machineReceivedFromDitio.name + " (Modified machine name)";
            dynamic? updatedMachine = await PutMachine(machineReceivedFromDitio);
            //DELETE
            dynamic? deletedMachine = await DeleteMachine(ditioMachineId);
        }
        
        #region Task integration

        private static async Task<dynamic?> GetTask(string ditioTaskId)
        {
            return await ExecuteRequest($"/api/v4/integration/tasks/{ditioTaskId}", HttpMethod.Get);
        }

        private static async Task<dynamic?> GetTaskByExternalIds(string externalProjectNumber, string externalId)
        {
            return await ExecuteRequest($"/api/v4/integration/tasks/by-external-project-number/{externalProjectNumber}/by-external-id/{externalId}", HttpMethod.Get);
        }

        private static async Task<dynamic?> PostTask(dynamic task)
        {
            return await ExecuteRequest("/api/v4/integration/tasks", HttpMethod.Post, task);
        }

        private static async Task<dynamic?> PutTask(dynamic? task)
        {
            if(task == null)
                throw new ArgumentNullException(nameof(task));
            return await ExecuteRequest($"/api/v4/integration/tasks/{task.id}", HttpMethod.Put, task);
        }

        private static async Task<dynamic?> DeleteTask(string ditioId)
        {
            return await ExecuteRequest($"/api/v4/integration/tasks/{ditioId}", HttpMethod.Delete);
        }

        #endregion

        #region Machine integration

        private static async Task<dynamic?> GetMachine(string ditioMachineId)
        {
            return await ExecuteRequest($"/api/v4/integration/machines/{ditioMachineId}", HttpMethod.Get);
        }

        private static async Task<dynamic?> GetMachineByResourceNumber(string resourceNumber)
        {
            return await ExecuteRequest($"/api/v4/integration/machines/by-machine-number/{resourceNumber}", HttpMethod.Get);
        }

        private static async Task<dynamic?> PostMachine(dynamic machine)
        {
            return await ExecuteRequest("/api/v4/integration/machines", HttpMethod.Post, machine);
        }

        private static async Task<dynamic?> PutMachine(dynamic? machine)
        {
            if(machine == null)
                throw new ArgumentNullException(nameof(machine));
            return await ExecuteRequest($"/api/v4/integration/machines/{machine.id}", HttpMethod.Put, machine);
        }

        private static async Task<dynamic?> DeleteMachine(string ditioId)
        {
            return await ExecuteRequest($"/api/v4/integration/machines/{ditioId}", HttpMethod.Delete);
        }

        #endregion

        #region Helpers

        private static async Task<dynamic?> ExecuteRequest(string path, HttpMethod httpMethod, object? data = null)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl)
            };
            var httpRequestMessage = await BuildRequest(path, httpMethod, data);
            var response = await httpClient.SendAsync(httpRequestMessage);
            var parsedResponse = ParseResponse(response);
            Console.WriteLine($"Path: {path}");
            Console.WriteLine($"HttpMethod: {httpMethod.ToString()}");
            Console.WriteLine($"Response: {JsonConvert.SerializeObject(parsedResponse)}");
            return parsedResponse;
        }

        private static async Task<HttpRequestMessage> BuildRequest(string endpointPath, HttpMethod httpMethod, object? data = null)
        {
            HttpRequestMessage httpRequest = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(apiBaseUrl + endpointPath),
            };
            if (data != null)
            {
                var dataAsString = JsonConvert.SerializeObject(data);
                var content = new StringContent(dataAsString, Encoding.UTF8, "application/json");
                httpRequest.Content = content;
            }

            httpRequest.SetBearerToken(await GenerateAuthenticationToken.GetToken());

            return httpRequest;
        }

        private static dynamic? ParseResponse(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync();
            content.Wait();
            var json = JsonConvert.DeserializeObject<dynamic>(content.Result);
            if (response.IsSuccessStatusCode)
                return json;
            if (response.StatusCode >= HttpStatusCode.InternalServerError)
            {
                if (json?["error"] != null && json?["error"]?["message"] != null)
                {
                    //Normal exception body
                    Console.WriteLine($"Unexpected response : {(string)json["error"]["message"]}");
                    return null;
                }
            }
            //Handles special cases where like app crashes before error handling middleware is initialzied
            Console.WriteLine($"Unexpected response : {content.Result}");
            return null;
        }

        #endregion
    }
}