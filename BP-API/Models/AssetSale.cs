using System;

namespace BP_API.Models
{
    public class AssetSale
    {
        public string YearMonth { get; set; }
        public string ProjectNumber { get; set; }
        public string AssetNumber { get; set; }
        public string Phase { get; set; }
        public string Building { get; set; }
        public string Floor { get; set; }
        public string UnitNumber { get; set; }
        public string UnitConfiguration { get; set; }
        public string UnitType { get; set; }
        public decimal SaleableArea { get; set; }
        public decimal CarpetArea { get; set; }
        public decimal CarpetAreaRR { get; set; }
        public string UniqueUnitNumber { get; set; }
        public string Owner { get; set; }

        // Sold & Registered Flags (Stored as "Y" or "N")
        private string _soldFlag;
        public string SoldFlag
        {
            get => _soldFlag ?? "N"; // Ensure default is "N" when null
            set => _soldFlag = (value == "Y" || value == "N") ? value : "N";
        }

        private string _registeredFlag;
        public string RegisteredFlag
        {
            get => _registeredFlag ?? "N"; // Default to "N" when null
            set => _registeredFlag = (value == "Y" || value == "N") ? value : "N";
        }

        public DateTime? RegistrationDate { get; set; }
        public DateTime? BookingDate { get; set; }
        public DateTime? AllotmentLetterDate { get; set; }
        public DateTime? AgreementDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerKycAadhar { get; set; }
        public string CustomerKycPan { get; set; }
        public string CustomerKycMobile { get; set; }
        public string CustomerKycEmail { get; set; }
        public string CustomerKycAddress { get; set; }
        public string NcIssuedFlag { get; set; }
        public string NcNumber { get; set; }

        // Sales Amounts
        public decimal SalesBasePrice { get; set; }
        public decimal SalesStampDutyAmount { get; set; }
        public decimal SalesRegistrationAmount { get; set; }
        public decimal SalesOtherCharges { get; set; }
        public decimal SalesPassThroughCharges { get; set; }
        public decimal SalesTaxesAmount { get; set; }
        public decimal SalesTotalAmount { get; set; }

        // Demand Amounts
        public decimal DemandBasePrice { get; set; }
        public decimal DemandStampDuty { get; set; }
        public decimal DemandRegistrationAmount { get; set; }
        public decimal DemandOtherCharges { get; set; }
        public decimal DemandPassThroughCharges { get; set; }
        public decimal DemandTaxesAmount { get; set; }
        public decimal DemandTotalAmount { get; set; }

        // Received Amounts
        public decimal ReceivedBasePrice { get; set; }
        public decimal ReceivedStampDutyAmount { get; set; }
        public decimal ReceivedRegistrationAmount { get; set; }
        public decimal ReceivedOtherCharges { get; set; }
        public decimal ReceivedPassThroughCharges { get; set; }
        public decimal ReceivedTaxesAmount { get; set; }
        public decimal ReceivedTotalAmount { get; set; }

        // New Columns
        public string ModeOfFinance { get; set; }
        public string FinancialInstitutionName { get; set; }
        public string PaymentPlanName { get; set; }
        public string SourceOfCustomer { get; set; }
        public string ChannelPartnerName { get; set; }
        public string ChannelPartnerMobile { get; set; }
        public string ChannelPartnerEmail { get; set; }
        public decimal BrokerageAmount { get; set; }
    }
}
