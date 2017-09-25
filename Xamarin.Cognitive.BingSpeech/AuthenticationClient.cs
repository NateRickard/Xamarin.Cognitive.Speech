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
		readonly string subscriptionId;

		internal string Token { get; private set; }


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Xamarin.Cognitive.BingSpeech.AuthenticationClient"/> class.
		/// </summary>
		/// <param name="subscriptionId">Subscription identifier.</param>
		public AuthenticationClient (string subscriptionId)
		{
			this.subscriptionId = subscriptionId;
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


		async Task<string> FetchToken ()
		{
			try
			{
				using (var client = new HttpClient ())
				{
					var uriBuilder = new UriBuilder (Endpoints.Authentication.Protocol,
													 Endpoints.Authentication.Host,
													 Endpoints.Authentication.Port,
													 Endpoints.Authentication.Path);

					client.DefaultRequestHeaders.Add (Constants.Keys.SubscriptionKey, subscriptionId);

					Debug.WriteLine ($"{DateTime.Now} :: Request Uri: {uriBuilder.Uri}");

					var result = await client.PostAsync (uriBuilder.Uri, null);

					if (result.IsSuccessStatusCode)
					{
						return await result.Content.ReadAsStringAsync ();
					}

					throw new Exception ($"Unable to authenticate, auth endpoint returned: status code {result.StatusCode} ; Reason: {result.ReasonPhrase}");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error during auth post: {0}", ex);
				throw;
			}
		}
	}
}