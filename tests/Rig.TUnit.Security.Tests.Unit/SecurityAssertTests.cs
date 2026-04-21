using System.Net;
using Rig.TUnit.Security.Assertions;

namespace Rig.TUnit.Security.Tests.Unit;

public sealed class SecurityAssertTests
{
    [Test]
    public async Task HttpIsUnauthorized_NullResponse_ThrowsArgumentNullException()
    {
        await Assert.That(() => SecurityAssert.HttpIsUnauthorized(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HttpIsUnauthorized_401Response_DoesNotThrow()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        await SecurityAssert.HttpIsUnauthorized(response);
    }

    [Test]
    public async Task HttpIsUnauthorized_200Response_ThrowsSecurityAssertionException()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        await Assert.That(() => SecurityAssert.HttpIsUnauthorized(response))
            .ThrowsExactly<SecurityAssertionException>();
    }

    [Test]
    public async Task HttpIsForbidden_NullResponse_ThrowsArgumentNullException()
    {
        await Assert.That(() => SecurityAssert.HttpIsForbidden(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HttpIsForbidden_403Response_DoesNotThrow()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden);

        await SecurityAssert.HttpIsForbidden(response);
    }

    [Test]
    public async Task HttpIsForbidden_200Response_ThrowsSecurityAssertionException()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        await Assert.That(() => SecurityAssert.HttpIsForbidden(response))
            .ThrowsExactly<SecurityAssertionException>();
    }

    [Test]
    public async Task HttpIsAuthenticated_NullResponse_ThrowsArgumentNullException()
    {
        await Assert.That(() => SecurityAssert.HttpIsAuthenticated(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HttpIsAuthenticated_200Response_DoesNotThrow()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        await SecurityAssert.HttpIsAuthenticated(response);
    }

    [Test]
    public async Task HttpIsAuthenticated_401Response_ThrowsSecurityAssertionException()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        await Assert.That(() => SecurityAssert.HttpIsAuthenticated(response))
            .ThrowsExactly<SecurityAssertionException>();
    }

    [Test]
    public async Task SecurityAssertionException_Message_IsPreserved()
    {
        var ex = new SecurityAssertionException("test message");

        await Assert.That(ex.Message).IsEqualTo("test message");
    }
}
