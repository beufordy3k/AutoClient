using System.Collections.Concurrent;

namespace VAutoClient
{
    public class VAutoClient
    {
        private ConcurrentDictionary<string, DealersResponse> Dealers { get; set; }
        private ConcurrentQueue<string> Vehicles { get; set; }
        

        public void Run()
        {
            // 1. Get Vehicles
            // 2. Get Vehicle Detail
            // 3. Get Dealer Info
            // 4. Build Answer Response with Vehicles grouped by dealer


        }
    }
}