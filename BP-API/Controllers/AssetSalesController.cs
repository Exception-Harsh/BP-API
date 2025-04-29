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
        [Route("api/assetsales/buildings/{projectNumber}/{yearMonth}")]
        public HttpResponseMessage GetBuildingsData(string projectNumber, string yearMonth)
        {
            try
            {
                // Step 1: Fetch buildings data using the provided projectNumber and yearMonth
                string fetchBuildingsQuery = @"
            SELECT DISTINCT ASM_BLDNG_V
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth";

                List<string> buildings = new List<string>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchBuildingsQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                buildings.Add(reader["ASM_BLDNG_V"].ToString());
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

        [HttpPost]
        [Route("api/assetsales/update/{projectNumber}/{yearMonth}/{buildingName}")]
        public HttpResponseMessage UpdateAssetSales(string projectNumber, string yearMonth, string buildingName, [FromBody] List<AssetSale> assetSales)
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