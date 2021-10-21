using PX.Data;
using PX.Objects.FS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tracumatica
{
    public class AppointmentEntryExt : PXGraphExtension<AppointmentEntry>
    {
        public static bool IsActive() => true;

        protected virtual void _(Events.RowPersisting<FSAppointment> e)
        {
            if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
            {
                SetGeoLocation(Base.ServiceOrder_Address.Current, Base.ServiceOrderTypeSelected.Current);
            }
        }

        public virtual void SetGeoLocation(FSAddress fsAddressRow, FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsSrvOrdTypeRow != null)
            {
                FSAppointment fsAppointmentRow = Base.AppointmentSelected.Current;

                if (fsAppointmentRow != null)
                {
                    try
                    {
                        GLocation[] results = Geocoder.Geocode(SharedFunctions.GetAppointmentAddress(fsAddressRow), Base.SetupRecord.Current.MapApiKey);

                        if (results != null
                            && results.Length > 0)
                        {
                            //If there are many locations, we just pick first one
                            fsAppointmentRow.MapLatitude = (decimal)results[0].LatLng.Latitude;
                            fsAppointmentRow.MapLongitude = (decimal)results[0].LatLng.Longitude;
                        }
                    }
                    catch
                    {
                        // Do nothing.
                    }
                }
            }
        }
    }
}
