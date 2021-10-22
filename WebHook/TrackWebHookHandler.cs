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
using PX.Objects.FS;
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
        public Location(string latitude, string longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

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

    public static class CustomStatus
    {
        public const string Traveling = "T";
        public class traveling : PX.Data.BQL.BqlString.Constant<traveling> { public traveling() : base(Traveling) { } }
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

                    var today = new DateTime(2021, 10, 22, 0, 0, 0);
                    var todayat23 = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);
                    DayOfWeek dayOfWeek = today.DayOfWeek;
                    Guid? currentTrackingID = null;
                    Guid gUid = Guid.Parse(parameters.Id);

                    var fsGPSTrackingRequestRows = 
                    PXSelectJoin<FSGPSTrackingRequest,
                    InnerJoin<Users,
                        On<
                            Users.username, Equal<FSGPSTrackingRequest.userName>>>,
                    Where<
                        Users.pKID, Equal<Required<Users.pKID>>>>.Select(graph, gUid);

                    foreach (FSGPSTrackingRequest fsGPSTrackingRequestRow in fsGPSTrackingRequestRows)
                    {
                        switch (dayOfWeek)
                        {
                            case DayOfWeek.Sunday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay1 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            case DayOfWeek.Monday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay2 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            case DayOfWeek.Tuesday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay3 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            case DayOfWeek.Wednesday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay4 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            case DayOfWeek.Thursday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay5 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            case DayOfWeek.Friday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay6 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            case DayOfWeek.Saturday:
                                if (fsGPSTrackingRequestRow.WeeklyOnDay7 == true)
                                {
                                    currentTrackingID = fsGPSTrackingRequestRow.TrackingID;
                                }
                                break;
                            default:
                                currentTrackingID = ((FSGPSTrackingRequest)fsGPSTrackingRequestRows[0]).TrackingID;
                                break;
                        }
                        if (currentTrackingID != null)
                        {
                            break;
                        }
                    }

                    MapLocation returnLocation = new MapLocation();
                    returnLocation.Id = parameters.Id;
                    returnLocation.CompanyId = parameters.CompanyId;

                    FSGPSTrackingHistory latestLocationRow = 
                    (FSGPSTrackingHistory)
                    PXSelect<FSGPSTrackingHistory,
                    Where<
                        FSGPSTrackingHistory.trackingID, Equal<Required<FSGPSTrackingHistory.trackingID>>>,
                    OrderBy<
                        Desc<FSGPSTrackingHistory.executionDate>>>.SelectWindowed(graph, 0, 1, currentTrackingID);

                    if(latestLocationRow != null)
                        returnLocation.LatestLocation = new Location(latestLocationRow.Latitude.ToString(), latestLocationRow.Longitude.ToString());

                    var nextAppointments =
                    PXSelectJoin<EPEmployee,
                    InnerJoin<FSAppointmentEmployee,
                        On<
                            FSAppointmentEmployee.employeeID, Equal<EPEmployee.bAccountID>>,
                    InnerJoin<FSAppointment,
                        On<
                            FSAppointment.appointmentID, Equal<FSAppointmentEmployee.appointmentID>>,
                    LeftJoin<Users,
                        On<
                            EPEmployee.userID, Equal<Users.pKID>>>>>,
                    Where<
                        FSAppointment.status, Equal<CustomStatus.traveling>,
                    And<
                        Users.pKID, Equal<Required<Users.pKID>>,
                    And2<
                       Where<
                            FSAppointment.scheduledDateTimeEnd, GreaterEqual<Required<FSAppointment.scheduledDateTimeEnd>>,
                        And<
                            FSAppointment.scheduledDateTimeBegin, LessEqual<Required<FSAppointment.scheduledDateTimeBegin>>>>,
                        And<
                            Where<FSAppointment.customerID, IsNotNull>>>>>,
                    OrderBy<
                        Asc<FSAppointment.scheduledDateTimeBegin>>>.Select(graph, gUid, today.Date, todayat23);

                    foreach (PXResult<EPEmployee, FSAppointmentEmployee, FSAppointment, Users> row in nextAppointments)
                    {
                        FSAppointment app = (FSAppointment)row;
                        returnLocation.Waypoints.Add(new Location(app.MapLatitude.ToString(), app.MapLongitude.ToString()));
                    }

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
