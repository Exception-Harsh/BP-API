using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using BP_API.Models;
using System.Configuration;
using System.Data;

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



        [HttpGet]
        [Route("api/assetsales/filters/{projectNumber}/{yearMonth}/{buildingName}/{uniqueUnitNumber}")]
        public HttpResponseMessage GetFiltersData(string projectNumber, string yearMonth, string buildingName)
        {
            try
            {
                // Fetch distinct unit numbers for the selected building
                string fetchUnitNumbersQuery = @"
            SELECT DISTINCT ASM_UNT_NMBR_V
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth
              AND ASM_BLDNG_V = :buildingName";

                List<string> unitNumbers = new List<string>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchUnitNumbersQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("buildingName", buildingName));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                unitNumbers.Add(reader["ASM_UNT_NMBR_V"].ToString());
                            }
                        }
                    }
                }

                // Fetch distinct unit configurations for the selected building
                string fetchUnitConfigurationsQuery = @"
            SELECT DISTINCT ASM_UNT_CNFGRTN_V
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth
              AND ASM_BLDNG_V = :buildingName";

                List<string> unitConfigurations = new List<string>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchUnitConfigurationsQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("buildingName", buildingName));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                unitConfigurations.Add(reader["ASM_UNT_CNFGRTN_V"].ToString());
                            }
                        }
                    }
                }

                // Fetch distinct customer names for the selected building
                string fetchCustomerNamesQuery = @"
            SELECT DISTINCT ASM_CSTMR_NM_V
            FROM TBL_ASST_SLS_MS
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth
              AND ASM_BLDNG_V = :buildingName";

                List<string> customerNames = new List<string>();
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    using (OracleCommand command = new OracleCommand(fetchCustomerNamesQuery, connection))
                    {
                        command.Parameters.Add(new OracleParameter("projectNumber", projectNumber));
                        command.Parameters.Add(new OracleParameter("yearMonth", yearMonth));
                        command.Parameters.Add(new OracleParameter("buildingName", buildingName));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                customerNames.Add(reader["ASM_CSTMR_NM_V"].ToString());
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { unitNumbers, unitConfigurations, customerNames });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching filters data: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/assetsales/update/{projectNumber}/{yearMonth}/{buildingName}/{uniqueUnitNumber}")]
        public HttpResponseMessage UpdateAssetSale([FromBody] newAssetSale updateModel)
        {
            try
            {
                if (updateModel == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid data provided." });
                }

                string selectQuery = @"SELECT * FROM TBL_ASST_SLS_MS
                                     WHERE ASM_PRJCT_NMBR_N = :projectNumber
                                     AND ASM_YR_MNTH_N = :yearMonth
                                     AND ASM_BLDNG_V = :buildingName
                                     AND ASM_UNT_UNQ_NMBR_N = :UniqueUnitNumber";
                string updateQuery = @"
            UPDATE TBL_ASST_SLS_MS
            SET
                ASM_UNT_SLD_FLG_C = :SoldFlag,
                ASM_UNT_RGSTRD_FLG_C = :RegisteredFlag,
                ASM_UNT_RGSTRTN_DT_D = :RegistrationDate,
                ASM_UNT_BKNG_DT_D = :BookingDate,
                ASM_ALLTMNT_LTTR_DT_D = :AllotmentLetterDate,
                ASM_UNT_AGRMNT_DT_D = :AgreementDate,
                ASM_CSTMR_NM_V = :CustomerName,
                ASM_CSTMR_KYC_AADHR_N = :CustomerKycAadhar,
                ASM_CSTMR_KYC_PN_V = :CustomerKycPan,
                ASM_CSTMR_KYC_MBL_V = :CustomerKycMobile,
                ASM_CSTMR_KYC_EML_V = :CustomerKycEmail,
                ASM_CSTMR_KYC_ADDRSS_V = :CustomerKycAddress,
                ASM_NC_ISSD_FLG_C = :NcIssuedFlag,
                ASM_NC_NMBR_V = :NcNumber,
                ASM_SLS_BS_PRC_N = :SalesBasePrice,
                ASM_SLS_STMP_DTY_AMNT_N = :SalesStampDutyAmount,
                ASM_SLS_RGSTRN_AMNT_N = :SalesRegistrationAmount,
                ASM_SLS_OC_AMNT_N = :SalesOtherCharges,
                ASM_SLS_PSS_THRGH_CHRGS_N = :SalesPassThroughCharges,
                ASM_SLS_TXS_AMNT_N = :SalesTaxesAmount,
                ASM_SLS_TTL_AMNT_N = :SalesTotalAmount,
                ASM_DMND_BS_PRC_N = :DemandBasePrice,
                ASM_DMND_STMP_DTY_N = :DemandStampDuty,
                ASM_DMND_RGSTRTN_AMNT_N = :DemandRegistrationAmount,
                ASM_DMND_OC_AMNT_N = :DemandOtherCharges,
                ASM_DMND_PSS_THRGH_CHRGS_N = :DemandPassThroughCharges,
                ASM_DMND_TXS_AMNT_N = :DemandTaxesAmount,
                ASM_DMND_TTL_AMNT_N = :DemandTotalAmount,
                ASM_RCVD_BS_PRC_N = :ReceivedBasePrice,
                ASM_RCVD_STMP_DTY_AMNT_N = :ReceivedStampDutyAmount,
                ASM_RCVD_RGSTRN_AMNT_N = :ReceivedRegistrationAmount,
                ASM_RCVD_OC_AMNT_N = :ReceivedOtherCharges,
                ASM_RCVD_PSS_THRGH_CHRGS_N = :ReceivedPassThroughCharges,
                ASM_RCVD_TXS_AMNT_N = :ReceivedTaxesAmount,
                ASM_RCVD_TTL_AMNT_N = :ReceivedTotalAmount,
                ASM_MD_OF_FNNC_C = :ModeOfFinance,
                ASM_FI_NM_V = :FinancialInstitutionName,
                ASM_PYMNT_PLN_NM_V = :PaymentPlanName,
                ASM_SRC_OF_CSTMR_C = :SourceOfCustomer,
                ASM_CHNNL_PRTNR_NM_V = :ChannelPartnerName,
                ASM_CHNNL_PRTNR_MBL_V = :ChannelPartnerMobile,
                ASM_CHNNL_PRTNR_EML_V = :ChannelPartnerEmail,
                ASM_BRKRG_AMNT_N = :BrokerageAmount
            WHERE ASM_PRJCT_NMBR_N = :projectNumber
              AND ASM_YR_MNTH_N = :yearMonth
              AND ASM_BLDNG_V = :buildingName
              AND ASM_UNT_UNQ_NMBR_N = :UniqueUnitNumber";

                using (OracleDataAdapter adapter = new OracleDataAdapter(selectQuery, _connectionString))
                {
                    adapter.SelectCommand.Parameters.Add(new OracleParameter("UniqueUnitNumber", updateModel.UniqueUnitNumber));

                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, "AssetSale");

                    if (dataSet.Tables["AssetSale"] != null && dataSet.Tables["AssetSale"].Rows.Count > 0)
                    {
                        DataTable table = dataSet.Tables["AssetSale"];
                        DataRow row = table.Rows[0];

                        // Update the DataRow with values from updateModel
                        row["ASM_UNT_SLD_FLG_C"] = updateModel.SoldFlag;
                        row["ASM_UNT_RGSTRD_FLG_C"] = updateModel.RegisteredFlag;
                        row["ASM_UNT_RGSTRTN_DT_D"] = updateModel.RegistrationDate ?? (object)DBNull.Value;
                        row["ASM_UNT_BKNG_DT_D"] = updateModel.BookingDate ?? (object)DBNull.Value;
                        row["ASM_ALLTMNT_LTTR_DT_D"] = updateModel.AllotmentLetterDate ?? (object)DBNull.Value;
                        row["ASM_UNT_AGRMNT_DT_D"] = updateModel.AgreementDate ?? (object)DBNull.Value;
                        row["ASM_CSTMR_NM_V"] = updateModel.CustomerName;
                        row["ASM_CSTMR_KYC_AADHR_N"] = updateModel.CustomerKycAadhar;
                        row["ASM_CSTMR_KYC_PN_V"] = updateModel.CustomerKycPan;
                        row["ASM_CSTMR_KYC_MBL_V"] = updateModel.CustomerKycMobile;
                        row["ASM_CSTMR_KYC_EML_V"] = updateModel.CustomerKycEmail;
                        row["ASM_CSTMR_KYC_ADDRSS_V"] = updateModel.CustomerKycAddress;
                        row["ASM_NC_ISSD_FLG_C"] = updateModel.NcIssuedFlag;
                        row["ASM_NC_NMBR_V"] = updateModel.NcNumber;
                        row["ASM_SLS_BS_PRC_N"] = updateModel.SalesBasePrice;
                        row["ASM_SLS_STMP_DTY_AMNT_N"] = updateModel.SalesStampDutyAmount;
                        row["ASM_SLS_RGSTRN_AMNT_N"] = updateModel.SalesRegistrationAmount;
                        row["ASM_SLS_OC_AMNT_N"] = updateModel.SalesOtherCharges;
                        row["ASM_SLS_PSS_THRGH_CHRGS_N"] = updateModel.SalesPassThroughCharges;
                        row["ASM_SLS_TXS_AMNT_N"] = updateModel.SalesTaxesAmount;
                        row["ASM_SLS_TTL_AMNT_N"] = updateModel.SalesTotalAmount;
                        row["ASM_DMND_BS_PRC_N"] = updateModel.DemandBasePrice;
                        row["ASM_DMND_STMP_DTY_N"] = updateModel.DemandStampDuty;
                        row["ASM_DMND_RGSTRTN_AMNT_N"] = updateModel.DemandRegistrationAmount;
                        row["ASM_DMND_OC_AMNT_N"] = updateModel.DemandOtherCharges;
                        row["ASM_DMND_PSS_THRGH_CHRGS_N"] = updateModel.DemandPassThroughCharges;
                        row["ASM_DMND_TXS_AMNT_N"] = updateModel.DemandTaxesAmount;
                        row["ASM_DMND_TTL_AMNT_N"] = updateModel.DemandTotalAmount;
                        row["ASM_RCVD_BS_PRC_N"] = updateModel.ReceivedBasePrice;
                        row["ASM_RCVD_STMP_DTY_AMNT_N"] = updateModel.ReceivedStampDutyAmount;
                        row["ASM_RCVD_RGSTRN_AMNT_N"] = updateModel.ReceivedRegistrationAmount;
                        row["ASM_RCVD_OC_AMNT_N"] = updateModel.ReceivedOtherCharges;
                        row["ASM_RCVD_PSS_THRGH_CHRGS_N"] = updateModel.ReceivedPassThroughCharges;
                        row["ASM_RCVD_TXS_AMNT_N"] = updateModel.ReceivedTaxesAmount;
                        row["ASM_RCVD_TTL_AMNT_N"] = updateModel.ReceivedTotalAmount;
                        row["ASM_MD_OF_FNNC_C"] = updateModel.ModeOfFinance;
                        row["ASM_FI_NM_V"] = updateModel.FinancialInstitutionName;
                        row["ASM_PYMNT_PLN_NM_V"] = updateModel.PaymentPlanName;
                        row["ASM_SRC_OF_CSTMR_C"] = updateModel.SourceOfCustomer;
                        row["ASM_CHNNL_PRTNR_NM_V"] = updateModel.ChannelPartnerName;
                        row["ASM_CHNNL_PRTNR_MBL_V"] = updateModel.ChannelPartnerMobile;
                        row["ASM_CHNNL_PRTNR_EML_V"] = updateModel.ChannelPartnerEmail;
                        row["ASM_BRKRG_AMNT_N"] = updateModel.BrokerageAmount;

                        // Configure the UpdateCommand - Important: Do this only once per adapter if possible for better performance
                        adapter.UpdateCommand = new OracleCommand(updateQuery, adapter.SelectCommand.Connection);
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SoldFlag", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_UNT_SLD_FLG_C", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("RegisteredFlag", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_UNT_RGSTRD_FLG_C", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("RegistrationDate", OracleDbType.Date, 0, ParameterDirection.Input, true, 0, 0, "ASM_UNT_RGSTRTN_DT_D", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("BookingDate", OracleDbType.Date, 0, ParameterDirection.Input, true, 0, 0, "ASM_UNT_BKNG_DT_D", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("AllotmentLetterDate", OracleDbType.Date, 0, ParameterDirection.Input, true, 0, 0, "ASM_ALLTMNT_LTTR_DT_D", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("AgreementDate", OracleDbType.Date, 0, ParameterDirection.Input, true, 0, 0, "ASM_UNT_AGRMNT_DT_D", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("CustomerName", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_CSTMR_NM_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("CustomerKycAadhar", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_CSTMR_KYC_AADHR_N", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("CustomerKycPan", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_CSTMR_KYC_PN_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("CustomerKycMobile", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_CSTMR_KYC_MBL_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("CustomerKycEmail", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_CSTMR_KYC_EML_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("CustomerKycAddress", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_CSTMR_KYC_ADDRSS_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("NcIssuedFlag", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_NC_ISSD_FLG_C", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("NcNumber", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_NC_NMBR_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesBasePrice", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_BS_PRC_N", DataRowVersion.Current, DBNull.Value)); // Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesStampDutyAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_STMP_DTY_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesRegistrationAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_RGSTRN_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesOtherCharges", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_OC_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesPassThroughCharges", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_PSS_THRGH_CHRGS_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesTaxesAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_TXS_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SalesTotalAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_SLS_TTL_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandBasePrice", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_BS_PRC_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandStampDuty", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_STMP_DTY_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandRegistrationAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_RGSTRTN_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandOtherCharges", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_OC_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandPassThroughCharges", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_PSS_THRGH_CHRGS_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandTaxesAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_TXS_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("DemandTotalAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_DMND_TTL_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedBasePrice", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_BS_PRC_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedStampDutyAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_STMP_DTY_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedRegistrationAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_RGSTRN_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedOtherCharges", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_OC_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedPassThroughCharges", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_PSS_THRGH_CHRGS_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedTaxesAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_TXS_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ReceivedTotalAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_RCVD_TTL_AMNT_N", DataRowVersion.Current, DBNull.Value)); // Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ModeOfFinance", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_MD_OF_FNNC_C", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("FinancialInstitutionName", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_FI_NM_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("PaymentPlanName", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_PYMNT_PLN_NM_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("SourceOfCustomer", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_SRC_OF_CSTMR_C", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ChannelPartnerName", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_CHNNL_PRTNR_NM_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ChannelPartnerMobile", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_CHNNL_PRTNR_MBL_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("ChannelPartnerEmail", OracleDbType.Varchar2, 255, ParameterDirection.Input, true, 0, 0, "ASM_CHNNL_PRTNR_EML_V", DataRowVersion.Current, DBNull.Value));
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("BrokerageAmount", OracleDbType.Decimal, 0, ParameterDirection.Input, true, 10, 2, "ASM_BRKRG_AMNT_N", DataRowVersion.Current, DBNull.Value));// Added precision and scale
                        adapter.UpdateCommand.Parameters.Add(new OracleParameter("UniqueUnitNumber", OracleDbType.Varchar2, 50, ParameterDirection.Input, true, 0, 0, "ASM_UNT_UNQ_NMBR_N", DataRowVersion.Current, DBNull.Value));

                        OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                        int rowsAffected = adapter.Update(dataSet, "AssetSale");

                        if (rowsAffected > 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, new { message = "Asset sale updated successfully." });
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "Asset sale not found or no updates were made." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error to the server console
                Console.WriteLine($"Error updating asset sale: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
            // Add this return statement to ensure all code paths return a value
            return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." });
        }
    }
}