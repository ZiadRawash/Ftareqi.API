namespace Ftareqi.API
{
	public class TokenBucketOptions
	{
		public int Capacity { get; set; } = 10;
		public double RefillRatePerSecond { get; set; } = 2;
	}

	public class AuthTokenBucketOptions : TokenBucketOptions { }
	public class UnauthTokenBucketOptions : TokenBucketOptions { }
}
