using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace ControlCenter.Gateway.Tests;

/// <summary>
/// Basic smoke tests to verify the test project is configured correctly.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void TestProjectRuns()
    {
        true.Should().BeTrue();
    }

    [Fact]
    public void CanReferenceGatewayProject()
    {
        // Verify we can reference types from the gateway project
        var eventType = typeof(ControlCenter.Gateway.Models.NormalizedEvent);
        eventType.Should().NotBeNull();
        eventType.Name.Should().Be("NormalizedEvent");
    }

    [Property]
    public bool FsCheckPropertyTestWorks(int x)
    {
        // Verify FsCheck property-based testing works
        return x + 0 == x;
    }
}
