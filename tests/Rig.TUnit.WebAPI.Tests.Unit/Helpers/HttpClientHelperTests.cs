using Rig.TUnit.WebAPI.Helpers;
using Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.WebAPI.Tests.Unit.Helpers;

public sealed class HttpClientHelperTests
{
    [Test]
    public async Task GetAsync_ReturnsDeserializedResponse()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        var result = await helper.GetAsync<TestEndpoints.EchoResponse>("/echo/hello");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).IsEqualTo("hello");
    }

    [Test]
    public async Task PostAsync_SendsBodyAndReturnsResponse()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        var result = await helper.PostAsync<TestEndpoints.EchoRequest, TestEndpoints.EchoResponse>(
            "/echo",
            new TestEndpoints.EchoRequest("posted"));

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).IsEqualTo("posted");
    }

    [Test]
    public async Task PutAsync_SendsBodyAndReturnsResponse()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        var response = await helper.PutAsync(
            "/echo/42",
            new TestEndpoints.EchoRequest("updated"));

        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task DeleteAsync_ReturnsResponse()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        var response = await helper.DeleteAsync("/echo/9");

        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task Client_LazyCreation_CreateOnFirstAccess()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act — accessing Client twice returns the same instance (lazy singleton)
        var first = helper.Client;
        var second = helper.Client;

        // Assert
        await Assert.That(first).IsNotNull();
        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task DisposeAsync_DisposesClient()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        var helper = new HttpClientHelper<TestProgram>(factory);
        var client = helper.Client;

        // Act
        await helper.DisposeAsync();

        // Assert — subsequent client use throws ObjectDisposedException
        await Assert.That(async () => await client.GetAsync("/echo/x"))
            .Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task CreateClient_WithOptions_ReturnsConfiguredClient()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        using var client = helper.CreateClient(options =>
        {
            options.BaseAddress = new Uri("http://localhost/");
        });

        // Assert
        await Assert.That(client).IsNotNull();
        await Assert.That(client.BaseAddress).IsNotNull();
    }

    [Test]
    public async Task WithBearerToken_SetsAuthorizationHeader()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        helper.WithBearerToken("abc.def.ghi");

        // Assert
        var auth = helper.Client.DefaultRequestHeaders.Authorization;
        await Assert.That(auth).IsNotNull();
        await Assert.That(auth!.Scheme).IsEqualTo("Bearer");
        await Assert.That(auth.Parameter).IsEqualTo("abc.def.ghi");
    }

    [Test]
    public async Task WithBearerToken_Null_ClearsAuthorizationHeader()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);
        helper.WithBearerToken("initial-token");

        // Act
        helper.WithBearerToken(null);

        // Assert
        await Assert.That(helper.Client.DefaultRequestHeaders.Authorization).IsNull();
    }

    [Test]
    public async Task WithBearerToken_ReturnsSelf_EnablesChaining()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        var result = helper.WithBearerToken("token");

        // Assert
        await Assert.That(result).IsSameReferenceAs(helper);
    }

    [Test]
    public async Task WithBearerToken_RoundTripsThroughTestServer()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);
        helper.WithBearerToken("round-trip-token");

        // Act
        var result = await helper.GetAsync<TestEndpoints.EchoResponse>("/headers/authorization");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).IsEqualTo("Bearer round-trip-token");
    }

    [Test]
    public async Task WithHeader_SetsHeader()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        helper.WithHeader("X-Correlation-Id", "corr-123");

        // Assert
        var values = helper.Client.DefaultRequestHeaders.GetValues("X-Correlation-Id");
        await Assert.That(values).Contains("corr-123");
    }

    [Test]
    public async Task WithHeader_OverwritesExistingValue()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);
        helper.WithHeader("X-Tenant", "first");

        // Act
        helper.WithHeader("X-Tenant", "second");

        // Assert
        var values = helper.Client.DefaultRequestHeaders.GetValues("X-Tenant").ToList();
        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo("second");
    }

    [Test]
    public async Task WithHeader_NullOrEmptyName_Throws()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act / Assert
        await Assert.That(() => helper.WithHeader(string.Empty, "v"))
            .Throws<ArgumentException>();
        await Assert.That(() => helper.WithHeader(null!, "v"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task WithHeader_ReturnsSelf_EnablesChaining()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);

        // Act
        var result = helper.WithHeader("X-Trace", "t-1");

        // Assert
        await Assert.That(result).IsSameReferenceAs(helper);
    }

    [Test]
    public async Task WithHeader_RoundTripsThroughTestServer()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        await using var helper = new HttpClientHelper<TestProgram>(factory);
        helper.WithHeader("X-Round-Trip", "hello");

        // Act
        var result = await helper.GetAsync<TestEndpoints.EchoResponse>("/headers/X-Round-Trip");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).IsEqualTo("hello");
    }
}
