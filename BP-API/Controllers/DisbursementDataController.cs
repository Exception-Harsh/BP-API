using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using BP_API.Models;
using System.Configuration;

namespace BP_API.Controllers
{
    public class DisbursementDataController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpPost]
        [Route("api/project-assistance")]
        public async Task<HttpResponseMessage> CreateProjectAssistanceRequest(DisbursementRequestDto requestDto)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (OracleTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert disbursement request data
                            string insertSql = @"
                                INSERT INTO tbl_prjct_asst_dsbrsmnt_rqst (
                                    padr_dr_nmbr_n, padr_prjct_nmbr_n, padr_asst_nmbr_n, padr_ctgry_v,
                                    padr_sb_ctgry_v, padr_prty_nm_v, padr_prty_gstn_v, padr_prty_pan_v,
                                    padr_prty_eml_v, padr_prty_mbl_v, padr_rsn_v, padr_po_wo_v,
                                    padr_ttl_ordr_amnt_n, padr_dcmnt_typ_v, padr_prty_dcmnt_nmbr_v,
                                    padr_prty_dcmnt_dt_d, padr_prty_dcmnt_pybl_dys_n, padr_prty_dcmnt_amnt_n,
                                    padr_prty_dcmnt_gst_amnt_n, padr_prty_dcmnt_ttl_amnt_n, padr_prty_tds_amnt_n,
                                    padr_prty_advnc_adjstd_n, padr_prty_rtntn_amnt_n, padr_prty_othr_ddctn_amnt_n,
                                    padr_prty_pybl_amnt_n, padr_prty_otstndng_amnt_n, padr_brrwr_accnt_nmbr_v,
                                    padr_prty_bnk_nm_v, padr_prty_accnt_nm_v, padr_prty_accnt_nmbr_v,
                                    padr_prty_accnt_ifsc_v, padr_stts_c, padr_apprvd_amnt_n, padr_rfrnc_dr_nmbr_n,
                                    padr_rmrks_v, padr_attchmnt_rfrnc_v
                                ) VALUES (
                                    :padr_dr_nmbr_n, :padr_prjct_nmbr_n, :padr_asst_nmbr_n, :padr_ctgry_v,
                                    :padr_sb_ctgry_v, :padr_prty_nm_v, :padr_prty_gstn_v, :padr_prty_pan_v,
                                    :padr_prty_eml_v, :padr_prty_mbl_v, :padr_rsn_v, :padr_po_wo_v,
                                    :padr_ttl_ordr_amnt_n, :padr_dcmnt_typ_v, :padr_prty_dcmnt_nmbr_v,
                                    :padr_prty_dcmnt_dt_d, :padr_prty_dcmnt_pybl_dys_n, :padr_prty_dcmnt_amnt_n,
                                    :padr_prty_dcmnt_gst_amnt_n, :padr_prty_dcmnt_ttl_amnt_n, :padr_prty_tds_amnt_n,
                                    :padr_prty_advnc_adjstd_n, :padr_prty_rtntn_amnt_n, :padr_prty_othr_ddctn_amnt_n,
                                    :padr_prty_pybl_amnt_n, :padr_prty_otstndng_amnt_n, :padr_brrwr_accnt_nmbr_v,
                                    :padr_prty_bnk_nm_v, :padr_prty_accnt_nm_v, :padr_prty_accnt_nmbr_v,
                                    :padr_prty_accnt_ifsc_v, :padr_stts_c, :padr_apprvd_amnt_n, :padr_rfrnc_dr_nmbr_n,
                                    :padr_rmrks_v, :padr_attchmnt_rfrnc_v
                                )";

                            using (OracleCommand command = new OracleCommand())
                            {
                                command.Connection = connection;
                                command.Transaction = transaction;
                                command.CommandText = insertSql;

                                // Map properties from requestDto to command parameters
                                command.Parameters.Add(new OracleParameter("padr_dr_nmbr_n", OracleDbType.Int32)).Value = requestDto.PadrDrNmbrN;
                                command.Parameters.Add(new OracleParameter("padr_prjct_nmbr_n", OracleDbType.Int32)).Value = requestDto.PadrPrjctNmbrN;
                                command.Parameters.Add(new OracleParameter("padr_asst_nmbr_n", OracleDbType.Int32)).Value = requestDto.PadrAsstNmbrN;
                                command.Parameters.Add(new OracleParameter("padr_ctgry_v", OracleDbType.Varchar2)).Value = requestDto.PadrCtgryV;
                                command.Parameters.Add(new OracleParameter("padr_sb_ctgry_v", OracleDbType.Varchar2)).Value = requestDto.PadrSbCtgryV;
                                command.Parameters.Add(new OracleParameter("padr_prty_nm_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyNmV;
                                command.Parameters.Add(new OracleParameter("padr_prty_gstn_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyGstnV;
                                command.Parameters.Add(new OracleParameter("padr_prty_pan_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyPanV;
                                command.Parameters.Add(new OracleParameter("padr_prty_eml_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyEmlV;
                                command.Parameters.Add(new OracleParameter("padr_prty_mbl_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyMblV;
                                command.Parameters.Add(new OracleParameter("padr_rsn_v", OracleDbType.Varchar2)).Value = requestDto.PadrRsnV;
                                command.Parameters.Add(new OracleParameter("padr_po_wo_v", OracleDbType.Varchar2)).Value = requestDto.PadrPoWoV;
                                command.Parameters.Add(new OracleParameter("padr_ttl_ordr_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrTtlOrdrAmntN;
                                command.Parameters.Add(new OracleParameter("padr_dcmnt_typ_v", OracleDbType.Varchar2)).Value = requestDto.PadrDcmntTypV;
                                command.Parameters.Add(new OracleParameter("padr_prty_dcmnt_nmbr_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyDcmntNmbrV;
                                command.Parameters.Add(new OracleParameter("padr_prty_dcmnt_dt_d", OracleDbType.Date)).Value = requestDto.PadrPrtyDcmntDtD;
                                command.Parameters.Add(new OracleParameter("padr_prty_dcmnt_pybl_dys_n", OracleDbType.Int32)).Value = requestDto.PadrPrtyDcmntPyblDysN;
                                command.Parameters.Add(new OracleParameter("padr_prty_dcmnt_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyDcmntAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_dcmnt_gst_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyDcmntGstAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_dcmnt_ttl_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyDcmntTtlAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_tds_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyTdsAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_advnc_adjstd_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyAdvncAdjstdN;
                                command.Parameters.Add(new OracleParameter("padr_prty_rtntn_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyRtntnAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_othr_ddctn_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyOthrDdctnAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_pybl_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyPyblAmntN;
                                command.Parameters.Add(new OracleParameter("padr_prty_otstndng_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrPrtyOtstndngAmntN;
                                command.Parameters.Add(new OracleParameter("padr_brrwr_accnt_nmbr_v", OracleDbType.Varchar2)).Value = requestDto.PadrBrrwrAccntNmbrV;
                                command.Parameters.Add(new OracleParameter("padr_prty_bnk_nm_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyBnkNmV;
                                command.Parameters.Add(new OracleParameter("padr_prty_accnt_nm_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyAccntNmV;
                                command.Parameters.Add(new OracleParameter("padr_prty_accnt_nmbr_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyAccntNmbrV;
                                command.Parameters.Add(new OracleParameter("padr_prty_accnt_ifsc_v", OracleDbType.Varchar2)).Value = requestDto.PadrPrtyAccntIfscV;
                                command.Parameters.Add(new OracleParameter("padr_stts_c", OracleDbType.Varchar2)).Value = requestDto.PadrSttsC;
                                command.Parameters.Add(new OracleParameter("padr_apprvd_amnt_n", OracleDbType.Decimal)).Value = requestDto.PadrApprvdAmntN;
                                command.Parameters.Add(new OracleParameter("padr_rfrnc_dr_nmbr_n", OracleDbType.Int32)).Value = requestDto.PadrRfrncDrNmbrN;
                                command.Parameters.Add(new OracleParameter("padr_rmrks_v", OracleDbType.Varchar2)).Value = requestDto.PadrRmrksV;
                                command.Parameters.Add(new OracleParameter("padr_attchmnt_rfrnc_v", OracleDbType.Varchar2)).Value = requestDto.AttachmentFileName;

                                await command.ExecuteNonQueryAsync();
                            }

                            // Insert file data if attachment is provided
                            if (requestDto.Attachment != null)
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await requestDto.Attachment.CopyToAsync(memoryStream);
                                    string insertFileSql = @"
                                        INSERT INTO tbl_fl_dt_strg_blb (
                                            fdsb_nmbr_n, fdsb_fl_nm_v, fdsb_fl_typ_v, fdsb_fl_b
                                        ) VALUES (
                                            :fdsb_nmbr_n, :fdsb_fl_nm_v, :fdsb_fl_typ_v, :fdsb_fl_b
                                        )";

                                    using (OracleCommand fileCommand = new OracleCommand())
                                    {
                                        fileCommand.Connection = connection;
                                        fileCommand.Transaction = transaction;
                                        fileCommand.CommandText = insertFileSql;

                                        fileCommand.Parameters.Add(new OracleParameter("fdsb_nmbr_n", OracleDbType.Int32)).Value = 1; // Set appropriate value
                                        fileCommand.Parameters.Add(new OracleParameter("fdsb_fl_nm_v", OracleDbType.Varchar2)).Value = requestDto.AttachmentFileName;
                                        fileCommand.Parameters.Add(new OracleParameter("fdsb_fl_typ_v", OracleDbType.Varchar2)).Value = requestDto.AttachmentContentType;
                                        fileCommand.Parameters.Add(new OracleParameter("fdsb_fl_b", OracleDbType.Blob)).Value = memoryStream.ToArray();

                                        await fileCommand.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // Commit the transaction
                            transaction.Commit();
                            return Request.CreateResponse(HttpStatusCode.OK, new { message = "Project assistance request created successfully" });
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of an error
                            transaction.Rollback();
                            Console.WriteLine($"Error in transaction: {ex.Message}");
                            return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = "An error occurred during the transaction." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating project assistance request: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}
