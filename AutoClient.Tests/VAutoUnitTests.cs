using System;
using System.Collections.Generic;

using FluentAssertions;
using Xunit;

namespace AutoClient.Tests
{
    public class VAutoUnitTests
    {
        [Theory]
        [MemberData(nameof(VAutoClientTestData.DealerNameTestData), MemberType = typeof(VAutoClientTestData))]
        public void Get_DealerName_Returns(int? id, Dictionary<int, DealersResponse> dealers, string expected)
        {
            Action act = () => VAutoClient.GetDealerName(id, dealers);
            act.Should().NotThrow("because valid input was supplied.");

            var result = VAutoClient.GetDealerName(id, dealers);

            result.Should().BeEquivalentTo(expected, $"because '{expected}' is the result desired.");
        }
    }
}
