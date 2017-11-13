using System.Collections.Concurrent;
using Common.Logging;

namespace AutoClient
{
    public class VAutoClient
    {
        private ILog Logger { get; }

        private ConcurrentDictionary<string, DealersResponse> Dealers { get; set; }
        private ConcurrentQueue<string> Vehicles { get; set; }

        public VAutoClient(ILog logger)
        {
            Logger = logger;
        }
        

        public AnswerResponse Run(string endpointUrl)
        {
            // 1. Get DataSet
            // 2. Get Vehicles
            // 3. Get Vehicle Detail
            // 4. Get Dealer Info
            // 5. Build Answer Response with Vehicles grouped by dealer
            // 6. Submit Answer
            // 7. Return AnswerResponse

            return null;
        }
    }
}