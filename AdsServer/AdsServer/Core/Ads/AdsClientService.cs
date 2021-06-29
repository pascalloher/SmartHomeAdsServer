using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace AdsServer.Core.Ads
{

	public class AdsClientService : IHostedService, IAdsService
	{
		private AdsClient client;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			EnsureClientConnected();
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task SwitchOutputAsync(string name, bool state, CancellationToken token)
		{
			EnsureClientConnected();
			var result = await client.WriteValueAsync(name, state, token);
			result.ThrowOnError();
		}

		private bool EnsureClientConnected()
		{
			if(client == null)
				client = new AdsClient();
			if(!client.IsConnected)
				client.Connect(AmsNetId.Local, 851);
			return client.IsConnected;
		}
	}
}
