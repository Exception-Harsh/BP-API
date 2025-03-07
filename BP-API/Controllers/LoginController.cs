using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BP_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace BP_API.Controllers
{
    public class LoginController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpPost]
        [Route("api/login")]
        public HttpResponseMessage Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (loginRequest == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request body");
                }

                Console.WriteLine($"Received login request for username: {loginRequest.Username}");

                UserCheckResult userResult = new UserCheckResult();

                // ✅ Check Borrower (no password required)
                userResult = CheckUser("TBL_BRRWR_CNTCT_PRSN", "BCP_EML_V", "BCP_EML_V", "BCP_BRRWR_CD_V", loginRequest);
                if (userResult.IsValid)
                {
                    return CreateSuccessResponse("Borrower", userResult.Code);
                }

                // ✅ Check Trustee (password required)
                userResult = CheckUser("TBL_TRST_CNTCT_PRSN", "TCP_EML_V", "TCP_PSSWRD_V", "TCP_TRST_CD_C", loginRequest);
                if (userResult.IsValid)
                {
                    return CreateSuccessResponse("Trustee", userResult.Code);
                }

                // ✅ Check PME (password required)
                userResult = CheckUser("TBL_PME_CNTCT_PRSN", "PCP_EML_V", "PCP_PSSWRD_V", "PCP_PME_CD_C", loginRequest);
                if (userResult.IsValid)
                {
                    return CreateSuccessResponse("PME", userResult.Code);
                }

                // ✅ Check Arbour (password required)
                userResult = CheckUser("TB_USR_MSTR", "USR_NM_V", "USR_NM_V", null, loginRequest);
                if (userResult.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new { role = "Arbour" });
                }

                // ❌ If no match found, return Unauthorized
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { message = "Login failed", error = ex.Message });
            }
        }

        private UserCheckResult CheckUser(string tableName, string usernameColumn, string passwordColumn, string codeColumn, LoginRequest request)
        {
            var result = new UserCheckResult { IsValid = false, Code = null };

            try
            {
                string query = codeColumn != null
                    ? $"SELECT {codeColumn} FROM {tableName} WHERE {usernameColumn} = :username AND {passwordColumn} = :password"
                    : $"SELECT COUNT(*) FROM {tableName} WHERE {usernameColumn} = :username AND {passwordColumn} = :password";

                using (OracleConnection connection = new OracleConnection(_connectionString))
                using (OracleCommand command = new OracleCommand(query, connection))
                {
                    command.Parameters.Add(new OracleParameter(":username", OracleDbType.Varchar2)).Value = request.Username;
                    command.Parameters.Add(new OracleParameter(":password", OracleDbType.Varchar2)).Value = request.Password;

                    connection.Open();
                    object resultValue = command.ExecuteScalar();
                    connection.Close();

                    if (resultValue != null && (codeColumn != null || Convert.ToInt32(resultValue) > 0))
                    {
                        result.IsValid = true;
                        result.Code = codeColumn != null ? resultValue.ToString() : null;
                    }
                }
            }
            catch (OracleException orex)
            {
                Console.WriteLine($"🔥 Oracle Error in {tableName}: {orex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ General Error in {tableName}: {ex.Message}");
            }

            return result;
        }

        private HttpResponseMessage CreateSuccessResponse(string role, string code)
        {
            var responseData = new { role, message = "Login successful", code = code };
            return Request.CreateResponse(HttpStatusCode.OK, responseData);
        }
    }
}
