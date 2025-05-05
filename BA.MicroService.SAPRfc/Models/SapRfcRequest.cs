using System.Collections.Generic;

namespace SapRfcMicroservice.Models
{
    public class SapRfcRequest
    {
        public string EncryptedConnection { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class SapConnectionInfo
    {
        public string Ashost { get; set; }
        public string Sysnr { get; set; }
        public string Client { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Lang { get; set; }
    }

    public class RfcResult
    {
        public bool Success { get; set; }
        public Dictionary<string, dynamic> Exports { get; set; } = new();
        public Dictionary<string, List<Dictionary<string, dynamic>>> Tables { get; set; } = new(); 
    }
}
