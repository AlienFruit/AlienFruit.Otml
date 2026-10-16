using AlienFruit.Otml.Configuration.Abstractions;
using AlienFruit.Otml.Configuration.Keyraft;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AlienFruit.Otml.Configuration.Tests
{
    [Collection("SecretProviderRegistry")]
    public class KeyraftSecretProviderTests
    {
        private static KeyraftSecretProviderOptions DefaultOptions() => new KeyraftSecretProviderOptions
        {
            BaseUrl = "http://keyraft.local:7200/",
            Token = "test-token",
            Namespace = "myapp/prod"
        };

        private static KeyraftSecretProvider Create(
            Func<HttpRequestMessage, HttpResponseMessage> responder,
            out StubHttpMessageHandler stub,
            Action<KeyraftSecretProviderOptions>? configureOptions = null)
        {
            stub = new StubHttpMessageHandler(responder);
            var factory = new StubHttpClientFactory(stub);
            var options = DefaultOptions();
            configureOptions?.Invoke(options);
            return new KeyraftSecretProvider(factory, options);
        }

        [Fact]
        public void Get_secret_returned_by_keyraft_on_200()
        {
            var provider = Create(req =>
                Response(HttpStatusCode.OK,
                    @"{""namespace"":""myapp/prod"",""key"":""DB_PASSWORD"",""value"":"" hunter2 "",""type"":""secret"",""version"":3}"),
                out var stub);

            var secret = provider.GetSecret("DB_PASSWORD");

            secret.Should().Be(" hunter2 "); // value returned verbatim
            stub.LastRequest!.Method.Should().Be(HttpMethod.Get);
            stub.LastRequest.RequestUri!.ToString().Should()
                .Be("http://keyraft.local:7200/v1/kv/myapp/prod/DB_PASSWORD");
            stub.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            stub.LastRequest.Headers.Authorization!.Parameter.Should().Be("test-token");
        }

        [Fact]
        public void Secret_name_is_escaped_in_path()
        {
            var provider = Create(_ => Response(HttpStatusCode.OK, @"{""value"":""v""}"), out var stub);

            provider.GetSecret("weird/key name");

            stub.LastRequest!.RequestUri!.AbsoluteUri.Should()
                .Be("http://keyraft.local:7200/v1/kv/myapp/prod/weird%2Fkey%20name");
        }

        [Fact]
        public void Keyraft_404_returns_null_so_resolution_falls_through_to_the_next_storage()
        {
            var provider = Create(_ => Response(HttpStatusCode.NotFound, @"{""error"":""key not found""}"), out var stub);

            provider.GetSecret("MISSING").Should().BeNull();
            stub.RequestCount.Should().Be(1); // a miss is not a transient failure: no retries
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public void Auth_failures_become_KeyraftCommunicationException_not_secret_missing(HttpStatusCode code)
        {
            var provider = Create(_ => Response(code, @"{""error"":""unauthorized""}"), out _);

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>()
                .Where(e => e.StatusCode == (int)code)
                .WithMessage("*denied access*");
        }

        [Fact]
        public void Server_errors_are_KeyraftCommunicationException_with_status_and_body()
        {
            var provider = Create(_ => Response(HttpStatusCode.InternalServerError, @"{""error"":""engine down""}"), out _, o => o.Retry.MaxRetryAttempts = 0);

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>()
                .Where(e => e.StatusCode == 500)
                .WithMessage("*unexpected status 500*engine down*");
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("")]
        [InlineData("{ \"value\": \"\" }")]
        [InlineData("{ \"key\": \"X\" }")]
        public void Invalid_or_empty_payloads_are_KeyraftCommunicationException(string body)
        {
            var provider = Create(_ => Response(HttpStatusCode.OK, body), out _);

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>();
        }

        [Fact]
        public void Network_failure_is_KeyraftCommunicationException()
        {
            var provider = Create(_ => throw new HttpRequestException("connect failed", null, HttpStatusCode.GatewayTimeout), out _, o => o.Retry.MaxRetryAttempts = 0);

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>().WithMessage("*Cannot reach*");
        }

        [Fact]
        public async Task GetSecretAsync_resolves_secret()
        {
            var provider = Create(_ => Response(HttpStatusCode.OK, @"{""value"":""async-v""}"), out _);

            (await provider.GetSecretAsync("K", CancellationToken.None)).Should().Be("async-v");
        }

        [Fact]
        public void Invalid_options_are_rejected_by_ctor()
        {
            var factory = new StubHttpClientFactory(new StubHttpMessageHandler(_ => Response(HttpStatusCode.OK, "{}")));

            var bad = new KeyraftSecretProviderOptions { BaseUrl = "not-a-url", Token = "t", Namespace = "n" };
            var act = () => new KeyraftSecretProvider(factory, bad);

            act.Should().Throw<InvalidOperationException>().WithMessage("*BaseUrl*");

            act = () => new KeyraftSecretProvider(factory, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void Empty_secret_name_returns_null_instead_of_hitting_network()
        {
            var provider = Create(_ => throw new InvalidOperationException("must not call network"), out _);

            provider.GetSecret(" ").Should().BeNull();
        }

        #region retries

        [Fact]
        public void Transient_server_errors_are_retried_until_success()
        {
            var attempts = 0;
            var provider = Create(_ => ++attempts switch
            {
                1 => Response(HttpStatusCode.ServiceUnavailable, @"{""error"":""quorum""}"),
                2 => Response(HttpStatusCode.BadGateway, @"{""error"":""bad gateway""}"),
                _ => Response(HttpStatusCode.OK, @"{""value"":""after-retry""}")
            }, out var stub, o => o.Retry.InitialDelay = TimeSpan.Zero);

            provider.GetSecret("DB_PASSWORD").Should().Be("after-retry");
            stub.RequestCount.Should().Be(3);
        }

        [Fact]
        public void Transport_failure_is_retried_until_success()
        {
            var attempts = 0;
            var provider = Create(_ =>
            {
                if (++attempts == 1)
                    throw new HttpRequestException("connect failed");

                return Response(HttpStatusCode.OK, @"{""value"":""v""}");
            }, out var stub, o => o.Retry.InitialDelay = TimeSpan.Zero);

            provider.GetSecret("DB_PASSWORD").Should().Be("v");
            stub.RequestCount.Should().Be(2);
        }

        [Fact]
        public void Per_attempt_timeout_is_retried_until_success()
        {
            var attempts = 0;
            var provider = Create(_ =>
            {
                if (++attempts == 1)
                    throw new TaskCanceledException("simulated client timeout");

                return Response(HttpStatusCode.OK, @"{""value"":""late-ok""}");
            }, out var stub, o => o.Retry.InitialDelay = TimeSpan.Zero);

            provider.GetSecret("DB_PASSWORD").Should().Be("late-ok");
            stub.RequestCount.Should().Be(2);
        }

        [Fact]
        public void Exhausted_retries_surface_the_last_transient_response()
        {
            var provider = Create(
                _ => Response(HttpStatusCode.InternalServerError, @"{""error"":""engine down""}"),
                out var stub,
                o => { o.Retry.MaxRetryAttempts = 1; o.Retry.InitialDelay = TimeSpan.Zero; });

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>()
                .Where(e => e.StatusCode == 500)
                .WithMessage("*unexpected status 500*");
            stub.RequestCount.Should().Be(2);
        }

        [Fact]
        public void Exhausted_timeouts_become_KeyraftCommunicationException()
        {
            var provider = Create(
                _ => throw new TaskCanceledException("simulated timeout"),
                out var stub,
                o => { o.Retry.MaxRetryAttempts = 1; o.Retry.InitialDelay = TimeSpan.Zero; });

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>().WithMessage("*timed out*");
            stub.RequestCount.Should().Be(2);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Conflict)]
        public void Non_transient_responses_are_not_retried(HttpStatusCode code)
        {
            var provider = Create(_ => Response(code, @"{""error"":""permanent""}"), out var stub);

            var act = () => provider.GetSecret("DB_PASSWORD");

            act.Should().Throw<KeyraftCommunicationException>();
            stub.RequestCount.Should().Be(1);
        }

        [Fact]
        public async Task Caller_cancellation_aborts_without_retrying()
        {
            using var cts = new CancellationTokenSource();
            var provider = Create(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            }, out var stub);

            Func<Task> act = () => provider.GetSecretAsync("DB_PASSWORD", cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            stub.RequestCount.Should().Be(1);
        }

        [Fact]
        public void Retry_delay_grows_exponentially_and_caps_at_MaxDelay()
        {
            // Default policy: 200 ms initial delay, multiplier 2.0.
            new KeyraftRetryOptions().GetDelay(0).Should().Be(TimeSpan.FromMilliseconds(200));
            new KeyraftRetryOptions().GetDelay(1).Should().Be(TimeSpan.FromMilliseconds(400));

            var retry = new KeyraftRetryOptions
            {
                InitialDelay = TimeSpan.FromMilliseconds(10),
                BackoffMultiplier = 3,
                MaxDelay = TimeSpan.FromMilliseconds(25)
            };

            retry.GetDelay(0).Should().Be(TimeSpan.FromMilliseconds(10));
            retry.GetDelay(1).Should().Be(TimeSpan.FromMilliseconds(25)); // 30 ms capped at MaxDelay
            retry.GetDelay(5).Should().Be(TimeSpan.FromMilliseconds(25));
        }

        [Fact]
        public void Invalid_retry_and_client_timeout_options_are_rejected_by_ctor()
        {
            var breakers = new Action<KeyraftSecretProviderOptions>[]
            {
                o => o.Retry.MaxRetryAttempts = -1,
                o => o.Retry.InitialDelay = TimeSpan.FromSeconds(-1),
                o => o.Retry.BackoffMultiplier = 0.5,
                o => o.Retry.MaxDelay = TimeSpan.FromMilliseconds(1), // smaller than default InitialDelay
                o => o.Retry = null!,
                o => o.ClientTimeout = TimeSpan.Zero,
                o => o.ClientTimeout = TimeSpan.FromSeconds(-3),
            };

            foreach (var breaker in breakers)
            {
                var options = DefaultOptions();
                breaker(options);
                var factory = new StubHttpClientFactory(
                    new StubHttpMessageHandler(_ => Response(HttpStatusCode.OK, "{}")));

                var act = () => new KeyraftSecretProvider(factory, options);

                act.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void CreateStandaloneClient_applies_options_ClientTimeout()
        {
            using var defaultClient = KeyraftServiceCollectionExtensions.CreateStandaloneClient(
                new KeyraftSecretProviderOptions());

            defaultClient.Timeout.Should().Be(System.Threading.Timeout.InfiniteTimeSpan);

            using var cappedClient = KeyraftServiceCollectionExtensions.CreateStandaloneClient(
                new KeyraftSecretProviderOptions { ClientTimeout = TimeSpan.FromSeconds(7) });

            cappedClient.Timeout.Should().Be(TimeSpan.FromSeconds(7));
        }

        #endregion

        #region DI

        private static KeyraftSecretProviderOptions ValidRegistration() => new KeyraftSecretProviderOptions
        {
            BaseUrl = "http://localhost:7200",
            Token = "root-token",
            Namespace = "myapp/dev",
            Timeout = TimeSpan.FromSeconds(5)
        };

        [Fact]
        public void AddKeyraftSecretProvider_resolves_ISecretProvider_as_keyraft()
        {
            var services = new ServiceCollection();

            services.AddKeyraftSecretProvider(ValidRegistration());

            var provider = services.BuildServiceProvider().GetRequiredService<ISecretProvider>();
            provider.Should().BeOfType<KeyraftSecretProvider>();
        }

        [Fact]
        public void AddKeyraftSecretProvider_registers_the_same_instance_in_SecretProviderRegistry()
        {
            var services = new ServiceCollection();

            services.AddKeyraftSecretProvider(o =>
            {
                o.BaseUrl = "http://keyraft.local";
                o.Token = "t";
                o.Namespace = "ns/registry-test";
            });

            var provider = services.BuildServiceProvider().GetRequiredService<ISecretProvider>();
            SecretProviderRegistry.GetAll().Should().Contain(provider);
        }

        [Fact]
        public void AddKeyraftSecretProvider_validates_options_at_registration()
        {
            var services = new ServiceCollection();

            var act = () => services.AddKeyraftSecretProvider(new KeyraftSecretProviderOptions());

            act.Should().Throw<InvalidOperationException>().WithMessage("*BaseUrl*");
        }

        [Fact]
        public void AddKeyraftSecretProvider_options_overload_snapshots_the_options_instance()
        {
            var services = new ServiceCollection();
            var options = ValidRegistration();

            services.AddKeyraftSecretProvider(options);
            options.Namespace = "myapp/other";
            options.Retry.MaxRetryAttempts = -1;

            // The registered provider keeps the clone: later mutation of the caller's instance
            // cannot change resolution behavior.
            SecretProviderRegistry.GetAll()[^1].Name.Should().Be("keyraft:myapp/dev");
        }

        #endregion

        #region stubs

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
                => this.responder = responder;

            public HttpRequestMessage? LastRequest { get; private set; }

            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                RequestCount++;
                return Task.FromResult(this.responder(request));
            }
        }

        private sealed class StubHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpMessageHandler handler;

            public StubHttpClientFactory(HttpMessageHandler handler) => this.handler = handler;

            public HttpClient CreateClient(string name) => new HttpClient(handler, disposeHandler: false);
        }

        private static HttpResponseMessage Response(HttpStatusCode code, string json)
            => new HttpResponseMessage(code) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

        #endregion
    }
}
