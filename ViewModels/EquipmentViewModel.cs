using MvcPaging;
using InstallationAPPNonUnify.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.ViewModels
{
    public class EquipmentViewModel : BaseViewModel
    {
        public Guid EquipmentId { get; set; }
        public Guid? SiteId { get; set; }
        public Guid? ModifiedBy { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public Guid? BusinessUnitId { get; set; }
        public string Skills { get; set; }
        public byte[] VersionNumber { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? TimeZoneCode { get; set; }
        public bool DisplayInServiceViews { get; set; }
        public bool IsDisabled { get; set; }
        public string Name { get; set; }
        public Guid? CalendarId { get; set; }
        public string Description { get; set; }
        public string EMailAddress { get; set; }
        public Guid OrganizationId { get; set; }
        public int? ImportSequenceNumber { get; set; }
        public DateTime? OverriddenCreatedOn { get; set; }
        public int? TimeZoneRuleVersionNumber { get; set; }
        public int? UTCConversionTimeZoneCode { get; set; }
        public decimal? ExchangeRate { get; set; }
        public Guid? ModifiedOnBehalfBy { get; set; }
        public Guid? CreatedOnBehalfBy { get; set; }
        public Guid? TransactionCurrencyId { get; set; }

    }
}
