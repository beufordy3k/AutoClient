using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Common.Logging;

namespace AutoClient
{
    public class VAutoClient
    {
        private const string DefaultEndpointUrl = "http://vautointerview.azurewebsites.net";

        private ILog Logger { get; }
        private HttpClient Client { get; }

        private ConcurrentDictionary<string, DealersResponse> Dealers { get; } = new ConcurrentDictionary<string, DealersResponse>();
        private ConcurrentQueue<int> VehiclesQueue { get; } = new ConcurrentQueue<int>();
        private ConcurrentDictionary<int, VehicleResponse> Vehicles = new ConcurrentDictionary<int, VehicleResponse>();

        public VAutoClient(ILog logger, HttpClient client)
        {
            Logger = logger;
            Client = client;
        }
        
        public async Task<AnswerResponse> Run(string endpointUrl = null)
        {
            endpointUrl = endpointUrl ?? DefaultEndpointUrl;

            var datasetClient = new DataSetClient(Client) {BaseUrl = endpointUrl};
            var vehicleClient = new VehiclesClient(Client) {BaseUrl = endpointUrl};
            var dealersClient = new DealersClient(Client) {BaseUrl = endpointUrl};

            string datasetId = null;

            // 1. Get DataSet
            try
            {
                var datasetResponse = await datasetClient.GetDataSetIdAsync();

                if (!string.IsNullOrEmpty(datasetResponse.DatasetId))
                {
                    Logger.Error(l => l($"Error encountered retrieving Dataset Id from endpoint '{endpointUrl}'. No Id returned."));

                    return null; //No Results
                }

                datasetId = datasetResponse.DatasetId;
            }
            catch (VAutoException vaex)
            {
                Logger.Error(l => l($"Exception encountered retrieving Dataset Id from endpoint '{endpointUrl}'"), vaex);

                return null; //No Results
            }

            // 2. Get Vehicles
            try
            {
                var vehicleIdsResponse = await vehicleClient.GetIdsAsync(datasetId);

                if (vehicleIdsResponse.VehicleIds == null || !vehicleIdsResponse.VehicleIds.Any())
                {
                    Logger.Error(l => l($"Error encountered retrieving Vehicle Id List from endpoint '{endpointUrl}', using datasetId '{datasetId}'. No Vehicles returned."));

                    return null; //No Results
                }

                foreach (var id in vehicleIdsResponse.VehicleIds)
                {
                    //Add Id to Vehicle Queue for processing - could be a TPL/other parallel mechanism for processing items.
                    VehiclesQueue.Enqueue(id);
                }
            }
            catch (VAutoException vaex)
            {
                Logger.Error(l => l($"Exception encountered retrieving Vehicle Id List from endpoint '{endpointUrl}', using dataset Id '{datasetId}'"), vaex);

                return null;  //No Results
            }

            // 3. Get Vehicle Detail
            while (VehiclesQueue.TryDequeue(out var vehicleId))
            {
                try
                {
                    var vehicleResponse = await vehicleClient.GetVehicleAsync(datasetId, vehicleId);

                    if (vehicleResponse == null)
                    {
                        var id = vehicleId;
                        Logger.Error(l => l($"Error encountered retrieving Vehicle Detail from endpoint '{endpointUrl}', using datasetId '{datasetId}' and vehicle Id '{id}'. No Vehicle Detail returned. Continuing."));
                        continue;
                    }



                }
                catch (VAutoException vaex)
                {
                    Logger.Error(l => l($"Exception encountered retrieving Vehicle Detail from endpoint '{endpointUrl}', using dataset Id '{datasetId}' and vehicle Id '{vehicleId}"), vaex);
                    throw;
                }
            }

            // 4. Get Dealer Info
            // 5. Build Answer Response with Vehicles grouped by dealer
            // 6. Submit Answer
            // 7. Return AnswerResponse

            return null;
        }
    }
}