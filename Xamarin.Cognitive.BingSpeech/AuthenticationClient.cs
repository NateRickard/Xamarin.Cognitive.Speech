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
					var uriBuilder = new UriBuilder ("https",
													 Constants.Endpoints.Authentication.Host,
													 Constants.Endpoints.Authentication.Port,
													 Constants.Endpoints.Authentication.Path);

					client.DefaultRequestHeaders.Add (Constants.Keys.SubscriptionKey, subscriptionId);

					var result = await client.PostAsync (uriBuilder.Uri, null);

					return await result.Content.ReadAsStringAsync ();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error during auth post: {0}", ex.Message);
				throw;
			}
		}
	}
}