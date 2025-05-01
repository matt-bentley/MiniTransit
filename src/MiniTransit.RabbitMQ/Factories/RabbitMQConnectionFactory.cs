using MiniTransit.RabbitMQ.Settings;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Net.Security;
using System.Security.Authentication;

namespace MiniTransit.RabbitMQ.Factories
{
    public sealed class RabbitMQConnectionFactory : IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public RabbitMQConnectionFactory(IOptions<RabbitMQMessageBusSettings> options)
        {
            var connectionFactory = new ConnectionFactory()
            {
                HostName = options.Value.HostName,
                VirtualHost = options.Value.VirtualHost,
                UserName = options.Value.UserName,
                Password = options.Value.Password,
                Port = options.Value.Port,
                AutomaticRecoveryEnabled = true
            };
            if (options.Value.EnableTls)
            {
                connectionFactory.Ssl = new SslOption()
                {
                    Enabled = true,
                    AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch,
                    Version = SslProtocols.Tls12
                };
            }
            _connectionFactory = connectionFactory;
        }

        public async ValueTask<IConnection> CreateAsync()
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _connection = await _connectionFactory.CreateConnectionAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return _connection;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
