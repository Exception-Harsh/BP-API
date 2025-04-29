using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using BP_API.Models;
using System.Configuration;
using System.Data;
using System.Linq;

namespace BP_API.Controllers
{
    public class AssetDisbursementController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpPost]
        [Route("api/assetdisbursement/insert/{projectNumber}")]
        public HttpResponseMessage InsertAssetDisbursementRequest(string projectNumber, [FromBody] NewDisbursementRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Request body is null" });
                }

                int drNumber;

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Fetch the maximum DR number
                    string maxDrNumberQuery = "SELECT MAX(PADR_DR_NMBR_N) FROM TBL_PRJCT_ASST_DSBRSMNT_RQST";
                    using (OracleCommand maxDrNumberCommand = new OracleCommand(maxDrNumberQuery, connection))
                    {
                        object result = maxDrNumberCommand.ExecuteScalar();
                        drNumber = result == DBNull.Value ? 1 : Convert.ToInt32(result) + 1;
                    }

                    string insertQuery = @"
                        INSERT INTO TBL_PRJCT_ASST_DSBRSMNT_RQST (
                            PADR_DR_NMBR_N, PADR_PRJCT_NMBR_N, PADR_ASST_NMBR_N, PADR_CTGRY_V, PADR_SB_CTGRY_V,
                            PADR_PRTY_NM_V, PADR_PRTY_GSTN_V, PADR_PRTY_PAN_V, PADR_PRTY_EML_V, PADR_PRTY_MBL_V,
                            PADR_RSN_V, PADR_PO_WO_V, PADR_TTL_ORDR_AMNT_N, PADR_DCMNT_TYP_V, PADR_PRTY_DCMNT_NMBR_V,
                            PADR_PRTY_DCMNT_DT_D, PADR_PRTY_DCMNT_PYBL_DYS_N, PADR_PRTY_DCMNT_AMNT_N, PADR_PRTY_DCMNT_GST_AMNT_N,
                            PADR_PRTY_DCMNT_TTL_AMNT_N, PADR_PRTY_TDS_AMNT_N, PADR_PRTY_ADVNC_ADJSTD_N, PADR_PRTY_RTNTN_AMNT_N,
                            PADR_PRTY_OTHR_DDCTN_AMNT_N, PADR_PRTY_PYBL_AMNT_N, PADR_PRTY_OTSTNDNG_AMNT_N, PADR_BRRWR_ACCNT_NMBR_V,
                            PADR_PRTY_BNK_NM_V, PADR_PRTY_ACCNT_NM_V, PADR_PRTY_ACCNT_NMBR_V, PADR_PRTY_ACCNT_IFSC_V,
                            PADR_STTS_C, PADR_APPRVD_AMNT_N, PADR_RFRNC_DR_NMBR_N, PADR_RMRKS_V, PADR_ATTCHMNT_RFRNC_V,
                            COIN_CRTN_USR_ID_V, COIN_CRTN_DT_D, COIN_LST_MDFD_USR_ID_V, COIN_LST_MDFD_DT_D
                        ) VALUES (
                            :drNumber, :projectNumber, :assetNumber, :category, :subCategory,
                            :partyName, :partyGSTIN, :partyPAN, :partyEmail, :partyMobile,
                            :reason, :purchaseOrder, :totalOrderAmount, :documentType, :partyDocumentNumber,
                            :partyDocumentDate, :partyDocumentPayableDays, :partyDocumentAmount, :partyDocumentGSTAmount,
                            :partyDocumentTotalAmount, :partyTDSAmount, :partyAdvanceAdjusted, :partyRetentionAmount,
                            :partyOtherDeductionAmount, :partyPayableAmount, :partyOutstandingAmount, :borrowerAccountNumber,
                            :partyBankName, :partyAccountName, :partyAccountNumber, :partyAccountIFSC,
                            :status, :approvedAmount, :referenceDRNumber, :remarks, :attachmentReference,
                            :createdBy, SYSDATE, :lastModifiedBy, SYSDATE
                        )";

                    using (OracleCommand command = new OracleCommand(insertQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("drNumber", drNumber));
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("assetNumber", request.AssetNumber));
                        command.Parameters.Add(new OracleParameter("category", request.Category));
                        command.Parameters.Add(new OracleParameter("subCategory", request.SubCategory));
                        command.Parameters.Add(new OracleParameter("partyName", request.PartyName));
                        command.Parameters.Add(new OracleParameter("partyGSTIN", request.PartyGSTIN ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyPAN", request.PartyPAN ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyEmail", request.PartyEmail ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyMobile", request.PartyMobile ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("reason", request.Reason));
                        command.Parameters.Add(new OracleParameter("purchaseOrder", request.PurchaseOrder ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("totalOrderAmount", request.TotalOrderAmount ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("documentType", request.DocumentType ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyDocumentNumber", request.PartyDocumentNumber ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyDocumentDate", request.PartyDocumentDate));
                        command.Parameters.Add(new OracleParameter("partyDocumentPayableDays", request.PartyDocumentPayableDays ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyDocumentAmount", request.PartyDocumentAmount));
                        command.Parameters.Add(new OracleParameter("partyDocumentGSTAmount", request.PartyDocumentGSTAmount));
                        command.Parameters.Add(new OracleParameter("partyDocumentTotalAmount", request.PartyDocumentTotalAmount));
                        command.Parameters.Add(new OracleParameter("partyTDSAmount", request.PartyTDSAmount ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyAdvanceAdjusted", request.PartyAdvanceAdjusted ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyRetentionAmount", request.PartyRetentionAmount ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyOtherDeductionAmount", request.PartyOtherDeductionAmount ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("partyPayableAmount", request.PartyPayableAmount));
                        command.Parameters.Add(new OracleParameter("partyOutstandingAmount", request.PartyOutstandingAmount));
                        command.Parameters.Add(new OracleParameter("borrowerAccountNumber", request.BorrowerAccountNumber));
                        command.Parameters.Add(new OracleParameter("partyBankName", request.PartyBankName));
                        command.Parameters.Add(new OracleParameter("partyAccountName", request.PartyAccountName));
                        command.Parameters.Add(new OracleParameter("partyAccountNumber", request.PartyAccountNumber));
                        command.Parameters.Add(new OracleParameter("partyAccountIFSC", request.PartyAccountIFSC));
                        command.Parameters.Add(new OracleParameter("status", request.Status));
                        command.Parameters.Add(new OracleParameter("approvedAmount", request.ApprovedAmount));
                        command.Parameters.Add(new OracleParameter("referenceDRNumber", request.ReferenceDRNumber ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("remarks", request.Remarks ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("attachmentReference", request.AttachmentReference ?? (object)DBNull.Value));
                        command.Parameters.Add(new OracleParameter("createdBy", request.CreatedBy));
                        command.Parameters.Add(new OracleParameter("lastModifiedBy", request.LastModifiedBy));

                        int rowsInserted = command.ExecuteNonQuery();
                        if (rowsInserted == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "No rows were inserted." });
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Insert successful" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting asset disbursement request: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}