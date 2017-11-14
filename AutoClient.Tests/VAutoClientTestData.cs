using System.Collections.Generic;

namespace AutoClient.Tests
{
    public static class VAutoClientTestData
    {
        public static IEnumerable<object[]> DealerNameTestData
        {
            get
            {
                yield return new object[] { 1, new Dictionary<int, DealersResponse>{{1, new DealersResponse{DealerId = 1, Name = "Bob's Dealer"}}}, "Bob's Dealer" }; //Key found
                yield return new object[] { 2, new Dictionary<int, DealersResponse> { { 1, new DealersResponse { DealerId = 1, Name = null } } }, null }; //Key found, name null
                yield return new object[] { 2, new Dictionary<int, DealersResponse> { { 1, new DealersResponse { DealerId = 1, Name = string.Empty } } }, null}; //Key found, name empty
                yield return new object[] { 3, new Dictionary<int, DealersResponse> { { 1, new DealersResponse { DealerId = 1, Name = "Bob's Dealer" } } }, null }; //Key not found 
                yield return new object[] { 4, null, null }; //null dealer data
                yield return new object[] { 5, new Dictionary<int, DealersResponse>(), null }; //No dealer data
                yield return new object[] { null, null, null }; //Key is null
            }
        }
    }
}