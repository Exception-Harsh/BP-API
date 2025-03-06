using System;
using System.Data;

public class Project
{
    public string ProjectName { get; set; }
    public string ProjectNumber { get; set; }
    public decimal TotalIRR { get; set; }
    public decimal BorrowerIRR { get; set; }

    public Project()
    {

    }

    public Project(DataRow row) : this()
    {
        ProjectName = row["PH_PRJCT_NM_V"].ToString();
        ProjectNumber = row["PH_PRJCT_NMBR_N"].ToString();
        TotalIRR = Convert.ToDecimal(row["PH_TTL_IRR_N"]);
        BorrowerIRR = Convert.ToDecimal(row["PH_BRRWR_IRR_N"]);
    }
}