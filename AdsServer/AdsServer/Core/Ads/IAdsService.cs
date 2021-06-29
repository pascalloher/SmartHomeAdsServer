using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace AdsServer.Core.Ads
{
	public interface IAdsService
	{
		public Task SwitchOutputAsync(string name, bool state, CancellationToken token);
	}
}
