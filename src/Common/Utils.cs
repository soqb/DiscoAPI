using Newtonsoft.Json.Serialization;

namespace DiscoAPI.Common;

public class NamingStrategyContractResolver : DefaultContractResolver
{
	public NamingStrategy strategy;
	public NamingStrategyContractResolver(NamingStrategy strategy)
	{
		this.strategy = strategy;
	}

	protected override string ResolvePropertyName(string propertyName) => strategy.GetPropertyName(propertyName, false);
}
