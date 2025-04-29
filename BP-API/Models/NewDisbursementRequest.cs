using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BP_API.Models
{
    public class NewDisbursementRequest
    {
        public long DrNumber { get; set; }
        public int ProjectNumber { get; set; }
        public int AssetNumber { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string PartyName { get; set; }
        public string PartyGSTIN { get; set; }
        public string PartyPAN { get; set; }
        public string PartyEmail { get; set; }
        public string PartyMobile { get; set; }
        public string Reason { get; set; }
        public string PurchaseOrder { get; set; }
        public decimal? TotalOrderAmount { get; set; }
        public string DocumentType { get; set; }
        public string PartyDocumentNumber { get; set; }
        public DateTime PartyDocumentDate { get; set; }
        public int? PartyDocumentPayableDays { get; set; }
        public decimal PartyDocumentAmount { get; set; }
        public decimal PartyDocumentGSTAmount { get; set; }
        public decimal PartyDocumentTotalAmount { get; set; }
        public decimal? PartyTDSAmount { get; set; }
        public decimal? PartyAdvanceAdjusted { get; set; }
        public decimal? PartyRetentionAmount { get; set; }
        public decimal? PartyOtherDeductionAmount { get; set; }
        public decimal PartyPayableAmount { get; set; }
        public decimal PartyOutstandingAmount { get; set; }
        public string BorrowerAccountNumber { get; set; }
        public string PartyBankName { get; set; }
        public string PartyAccountName { get; set; }
        public string PartyAccountNumber { get; set; }
        public string PartyAccountIFSC { get; set; }

        private char _status;
        public char Status
        {
            get => _status;
            set => _status = value;
        }

        public decimal ApprovedAmount { get; set; }
        public int? ReferenceDRNumber { get; set; }
        public string Remarks { get; set; }
        public string AttachmentReference { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}