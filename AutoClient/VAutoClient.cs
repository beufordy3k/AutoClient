using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

using Common.Logging;

namespace AutoClient
{
    public class VAutoClient
    {
        private const string DefaultEndpointUrl = "http://vautointerview.azurewebsites.net";

        private ILog Logger { get; }
        private HttpClient Client { get; }

        private ConcurrentQueue<int> VehicleQueue { get; } = new ConcurrentQueue<int>();
        private ConcurrentDictionary<int, VehicleResponse> Vehicles { get; } = new ConcurrentDictionary<int, VehicleResponse>();

        private ConcurrentQueue<int> DealerQueue { get; } = new ConcurrentQueue<int>();
        private ConcurrentDictionary<int, DealersResponse> Dealers { get; } = new ConcurrentDictionary<int, DealersResponse>();

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
                    Logger.Error(l => l($"Error encountered retrieving Vehicle Id List from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'. No Vehicles returned."));

                    return null; //No Results
                }

                foreach (var id in vehicleIdsResponse.VehicleIds)
                {
                    //Add Id to Vehicle Queue for processing - could be a TPL/other parallel mechanism for processing items.
                    VehicleQueue.Enqueue(id);
                }
            }
            catch (VAutoException vaex)
            {
                Logger.Error(l => l($"Exception encountered retrieving Vehicle Id List from endpoint '{endpointUrl}', using dataset Id '{datasetId}'"), vaex);

                return null;  //No Results
            }

            // 3. Get Vehicle Detail
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
                    Logger.Error(l => l($"Exception encountered retrieving Vehicle Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Vehicle Id '{id}"), vaex);

                    continue; //Process next item in queue
                }

                if (vehicleResponse == null)
                {
                    Logger.Error(l => l($"Error encountered retrieving Vehicle Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Vehicle Id '{id}'. No Vehicle Detail returned. Continuing."));
                    continue;
                }

                if (Vehicles.ContainsKey(vehicleId) || !Vehicles.TryAdd(vehicleId, vehicleResponse))
                {
                    Logger.Warn(l => l($"Vehicle Detail from endpoint '{endpointUrl}' already added. Dataset Id '{datasetId}', Vehicle Id '{id}'."));

                    continue;
                }

                Logger.Debug(l => l($"Vehicle Detail added for Vehicle Id '{id}' from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'"));

                if (vehicleResponse.DealerId.HasValue)
                {
                    DealerQueue.Enqueue(vehicleResponse.DealerId.GetValueOrDefault());
                }

            }

            // 4. Get Dealer Info
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
                    Logger.Error(l => l($"Exception encountered retrieving Dealer Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Dealer Id '{id}"), vaex);
                    throw;
                }

                if (dealerResponse == null)
                {
                    Logger.Error(l => l($"Error encountered retrieving Dealer Detail from endpoint '{endpointUrl}', using Dataset Id '{datasetId}' and Dealer Id '{id}'. No Vehicle Detail returned. Continuing."));
                    continue;
                }

                if (Vehicles.ContainsKey(dealerId) || !Dealers.TryAdd(dealerId, dealerResponse))
                {
                    Logger.Debug(l => l($"Dealer Detail from endpoint '{endpointUrl}' already added. Dataset Id '{datasetId}', Dealer Id '{id}'."));

                    continue;
                }

                Logger.Debug(l => l($"Dealer Detail added for Dealer Id '{id}' from endpoint '{endpointUrl}', using Dataset Id '{datasetId}'"));
            }

            var groupedVehicles = Vehicles.GroupBy(v => v.Value.DealerId); // This is *not* a good idea (memory usage and CPU) for large volume data sets

            // 5. Build Answer Response with Vehicles grouped by dealer
            var answer = new Answer
            {
                Dealers = (ObservableCollection<DealerAnswer>) groupedVehicles
                    .Where(gv => gv.Key.HasValue)
                    .Select(gv => new DealerAnswer
                    {
                        DealerId = gv.Key,
                        Name = GetDealerName(gv.Key, Dealers),
                        Vehicles = (ObservableCollection<VehicleAnswer>) gv.Select(v =>
                        {
                            var vehicle = v.Value;

                            return new VehicleAnswer
                            {
                                VehicleId = vehicle.VehicleId,
                                Make = vehicle.Make,
                                Model = vehicle.Model,
                                Year = vehicle.Year,
                            };
                        })
                    })
            };

            // 6. Submit Answer
            // 7. Return AnswerResponse

            return null;
        }

        private static string GetDealerName(int? id, IReadOnlyDictionary<int, DealersResponse> dealers)
        {
            if (!id.HasValue)
            {
                return null;
            }

            var idValue = id.GetValueOrDefault();

            return dealers.ContainsKey(idValue) ? dealers[idValue].Name : null;
        }
    }
}