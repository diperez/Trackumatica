using PX.Data;
using PX.FS;
using PX.Objects.FS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tracumatica
{
    public class SetupMaintExt : PXGraphExtension<SetupMaint>
    {
        public static bool IsActive() => true;
        public const string trackingID = "3427D34D-30AD-4CB3-8E08-CA79F81DF768";

        #region StartRouteApp1
        public PXAction<FSSetup> startRouteApp1;
        [PXUIField(DisplayName = "Start Route App1", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable StartRouteApp1(PXAdapter adapter)
        {
            PXLongOperation.StartOperation(
                Base,
                delegate ()
                {
                    PXCache<FSGPSTrackingHistory> cache = new PXCache<FSGPSTrackingHistory>(Base);

                    double? latitude = null;
                    double? longitude = null;
                    FSGPSTrackingHistory latestLocationRow =
                    (FSGPSTrackingHistory)
                    PXSelect<FSGPSTrackingHistory,
                    Where<
                        FSGPSTrackingHistory.trackingID, Equal<Required<FSGPSTrackingHistory.trackingID>>>,
                    OrderBy<
                        Desc<FSGPSTrackingHistory.executionDate>>>.SelectWindowed(Base, 0, 1, Guid.Parse(trackingID));

                    var today = latestLocationRow.ExecutionDate;
                    if (today == null)
                        today = new DateTime(2021, 10, 22, 0, 0, 0);

                    foreach (var point in GeoLocations.app1)
                    {
                        if (latitude == null)
                        {
                            latitude = point;
                        } else if(longitude == null)
                        {
                            longitude = point;
                        }

                        if (latitude != null && longitude != null)
                        {
                            FSGPSTrackingHistory row = new FSGPSTrackingHistory();

                            row.Latitude = (decimal?)latitude;
                            row.Longitude = (decimal?)longitude;

                            row.ExecutionDate = today = today.Value.AddMinutes(5);

                            row.TrackingID = Guid.Parse(trackingID);
                            row.Altitude = 0;

                            cache.PersistInserted(row);
                            latitude = longitude = null;

                            Thread.Sleep(500);
                        }
                    }
                });

            return adapter.Get();
        }
        #endregion

        #region StartRouteApp2
        public PXAction<FSSetup> startRouteApp2;
        [PXUIField(DisplayName = "Start Route App2", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        public virtual IEnumerable StartRouteApp2(PXAdapter adapter)
        {
            PXLongOperation.StartOperation(
                Base,
                delegate ()
                {
                    PXCache<FSGPSTrackingHistory> cache = new PXCache<FSGPSTrackingHistory>(Base);

                    double? latitude = null;
                    double? longitude = null;

                    FSGPSTrackingHistory latestLocationRow =
                    (FSGPSTrackingHistory)
                    PXSelect<FSGPSTrackingHistory,
                    Where<
                        FSGPSTrackingHistory.trackingID, Equal<Required<FSGPSTrackingHistory.trackingID>>>,
                    OrderBy<
                        Desc<FSGPSTrackingHistory.executionDate>>>.SelectWindowed(Base, 0, 1, Guid.Parse(trackingID));

                    var today = latestLocationRow.ExecutionDate;
                    if (today == null)
                        today = new DateTime(2021, 10, 22, 0, 0, 0);

                    foreach (var point in GeoLocations.app2)
                    {
                        if (latitude == null)
                        {
                            latitude = point;
                        }
                        else if (longitude == null)
                        {
                            longitude = point;
                        }

                        if (latitude != null && longitude != null)
                        {
                            FSGPSTrackingHistory row = new FSGPSTrackingHistory();

                            row.Latitude = (decimal?)latitude;
                            row.Longitude = (decimal?)longitude;

                            row.ExecutionDate = today = today.Value.AddMinutes(5);

                            row.TrackingID = Guid.Parse(trackingID);
                            row.Altitude = 0;

                            cache.PersistInserted(row);
                            latitude = longitude = null;

                            Thread.Sleep(500);
                        }
                    }
                });

            return adapter.Get();
        }
        #endregion
    }
}
