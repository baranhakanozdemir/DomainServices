using System.Net;
using DomainServices.Core.Models;
using DomainServices.Core.Party;
using DomainServices.Core.Services;
using Xunit;

namespace DomainServices.Core.Tests.Services;

public class ReadOnlyDomainServiceClientTests
{
    // Regression for issue #13: ReadOnlyDomainServiceClient<T> cctor used to call
    // JsonSerializerOptions.MakeReadOnly() without a TypeInfoResolver, which throws
    // on .NET 10 the first time any client type is touched.
    [Fact]
    public void StaticInitializer_DoesNotThrow()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://example.test/") };
        var client = new ReadOnlyDomainServiceClient<Person>(http);
        Assert.Equal(new Uri("https://example.test/"), client.BaseAddress);
    }

    [Fact]
    public void DomainServiceClient_Constructor_DoesNotThrow()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://example.test/") };
        var client = new DomainServiceClient<Person>(http);
        Assert.Equal(new Uri("https://example.test/"), client.BaseAddress);
    }

    [Fact]
    public void BaseAddress_WithoutHttpClientBaseAddress_Throws()
    {
        var http = new HttpClient();
        var client = new ReadOnlyDomainServiceClient<Person>(http);
        Assert.Throws<InvalidOperationException>(() => _ = client.BaseAddress);
    }

    [Fact]
    public async Task GetAsync_WhenServerReturnsNotFound_ReturnsNotFoundResponse()
    {
        var http = new HttpClient(new StubHandler(HttpStatusCode.NotFound, body: ""))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var client = new ReadOnlyDomainServiceClient<Person>(http, "people");

        var response = await client.GetAsync(Guid.NewGuid());

        Assert.False(response.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body)
            });
    }
}
