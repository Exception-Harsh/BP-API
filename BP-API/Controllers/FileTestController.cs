using System;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using BP_API.Models;
using System.Configuration;

namespace BP_API.Controllers
{
    public class FileTestController : ApiController
    {
        private string _connectionString = ConfigurationManager.AppSettings["connection"];

        [HttpPost]
        [Route("api/filetest/upload")]
        public async Task<HttpResponseMessage> UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return Request.CreateResponse(HttpStatusCode.UnsupportedMediaType, new { error = "Unsupported media type." });
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                foreach (var file in provider.Contents)
                {
                    var fileBytes = await file.ReadAsByteArrayAsync();

                    // Check if ContentDisposition and FileName are not null
                    if (file.Headers.ContentDisposition == null || string.IsNullOrEmpty(file.Headers.ContentDisposition.FileName))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = "Invalid file header." });
                    }

                    var fileName = file.Headers.ContentDisposition.FileName.Trim('\"');
                    var fileType = file.Headers.ContentType.MediaType;

                    // Map file types to shorter strings
                    string mappedFileType;
                    if (fileType == "application/pdf")
                    {
                        mappedFileType = "Pdf";
                    }
                    else if (fileType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                             fileType == "application/vnd.ms-excel")
                    {
                        mappedFileType = "Excel";
                    }
                    else
                    {
                        mappedFileType = "Other";
                    }

                    // Save the file details to the database
                    using (OracleConnection connection = new OracleConnection(_connectionString))
                    {
                        connection.Open();

                        // Get the next FDSB_NMBR_N value
                        string getNextNumberQuery = @"
                            SELECT NVL(MAX(FDSB_NMBR_N), 0) + 1
                            FROM TBL_FL_DT_STRG_BLB";

                        using (OracleCommand cmd = new OracleCommand(getNextNumberQuery, connection))
                        {
                            var nextNumber = Convert.ToInt32(cmd.ExecuteScalar());

                            // Insert the file details into the database
                            string insertQuery = @"
                                INSERT INTO TBL_FL_DT_STRG_BLB (
                                    FDSB_NMBR_N, FDSB_FL_NM_V, FDSB_FL_TYP_V, FDSB_FL_B
                                ) VALUES (
                                    :FDSB_NMBR_N, :FDSB_FL_NM_V, :FDSB_FL_TYP_V, :FDSB_FL_B
                                )";

                            using (OracleCommand insertCmd = new OracleCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.Add(new OracleParameter("FDSB_NMBR_N", nextNumber));
                                insertCmd.Parameters.Add(new OracleParameter("FDSB_FL_NM_V", fileName));
                                insertCmd.Parameters.Add(new OracleParameter("FDSB_FL_TYP_V", mappedFileType));
                                insertCmd.Parameters.Add(new OracleParameter("FDSB_FL_B", fileBytes));

                                int rowsInserted = insertCmd.ExecuteNonQuery();
                                if (rowsInserted == 0)
                                {
                                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = "Failed to insert file details." });
                                }
                            }
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "File uploaded successfully." });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = ex.Message });
            }
        }
    }
}