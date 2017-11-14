using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace AutoClient
{
    public class VAutoClient
    {
        private const string DefaultEndpointUrl = "http://vautointerview.azurewebsites.net";

        private ILogger<VAutoClient> Logger { get; }
        private HttpClient Client { get; }

        private ConcurrentQueue<int> VehicleQueue { get; } = new ConcurrentQueue<int>();
        private ConcurrentDictionary<int, VehicleResponse> Vehicles { get; } = new ConcurrentDictionary<int, VehicleResponse>();

        private ConcurrentQueue<int> DealerQueue { get; } = new ConcurrentQueue<int>();
        private ConcurrentDictionary<int, DealersResponse> Dealers { get; } = new ConcurrentDictionary<int, DealersResponse>();

        public VAutoClient(ILogger<VAutoClient> logger, HttpClient client)
        {
            Logger = logger;
            Client = client;
        }

        /// <summary>
        /// Entry point for retrieving auto client data
        /// </summary>
        /// <param name="endpointUrl">Used to override the default Endpoint Url</param>
        /// <returns></returns>
        public async Task<AnswerResponse> Run(string endpointUrl = null)
        {
            endpointUrl = endpointUrl ?? DefaultEndpointUrl;

            var datasetClient = new DataSetClient(Client) {BaseUrl = endpointUrl};
            var vehicleClient = new VehiclesClient(Client) {BaseUrl = endpointUrl};
            var dealersClient = new DealersClient(Client) {BaseUrl = endpointUrl};

            // 1. Get Dataset
            var datasetId = await GetDataset(endpointUrl, datasetClient);

            if (datasetId == null)
            {
                return null;
            }

            // 2. Get Vehicles
            if (await GetVehicles(endpointUrl, vehicleClient, datasetId)) return null;

            // 3. Get Vehicle Detail
            GetVehicleDetail(endpointUrl, vehicleClient, datasetId);

            // 4. Get Dealer Info
            await GetDealerDetail(endpointUrl, dealersClient, datasetId);

            // 5. Build Answer Response with Vehicles grouped by dealer
            var answer = BuildAnswer(Vehicles, Dealers);

            // 6. Submit Answer
            var answerResult = await datasetClient.PostAnswerAsync(datasetId, answer);

            // 7. Return AnswerResponse
            return answerResult;
        }

        /// <summary>
        /// Retrieve the dataset from the endpoint
        /// </summary>
        private async Task<string> GetDataset(string endpointUrl, DataSetClient datasetClient)
        {
            try
            {
                var datasetResponse = await datasetClient.GetDataSetIdAsync();

                if (!string.IsNullOrEmpty(datasetResponse.DatasetId))
                {
                    return datasetResponse.DatasetId;
                }

                Logger.LogError($"Error encountered retrieving Dataset Id from endpoint '{endpointUrl}'. No Id returned.");

                return null;
            }
            catch (VAutoException vaex)
            {
                Logger.LogError($"Exception encountered retrieving Dataset Id from endpoint '{endpointUrl}'", vaex);
            }

            return null;
        }

        /// <summary>
        /// Get the vehicle Ids using the dataset Id
        /// </summary>
        private async Task<bool> GetVehicles(string endpointUrl, VehiclesClient vehicleClient, string datasetId)
        {
            try
            {
                var vehicleIdsResponse = await vehicleClient.GetIdsAsync(datasetId);

                if (vehicleIdsResponse.VehicleIds == null || !vehicleIdsResponse.VehicleIds.Any())
                {
                    Logger.LogError($"Error encountered retrieving Vehicle Id List from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'. No Vehicles returned.");

                    return true; //Error condition exists
                }

                foreach (var id in vehicleIdsResponse.VehicleIds)
                {
                    //Add Id to Vehicle Queue for processing - could be a TPL/other parallel mechanism for processing items.
                    VehicleQueue.Enqueue(id);
                }
            }
            catch (VAutoException vaex)
            {
                Logger.LogError($"Exception encountered retrieving Vehicle Id List from endpoint '{endpointUrl}', using dataset Id '{datasetId}'", vaex);

                return true; //Error condition exists
            }

            return false; //No error condition
        }

        /// <summary>
        /// Get the vehicle detail for all vehicles
        /// </summary>
        private void GetVehicleDetail(string endpointUrl, VehiclesClient vehicleClient, string datasetId)
        {
            var cpuCount = Environment.ProcessorCount;

            var tasks = new Task[cpuCount];

            for (var i=0; i < cpuCount; i++)
            {
                tasks[i] = Task.Run(async () => await ProcessVehicleQueue(endpointUrl, vehicleClient, datasetId));
            }

            Task.WaitAll(tasks);
        }

        private async Task ProcessVehicleQueue(string endpointUrl, VehiclesClient vehicleClient, string datasetId)
        {
            while (VehicleQueue.TryDequeue(out var vehicleId))
            {
                var id = vehicleId;


                if (Vehicles.ContainsKey(vehicleId))
                {
                    continue; //Vehicle detail already retrieved
                }

                VehicleResponse vehicleResponse;

                try
                {
                    vehicleResponse = await vehicleClient.GetVehicleAsync(datasetId, vehicleId);
                }
                catch (VAutoException vaex)
                {
                    Logger.LogError($"Exception encountered retrieving Vehicle Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Vehicle Id '{id}", vaex);

                    continue; //Process next item in queue
                }

                if (vehicleResponse == null)
                {
                    Logger.LogError($"Error encountered retrieving Vehicle Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Vehicle Id '{id}'. No Vehicle Detail returned. Continuing.");

                    continue;
                }

                if (Vehicles.ContainsKey(vehicleId) || !Vehicles.TryAdd(vehicleId, vehicleResponse))
                {
                    Logger.LogWarning($"Vehicle Detail from endpoint '{endpointUrl}' already added. Dataset Id '{datasetId}', Vehicle Id '{id}'.");

                    continue;
                }

                Logger.LogDebug($"Vehicle Detail added for Vehicle Id '{id}' from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'.");

                if (vehicleResponse.DealerId.HasValue)
                {
                    DealerQueue.Enqueue(vehicleResponse.DealerId.GetValueOrDefault());

                    continue;
                }

                Logger.LogDebug($"Dealer Id not available for Vehicle Id '{id}' from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'.");
            }
        }

        /// <summary>
        /// Get the dealer detail for each unique dealer
        /// </summary>
        private async Task GetDealerDetail(string endpointUrl, DealersClient dealersClient, string datasetId)
        {
            while (DealerQueue.TryDequeue(out var dealerId))
            {
                var id = dealerId;

                if (Dealers.ContainsKey(dealerId))
                {
                    continue; //Dealer detail already retrieved
                }

                DealersResponse dealerResponse;

                try
                {
                    dealerResponse = await dealersClient.GetDealerAsync(datasetId, dealerId);
                }
                catch (VAutoException vaex)
                {
                    Logger.LogError($"Exception encountered retrieving Dealer Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Dealer Id '{id}", vaex);
                    throw;
                }

                if (dealerResponse == null)
                {
                    Logger.LogError($"Error encountered retrieving Dealer Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Dealer Id '{id}'. No Vehicle Detail returned. Continuing.");

                    continue;
                }

                if (Dealers.ContainsKey(dealerId) || !Dealers.TryAdd(dealerId, dealerResponse))
                {
                    Logger.LogDebug($"Dealer Detail from endpoint '{endpointUrl}' already added. Dataset Id '{datasetId}', Dealer Id '{id}'.");

                    continue;
                }

                Logger.LogDebug($"Dealer Detail added for Dealer Id '{id}' from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'");
            }
        }

        /// <summary>
        /// Build the return answer
        /// </summary>
        internal static Answer BuildAnswer(ConcurrentDictionary<int, VehicleResponse> vehicles, ConcurrentDictionary<int, DealersResponse> dealers)
        {
            var groupedVehicles = vehicles.GroupBy(v => v.Value.DealerId); // This is *not* a good idea (memory usage and CPU) for large volume data sets

            var answer = new Answer
            {
                Dealers = new ObservableCollection<DealerAnswer>(groupedVehicles
                    .Where(gv => gv.Key.HasValue)
                    .Select(gv => new DealerAnswer
                    {
                        DealerId = gv.Key,
                        Name = GetDealerName(gv.Key, dealers),
                        Vehicles = new ObservableCollection<VehicleAnswer>(gv.Select(v =>
                        {
                            var vehicle = v.Value;

                            return new VehicleAnswer
                            {
                                VehicleId = vehicle.VehicleId,
                                Make = vehicle.Make,
                                Model = vehicle.Model,
                                Year = vehicle.Year
                            };
                        }))
                    }))
            };

            return answer;
        }

        /// <summary>
        /// Get the dealer name from the dealers dictionary
        /// </summary>
        internal static string GetDealerName(int? id, IReadOnlyDictionary<int, DealersResponse> dealers)
        {
            if (!id.HasValue)
            {
                return null;
            }

            if (dealers == null)
            {
                return null;
            }

            var idValue = id.GetValueOrDefault();

            return dealers.ContainsKey(idValue) ? dealers[idValue].Name : null;
        }
    }
}