using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using BP_API.Models;
using System.Configuration;
using System.Data;
using System.Net.Http.Headers;
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




        [HttpGet]
        [Route("api/assetdisbursement/all")]
        public HttpResponseMessage GetAllAssetDisbursementRequests()
        {
            try
            {
                List<NewDisbursementRequest> requests = new List<NewDisbursementRequest>();

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT
                    PADR_DR_NMBR_N, PADR_PRJCT_NMBR_N, PADR_ASST_NMBR_N, PADR_CTGRY_V, PADR_SB_CTGRY_V,
                    PADR_PRTY_NM_V, PADR_PRTY_GSTN_V, PADR_PRTY_PAN_V, PADR_PRTY_EML_V, PADR_PRTY_MBL_V,
                    PADR_RSN_V, PADR_PO_WO_V, PADR_TTL_ORDR_AMNT_N, PADR_DCMNT_TYP_V, PADR_PRTY_DCMNT_NMBR_V,
                    PADR_PRTY_DCMNT_DT_D, PADR_PRTY_DCMNT_PYBL_DYS_N, PADR_PRTY_DCMNT_AMNT_N, PADR_PRTY_DCMNT_GST_AMNT_N,
                    PADR_PRTY_DCMNT_TTL_AMNT_N, PADR_PRTY_TDS_AMNT_N, PADR_PRTY_ADVNC_ADJSTD_N, PADR_PRTY_RTNTN_AMNT_N,
                    PADR_PRTY_OTHR_DDCTN_AMNT_N, PADR_PRTY_PYBL_AMNT_N, PADR_PRTY_OTSTNDNG_AMNT_N, PADR_BRRWR_ACCNT_NMBR_V,
                    PADR_PRTY_BNK_NM_V, PADR_PRTY_ACCNT_NM_V, PADR_PRTY_ACCNT_NMBR_V, PADR_PRTY_ACCNT_IFSC_V,
                    PADR_STTS_C, PADR_APPRVD_AMNT_N, PADR_RFRNC_DR_NMBR_N, PADR_RMRKS_V, PADR_ATTCHMNT_RFRNC_V,
                    COIN_CRTN_USR_ID_V, COIN_CRTN_DT_D, COIN_LST_MDFD_USR_ID_V, COIN_LST_MDFD_DT_D
                FROM TBL_PRJCT_ASST_DSBRSMNT_RQST";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                NewDisbursementRequest request = new NewDisbursementRequest
                                {
                                    DrNumber = Convert.ToInt64(reader["PADR_DR_NMBR_N"]),
                                    ProjectNumber = Convert.ToInt32(reader["PADR_PRJCT_NMBR_N"]),
                                    AssetNumber = Convert.ToInt32(reader["PADR_ASST_NMBR_N"]),
                                    Category = reader["PADR_CTGRY_V"].ToString(),
                                    SubCategory = reader["PADR_SB_CTGRY_V"].ToString(),
                                    PartyName = reader["PADR_PRTY_NM_V"].ToString(),
                                    PartyGSTIN = reader["PADR_PRTY_GSTN_V"].ToString(),
                                    PartyPAN = reader["PADR_PRTY_PAN_V"].ToString(),
                                    PartyEmail = reader["PADR_PRTY_EML_V"].ToString(),
                                    PartyMobile = reader["PADR_PRTY_MBL_V"].ToString(),
                                    Reason = reader["PADR_RSN_V"].ToString(),
                                    PurchaseOrder = reader["PADR_PO_WO_V"].ToString(),
                                    TotalOrderAmount = reader["PADR_TTL_ORDR_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["PADR_TTL_ORDR_AMNT_N"]) : (decimal?)null,
                                    DocumentType = reader["PADR_DCMNT_TYP_V"].ToString(),
                                    PartyDocumentNumber = reader["PADR_PRTY_DCMNT_NMBR_V"].ToString(),
                                    PartyDocumentDate = Convert.ToDateTime(reader["PADR_PRTY_DCMNT_DT_D"]),
                                    PartyDocumentPayableDays = reader["PADR_PRTY_DCMNT_PYBL_DYS_N"] != DBNull.Value ? Convert.ToInt32(reader["PADR_PRTY_DCMNT_PYBL_DYS_N"]) : (int?)null,
                                    PartyDocumentAmount = Convert.ToDecimal(reader["PADR_PRTY_DCMNT_AMNT_N"]),
                                    PartyDocumentGSTAmount = Convert.ToDecimal(reader["PADR_PRTY_DCMNT_GST_AMNT_N"]),
                                    PartyDocumentTotalAmount = Convert.ToDecimal(reader["PADR_PRTY_DCMNT_TTL_AMNT_N"]),
                                    PartyTDSAmount = reader["PADR_PRTY_TDS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["PADR_PRTY_TDS_AMNT_N"]) : (decimal?)null,
                                    PartyAdvanceAdjusted = reader["PADR_PRTY_ADVNC_ADJSTD_N"] != DBNull.Value ? Convert.ToDecimal(reader["PADR_PRTY_ADVNC_ADJSTD_N"]) : (decimal?)null,
                                    PartyRetentionAmount = reader["PADR_PRTY_RTNTN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["PADR_PRTY_RTNTN_AMNT_N"]) : (decimal?)null,
                                    PartyOtherDeductionAmount = reader["PADR_PRTY_OTHR_DDCTN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["PADR_PRTY_OTHR_DDCTN_AMNT_N"]) : (decimal?)null,
                                    PartyPayableAmount = Convert.ToDecimal(reader["PADR_PRTY_PYBL_AMNT_N"]),
                                    PartyOutstandingAmount = Convert.ToDecimal(reader["PADR_PRTY_OTSTNDNG_AMNT_N"]),
                                    BorrowerAccountNumber = reader["PADR_BRRWR_ACCNT_NMBR_V"].ToString(),
                                    PartyBankName = reader["PADR_PRTY_BNK_NM_V"].ToString(),
                                    PartyAccountName = reader["PADR_PRTY_ACCNT_NM_V"].ToString(),
                                    PartyAccountNumber = reader["PADR_PRTY_ACCNT_NMBR_V"].ToString(),
                                    PartyAccountIFSC = reader["PADR_PRTY_ACCNT_IFSC_V"].ToString(),
                                    Status = Convert.ToChar(reader["PADR_STTS_C"]),
                                    ApprovedAmount = Convert.ToDecimal(reader["PADR_APPRVD_AMNT_N"]),
                                    ReferenceDRNumber = reader["PADR_RFRNC_DR_NMBR_N"] != DBNull.Value ? Convert.ToInt32(reader["PADR_RFRNC_DR_NMBR_N"]) : (int?)null,
                                    Remarks = reader["PADR_RMRKS_V"].ToString(),
                                    AttachmentReference = reader["PADR_ATTCHMNT_RFRNC_V"].ToString(),
                                    CreatedBy = reader["COIN_CRTN_USR_ID_V"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["COIN_CRTN_DT_D"]),
                                    LastModifiedBy = reader["COIN_LST_MDFD_USR_ID_V"].ToString(),
                                    LastModifiedDate = Convert.ToDateTime(reader["COIN_LST_MDFD_DT_D"])
                                };
                                requests.Add(request);
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, requests);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asset disbursement requests: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }



        [HttpGet]
        [Route("api/filestorage/max-fdsb-number")]
        public HttpResponseMessage GetMaxFdsbNumber()
        {
            try
            {
                int maxNumber;

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT MAX(FDSB_NMBR_N) FROM TBL_FL_DT_STRG_BLB";
                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        maxNumber = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { maxFdsbNumber = maxNumber });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching max FDSB_NMBR_N: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/borrowerAccountNumber/{projectNumber}/{assetNumber}")]
        public IHttpActionResult AccountNumber(string projectNumber, string assetNumber)
        {
            List<ProjectAssetBankAccount> accounts = new List<ProjectAssetBankAccount>();

            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                string query = "SELECT PABA_ACCNT_NMMBR_V, PABA_TYP_C FROM tbl_prjct_asst_bnk_accnts WHERE paba_prjct_nmbr_n = :projectNumber AND paba_asst_nmbr_n = :assetNumber";

                OracleCommand command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                command.Parameters.Add(new OracleParameter("assetNumber", assetNumber));

                try
                {
                    connection.Open();
                    OracleDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        ProjectAssetBankAccount account = new ProjectAssetBankAccount
                        {
                            AccountNumber = reader["PABA_ACCNT_NMMBR_V"].ToString(),
                            AccountType = reader["PABA_TYP_C"].ToString()
                        };
                        accounts.Add(account);
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }

            if (accounts.Count == 0)
            {
                return NotFound();
            }

            return Ok(accounts);
        }

        [HttpGet]
        [Route("api/filestorage/download/{fileNumber}")]
        public HttpResponseMessage DownloadFile(int fileNumber)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT FDSB_FL_NM_V, FDSB_FL_TYP_V, FDSB_FL_B FROM TBL_FL_DT_STRG_BLB WHERE FDSB_NMBR_N = :fileNumber";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("fileNumber", fileNumber));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                byte[] fileData = (byte[])reader["FDSB_FL_B"];
                                string fileName = reader["FDSB_FL_NM_V"].ToString();
                                string fileType = reader["FDSB_FL_TYP_V"].ToString();

                                var result = new HttpResponseMessage(HttpStatusCode.OK)
                                {
                                    Content = new ByteArrayContent(fileData)
                                };
                                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                                {
                                    FileName = fileName
                                };

                                // Set the correct ContentType based on the file type
                                switch (fileType.ToLower())
                                {
                                    case "pdf":
                                        result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                                        break;
                                    case "excel":
                                        result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                                        break;
                                    // Add more cases for other file types as needed
                                    default:
                                        result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                        break;
                                }

                                return result;
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "File not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }



        [HttpGet]
        [Route("api/filestorage/files/{attachmentReferences}")]
        public HttpResponseMessage GetFilesByAttachmentReferences(string attachmentReferences)
        {
            try
            {
                List<FileStorage> files = new List<FileStorage>();
                var references = attachmentReferences.Split(',').Select(int.Parse).ToList();

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Create a query with placeholders for each reference
                    string placeholders = string.Join(",", references.Select((_, i) => $":ref{i}"));
                    string query = $"SELECT FDSB_NMBR_N, FDSB_FL_NM_V, FDSB_FL_TYP_V, FDSB_FL_B FROM TBL_FL_DT_STRG_BLB WHERE FDSB_NMBR_N IN ({placeholders})";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        // Add parameters for each reference
                        for (int i = 0; i < references.Count; i++)
                        {
                            command.Parameters.Add(new OracleParameter($":ref{i}", references[i]));
                        }

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                FileStorage file = new FileStorage
                                {
                                    Number = Convert.ToInt32(reader["FDSB_NMBR_N"]),
                                    FileName = reader["FDSB_FL_NM_V"].ToString(),
                                    FileType = reader["FDSB_FL_TYP_V"].ToString(),
                                    FileData = (byte[])reader["FDSB_FL_B"]
                                };
                                files.Add(file);
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, files);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching files by attachment references: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }






        [HttpGet]
        [Route("api/dpaccountnum/{projectNumber}")]
        public IHttpActionResult AccountNumber(string projectNumber)
        {
            List<ProjectAssetBankAccount> accounts = new List<ProjectAssetBankAccount>();

            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                string query = "SELECT PH_DP_ACCNT_NMMBR_V FROM tbl_prjct_hdr WHERE PH_DP_ACCNT_NMMBR_V != '-' AND PH_PRJCT_NMBR_N = :projectNumber";

                OracleCommand command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));

                try
                {
                    connection.Open();
                    OracleDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        ProjectAssetBankAccount account = new ProjectAssetBankAccount
                        {
                            AccountNumber = reader["PH_DP_ACCNT_NMMBR_V"].ToString(),
                            AccountType = "DP" // Mark DP accounts with a specific type
                        };
                        accounts.Add(account);
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }

            if (accounts.Count == 0)
            {
                return NotFound();
            }

            return Ok(accounts);
        }


    }
}

