using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
//using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
//using System.Web.Http;
//using System.Web.Http.Results;
using Newtonsoft.Json;
using PX.Common;
using PX.Data;
using PX.Data.Webhooks;
using PX.Objects;
using PX.Objects.IN;

namespace Tracumatica
{
	public class Notification
	{
		public string CompanyId { get; set; }
		public string Id { get; set; }
		public ulong TimeStamp { get; set; }
	}

	public class TrackWebHookHandler : IWebhookHandler
    {
        public async Task<IHttpActionResult> ProcessRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
			var notification = JsonConvert.DeserializeObject<Notification>(await request.Content.ReadAsStringAsync());
			return new JsonTextActionResult(request, "{\"sessionId\":\"" + "chechy" + "\",\"code\":\"" + "1" + "\",\"expiration\":\"" + "23" + "\"}");
			throw new NotImplementedException();
        }

        public class JsonTextActionResult : IHttpActionResult
        {
            public HttpRequestMessage Request { get; }
            public string JsonText { get; }
            public JsonTextActionResult(HttpRequestMessage request, string jsonText)
            {
                Request = request;
                JsonText = jsonText;
            }
            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute());
            }
            public HttpResponseMessage Execute()
            {
                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JsonText, Encoding.UTF8, "application/json");
                return response;
            }
        }
    }
}
