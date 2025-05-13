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
using BP_API.Queries;

namespace BP_API.Controllers
{
    public class AssetSalesController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpGet]
        [Route("api/assetsales/nextyearmonth/{projectNumber}/{role}")]
        public HttpResponseMessage GetNextYearMonth(string projectNumber, string role)
        {
            try
            {
                string maxAsmYearMonth = null;
                string maxAsmawYearMonth = null;

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Get max ASM_YR_MNTH_N
                    using (OracleCommand cmd1 = new OracleCommand(@"
                SELECT MAX(ASM_YR_MNTH_N) FROM TBL_ASST_SLS_MS
                WHERE ASM_PRJCT_NMBR_N = :projectNumber", connection))
                    {
                        cmd1.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        maxAsmYearMonth = cmd1.ExecuteScalar()?.ToString();
                        Console.WriteLine($"DEBUG: maxAsmYearMonth = {maxAsmYearMonth}");
                    }

                    // Get max ASMAW_YR_MNTH_N
                    using (OracleCommand cmd2 = new OracleCommand(@"
                SELECT MAX(ASMAW_YR_MNTH_N)
                FROM TBL_ASST_SLS_MS_APPRVL_WRKFLW
                WHERE ASMAW_PRJCT_NMBR_N = :projectNumber
                  AND ASMAW_USR_NM_V = :username
                  AND ASMAW_STTS_FLG_C = 'A'", connection))
                    {
                        cmd2.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        cmd2.Parameters.Add(new OracleParameter("username", "Arbour")); // or pass dynamically
                        maxAsmawYearMonth = cmd2.ExecuteScalar()?.ToString();
                        Console.WriteLine($"DEBUG: maxAsmawYearMonth = {maxAsmawYearMonth}");
                    }

                    if (maxAsmYearMonth == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Missing ASM YearMonth data." });
                    }

                    if (maxAsmawYearMonth == null || maxAsmYearMonth != maxAsmawYearMonth)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new { maxYearMonth = maxAsmYearMonth, message = "Approval workflow not up-to-date. No rows inserted." });
                    }

                    // If both match and role is "Borrower", proceed to insert
                    if (role == "Borrower")
                    {
                        // Insert new rows with the updated YearMonth using ADD_MONTHS
                        string insertQuery = @"
                    INSERT INTO TBL_ASST_SLS_MS (
                        ASM_YR_MNTH_N, ASM_PRJCT_NMBR_N, ASM_ASST_NMBR_N, ASM_PHS_V, ASM_BLDNG_V, ASM_FLR_V, ASM_UNT_NMBR_V,
                        ASM_UNT_CNFGRTN_V, ASM_UNT_TYP_C, ASM_SLBL_ARA_N, ASM_CRPT_ARA_N, ASM_CRPT_ARA_RR_N, ASM_UNT_UNQ_NMBR_N,
                        ASM_UNT_OWNR_C, ASM_UNT_SLD_FLG_C, ASM_UNT_RGSTRD_FLG_C, ASM_UNT_RGSTRTN_DT_D, ASM_UNT_BKNG_DT_D,
                        ASM_ALLTMNT_LTTR_DT_D, ASM_UNT_AGRMNT_DT_D, ASM_CSTMR_NM_V, ASM_CSTMR_KYC_AADHR_N, ASM_CSTMR_KYC_PN_V,
                        ASM_CSTMR_KYC_MBL_V, ASM_CSTMR_KYC_EML_V, ASM_CSTMR_KYC_ADDRSS_V, ASM_NC_ISSD_FLG_C, ASM_NC_NMBR_V,
                        ASM_SLS_BS_PRC_N, ASM_SLS_STMP_DTY_AMNT_N, ASM_SLS_RGSTRN_AMNT_N, ASM_SLS_OC_AMNT_N, ASM_SLS_PSS_THRGH_CHRGS_N,
                        ASM_SLS_TXS_AMNT_N, ASM_SLS_TTL_AMNT_N, ASM_DMND_BS_PRC_N, ASM_DMND_STMP_DTY_N, ASM_DMND_RGSTRTN_AMNT_N,
                        ASM_DMND_OC_AMNT_N, ASM_DMND_PSS_THRGH_CHRGS_N, ASM_DMND_TXS_AMNT_N, ASM_DMND_TTL_AMNT_N, ASM_RCVD_BS_PRC_N,
                        ASM_RCVD_STMP_DTY_AMNT_N, ASM_RCVD_RGSTRN_AMNT_N, ASM_RCVD_OC_AMNT_N, ASM_RCVD_PSS_THRGH_CHRGS_N,
                        ASM_RCVD_TXS_AMNT_N, ASM_RCVD_TTL_AMNT_N, ASM_MD_OF_FNNC_C, ASM_FI_NM_V, ASM_PYMNT_PLN_NM_V,
                        ASM_SRC_OF_CSTMR_C, ASM_CHNNL_PRTNR_NM_V, ASM_CHNNL_PRTNR_MBL_V, ASM_CHNNL_PRTNR_EML_V, ASM_BRKRG_AMNT_N,
                        COIN_CRTN_USR_ID_V, COIN_CRTN_DT_D, COIN_LST_MDFD_USR_ID_V, COIN_LST_MDFD_DT_D
                    )
                    SELECT
                        TO_CHAR(ADD_MONTHS(TO_DATE(B.ASM_YR_MNTH_N || '01', 'YYYYMMDD'), 1), 'YYYYMM') AS VL_NXT_MNTH_N,
                        A.ASM_PRJCT_NMBR_N, A.ASM_ASST_NMBR_N, A.ASM_PHS_V, A.ASM_BLDNG_V, A.ASM_FLR_V, A.ASM_UNT_NMBR_V,
                        A.ASM_UNT_CNFGRTN_V, A.ASM_UNT_TYP_C, A.ASM_SLBL_ARA_N, A.ASM_CRPT_ARA_N, A.ASM_CRPT_ARA_RR_N, A.ASM_UNT_UNQ_NMBR_N,
                        A.ASM_UNT_OWNR_C, A.ASM_UNT_SLD_FLG_C, A.ASM_UNT_RGSTRD_FLG_C, A.ASM_UNT_RGSTRTN_DT_D, A.ASM_UNT_BKNG_DT_D,
                        A.ASM_ALLTMNT_LTTR_DT_D, A.ASM_UNT_AGRMNT_DT_D, A.ASM_CSTMR_NM_V, A.ASM_CSTMR_KYC_AADHR_N, A.ASM_CSTMR_KYC_PN_V,
                        A.ASM_CSTMR_KYC_MBL_V, A.ASM_CSTMR_KYC_EML_V, A.ASM_CSTMR_KYC_ADDRSS_V, A.ASM_NC_ISSD_FLG_C, A.ASM_NC_NMBR_V,
                        A.ASM_SLS_BS_PRC_N, A.ASM_SLS_STMP_DTY_AMNT_N, A.ASM_SLS_RGSTRN_AMNT_N, A.ASM_SLS_OC_AMNT_N, A.ASM_SLS_PSS_THRGH_CHRGS_N,
                        A.ASM_SLS_TXS_AMNT_N, A.ASM_SLS_TTL_AMNT_N, A.ASM_DMND_BS_PRC_N, A.ASM_DMND_STMP_DTY_N, A.ASM_DMND_RGSTRTN_AMNT_N,
                        A.ASM_DMND_OC_AMNT_N, A.ASM_DMND_PSS_THRGH_CHRGS_N, A.ASM_DMND_TXS_AMNT_N, A.ASM_DMND_TTL_AMNT_N, A.ASM_RCVD_BS_PRC_N,
                        A.ASM_RCVD_STMP_DTY_AMNT_N, A.ASM_RCVD_RGSTRN_AMNT_N, A.ASM_RCVD_OC_AMNT_N, A.ASM_RCVD_PSS_THRGH_CHRGS_N,
                        A.ASM_RCVD_TXS_AMNT_N, A.ASM_RCVD_TTL_AMNT_N, A.ASM_MD_OF_FNNC_C, A.ASM_FI_NM_V, A.ASM_PYMNT_PLN_NM_V,
                        A.ASM_SRC_OF_CSTMR_C, A.ASM_CHNNL_PRTNR_NM_V, A.ASM_CHNNL_PRTNR_MBL_V, A.ASM_CHNNL_PRTNR_EML_V, A.ASM_BRKRG_AMNT_N,
                        COIN_CRTN_USR_ID_V, COIN_CRTN_DT_D, COIN_LST_MDFD_USR_ID_V, COIN_LST_MDFD_DT_D
                    FROM TBL_ASST_SLS_MS A
                    INNER JOIN (
                        SELECT ASM_PRJCT_NMBR_N, MAX(ASM_YR_MNTH_N) AS ASM_YR_MNTH_N
                        FROM TBL_ASST_SLS_MS
                        GROUP BY ASM_PRJCT_NMBR_N
                    ) B ON A.ASM_PRJCT_NMBR_N = B.ASM_PRJCT_NMBR_N AND A.ASM_YR_MNTH_N = B.ASM_YR_MNTH_N
                    WHERE A.ASM_PRJCT_NMBR_N = :projectNumber";

                        using (OracleCommand command = new OracleCommand(insertQuery, connection))
                        {
                            command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));

                            int rowsInserted = command.ExecuteNonQuery();

                            if (rowsInserted == 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "No rows were inserted." });
                            }
                        }

                        // Fetch the max ASM_YR_MNTH_N again after insertion
                        using (OracleCommand cmd3 = new OracleCommand(@"
                    SELECT MAX(ASM_YR_MNTH_N) FROM TBL_ASST_SLS_MS
                    WHERE ASM_PRJCT_NMBR_N = :projectNumber", connection))
                        {
                            cmd3.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                            maxAsmYearMonth = cmd3.ExecuteScalar()?.ToString();
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new { nextYearMonth = maxAsmYearMonth });
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new { maxYearMonth = maxAsmYearMonth });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching next YearMonth: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }




        [HttpGet]
        [Route("api/assetsales/buildings/{projectNumber}")]
        public HttpResponseMessage GetBuildingsData(string projectNumber)
        {
            try
            {
                string fetchBuildingsQuery = @"
            SELECT DISTINCT ASM_BLDNG_V, ASM_ASST_NMBR_N
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
            ORDER BY ASM_BLDNG_V";

                var buildings = new List<object>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchBuildingsQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                buildings.Add(new
                                {
                                    buildingName = reader["ASM_BLDNG_V"].ToString(),
                                    assetNumber = reader["ASM_ASST_NMBR_N"].ToString()
                                });
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { buildings });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching buildings data: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }


        [HttpGet]
        [Route("api/assetsales/data/{projectNumber}/{yearMonth}/{buildingName}")]
        public HttpResponseMessage GetAssetSalesData(string projectNumber, string yearMonth, string buildingName, string unitNumber = null, string unitConfiguration = null, string customerName = null, string soldFlag = null)
        {
            try
            {
                // Define the columns to fetch
                string columns = @"
            ASM_FLR_V, ASM_UNT_NMBR_V, ASM_UNT_CNFGRTN_V, ASM_UNT_TYP_C, ASM_SLBL_ARA_N,
            ASM_CRPT_ARA_N, ASM_CRPT_ARA_RR_N, ASM_UNT_UNQ_NMBR_N, ASM_UNT_OWNR_C, ASM_UNT_SLD_FLG_C,
            ASM_UNT_RGSTRD_FLG_C, ASM_UNT_RGSTRTN_DT_D, ASM_UNT_BKNG_DT_D, ASM_ALLTMNT_LTTR_DT_D,
            ASM_UNT_AGRMNT_DT_D, ASM_CSTMR_NM_V, ASM_CSTMR_KYC_AADHR_N, ASM_CSTMR_KYC_PN_V,
            ASM_CSTMR_KYC_MBL_V, ASM_CSTMR_KYC_EML_V, ASM_CSTMR_KYC_ADDRSS_V, ASM_NC_ISSD_FLG_C,
            ASM_NC_NMBR_V, ASM_SLS_BS_PRC_N, ASM_SLS_STMP_DTY_AMNT_N, ASM_SLS_RGSTRN_AMNT_N,
            ASM_SLS_OC_AMNT_N, ASM_SLS_PSS_THRGH_CHRGS_N, ASM_SLS_TXS_AMNT_N, ASM_SLS_TTL_AMNT_N,
            ASM_DMND_BS_PRC_N, ASM_DMND_STMP_DTY_N, ASM_DMND_RGSTRTN_AMNT_N, ASM_DMND_OC_AMNT_N,
            ASM_DMND_PSS_THRGH_CHRGS_N, ASM_DMND_TXS_AMNT_N, ASM_DMND_TTL_AMNT_N, ASM_RCVD_BS_PRC_N,
            ASM_RCVD_STMP_DTY_AMNT_N, ASM_RCVD_RGSTRN_AMNT_N, ASM_RCVD_OC_AMNT_N, ASM_RCVD_PSS_THRGH_CHRGS_N,
            ASM_RCVD_TXS_AMNT_N, ASM_RCVD_TTL_AMNT_N, ASM_MD_OF_FNNC_C, ASM_FI_NM_V, ASM_PYMNT_PLN_NM_V,
            ASM_SRC_OF_CSTMR_C, ASM_CHNNL_PRTNR_NM_V, ASM_CHNNL_PRTNR_MBL_V, ASM_CHNNL_PRTNR_EML_V, ASM_BRKRG_AMNT_N";

                // SQL query to fetch data with filters
                string fetchDataQuery = $@"
            SELECT {columns}
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth
              AND ASM_BLDNG_V = :buildingName";

                if (!string.IsNullOrEmpty(unitNumber))
                {
                    fetchDataQuery += " AND ASM_UNT_NMBR_V = :unitNumber";
                }
                if (!string.IsNullOrEmpty(unitConfiguration))
                {
                    fetchDataQuery += " AND ASM_UNT_CNFGRTN_V = :unitConfiguration";
                }
                if (!string.IsNullOrEmpty(customerName))
                {
                    fetchDataQuery += " AND ASM_CSTMR_NM_V = :customerName";
                }
                if (!string.IsNullOrEmpty(soldFlag))
                {
                    fetchDataQuery += " AND ASM_UNT_SLD_FLG_C = :soldFlag";
                }

                fetchDataQuery += " ORDER BY ASM_FLR_V";

                List<newAssetSale> assetSales = new List<newAssetSale>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchDataQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("buildingName", buildingName));
                        if (!string.IsNullOrEmpty(unitNumber))
                        {
                            command.Parameters.Add(new OracleParameter("unitNumber", unitNumber));
                        }
                        if (!string.IsNullOrEmpty(unitConfiguration))
                        {
                            command.Parameters.Add(new OracleParameter("unitConfiguration", unitConfiguration));
                        }
                        if (!string.IsNullOrEmpty(customerName))
                        {
                            command.Parameters.Add(new OracleParameter("customerName", customerName));
                        }
                        if (!string.IsNullOrEmpty(soldFlag))
                        {
                            command.Parameters.Add(new OracleParameter("soldFlag", soldFlag));
                        }

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                assetSales.Add(new newAssetSale
                                {
                                    Floor = reader["ASM_FLR_V"].ToString(),
                                    UnitNumber = reader["ASM_UNT_NMBR_V"].ToString(),
                                    UnitConfiguration = reader["ASM_UNT_CNFGRTN_V"].ToString(),
                                    UnitType = reader["ASM_UNT_TYP_C"].ToString(),
                                    SaleableArea = reader["ASM_SLBL_ARA_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLBL_ARA_N"]) : 0,
                                    CarpetArea = reader["ASM_CRPT_ARA_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_CRPT_ARA_N"]) : 0,
                                    CarpetAreaRR = reader["ASM_CRPT_ARA_RR_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_CRPT_ARA_RR_N"]) : 0,
                                    UniqueUnitNumber = reader["ASM_UNT_UNQ_NMBR_N"].ToString(),
                                    Owner = reader["ASM_UNT_OWNR_C"].ToString(),
                                    SoldFlag = reader["ASM_UNT_SLD_FLG_C"].ToString(),
                                    RegisteredFlag = reader["ASM_UNT_RGSTRD_FLG_C"].ToString(),
                                    RegistrationDate = reader["ASM_UNT_RGSTRTN_DT_D"] as DateTime?,
                                    BookingDate = reader["ASM_UNT_BKNG_DT_D"] as DateTime?,
                                    AllotmentLetterDate = reader["ASM_ALLTMNT_LTTR_DT_D"] as DateTime?,
                                    AgreementDate = reader["ASM_UNT_AGRMNT_DT_D"] as DateTime?,
                                    CustomerName = reader["ASM_CSTMR_NM_V"].ToString(),
                                    CustomerKycAadhar = reader["ASM_CSTMR_KYC_AADHR_N"].ToString(),
                                    CustomerKycPan = reader["ASM_CSTMR_KYC_PN_V"].ToString(),
                                    CustomerKycMobile = reader["ASM_CSTMR_KYC_MBL_V"].ToString(),
                                    CustomerKycEmail = reader["ASM_CSTMR_KYC_EML_V"].ToString(),
                                    CustomerKycAddress = reader["ASM_CSTMR_KYC_ADDRSS_V"].ToString(),
                                    NcIssuedFlag = reader["ASM_NC_ISSD_FLG_C"].ToString(),
                                    NcNumber = reader["ASM_NC_NMBR_V"].ToString(),
                                    SalesBasePrice = reader["ASM_SLS_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_BS_PRC_N"]) : 0,
                                    SalesStampDutyAmount = reader["ASM_SLS_STMP_DTY_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_STMP_DTY_AMNT_N"]) : 0,
                                    SalesRegistrationAmount = reader["ASM_SLS_RGSTRN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_RGSTRN_AMNT_N"]) : 0,
                                    SalesOtherCharges = reader["ASM_SLS_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_OC_AMNT_N"]) : 0,
                                    SalesPassThroughCharges = reader["ASM_SLS_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_PSS_THRGH_CHRGS_N"]) : 0,
                                    SalesTaxesAmount = reader["ASM_SLS_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_TXS_AMNT_N"]) : 0,
                                    SalesTotalAmount = reader["ASM_SLS_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_TTL_AMNT_N"]) : 0,
                                    DemandBasePrice = reader["ASM_DMND_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_BS_PRC_N"]) : 0,
                                    DemandStampDuty = reader["ASM_DMND_STMP_DTY_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_STMP_DTY_N"]) : 0,
                                    DemandRegistrationAmount = reader["ASM_DMND_RGSTRTN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_RGSTRTN_AMNT_N"]) : 0,
                                    DemandOtherCharges = reader["ASM_DMND_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_OC_AMNT_N"]) : 0,
                                    DemandPassThroughCharges = reader["ASM_DMND_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_PSS_THRGH_CHRGS_N"]) : 0,
                                    DemandTaxesAmount = reader["ASM_DMND_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_TXS_AMNT_N"]) : 0,
                                    DemandTotalAmount = reader["ASM_DMND_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_TTL_AMNT_N"]) : 0,
                                    ReceivedBasePrice = reader["ASM_RCVD_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_BS_PRC_N"]) : 0,
                                    ReceivedStampDutyAmount = reader["ASM_RCVD_STMP_DTY_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_STMP_DTY_AMNT_N"]) : 0,
                                    ReceivedRegistrationAmount = reader["ASM_RCVD_RGSTRN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_RGSTRN_AMNT_N"]) : 0,
                                    ReceivedOtherCharges = reader["ASM_RCVD_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_OC_AMNT_N"]) : 0,
                                    ReceivedPassThroughCharges = reader["ASM_RCVD_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_PSS_THRGH_CHRGS_N"]) : 0,
                                    ReceivedTaxesAmount = reader["ASM_RCVD_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_TXS_AMNT_N"]) : 0,
                                    ReceivedTotalAmount = reader["ASM_RCVD_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_TTL_AMNT_N"]) : 0,
                                    ModeOfFinance = reader["ASM_MD_OF_FNNC_C"].ToString(),
                                    FinancialInstitutionName = reader["ASM_FI_NM_V"].ToString(),
                                    PaymentPlanName = reader["ASM_PYMNT_PLN_NM_V"].ToString(),
                                    SourceOfCustomer = reader["ASM_SRC_OF_CSTMR_C"].ToString(),
                                    ChannelPartnerName = reader["ASM_CHNNL_PRTNR_NM_V"].ToString(),
                                    ChannelPartnerMobile = reader["ASM_CHNNL_PRTNR_MBL_V"].ToString(),
                                    ChannelPartnerEmail = reader["ASM_CHNNL_PRTNR_EML_V"].ToString(),
                                    BrokerageAmount = reader["ASM_BRKRG_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_BRKRG_AMNT_N"]) : 0
                                });
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { data = assetSales });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asset sales data: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/assetsales/dataByAsset/{projectNumber}/{yearMonth}/{assetNumber}")]
        public HttpResponseMessage GetAssetSalesDataByAsset(string projectNumber, string yearMonth, string assetNumber, string unitNumber = null, string unitConfiguration = null, string customerName = null, string soldFlag = null)
        {
            try
            {
                // Define the columns to fetch
                string columns = @"
            ASM_FLR_V, ASM_UNT_NMBR_V, ASM_UNT_CNFGRTN_V, ASM_UNT_TYP_C, ASM_SLBL_ARA_N,
            ASM_CRPT_ARA_N, ASM_CRPT_ARA_RR_N, ASM_UNT_UNQ_NMBR_N, ASM_UNT_OWNR_C, ASM_UNT_SLD_FLG_C,
            ASM_UNT_RGSTRD_FLG_C, ASM_UNT_RGSTRTN_DT_D, ASM_UNT_BKNG_DT_D, ASM_ALLTMNT_LTTR_DT_D,
            ASM_UNT_AGRMNT_DT_D, ASM_CSTMR_NM_V, ASM_CSTMR_KYC_AADHR_N, ASM_CSTMR_KYC_PN_V,
            ASM_CSTMR_KYC_MBL_V, ASM_CSTMR_KYC_EML_V, ASM_CSTMR_KYC_ADDRSS_V, ASM_NC_ISSD_FLG_C,
            ASM_NC_NMBR_V, ASM_SLS_BS_PRC_N, ASM_SLS_STMP_DTY_AMNT_N, ASM_SLS_RGSTRN_AMNT_N,
            ASM_SLS_OC_AMNT_N, ASM_SLS_PSS_THRGH_CHRGS_N, ASM_SLS_TXS_AMNT_N, ASM_SLS_TTL_AMNT_N,
            ASM_DMND_BS_PRC_N, ASM_DMND_STMP_DTY_N, ASM_DMND_RGSTRTN_AMNT_N, ASM_DMND_OC_AMNT_N,
            ASM_DMND_PSS_THRGH_CHRGS_N, ASM_DMND_TXS_AMNT_N, ASM_DMND_TTL_AMNT_N, ASM_RCVD_BS_PRC_N,
            ASM_RCVD_STMP_DTY_AMNT_N, ASM_RCVD_RGSTRN_AMNT_N, ASM_RCVD_OC_AMNT_N, ASM_RCVD_PSS_THRGH_CHRGS_N,
            ASM_RCVD_TXS_AMNT_N, ASM_RCVD_TTL_AMNT_N, ASM_MD_OF_FNNC_C, ASM_FI_NM_V, ASM_PYMNT_PLN_NM_V,
            ASM_SRC_OF_CSTMR_C, ASM_CHNNL_PRTNR_NM_V, ASM_CHNNL_PRTNR_MBL_V, ASM_CHNNL_PRTNR_EML_V, ASM_BRKRG_AMNT_N";

                // SQL query to fetch data with filters
                string fetchDataQuery = $@"
            SELECT {columns}
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth
              AND ASM_ASST_NMBR_N = :assetNumber";

                if (!string.IsNullOrEmpty(unitNumber))
                {
                    fetchDataQuery += " AND ASM_UNT_NMBR_V = :unitNumber";
                }
                if (!string.IsNullOrEmpty(unitConfiguration))
                {
                    fetchDataQuery += " AND ASM_UNT_CNFGRTN_V = :unitConfiguration";
                }
                if (!string.IsNullOrEmpty(customerName))
                {
                    fetchDataQuery += " AND ASM_CSTMR_NM_V = :customerName";
                }
                if (!string.IsNullOrEmpty(soldFlag))
                {
                    fetchDataQuery += " AND ASM_UNT_SLD_FLG_C = :soldFlag";
                }

                fetchDataQuery += " ORDER BY ASM_FLR_V";

                List<newAssetSale> assetSales = new List<newAssetSale>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchDataQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("assetNumber", assetNumber));
                        if (!string.IsNullOrEmpty(unitNumber))
                        {
                            command.Parameters.Add(new OracleParameter("unitNumber", unitNumber));
                        }
                        if (!string.IsNullOrEmpty(unitConfiguration))
                        {
                            command.Parameters.Add(new OracleParameter("unitConfiguration", unitConfiguration));
                        }
                        if (!string.IsNullOrEmpty(customerName))
                        {
                            command.Parameters.Add(new OracleParameter("customerName", customerName));
                        }
                        if (!string.IsNullOrEmpty(soldFlag))
                        {
                            command.Parameters.Add(new OracleParameter("soldFlag", soldFlag));
                        }

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                assetSales.Add(new newAssetSale
                                {
                                    Floor = reader["ASM_FLR_V"].ToString(),
                                    UnitNumber = reader["ASM_UNT_NMBR_V"].ToString(),
                                    UnitConfiguration = reader["ASM_UNT_CNFGRTN_V"].ToString(),
                                    UnitType = reader["ASM_UNT_TYP_C"].ToString(),
                                    SaleableArea = reader["ASM_SLBL_ARA_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLBL_ARA_N"]) : 0,
                                    CarpetArea = reader["ASM_CRPT_ARA_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_CRPT_ARA_N"]) : 0,
                                    CarpetAreaRR = reader["ASM_CRPT_ARA_RR_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_CRPT_ARA_RR_N"]) : 0,
                                    UniqueUnitNumber = reader["ASM_UNT_UNQ_NMBR_N"].ToString(),
                                    Owner = reader["ASM_UNT_OWNR_C"].ToString(),
                                    SoldFlag = reader["ASM_UNT_SLD_FLG_C"].ToString(),
                                    RegisteredFlag = reader["ASM_UNT_RGSTRD_FLG_C"].ToString(),
                                    RegistrationDate = reader["ASM_UNT_RGSTRTN_DT_D"] as DateTime?,
                                    BookingDate = reader["ASM_UNT_BKNG_DT_D"] as DateTime?,
                                    AllotmentLetterDate = reader["ASM_ALLTMNT_LTTR_DT_D"] as DateTime?,
                                    AgreementDate = reader["ASM_UNT_AGRMNT_DT_D"] as DateTime?,
                                    CustomerName = reader["ASM_CSTMR_NM_V"].ToString(),
                                    CustomerKycAadhar = reader["ASM_CSTMR_KYC_AADHR_N"].ToString(),
                                    CustomerKycPan = reader["ASM_CSTMR_KYC_PN_V"].ToString(),
                                    CustomerKycMobile = reader["ASM_CSTMR_KYC_MBL_V"].ToString(),
                                    CustomerKycEmail = reader["ASM_CSTMR_KYC_EML_V"].ToString(),
                                    CustomerKycAddress = reader["ASM_CSTMR_KYC_ADDRSS_V"].ToString(),
                                    NcIssuedFlag = reader["ASM_NC_ISSD_FLG_C"].ToString(),
                                    NcNumber = reader["ASM_NC_NMBR_V"].ToString(),
                                    SalesBasePrice = reader["ASM_SLS_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_BS_PRC_N"]) : 0,
                                    SalesStampDutyAmount = reader["ASM_SLS_STMP_DTY_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_STMP_DTY_AMNT_N"]) : 0,
                                    SalesRegistrationAmount = reader["ASM_SLS_RGSTRN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_RGSTRN_AMNT_N"]) : 0,
                                    SalesOtherCharges = reader["ASM_SLS_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_OC_AMNT_N"]) : 0,
                                    SalesPassThroughCharges = reader["ASM_SLS_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_PSS_THRGH_CHRGS_N"]) : 0,
                                    SalesTaxesAmount = reader["ASM_SLS_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_TXS_AMNT_N"]) : 0,
                                    SalesTotalAmount = reader["ASM_SLS_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_TTL_AMNT_N"]) : 0,
                                    DemandBasePrice = reader["ASM_DMND_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_BS_PRC_N"]) : 0,
                                    DemandStampDuty = reader["ASM_DMND_STMP_DTY_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_STMP_DTY_N"]) : 0,
                                    DemandRegistrationAmount = reader["ASM_DMND_RGSTRTN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_RGSTRTN_AMNT_N"]) : 0,
                                    DemandOtherCharges = reader["ASM_DMND_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_OC_AMNT_N"]) : 0,
                                    DemandPassThroughCharges = reader["ASM_DMND_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_PSS_THRGH_CHRGS_N"]) : 0,
                                    DemandTaxesAmount = reader["ASM_DMND_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_TXS_AMNT_N"]) : 0,
                                    DemandTotalAmount = reader["ASM_DMND_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_TTL_AMNT_N"]) : 0,
                                    ReceivedBasePrice = reader["ASM_RCVD_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_BS_PRC_N"]) : 0,
                                    ReceivedStampDutyAmount = reader["ASM_RCVD_STMP_DTY_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_STMP_DTY_AMNT_N"]) : 0,
                                    ReceivedRegistrationAmount = reader["ASM_RCVD_RGSTRN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_RGSTRN_AMNT_N"]) : 0,
                                    ReceivedOtherCharges = reader["ASM_RCVD_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_OC_AMNT_N"]) : 0,
                                    ReceivedPassThroughCharges = reader["ASM_RCVD_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_PSS_THRGH_CHRGS_N"]) : 0,
                                    ReceivedTaxesAmount = reader["ASM_RCVD_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_TXS_AMNT_N"]) : 0,
                                    ReceivedTotalAmount = reader["ASM_RCVD_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_TTL_AMNT_N"]) : 0,
                                    ModeOfFinance = reader["ASM_MD_OF_FNNC_C"].ToString(),
                                    FinancialInstitutionName = reader["ASM_FI_NM_V"].ToString(),
                                    PaymentPlanName = reader["ASM_PYMNT_PLN_NM_V"].ToString(),
                                    SourceOfCustomer = reader["ASM_SRC_OF_CSTMR_C"].ToString(),
                                    ChannelPartnerName = reader["ASM_CHNNL_PRTNR_NM_V"].ToString(),
                                    ChannelPartnerMobile = reader["ASM_CHNNL_PRTNR_MBL_V"].ToString(),
                                    ChannelPartnerEmail = reader["ASM_CHNNL_PRTNR_EML_V"].ToString(),
                                    BrokerageAmount = reader["ASM_BRKRG_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_BRKRG_AMNT_N"]) : 0
                                });
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { data = assetSales });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching asset sales data: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/assetsales/updatedDataByAsset/{projectNumber}/{yearMonth}/{assetNumber}")]
        public HttpResponseMessage GetUpdatedAssetSalesDataByAsset(string projectNumber, string yearMonth, string assetNumber, string unitNumber = null, string unitConfiguration = null, string customerName = null, string soldFlag = null)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Step 1: Get the previous month's yearMonth
                    string prevYearMonthQuery = @"
            SELECT TO_CHAR(ADD_MONTHS(TO_DATE(:yearMonth || '01', 'YYYYMMDD'), -1), 'YYYYMM')
            FROM DUAL";
                    string prevYearMonth;
                    using (OracleCommand cmd = new OracleCommand(prevYearMonthQuery, connection))
                    {
                        cmd.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        prevYearMonth = cmd.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(prevYearMonth))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Unable to determine previous yearMonth." });
                    }

                    // Step 2: Define columns to fetch
                    string columns = @"
            ASM_FLR_V, ASM_UNT_NMBR_V, ASM_UNT_CNFGRTN_V, ASM_UNT_TYP_C, ASM_SLBL_ARA_N,
            ASM_CRPT_ARA_N, ASM_CRPT_ARA_RR_N, ASM_UNT_UNQ_NMBR_N, ASM_UNT_OWNR_C, ASM_UNT_SLD_FLG_C,
            ASM_UNT_RGSTRD_FLG_C, ASM_UNT_RGSTRTN_DT_D, ASM_UNT_BKNG_DT_D, ASM_ALLTMNT_LTTR_DT_D,
            ASM_UNT_AGRMNT_DT_D, ASM_CSTMR_NM_V, ASM_CSTMR_KYC_AADHR_N, ASM_CSTMR_KYC_PN_V,
            ASM_CSTMR_KYC_MBL_V, ASM_CSTMR_KYC_EML_V, ASM_CSTMR_KYC_ADDRSS_V, ASM_NC_ISSD_FLG_C,
            ASM_NC_NMBR_V, ASM_SLS_BS_PRC_N, ASM_SLS_STMP_DTY_AMNT_N, ASM_SLS_RGSTRN_AMNT_N,
            ASM_SLS_OC_AMNT_N, ASM_SLS_PSS_THRGH_CHRGS_N, ASM_SLS_TXS_AMNT_N, ASM_SLS_TTL_AMNT_N,
            ASM_DMND_BS_PRC_N, ASM_DMND_STMP_DTY_N, ASM_DMND_RGSTRTN_AMNT_N, ASM_DMND_OC_AMNT_N,
            ASM_DMND_PSS_THRGH_CHRGS_N, ASM_DMND_TXS_AMNT_N, ASM_DMND_TTL_AMNT_N, ASM_RCVD_BS_PRC_N,
            ASM_RCVD_STMP_DTY_AMNT_N, ASM_RCVD_RGSTRN_AMNT_N, ASM_RCVD_OC_AMNT_N, ASM_RCVD_PSS_THRGH_CHRGS_N,
            ASM_RCVD_TXS_AMNT_N, ASM_RCVD_TTL_AMNT_N, ASM_MD_OF_FNNC_C, ASM_FI_NM_V, ASM_PYMNT_PLN_NM_V,
            ASM_SRC_OF_CSTMR_C, ASM_CHNNL_PRTNR_NM_V, ASM_CHNNL_PRTNR_MBL_V, ASM_CHNNL_PRTNR_EML_V, ASM_BRKRG_AMNT_N";

                    // Step 3: Fetch current and previous month's data and compare - FIX THE COMPARISON OPERATORS
                    string fetchUpdatedDataQuery = $@"
            SELECT {columns}
            FROM TBL_ASST_SLS_MS curr
            WHERE curr.ASM_PRJCT_NMBR_N = :projectNumber
              AND curr.ASM_YR_MNTH_N = :yearMonth
              AND curr.ASM_ASST_NMBR_N = :assetNumber
              AND EXISTS (
                  SELECT 1
                  FROM TBL_ASST_SLS_MS prev
                  WHERE prev.ASM_PRJCT_NMBR_N = curr.ASM_PRJCT_NMBR_N
                    AND prev.ASM_YR_MNTH_N = :prevYearMonth
                    AND prev.ASM_ASST_NMBR_N = curr.ASM_ASST_NMBR_N
                    AND prev.ASM_UNT_UNQ_NMBR_N = curr.ASM_UNT_UNQ_NMBR_N
                    AND (
                        NVL(prev.ASM_UNT_SLD_FLG_C, 'N') != NVL(curr.ASM_UNT_SLD_FLG_C, 'N') OR
                        NVL(prev.ASM_UNT_RGSTRD_FLG_C, 'N') != NVL(curr.ASM_UNT_RGSTRD_FLG_C, 'N') OR
                        NVL(prev.ASM_UNT_RGSTRTN_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) != NVL(curr.ASM_UNT_RGSTRTN_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) OR
                        NVL(prev.ASM_UNT_BKNG_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) != NVL(curr.ASM_UNT_BKNG_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) OR
                        NVL(prev.ASM_ALLTMNT_LTTR_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) != NVL(curr.ASM_ALLTMNT_LTTR_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) OR
                        NVL(prev.ASM_UNT_AGRMNT_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) != NVL(curr.ASM_UNT_AGRMNT_DT_D, TO_DATE('1900-01-01', 'YYYY-MM-DD')) OR
                        NVL(prev.ASM_CSTMR_NM_V, ' ') != NVL(curr.ASM_CSTMR_NM_V, ' ') OR
                        NVL(prev.ASM_CSTMR_KYC_AADHR_N, 0) != NVL(curr.ASM_CSTMR_KYC_AADHR_N, 0) OR
                        NVL(prev.ASM_CSTMR_KYC_PN_V, ' ') != NVL(curr.ASM_CSTMR_KYC_PN_V, ' ') OR
                        NVL(prev.ASM_CSTMR_KYC_MBL_V, ' ') != NVL(curr.ASM_CSTMR_KYC_MBL_V, ' ') OR
                        NVL(prev.ASM_CSTMR_KYC_EML_V, ' ') != NVL(curr.ASM_CSTMR_KYC_EML_V, ' ') OR
                        NVL(prev.ASM_CSTMR_KYC_ADDRSS_V, ' ') != NVL(curr.ASM_CSTMR_KYC_ADDRSS_V, ' ') OR
                        NVL(prev.ASM_NC_ISSD_FLG_C, 'N') != NVL(curr.ASM_NC_ISSD_FLG_C, 'N') OR
                        NVL(prev.ASM_NC_NMBR_V, ' ') != NVL(curr.ASM_NC_NMBR_V, ' ') OR
                        NVL(prev.ASM_SLS_BS_PRC_N, 0) != NVL(curr.ASM_SLS_BS_PRC_N, 0) OR
                        NVL(prev.ASM_SLS_STMP_DTY_AMNT_N, 0) != NVL(curr.ASM_SLS_STMP_DTY_AMNT_N, 0) OR
                        NVL(prev.ASM_SLS_RGSTRN_AMNT_N, 0) != NVL(curr.ASM_SLS_RGSTRN_AMNT_N, 0) OR
                        NVL(prev.ASM_SLS_OC_AMNT_N, 0) != NVL(curr.ASM_SLS_OC_AMNT_N, 0) OR
                        NVL(prev.ASM_SLS_PSS_THRGH_CHRGS_N, 0) != NVL(curr.ASM_SLS_PSS_THRGH_CHRGS_N, 0) OR
                        NVL(prev.ASM_SLS_TXS_AMNT_N, 0) != NVL(curr.ASM_SLS_TXS_AMNT_N, 0) OR
                        NVL(prev.ASM_SLS_TTL_AMNT_N, 0) != NVL(curr.ASM_SLS_TTL_AMNT_N, 0) OR
                        NVL(prev.ASM_DMND_BS_PRC_N, 0) != NVL(curr.ASM_DMND_BS_PRC_N, 0) OR
                        NVL(prev.ASM_DMND_STMP_DTY_N, 0) != NVL(curr.ASM_DMND_STMP_DTY_N, 0) OR
                        NVL(prev.ASM_DMND_RGSTRTN_AMNT_N, 0) != NVL(curr.ASM_DMND_RGSTRTN_AMNT_N, 0) OR
                        NVL(prev.ASM_DMND_OC_AMNT_N, 0) != NVL(curr.ASM_DMND_OC_AMNT_N, 0) OR
                        NVL(prev.ASM_DMND_PSS_THRGH_CHRGS_N, 0) != NVL(curr.ASM_DMND_PSS_THRGH_CHRGS_N, 0) OR
                        NVL(prev.ASM_DMND_TXS_AMNT_N, 0) != NVL(curr.ASM_DMND_TXS_AMNT_N, 0) OR
                        NVL(prev.ASM_DMND_TTL_AMNT_N, 0) != NVL(curr.ASM_DMND_TTL_AMNT_N, 0) OR
                        NVL(prev.ASM_RCVD_BS_PRC_N, 0) != NVL(curr.ASM_RCVD_BS_PRC_N, 0) OR
                        NVL(prev.ASM_RCVD_STMP_DTY_AMNT_N, 0) != NVL(curr.ASM_RCVD_STMP_DTY_AMNT_N, 0) OR
                        NVL(prev.ASM_RCVD_RGSTRN_AMNT_N, 0) != NVL(curr.ASM_RCVD_RGSTRN_AMNT_N, 0) OR
                        NVL(prev.ASM_RCVD_OC_AMNT_N, 0) != NVL(curr.ASM_RCVD_OC_AMNT_N, 0) OR
                        NVL(prev.ASM_RCVD_PSS_THRGH_CHRGS_N, 0) != NVL(curr.ASM_RCVD_PSS_THRGH_CHRGS_N, 0) OR
                        NVL(prev.ASM_RCVD_TXS_AMNT_N, 0) != NVL(curr.ASM_RCVD_TXS_AMNT_N, 0) OR
                        NVL(prev.ASM_RCVD_TTL_AMNT_N, 0) != NVL(curr.ASM_RCVD_TTL_AMNT_N, 0) OR
                        NVL(prev.ASM_MD_OF_FNNC_C, ' ') != NVL(curr.ASM_MD_OF_FNNC_C, ' ') OR
                        NVL(prev.ASM_FI_NM_V, ' ') != NVL(curr.ASM_FI_NM_V, ' ') OR
                        NVL(prev.ASM_PYMNT_PLN_NM_V, ' ') != NVL(curr.ASM_PYMNT_PLN_NM_V, ' ') OR
                        NVL(prev.ASM_SRC_OF_CSTMR_C, ' ') != NVL(curr.ASM_SRC_OF_CSTMR_C, ' ') OR
                        NVL(prev.ASM_CHNNL_PRTNR_NM_V, ' ') != NVL(curr.ASM_CHNNL_PRTNR_NM_V, ' ') OR
                        NVL(prev.ASM_CHNNL_PRTNR_MBL_V, ' ') != NVL(curr.ASM_CHNNL_PRTNR_MBL_V, ' ') OR
                        NVL(prev.ASM_CHNNL_PRTNR_EML_V, ' ') != NVL(curr.ASM_CHNNL_PRTNR_EML_V, ' ') OR
                        NVL(prev.ASM_BRKRG_AMNT_N, 0) != NVL(curr.ASM_BRKRG_AMNT_N, 0)
                    )
              )";

                    // Apply additional filters
                    if (!string.IsNullOrEmpty(unitNumber))
                    {
                        fetchUpdatedDataQuery += " AND curr.ASM_UNT_NMBR_V = :unitNumber";
                    }
                    if (!string.IsNullOrEmpty(unitConfiguration))
                    {
                        fetchUpdatedDataQuery += " AND curr.ASM_UNT_CNFGRTN_V = :unitConfiguration";
                    }
                    if (!string.IsNullOrEmpty(customerName))
                    {
                        fetchUpdatedDataQuery += " AND curr.ASM_CSTMR_NM_V = :customerName";
                    }
                    if (!string.IsNullOrEmpty(soldFlag))
                    {
                        fetchUpdatedDataQuery += " AND curr.ASM_UNT_SLD_FLG_C = :soldFlag";
                    }

                    fetchUpdatedDataQuery += " ORDER BY curr.ASM_FLR_V";

                    List<newAssetSale> assetSales = new List<newAssetSale>();
                    using (OracleCommand command = new OracleCommand(fetchUpdatedDataQuery, connection))
                    {
                        // Convert strings to appropriate number types for numeric parameters
                        command.Parameters.Add(new OracleParameter("projectNumber", OracleDbType.Int32)).Value = int.Parse(projectNumber);
                        command.Parameters.Add(new OracleParameter("yearMonth", OracleDbType.Int32)).Value = int.Parse(yearMonth);
                        command.Parameters.Add(new OracleParameter("assetNumber", OracleDbType.Int32)).Value = int.Parse(assetNumber);
                        command.Parameters.Add(new OracleParameter("prevYearMonth", OracleDbType.Int32)).Value = int.Parse(prevYearMonth);

                        if (!string.IsNullOrEmpty(unitNumber))
                        {
                            command.Parameters.Add(new OracleParameter("unitNumber", unitNumber));
                        }
                        if (!string.IsNullOrEmpty(unitConfiguration))
                        {
                            command.Parameters.Add(new OracleParameter("unitConfiguration", unitConfiguration));
                        }
                        if (!string.IsNullOrEmpty(customerName))
                        {
                            command.Parameters.Add(new OracleParameter("customerName", customerName));
                        }
                        if (!string.IsNullOrEmpty(soldFlag))
                        {
                            command.Parameters.Add(new OracleParameter("soldFlag", soldFlag));
                        }

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                assetSales.Add(new newAssetSale
                                {
                                    Floor = reader["ASM_FLR_V"].ToString(),
                                    UnitNumber = reader["ASM_UNT_NMBR_V"].ToString(),
                                    UnitConfiguration = reader["ASM_UNT_CNFGRTN_V"].ToString(),
                                    UnitType = reader["ASM_UNT_TYP_C"].ToString(),
                                    SaleableArea = reader["ASM_SLBL_ARA_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLBL_ARA_N"]) : 0,
                                    CarpetArea = reader["ASM_CRPT_ARA_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_CRPT_ARA_N"]) : 0,
                                    CarpetAreaRR = reader["ASM_CRPT_ARA_RR_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_CRPT_ARA_RR_N"]) : 0,
                                    UniqueUnitNumber = reader["ASM_UNT_UNQ_NMBR_N"].ToString(),
                                    Owner = reader["ASM_UNT_OWNR_C"].ToString(),
                                    SoldFlag = reader["ASM_UNT_SLD_FLG_C"].ToString(),
                                    RegisteredFlag = reader["ASM_UNT_RGSTRD_FLG_C"].ToString(),
                                    RegistrationDate = reader["ASM_UNT_RGSTRTN_DT_D"] != DBNull.Value ? Convert.ToDateTime(reader["ASM_UNT_RGSTRTN_DT_D"]) : (DateTime?)null,
                                    BookingDate = reader["ASM_UNT_BKNG_DT_D"] != DBNull.Value ? Convert.ToDateTime(reader["ASM_UNT_BKNG_DT_D"]) : (DateTime?)null,
                                    AllotmentLetterDate = reader["ASM_ALLTMNT_LTTR_DT_D"] != DBNull.Value ? Convert.ToDateTime(reader["ASM_ALLTMNT_LTTR_DT_D"]) : (DateTime?)null,
                                    AgreementDate = reader["ASM_UNT_AGRMNT_DT_D"] != DBNull.Value ? Convert.ToDateTime(reader["ASM_UNT_AGRMNT_DT_D"]) : (DateTime?)null,
                                    CustomerName = reader["ASM_CSTMR_NM_V"].ToString(),
                                    CustomerKycAadhar = reader["ASM_CSTMR_KYC_AADHR_N"] != DBNull.Value ? reader["ASM_CSTMR_KYC_AADHR_N"].ToString() : string.Empty,
                                    CustomerKycPan = reader["ASM_CSTMR_KYC_PN_V"].ToString(),
                                    CustomerKycMobile = reader["ASM_CSTMR_KYC_MBL_V"].ToString(),
                                    CustomerKycEmail = reader["ASM_CSTMR_KYC_EML_V"].ToString(),
                                    CustomerKycAddress = reader["ASM_CSTMR_KYC_ADDRSS_V"].ToString(),
                                    NcIssuedFlag = reader["ASM_NC_ISSD_FLG_C"].ToString(),
                                    NcNumber = reader["ASM_NC_NMBR_V"].ToString(),
                                    SalesBasePrice = reader["ASM_SLS_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_BS_PRC_N"]) : 0,
                                    SalesStampDutyAmount = reader["ASM_SLS_STMP_DTY_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_STMP_DTY_AMNT_N"]) : 0,
                                    SalesRegistrationAmount = reader["ASM_SLS_RGSTRN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_RGSTRN_AMNT_N"]) : 0,
                                    SalesOtherCharges = reader["ASM_SLS_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_OC_AMNT_N"]) : 0,
                                    SalesPassThroughCharges = reader["ASM_SLS_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_PSS_THRGH_CHRGS_N"]) : 0,
                                    SalesTaxesAmount = reader["ASM_SLS_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_TXS_AMNT_N"]) : 0,
                                    SalesTotalAmount = reader["ASM_SLS_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_SLS_TTL_AMNT_N"]) : 0,
                                    DemandBasePrice = reader["ASM_DMND_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_BS_PRC_N"]) : 0,
                                    DemandStampDuty = reader["ASM_DMND_STMP_DTY_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_STMP_DTY_N"]) : 0,
                                    DemandRegistrationAmount = reader["ASM_DMND_RGSTRTN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_RGSTRTN_AMNT_N"]) : 0,
                                    DemandOtherCharges = reader["ASM_DMND_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_OC_AMNT_N"]) : 0,
                                    DemandPassThroughCharges = reader["ASM_DMND_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_PSS_THRGH_CHRGS_N"]) : 0,
                                    DemandTaxesAmount = reader["ASM_DMND_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_TXS_AMNT_N"]) : 0,
                                    DemandTotalAmount = reader["ASM_DMND_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_DMND_TTL_AMNT_N"]) : 0,
                                    ReceivedBasePrice = reader["ASM_RCVD_BS_PRC_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_BS_PRC_N"]) : 0,
                                    ReceivedStampDutyAmount = reader["ASM_RCVD_STMP_DTY_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_STMP_DTY_AMNT_N"]) : 0,
                                    ReceivedRegistrationAmount = reader["ASM_RCVD_RGSTRN_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_RGSTRN_AMNT_N"]) : 0,
                                    ReceivedOtherCharges = reader["ASM_RCVD_OC_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_OC_AMNT_N"]) : 0,
                                    ReceivedPassThroughCharges = reader["ASM_RCVD_PSS_THRGH_CHRGS_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_PSS_THRGH_CHRGS_N"]) : 0,
                                    ReceivedTaxesAmount = reader["ASM_RCVD_TXS_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_TXS_AMNT_N"]) : 0,
                                    ReceivedTotalAmount = reader["ASM_RCVD_TTL_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_RCVD_TTL_AMNT_N"]) : 0,
                                    ModeOfFinance = reader["ASM_MD_OF_FNNC_C"].ToString(),
                                    FinancialInstitutionName = reader["ASM_FI_NM_V"].ToString(),
                                    PaymentPlanName = reader["ASM_PYMNT_PLN_NM_V"].ToString(),
                                    SourceOfCustomer = reader["ASM_SRC_OF_CSTMR_C"].ToString(),
                                    ChannelPartnerName = reader["ASM_CHNNL_PRTNR_NM_V"].ToString(),
                                    ChannelPartnerMobile = reader["ASM_CHNNL_PRTNR_MBL_V"].ToString(),
                                    ChannelPartnerEmail = reader["ASM_CHNNL_PRTNR_EML_V"].ToString(),
                                    BrokerageAmount = reader["ASM_BRKRG_AMNT_N"] != DBNull.Value ? Convert.ToDecimal(reader["ASM_BRKRG_AMNT_N"]) : 0
                                });
                            }
                        }
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new { data = assetSales });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching updated asset sales data: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }


        [HttpGet]
        [Route("api/assetsales/workflow/status/{projectNumber}/{yearMonth}/{role}")]
        public HttpResponseMessage GetWorkflowStatus(string projectNumber, string yearMonth, string role)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT ASMAW_STTS_FLG_C
                        FROM TBL_ASST_SLS_MS_APPRVL_WRKFLW
                        WHERE ASMAW_PRJCT_NMBR_N = :projectNumber
                          AND ASMAW_YR_MNTH_N = :yearMonth
                          AND ASMAW_USR_NM_V = :role";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("role", role));

                        var status = command.ExecuteScalar()?.ToString();

                        if (string.IsNullOrEmpty(status))
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new { status = "NotFound" });
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new { status });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching workflow status: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/assetsales/workflow/check/{projectNumber}/{yearMonth}/{role}")]
        public HttpResponseMessage CheckWorkflowStatus(string projectNumber, string yearMonth, string role)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Fetch the latest row based on ASMAW_UNQ_NMBR_N, regardless of role
                    string query = @"
                    SELECT ASMAW_STTS_FLG_C, ASMAW_CMMNTS_V
                    FROM TBL_ASST_SLS_MS_APPRVL_WRKFLW
                    WHERE ASMAW_PRJCT_NMBR_N = :projectNumber
                      AND ASMAW_YR_MNTH_N = :yearMonth
                    ORDER BY ASMAW_UNQ_NMBR_N DESC";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string status = reader["ASMAW_STTS_FLG_C"].ToString();
                                string comment = reader["ASMAW_CMMNTS_V"] != DBNull.Value ? reader["ASMAW_CMMNTS_V"].ToString() : null;
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, comment });
                            }
                            else
                            {
                                // No workflow status found; return appropriate response based on role
                                if (role == "Borrower")
                                {
                                    // Borrower sees no status as an opportunity to submit
                                    return Request.CreateResponse(HttpStatusCode.OK, new { status = "null", comment = "null" });
                                }
                                else
                                {
                                    // Arbour/PME see no status as no submission
                                    return Request.CreateResponse(HttpStatusCode.OK, new { status = "null", comment = "null", message = "No workflow status found" });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching workflow status: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }


        [HttpPost]
        [Route("api/assetsales/update/{projectNumber}/{yearMonth}")]
        public HttpResponseMessage UpdateAssetSales(string projectNumber, string yearMonth, [FromBody] List<AssetSale> assetSales)
        {
            try
            {
                if (assetSales == null || !assetSales.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid data provided." });
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();



                    int i = 0;
                    foreach (var assetSale in assetSales)
                    {
                        string sql = string.Format(AssetSalesQueries.UpdateAssetSalesMisRow,
                            projectNumber,
                            yearMonth,
                            assetSale.UniqueUnitNumber,
                            assetSale.SoldFlag,
                            assetSale.RegisteredFlag,
                            assetSale.RegistrationDate.HasValue ? "'" + assetSale.RegistrationDate.Value.ToString("dd-MMM-yyyy") + "'" : "null",
                            assetSale.BookingDate.HasValue ? "'" + assetSale.BookingDate.Value.ToString("dd-MMM-yyyy") + "'" : "null",
                            assetSale.AllotmentLetterDate.HasValue ? "'" + assetSale.AllotmentLetterDate.Value.ToString("dd-MMM-yyyy") + "'" : "null",
                            assetSale.AgreementDate.HasValue ? "'" + assetSale.AgreementDate.Value.ToString("dd-MMM-yyyy") + "'" : "null",
                            assetSale.CustomerName,
                            assetSale.CustomerKycAadhar,
                            assetSale.CustomerKycPan,
                            assetSale.CustomerKycMobile,
                            assetSale.CustomerKycEmail,
                            assetSale.CustomerKycAddress,
                            assetSale.NcIssuedFlag,
                            assetSale.NcNumber,
                            assetSale.SalesBasePrice,
                            assetSale.SalesStampDutyAmount,
                            assetSale.SalesRegistrationAmount,
                            assetSale.SalesOtherCharges,
                            assetSale.SalesPassThroughCharges,
                            assetSale.SalesTaxesAmount,
                            assetSale.SalesTotalAmount,
                            assetSale.DemandBasePrice,
                            assetSale.DemandStampDuty,
                            assetSale.DemandRegistrationAmount,
                            assetSale.DemandOtherCharges,
                            assetSale.DemandPassThroughCharges,
                            assetSale.DemandTaxesAmount,
                            assetSale.DemandTotalAmount,
                            assetSale.ReceivedBasePrice,
                            assetSale.ReceivedStampDutyAmount,
                            assetSale.ReceivedRegistrationAmount,
                            assetSale.ReceivedOtherCharges,
                            assetSale.ReceivedPassThroughCharges,
                            assetSale.ReceivedTaxesAmount,
                            assetSale.ReceivedTotalAmount,
                            assetSale.ModeOfFinance,
                            assetSale.FinancialInstitutionName,
                            assetSale.PaymentPlanName,
                            assetSale.SourceOfCustomer,
                            assetSale.ChannelPartnerName,
                            assetSale.ChannelPartnerMobile,
                            assetSale.ChannelPartnerEmail,
                            assetSale.BrokerageAmount);

                        OracleCommand command = new OracleCommand(sql, connection);
                        // Execute the update command
                        int rowsAffected = command.ExecuteNonQuery();
                        ++i;
                        if (rowsAffected == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { error = $"Asset sale with UniqueUnitNumber {assetSale.UniqueUnitNumber} not found or no updates were made." });
                        }
                    }

                }

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "All asset sales updated successfully." });
            }
            catch (Exception ex)
            {
                // Log the error to the server console
                Console.WriteLine($"Error updating asset sales: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/assetsales/updateByAsset/{projectNumber}/{yearMonth}/{assetNumber}")]
        public HttpResponseMessage UpdateAssetSalesByAsset(string projectNumber, string yearMonth, string assetNumber, [FromBody] List<AssetSale> assetSales)
        {
            try
            {
                if (assetSales == null || !assetSales.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid data provided." });
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    int updatedRows = 0;

                    foreach (var assetSale in assetSales)
                    {
                        // Use parameterized query instead of string formatting
                        string sql = AssetSalesQueries.UpdateAssetSalesMisRow;

                        using (OracleCommand command = new OracleCommand(sql, connection))
                        {
                            // Bind parameters
                            command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                            command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                            command.Parameters.Add(new OracleParameter("assetNumber", assetNumber));
                            command.Parameters.Add(new OracleParameter("uniqueUnitNumber", assetSale.UniqueUnitNumber ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("soldFlag", assetSale.SoldFlag ?? "N"));
                            command.Parameters.Add(new OracleParameter("registeredFlag", assetSale.RegisteredFlag ?? "N"));
                            command.Parameters.Add(new OracleParameter("registrationDate", assetSale.RegistrationDate.HasValue ? assetSale.RegistrationDate.Value : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("bookingDate", assetSale.BookingDate.HasValue ? assetSale.BookingDate.Value : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("allotmentLetterDate", assetSale.AllotmentLetterDate.HasValue ? assetSale.AllotmentLetterDate.Value : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("agreementDate", assetSale.AgreementDate.HasValue ? assetSale.AgreementDate.Value : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("customerName", assetSale.CustomerName ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("customerKycAadhar", assetSale.CustomerKycAadhar ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("customerKycPan", assetSale.CustomerKycPan ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("customerKycMobile", assetSale.CustomerKycMobile ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("customerKycEmail", assetSale.CustomerKycEmail ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("customerKycAddress", assetSale.CustomerKycAddress ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("ncIssuedFlag", assetSale.NcIssuedFlag ?? "N"));
                            command.Parameters.Add(new OracleParameter("ncNumber", assetSale.NcNumber ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesBasePrice", assetSale.SalesBasePrice != 0 ? assetSale.SalesBasePrice : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesStampDutyAmount", assetSale.SalesStampDutyAmount != 0 ? assetSale.SalesStampDutyAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesRegistrationAmount", assetSale.SalesRegistrationAmount != 0 ? assetSale.SalesRegistrationAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesOtherCharges", assetSale.SalesOtherCharges != 0 ? assetSale.SalesOtherCharges : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesPassThroughCharges", assetSale.SalesPassThroughCharges != 0 ? assetSale.SalesPassThroughCharges : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesTaxesAmount", assetSale.SalesTaxesAmount != 0 ? assetSale.SalesTaxesAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("salesTotalAmount", assetSale.SalesTotalAmount != 0 ? assetSale.SalesTotalAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandBasePrice", assetSale.DemandBasePrice != 0 ? assetSale.DemandBasePrice : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandStampDuty", assetSale.DemandStampDuty != 0 ? assetSale.DemandStampDuty : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandRegistrationAmount", assetSale.DemandRegistrationAmount != 0 ? assetSale.DemandRegistrationAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandOtherCharges", assetSale.DemandOtherCharges != 0 ? assetSale.DemandOtherCharges : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandPassThroughCharges", assetSale.DemandPassThroughCharges != 0 ? assetSale.DemandPassThroughCharges : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandTaxesAmount", assetSale.DemandTaxesAmount != 0 ? assetSale.DemandTaxesAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("demandTotalAmount", assetSale.DemandTotalAmount != 0 ? assetSale.DemandTotalAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedBasePrice", assetSale.ReceivedBasePrice != 0 ? assetSale.ReceivedBasePrice : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedStampDutyAmount", assetSale.ReceivedStampDutyAmount != 0 ? assetSale.ReceivedStampDutyAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedRegistrationAmount", assetSale.ReceivedRegistrationAmount != 0 ? assetSale.ReceivedRegistrationAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedOtherCharges", assetSale.ReceivedOtherCharges != 0 ? assetSale.ReceivedOtherCharges : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedPassThroughCharges", assetSale.ReceivedPassThroughCharges != 0 ? assetSale.ReceivedPassThroughCharges : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedTaxesAmount", assetSale.ReceivedTaxesAmount != 0 ? assetSale.ReceivedTaxesAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("receivedTotalAmount", assetSale.ReceivedTotalAmount != 0 ? assetSale.ReceivedTotalAmount : (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("modeOfFinance", assetSale.ModeOfFinance ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("financialInstitutionName", assetSale.FinancialInstitutionName ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("paymentPlanName", assetSale.PaymentPlanName ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("sourceOfCustomer", assetSale.SourceOfCustomer ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("channelPartnerName", assetSale.ChannelPartnerName ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("channelPartnerMobile", assetSale.ChannelPartnerMobile ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("channelPartnerEmail", assetSale.ChannelPartnerEmail ?? (object)DBNull.Value));
                            command.Parameters.Add(new OracleParameter("brokerageAmount", assetSale.BrokerageAmount != 0 ? assetSale.BrokerageAmount : (object)DBNull.Value));

                            // Execute the update command
                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                updatedRows++;
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.NotFound, new { error = $"Asset sale with UniqueUnitNumber {assetSale.UniqueUnitNumber} not found or no updates were made." });
                            }
                        }
                    }

                    if (updatedRows == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "No asset sales were updated." });
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new { message = $"{updatedRows} asset sales updated successfully." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating asset sales: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/assetsales/workflow/{projectNumber}/{yearMonth}/{role}")]
        public HttpResponseMessage ApprovalWorkflow(string projectNumber, string yearMonth, string role, [FromBody] ApprovalWorkflow request)
        {
            try
            {
                int incrmntUnqNum = 0;

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Get next unique number
                    using (OracleCommand cmd1 = new OracleCommand(@"
                SELECT NVL(MAX(ASMAW_UNQ_NMBR_N), 0) + 1
                FROM TBL_ASST_SLS_MS_APPRVL_WRKFLW", connection))
                    {
                        var result = cmd1.ExecuteScalar();
                        incrmntUnqNum = Convert.ToInt32(result);
                    }

                    string insertQuery = @"
                INSERT INTO TBL_ASST_SLS_MS_APPRVL_WRKFLW
                (
                    ASMAW_YR_MNTH_N, ASMAW_PRJCT_NMBR_N, ASMAW_UNQ_NMBR_N,
                    ASMAW_USR_NM_V, ASMAW_STTS_FLG_C, ASMAW_CMMNTS_V,
                    COIN_CRTN_USR_ID_V, COIN_CRTN_DT_D,
                    COIN_LST_MDFD_USR_ID_V, COIN_LST_MDFD_DT_D
                )
                VALUES (
                    :yearMonth, :projectNumber, :incrmntUnqNum,
                    :role, :statusFlag, :workflowComment,
                    :createdBy, :createdDate,
                    :modifiedBy, :modifiedDate
                )";

                    using (OracleCommand command = new OracleCommand(insertQuery, connection))
                    {
                        // Common parameters
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("incrmntUnqNum", incrmntUnqNum));
                        command.Parameters.Add(new OracleParameter("role", role));

                        string statusFlag = request?.StatusFlag ?? "0";
                        string workflowComment = request?.WorkflowComment ?? "";
                        string username = request?.Username;
                        if (string.IsNullOrEmpty(username))
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Username is required." });
                        }

                        if (role == "PME" || role == "Arbour")
                        {
                            if (request == null || string.IsNullOrEmpty(request.StatusFlag))
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "StatusFlag is required for PME or Arbour roles." });

                            statusFlag = request.StatusFlag;
                            workflowComment = request.WorkflowComment ?? "";
                        }

                        command.Parameters.Add(new OracleParameter("statusFlag", statusFlag));
                        command.Parameters.Add(new OracleParameter("workflowComment", workflowComment));

                        // Audit info using username from request
                        command.Parameters.Add(new OracleParameter("createdBy", username));
                        command.Parameters.Add(new OracleParameter("createdDate", DateTime.Now));
                        command.Parameters.Add(new OracleParameter("modifiedBy", username));
                        command.Parameters.Add(new OracleParameter("modifiedDate", DateTime.Now));

                        int rowsInserted = command.ExecuteNonQuery();
                        if (rowsInserted == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "No rows were inserted." });
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Workflow saved successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/assetsales/hdr/{projectNumber}/{yearMonth}")]
        public HttpResponseMessage InsertHeader(string projectNumber, string yearMonth, [FromBody] ProjectHeader request)
        {
            try
            {
                if (request == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Request body cannot be null." });
                }

                if (string.IsNullOrEmpty(request.UserName))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Username is required." });
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Check if a row with the same projectNumber and yearMonth already exists
                    string checkQuery = @"
                        SELECT COUNT(*)
                        FROM tbl_asst_sls_ms_hdr
                        WHERE ASMH_PRJCT_NMBR_N = :projectNumber AND ASMH_YR_MNTH_N = :yearMonth";

                    using (OracleCommand checkCmd = new OracleCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        checkCmd.Parameters.Add(new OracleParameter("yearMonth", yearMonth));

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.Conflict, new { error = "A row with the same projectNumber and yearMonth already exists." });
                        }
                    }

                    // Insert the new row
                    string insertQuery = @"
                        INSERT INTO tbl_asst_sls_ms_hdr (
                            ASMH_YR_MNTH_N, ASMH_PRJCT_NMBR_N, ASMH_APPRVL_FLG_C,
                            ASMH_HS_ERRRS_C, ASMH_RMRKS_V, COIN_CRTN_USR_ID_V,
                            COIN_CRTN_DT_D, COIN_LST_MDFD_USR_ID_V, COIN_LST_MDFD_DT_D
                        ) VALUES (
                            :yearMonth, :projectNumber, 'N',
                            'N', :remarks, :userName,
                            SYSDATE, :userName, SYSDATE
                        )";

                    using (OracleCommand insertCmd = new OracleCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        insertCmd.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        insertCmd.Parameters.Add(new OracleParameter("remarks", (object)request.Remarks ?? DBNull.Value));
                        insertCmd.Parameters.Add(new OracleParameter("userName", request.UserName));

                        int rowsInserted = insertCmd.ExecuteNonQuery();
                        if (rowsInserted == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "No rows were inserted." });
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Header row inserted successfully." });
            }
            catch (OracleException oracleEx)
            {
                // Handle Oracle-specific exceptions
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = oracleEx.Message });
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/assetsales/updatehdr/{projectNumber}/{yearMonth}")]
        public HttpResponseMessage UpdateApprovalFlag(string projectNumber, string yearMonth, [FromBody] ProjectHeader request)
        {
            try
            {
                if (request == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Request body cannot be null." });
                }

                if (string.IsNullOrEmpty(request.UserName))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Username is required." });
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    // Check the approval status
                    string checkStatusQuery = @"
                SELECT asmaw_stts_flg_c
                FROM TBL_ASST_SLS_MS_APPRVL_WRKFLW
                WHERE asmaw_prjct_nmbr_n = :projectNumber
                AND asmaw_yr_mnth_n = :yearMonth
                AND asmaw_usr_nm_v = :userName";

                    using (OracleCommand checkStatusCmd = new OracleCommand(checkStatusQuery, connection))
                    {
                        checkStatusCmd.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        checkStatusCmd.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        checkStatusCmd.Parameters.Add(new OracleParameter("userName", request.UserName));

                        string status = checkStatusCmd.ExecuteScalar()?.ToString();

                        if (status != "A")
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Approval status is not 'A'." });
                        }
                    }

                    // Update the approval flag
                    string updateQuery = @"
                UPDATE tbl_asst_sls_ms_hdr
                SET ASMH_APPRVL_FLG_C = 'Y',
                    COIN_LST_MDFD_USR_ID_V = :userName,
                    COIN_LST_MDFD_DT_D = SYSDATE
                WHERE ASMH_PRJCT_NMBR_N = :projectNumber
                AND ASMH_YR_MNTH_N = :yearMonth";

                    using (OracleCommand updateCmd = new OracleCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.Add(new OracleParameter("userName", request.UserName));
                        updateCmd.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        updateCmd.Parameters.Add(new OracleParameter("yearMonth", yearMonth));

                        int rowsUpdated = updateCmd.ExecuteNonQuery();
                        if (rowsUpdated == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "No rows were updated." });
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Approval flag updated successfully." });
            }
            catch (OracleException oracleEx)
            {
                // Handle Oracle-specific exceptions
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = oracleEx.Message });
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}