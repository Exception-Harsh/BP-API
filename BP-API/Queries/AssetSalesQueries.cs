using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BP_API.Queries
{
    public class AssetSalesQueries
    {
        public static string UpdateAssetSalesMisRow
        {
            get
            {
                return @"
UPDATE TBL_ASST_SLS_MS
            SET
                ASM_UNT_SLD_FLG_C = '{3}',
                ASM_UNT_RGSTRD_FLG_C = '{4}',
                ASM_UNT_RGSTRTN_DT_D = {5},
                ASM_UNT_BKNG_DT_D = {6},
                ASM_ALLTMNT_LTTR_DT_D = {7},
                ASM_UNT_AGRMNT_DT_D = {8},
                ASM_CSTMR_NM_V = '{9}',
                ASM_CSTMR_KYC_AADHR_N = '{10}',
                ASM_CSTMR_KYC_PN_V = '{11}',
                ASM_CSTMR_KYC_MBL_V = '{12}',
                ASM_CSTMR_KYC_EML_V = '{13}',
                ASM_CSTMR_KYC_ADDRSS_V = '{14}',
                ASM_NC_ISSD_FLG_C = '{15}',
                ASM_NC_NMBR_V = '{16}',
                ASM_SLS_BS_PRC_N = {17},
                ASM_SLS_STMP_DTY_AMNT_N = {18},
                ASM_SLS_RGSTRN_AMNT_N = {19},
                ASM_SLS_OC_AMNT_N = {20},
                ASM_SLS_PSS_THRGH_CHRGS_N = {21},
                ASM_SLS_TXS_AMNT_N = {22},
                ASM_SLS_TTL_AMNT_N = {23},
                ASM_DMND_BS_PRC_N = {24},
                ASM_DMND_STMP_DTY_N = {25},
                ASM_DMND_RGSTRTN_AMNT_N = {26},
                ASM_DMND_OC_AMNT_N = {27},
                ASM_DMND_PSS_THRGH_CHRGS_N = {28},
                ASM_DMND_TXS_AMNT_N = {29},
                ASM_DMND_TTL_AMNT_N = {30},
                ASM_RCVD_BS_PRC_N = {31},
                ASM_RCVD_STMP_DTY_AMNT_N = {32},
                ASM_RCVD_RGSTRN_AMNT_N = {33},
                ASM_RCVD_OC_AMNT_N = {34},
                ASM_RCVD_PSS_THRGH_CHRGS_N = {35},
                ASM_RCVD_TXS_AMNT_N = {36},
                ASM_RCVD_TTL_AMNT_N = {37},
                ASM_MD_OF_FNNC_C = '{38}',
                ASM_FI_NM_V = '{39}',
                ASM_PYMNT_PLN_NM_V = '{40}',
                ASM_SRC_OF_CSTMR_C = '{41}',
                ASM_CHNNL_PRTNR_NM_V = '{42}',
                ASM_CHNNL_PRTNR_MBL_V = '{43}',
                ASM_CHNNL_PRTNR_EML_V = '{44}',
                ASM_BRKRG_AMNT_N = {45}
            WHERE ASM_PRJCT_NMBR_N = {0}
              AND ASM_YR_MNTH_N = {1}              
              AND ASM_UNT_UNQ_NMBR_N = {2}";
            }
        }
    }
}