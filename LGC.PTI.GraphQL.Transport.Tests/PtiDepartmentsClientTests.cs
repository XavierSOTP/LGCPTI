using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.Plugin.Client;
using NUnit.Framework;

namespace LGC.PTI.GraphQL.Transport.Tests
{
    [TestFixture]
    public sealed class PtiDepartmentsClientTests
    {
        private sealed class FakeHandler : HttpMessageHandler
        {
            public HttpStatusCode StatusCode = HttpStatusCode.OK;
            public string Body = "{\"data\":{\"getDepartments\":[{\"number\":\"0007\",\"name\":\"Operations\"},{\"number\":\"ENG\",\"name\":\"Engineering\"}]}}";
            public string Authorization;
            public string RequestBody;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Authorization = request.Headers.TryGetValues("Authorization", out var values)
                    ? values.Single() : null;
                RequestBody = await request.Content.ReadAsStringAsync();
                return new HttpResponseMessage(StatusCode)
                {
                    Content = new StringContent(Body, Encoding.UTF8, "application/json")
                };
            }
        }

        private static Task<DepartmentsResult> Query(FakeHandler fake, string token = "opaque-test-token") =>
            LGCConnectionTester.GetDepartmentsAsync("https://example.test/graphql",
                token, new HttpClient(fake));

        [Test]
        public async Task GetDepartments_UsesRawTokenAndReturnsNumberAndName()
        {
            var fake = new FakeHandler();
            var result = await Query(fake);

            Assert.True(result.Success);
            Assert.AreEqual(2, result.Departments.Count);
            Assert.AreEqual("0007", result.Departments[0].Number);
            Assert.AreEqual("Operations", result.Departments[0].Name);
            Assert.AreEqual("ENG", result.Departments[1].Number);
            Assert.AreEqual("opaque-test-token", fake.Authorization);
            StringAssert.Contains("getDepartments", fake.RequestBody);
            StringAssert.Contains("number name", fake.RequestBody);
        }

        [Test]
        public async Task GetDepartments_EmptyListIsSuccessful()
        {
            var fake = new FakeHandler { Body = "{\"data\":{\"getDepartments\":[]}}" };
            var result = await Query(fake);

            Assert.True(result.Success);
            Assert.AreEqual(0, result.Departments.Count);
        }

        [Test]
        public async Task GetDepartments_UnauthorizedRequiresNewConnectionTest()
        {
            var fake = new FakeHandler { StatusCode = HttpStatusCode.Unauthorized };
            var result = await Query(fake);

            Assert.False(result.Success);
            Assert.True(result.AuthenticationFailed);
            StringAssert.Contains("Test Connection", result.Message);
            StringAssert.DoesNotContain("opaque-test-token", result.Message);
        }

        [Test]
        public async Task GetDepartments_AuthenticationErrorRequiresNewConnectionTest()
        {
            var fake = new FakeHandler
            {
                Body = "{\"errors\":[{\"message\":\"Invalid authentication token\"}],\"data\":null}"
            };
            var result = await Query(fake);

            Assert.False(result.Success);
            Assert.True(result.AuthenticationFailed);
        }

        [Test]
        public async Task GetDepartments_InvalidEntryDoesNotShowPartialList()
        {
            var fake = new FakeHandler
            {
                Body = "{\"data\":{\"getDepartments\":[{\"number\":\"0007\",\"name\":\"Operations\"},{\"number\":null,\"name\":\"Bad\"}]}}"
            };
            var result = await Query(fake);

            Assert.False(result.Success);
            Assert.AreEqual(0, result.Departments.Count);
        }
    }
