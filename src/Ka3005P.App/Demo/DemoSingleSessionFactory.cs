using Ka3005P.App.Services;
using Ka3005P.Core.Sessions;

namespace Ka3005P.App.Demo;

public sealed class DemoSingleSessionFactory : ISingleSessionFactory
{
	private readonly TimeProvider timeProvider;

	public DemoSingleSessionFactory(TimeProvider timeProvider)
	{
		ArgumentNullException.ThrowIfNull(timeProvider);
		this.timeProvider=timeProvider;
	}

	public async ValueTask<IPowerSupplySession> CreateAsync(
		string portName,
		CancellationToken cancellationToken)
	{
		DemoPowerSupplyDevice device=new(TimeSpan.FromMilliseconds(25));
		PowerSupplySession session=new(
			device,
			timeProvider,
			TimeSpan.FromMilliseconds(100));
		await session.StartAsync(cancellationToken);
		return session;
	}
}
