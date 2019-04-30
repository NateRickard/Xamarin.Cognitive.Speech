using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Xamarin.Cognitive.BingSpeech
{
	/// <summary>
	/// Client that authenticates to the Bing Speech API.
	/// </summary>
	class AuthenticationClient
	{
		readonly string subscriptionKey;
		readonly Endpoint authEndpoint;
		readonly SpeechRegion speechRegion;
		readonly HttpClient client;

		internal string Token { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Cognitive.BingSpeech.AuthenticationClient"/> class.
		/// </summary>
		/// <param name="authEndpoint">The auth endpoint to get an auth token from.</param>
		/// <param name="subscriptionKey">Subscription identifier.</param>
		public AuthenticationClient (Endpoint authEndpoint, string subscriptionKey, SpeechRegion speechRegion)
		{
			this.authEndpoint = authEndpoint;
			this.subscriptionKey = subscriptionKey;
			this.speechRegion = speechRegion;

			client = new HttpClient ();
			client.DefaultRequestHeaders.Add (Constants.Keys.SubscriptionKey, this.subscriptionKey);
		}

		/// <summary>
		/// Calls to the authentication endpoint to get a JWT token that is cached.
		/// </summary>
		/// <param name="forceNewToken">If set to <c>true</c>, force new token even if there is already a cached token.</param>
		public async Task Authenticate (bool forceNewToken = false)
		{
			if (string.IsNullOrEmpty (Token) || forceNewToken)
			{
				Token = null;
				Token = await FetchToken ();
			}
		}

		public void ClearToken ()
		{
			Token = null;
		}

		async Task<string> FetchToken ()
		{
			try
			{
				var uriBuilder = new UriBuilder (authEndpoint.Protocol,
												 $"{speechRegion.ToString().ToLower()}.{authEndpoint.Host}",
												authEndpoint.Port,
												authEndpoint.Path);

				Debug.WriteLine ($"{DateTime.Now} :: Request Uri: {uriBuilder.Uri}");

				var result = await client.PostAsync (uriBuilder.Uri, null);

				if (result.IsSuccessStatusCode)
				{
					Debug.WriteLine ("New authentication token retrieved at {0}", DateTime.Now);

					return await result.Content.ReadAsStringAsync ();
				}

				throw new Exception ($"Unable to authenticate, auth endpoint returned: status code {result.StatusCode} ; Reason: {result.ReasonPhrase}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error during auth post: {0}", ex);
				throw;
			}
		}
	}
}