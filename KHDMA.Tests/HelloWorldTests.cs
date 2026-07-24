using Xunit.Abstractions;

namespace KHDMA.Tests;

/// <summary>
/// Smoke test used to confirm the GitHub Actions runner discovers, builds and
/// executes the test suite. Prints "hello" to the test output so it is visible
/// in the runner logs. Safe to delete once CI is confirmed working.
/// </summary>
public class HelloWorldTests
{
    private readonly ITestOutputHelper _output;

    public HelloWorldTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Prints_Hello()
    {
        _output.WriteLine("hello");
        Assert.Equal("hello", "hello");
    }
}
