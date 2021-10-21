using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json;
using PX.Data;
using PX.Data.Webhooks;
using PX.FS;
using PX.Objects.EP;
using PX.SM;

namespace Tracumatica
{
    public class Parameters
    {
        public string CompanyId { get; set; }
        public string Id { get; set; }
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

    public class Location
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class MapLocation    
    {
        public MapLocation()
        {
            this.Waypoints = new List<Location>();
        }

        public string CompanyId { get; set; }
        public string Id { get; set; }
        public Location LatestLocation { get; set; }
        public List<Location> Waypoints { get; set; }
    }

    public class TrackWebHookHandler : IWebhookHandler
    {
        public async Task<IHttpActionResult> ProcessRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parameters = JsonConvert.DeserializeObject<Parameters>(await request.Content.ReadAsStringAsync());

            //logic to read latest location and find way points.
            using (var scope = GetAdminScope())
            {
                try
                {
                    var secret = string.Empty;
                    var graph = new PXGraph();

                    if (request.Headers.TryGetValues("CustomAuthorization", out IEnumerable<string> headerValues))
                    {
                        secret = headerValues.FirstOrDefault();
                    }

                    //if (secret != "secretValue") return new StatusCodeResult(System.Net.HttpStatusCode.Unauthorized, request);

                    DayOfWeek dayOfWeek = DateTime.Now.DayOfWeek;
                    string currentTrackingID = string.Empty;

                    var fsGPSTrackingRequestRows = 
                    PXSelectJoin<FSGPSTrackingRequest,
                    InnerJoin<Users,
                        On<
                            Users.username, Equal<FSGPSTrackingRequest.userName>>>,
                    Where<
                        Users.pKID, Equal<Required<Users.pKID>>>>.Select(graph, parameters.Id);

                    if (fsGPSTrackingRequestRows != null && fsGPSTrackingRequestRows.Count > 0)
                    {
                        foreach (FSGPSTrackingRequest fsGPSTrackingRequestRow in fsGPSTrackingRequestRows)
                        {
                            switch (dayOfWeek)
                            {
                                case DayOfWeek.Sunday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay1 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                case DayOfWeek.Monday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay2 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                case DayOfWeek.Tuesday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay3 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                case DayOfWeek.Wednesday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay4 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                case DayOfWeek.Thursday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay5 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                case DayOfWeek.Friday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay6 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                case DayOfWeek.Saturday:
                                    if (fsGPSTrackingRequestRow.WeeklyOnDay7 == true)
                                    {
                                        currentTrackingID = fsGPSTrackingRequestRow.TrackingID.ToString();
                                    }
                                    break;
                                default:
                                    currentTrackingID = ((FSGPSTrackingRequest)fsGPSTrackingRequestRows[0]).TrackingID.ToString();
                                    break;
                            }
                            if (currentTrackingID != string.Empty)
                            {
                                break;
                            }
                        }
                    }

                    FSGPSTrackingHistory latestLocationRow = 
                    PXSelect<FSGPSTrackingHistory,
                    Where<
                        FSGPSTrackingHistory.trackingID, Equal<Required<FSGPSTrackingHistory.trackingID>>>,
                    OrderBy<
                        Desc<FSGPSTrackingHistory.executionDate>>>.SelectWindowed(graph, 0, 1, currentTrackingID);

                    MapLocation returnLocation = new MapLocation();
                    returnLocation.Id = parameters.Id;
                    returnLocation.CompanyId = parameters.CompanyId;
                    returnLocation.LatestLocation = new Location { latitude = latestLocationRow.Latitude.ToString(), longitude = latestLocationRow .Longitude.ToString() };

                    return new JsonTextActionResult(request, JsonConvert.SerializeObject(returnLocation));
                }
                catch (Exception ex)
                {
                    var failed = new ExceptionResult(ex, false, new DefaultContentNegotiator(), request, new[] { new JsonMediaTypeFormatter() });

                    return failed;
                }
            }
        }

        private IDisposable GetAdminScope()
        {
            var userName = "admin";
            if (PXDatabase.Companies.Length > 0)
            {
                var company = PXAccess.GetCompanyName();
                if (string.IsNullOrEmpty(company))
                {
                    company = PXDatabase.Companies[0];
                }
                userName = userName + "@" + company;
            }
            return new PXLoginScope(userName);
        }
    }
}
