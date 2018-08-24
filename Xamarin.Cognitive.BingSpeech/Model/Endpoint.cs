namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// Endpoint details.
	/// </summary>
	public class Endpoint
	{
		/// <summary>
		/// Gets the protocol.
		/// </summary>
		public string Protocol { get; private set; } = "https";


		/// <summary>
		/// Gets the host.
		/// </summary>
		public string Host { get; private set; }


		/// <summary>
		/// Gets the port.
		/// </summary>
		public int Port { get; private set; } = 443;


		/// <summary>
		/// Gets the path.
		/// </summary>
		public string Path { get; private set; }


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Cognitive.BingSpeech.Endpoint"/> class.
		/// </summary>
		/// <param name="protocol">Protocol.</param>
		/// <param name="host">Host.</param>
		/// <param name="port">Port.</param>
		/// <param name="path">Path, including the leading '/'</param>
		public Endpoint (string protocol, string host, int port, string path)
		{
			Protocol = protocol;
			Host = host;
			Port = port;
			Path = path;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Cognitive.BingSpeech.Endpoint"/> class. 
		/// Assumes Https protocol.
		/// </summary>
		/// <param name="host">Host.</param>
		/// <param name="port">Port.</param>
		/// <param name="path">Path, including the leading '/'</param>
		public Endpoint (string host, int port, string path)
		{
			Host = host;
			Port = port;
			Path = path;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Cognitive.BingSpeech.Endpoint"/> class. 
		/// Assumes Https protocol and port 443.
		/// </summary>
		/// <param name="host">Host.</param>
		/// <param name="path">Path, including the leading '/'</param>
		public Endpoint (string host, string path)
		{
			Host = host;
			Path = path;
		}
	}
}