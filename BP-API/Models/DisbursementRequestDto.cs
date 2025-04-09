using System;
using System.IO;

namespace BP_API.Models
{
    public class DisbursementRequestDto
    {
        public int PadrDrNmbrN { get; set; }
        public int PadrPrjctNmbrN { get; set; }
        public int PadrAsstNmbrN { get; set; }
        public string PadrCtgryV { get; set; }
        public string PadrSbCtgryV { get; set; }
        public string PadrPrtyNmV { get; set; }
        public string PadrPrtyGstnV { get; set; }
        public string PadrPrtyPanV { get; set; }
        public string PadrPrtyEmlV { get; set; }
        public string PadrPrtyMblV { get; set; }
        public string PadrRsnV { get; set; }
        public string PadrPoWoV { get; set; }
        public decimal PadrTtlOrdrAmntN { get; set; }
        public string PadrDcmntTypV { get; set; }
        public string PadrPrtyDcmntNmbrV { get; set; }
        public DateTime PadrPrtyDcmntDtD { get; set; }
        public int PadrPrtyDcmntPyblDysN { get; set; }
        public decimal PadrPrtyDcmntAmntN { get; set; }
        public decimal PadrPrtyDcmntGstAmntN { get; set; }
        public decimal PadrPrtyDcmntTtlAmntN { get; set; }
        public decimal PadrPrtyTdsAmntN { get; set; }
        public decimal PadrPrtyAdvncAdjstdN { get; set; }
        public decimal PadrPrtyRtntnAmntN { get; set; }
        public decimal PadrPrtyOthrDdctnAmntN { get; set; }
        public decimal PadrPrtyPyblAmntN { get; set; }
        public decimal PadrPrtyOtstndngAmntN { get; set; }
        public string PadrBrrwrAccntNmbrV { get; set; }
        public string PadrPrtyBnkNmV { get; set; }
        public string PadrPrtyAccntNmV { get; set; }
        public string PadrPrtyAccntNmbrV { get; set; }
        public string PadrPrtyAccntIfscV { get; set; }
        public string PadrSttsC { get; set; }
        public decimal PadrApprvdAmntN { get; set; }
        public int PadrRfrncDrNmbrN { get; set; }
        public string PadrRmrksV { get; set; }

        // File attachment properties
        public string AttachmentFileName { get; set; }
        public string AttachmentContentType { get; set; }
        public Stream Attachment { get; set; }
    }


    public class FileDataStorageBlobDto
    {
        public int FdsbNmbrN { get; set; }
        public string FdsbFlNmV { get; set; }
        public string FdsbFlTypV { get; set; }
        public byte[] FdsbFlB { get; set; }
    }
}
