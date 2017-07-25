using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
				Token = await HttpPost (Constants.Endpoints.AuthApi);
			}
		}


		//public async Task RenewAccessToken ()
		//{
		//	Token = null;
		//	Token = await HttpPost (Constants.Endpoints.AuthApi);

		//	Debug.WriteLine (string.Format ("Renewed token for user: {0} is: {1}",
		//									subscriptionId,
		//									Token));
		//}


		async Task<string> HttpPost (string accessUri)
		{
			try
			{
				using (var client = new HttpClient ())
				{
					var request = new HttpRequestMessage (HttpMethod.Post, accessUri)
					{
						Content = new FormUrlEncodedContent (new KeyValuePair<string, string> [0])
					};

					client.DefaultRequestHeaders.Add (Constants.Keys.SubscriptionKey, subscriptionId);

					var result = await client.SendAsync (request);
					string resultContent = await result.Content.ReadAsStringAsync ();

					return resultContent;
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