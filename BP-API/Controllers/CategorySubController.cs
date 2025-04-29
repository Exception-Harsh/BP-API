using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;

namespace BP_API.Controllers
{
    public class CategorySubController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpGet]
        [Route("api/categories")]
        public HttpResponseMessage GetCategories()
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT ccd_ctgry_v FROM tbl_cst_ctgry_dctnry ORDER BY 1";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            List<string> categories = new List<string>();
                            while (reader.Read())
                            {
                                categories.Add(reader.GetString(0));
                            }
                            // Explicitly set the response format to JSON
                            return Request.CreateResponse(HttpStatusCode.OK, categories, "application/json");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving categories: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
        [HttpGet]
        [Route("api/subcategories")]
        public HttpResponseMessage GetSubCategories(string category)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT CCD_SB_CTGRY_V FROM tbl_cst_ctgry_dctnry WHERE CCD_CTGRY_V = :category ORDER BY 1";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("category", category));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            List<string> subCategories = new List<string>();
                            while (reader.Read())
                            {
                                subCategories.Add(reader.GetString(0));
                            }
                            return Request.CreateResponse(HttpStatusCode.OK, subCategories, "application/json");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving subcategories: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }

    }
}