namespace Arma3WebService
{
	public class Arma3Payload
	{
		public string? Log { get; set; }
	}

	public class ServiceReturnPayload
	{
		public DateTime Date { get { return DateTime.Now; } }
	}
}
