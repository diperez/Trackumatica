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

        // overriding this to "depart" next available appointment.
        // depart action shows smartpanel so invoking underlying method
        public delegate void BaseCalculateCosts();

        [PXOverride]
        public void CalculateCosts(BaseCalculateCosts BaseInvoke)
        {
            BaseInvoke();
            if (Base.AppointmentRecords.Current == null) { return; }

            DateTime? dtSchdDate = Base.Accessinfo.BusinessDate;
            FSAppointmentEmployee employee = Base.AppointmentServiceEmployees.Select().First();

            FSAppointment nextAppointment = PXSelectJoin<FSAppointment,
                                    InnerJoin<FSAppointmentEmployee, On<FSAppointmentEmployee.appointmentID, Equal<FSAppointment.appointmentID>>>,
                                    Where<FSAppointment.appointmentID, NotEqual<Required<FSAppointment.appointmentID>>,
                                        And<FSAppointment.scheduledDateTimeBegin, GreaterEqual<Required<FSAppointment.scheduledDateTimeBegin>>,
                                        And<FSAppointmentEmployee.employeeID, Equal<Required<FSAppointmentEmployee.employeeID>>,
                                        And<FSAppointment.canceled, Equal<False>,
                                        And<FSAppointment.completed, Equal<False>,
                                        And<FSAppointment.closed, Equal<False>>>>>>>,
                                    OrderBy<Asc<FSAppointment.scheduledDateTimeBegin>>>.
                                    SelectWindowed(Base, 0, 1, Base.AppointmentRecords.Current.AppointmentID, dtSchdDate, employee.EmployeeID);

            if (nextAppointment != null)
            {
                AppointmentEntry apptGraph = PXGraph.CreateInstance<AppointmentEntry>();
                apptGraph.AppointmentRecords.Current = apptGraph.AppointmentRecords.Search<FSAppointment.refNbr>
                                                            (nextAppointment.RefNbr, nextAppointment.SrvOrdType);
                //apptGraph.departStaff.Press();
                FSLogActionStartFilter filter = apptGraph.LogActionStartFilter.Current;
                apptGraph.SetLogActionPanelDefaults(apptGraph.LogActionStartFilter.View, filter, ID.LogActions.START, FSLogActionFilter.type.Values.Travel, true);

                foreach (FSAppointmentStaffDistinct row in apptGraph.LogActionStaffDistinctRecords.Select())
                {
                    row.Selected = true;
                    apptGraph.LogActionStaffDistinctRecords.Update(row);
                }


                string travelItemLineRef = apptGraph.GetItemLineRef(Base, apptGraph.AppointmentRecords.Current.AppointmentID, true);

                if (travelItemLineRef == null)
                {
                    FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)apptGraph.AppointmentDetails.Cache.CreateInstance();
                    fsAppointmentDetRow.LineType = ID.LineType_ALL.SERVICE;
                    fsAppointmentDetRow.InventoryID = apptGraph.ServiceOrderTypeSelected.Current.DfltBillableTravelItem;
                    fsAppointmentDetRow = apptGraph.AppointmentDetails.Insert(fsAppointmentDetRow);

                    apptGraph.LogActionFilter.Current.DetLineRef = fsAppointmentDetRow.LineRef;
                }
                else
                {
                    apptGraph.LogActionFilter.Current.DetLineRef = travelItemLineRef;
                }

                apptGraph.StartTravelAction();
                apptGraph.AppointmentRecords.Current.Status = CustomStatus.Traveling; // New Status is added via workflow. 
                apptGraph.AppointmentRecords.UpdateCurrent();
                apptGraph.Save.Press();
            }
        }


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
