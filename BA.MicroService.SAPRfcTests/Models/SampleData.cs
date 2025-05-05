using SapRfcMicroservice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BA.MicroService.SAPRfcTests.Models
{
    public static class SampleData
    {
        public static RfcResult SampleRfcResult() => new RfcResult
        {
            Success = true,
            Exports = new Dictionary<string, dynamic>
            {
                { "MESSAGE", "Operation successful" },
                { "COUNT", 5 }
            },
            Tables = new Dictionary<string, List<Dictionary<string, dynamic>>>
            {
                {
                    "ITEMS",
                    new List<Dictionary<string, dynamic>>
                    {
                        new Dictionary<string, dynamic>
                        {
                            { "ID", 1001 },
                            { "DESCRIPTION", "Item A" },
                            { "QUANTITY", 10 }
                        },
                        new Dictionary<string, dynamic>
                        {
                            { "ID", 1002 },
                            { "DESCRIPTION", "Item B" },
                            { "QUANTITY", 5 }
                        }
                    }
                }
            }
        };
    }
}
