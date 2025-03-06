//using System;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Http;
//using System.Web.Http;
//using Oracle.ManagedDataAccess.Client;
//using BP_API.Models;
//using System.Configuration;
//using System.Data;

//namespace BP_API.Controllers
//{
//    public class ProjectController : ApiController
//    {
//        private string _connectionString = ConfigurationManager.AppSettings["connection"];

//        [HttpGet]
//        [Route("api/project/{role}/{code}")]
//        public HttpResponseMessage GetProjects(string role, string code)
//        {
//            try
//            {
//                using (OracleConnection connection = new OracleConnection(_connectionString))
//                {
//                    string sql;
//                    OracleCommand command = new OracleCommand();
//                    command.Connection = connection;

//                    if (role.Equals("Arbour", StringComparison.OrdinalIgnoreCase))
//                    {
//                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_STTS_C = 'A'";
//                    }
//                    else if (role.Equals("Borrower", StringComparison.OrdinalIgnoreCase))
//                    {
//                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_BRRWR_CD_C = :code AND PH_STTS_C = 'A'";
//                        command.Parameters.Add(new OracleParameter(":code", OracleDbType.Varchar2)).Value = code;
//                    }
//                    else if (role.Equals("Trustee", StringComparison.OrdinalIgnoreCase))
//                    {
//                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_TRST_CD_C = :code AND PH_STTS_C = 'A'";
//                        command.Parameters.Add(new OracleParameter(":code", OracleDbType.Varchar2)).Value = code;
//                    }
//                    else if (role.Equals("PME", StringComparison.OrdinalIgnoreCase))
//                    {
//                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_PME_CD_C = :code AND PH_STTS_C = 'A'";
//                        command.Parameters.Add(new OracleParameter(":code", OracleDbType.Varchar2)).Value = code;
//                    }
//                    else
//                    {
//                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid role" });
//                    }

//                    command.CommandText = sql;
//                    OracleDataAdapter adapter = new OracleDataAdapter(command);
//                    DataSet results = new DataSet();
//                    adapter.Fill(results);

//                    List<Project> projects = new List<Project>();
//                    foreach (DataRow row in results.Tables[0].Rows)
//                    {
//                        projects.Add(new Project(row));
//                    }

//                    return Request.CreateResponse(HttpStatusCode.OK, projects);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error fetching projects: {ex.Message}");
//                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
//            }
//        }
//    }
//}

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
    public class ProjectController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpGet]
        [Route("api/project/{role}/{code?}")]
        public HttpResponseMessage GetProjects(string role, string code = null)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    string sql;
                    OracleCommand command = new OracleCommand();
                    command.Connection = connection;

                    if (role.Equals("Arbour", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_STTS_C = 'A'";
                    }
                    else if (role.Equals("Borrower", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(code))
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Code is required for Borrower role" });

                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_BRRWR_CD_C = :code AND PH_STTS_C = 'A'";
                        command.Parameters.Add(new OracleParameter(":code", OracleDbType.Varchar2)).Value = code;
                    }
                    else if (role.Equals("Trustee", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(code))
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Code is required for Trustee role" });

                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_TRST_CD_C = :code AND PH_STTS_C = 'A'";
                        command.Parameters.Add(new OracleParameter(":code", OracleDbType.Varchar2)).Value = code;
                    }
                    else if (role.Equals("PME", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(code))
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Code is required for PME role" });

                        sql = "SELECT PH_PRJCT_NM_V, PH_PRJCT_NMBR_N, PH_TTL_IRR_N, PH_BRRWR_IRR_N FROM TBL_PRJCT_HDR WHERE PH_PME_CD_C = :code AND PH_STTS_C = 'A'";
                        command.Parameters.Add(new OracleParameter(":code", OracleDbType.Varchar2)).Value = code;
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid role" });
                    }

                    command.CommandText = sql;
                    OracleDataAdapter adapter = new OracleDataAdapter(command);
                    DataSet results = new DataSet();
                    adapter.Fill(results);

                    List<Project> projects = new List<Project>();
                    foreach (DataRow row in results.Tables[0].Rows)
                    {
                        projects.Add(new Project(row));
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, projects);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching projects: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}

