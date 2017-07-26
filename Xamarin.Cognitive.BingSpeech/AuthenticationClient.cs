using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Xamarin.Cognitive.BingSpeech
{
	public class AuthenticationClient
	{
		readonly string subscriptionId;

		public string Token { get; private set; }


		public AuthenticationClient (string subscriptionId)
		{
			this.subscriptionId = subscriptionId;
		}


		public async Task Authenticate (bool forceNewToken = false)
		{
			if (string.IsNullOrEmpty (Token) || forceNewToken)
			{
				Token = null;
				Token = await FetchToken (Constants.Endpoints.AuthApi);
			}
		}


		async Task<string> FetchToken (string accessUri)
		{
			try
			{
				using (var client = new HttpClient ())
				{
					client.DefaultRequestHeaders.Add (Constants.Keys.SubscriptionKey, subscriptionId);

					var result = await client.PostAsync (accessUri, null);

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