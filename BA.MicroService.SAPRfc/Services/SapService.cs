// Services/SapService.cs
using SAP.Middleware.Connector;
using SapRfcMicroservice.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SapRfcMicroservice
{
    public class SapService
    {
        private readonly AesCryptoService _cryptoService;

        public SapService(AesCryptoService cryptoService) => _cryptoService = cryptoService;

        public async Task<RfcResult> CallRfcAsync(SapRfcRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.EncryptedConnection))
                    throw new ArgumentException("Missing EncryptedConnection");
                if (string.IsNullOrWhiteSpace(request.FunctionName))
                    throw new ArgumentException("Missing FunctionName");

                var connInfo = _cryptoService.DecryptConnection(request.EncryptedConnection);
                var dest = CreateDestination(connInfo);
                var func = dest.Repository.CreateFunction(request.FunctionName);

                foreach (var param in request.Parameters)
                {
                    if (func.Metadata.TryNameToIndex(param.Key) > 0)
                    {
                        func.SetValue(param.Key, param.Value);
                    }
                }

                func.Invoke(dest);

                var result = new RfcResult { Success = true };

                for (int i = 0; i < func.Metadata.ParameterCount; i++)
                {
                    var param = func.Metadata[i];

                    if (param.Direction == RfcDirection.EXPORT || param.Direction == RfcDirection.CHANGING)
                    {
                        result.Exports[param.Name] = func.GetValue(param.Name);
                    }

                    if (param.Direction == RfcDirection.TABLES)
                    {
                        var table = func.GetTable(param.Name);
                        var rows = new List<Dictionary<string, dynamic>>();

                        for (int r = 0; r < table.RowCount; r++)
                        {
                            table.CurrentIndex = r;
                            var dict = new Dictionary<string, dynamic>();

                            var structure = table[r];
                            var lineType = structure.Metadata;
                            for (int f = 0; f < lineType.FieldCount; f++)
                            {
                                var fieldMeta = lineType[f];
                                dict[fieldMeta.Name] = structure.GetString(fieldMeta.Name);
                            }

                            rows.Add(dict);
                        }

                        result.Tables[param.Name] = rows;
                    }
                }

                return await Task.FromResult(result);
            }
            catch (RfcAbapException ex)
            {
                return new RfcResult
                {
                    Success = false,
                    Exports = new Dictionary<string, dynamic> { { "error", $"ABAP Exception: {ex.Message}" } }
                };
            }
            catch (RfcCommunicationException ex)
            {
                return new RfcResult
                {
                    Success = false,
                    Exports = new Dictionary<string, dynamic> { { "error", $"Communication Error: {ex.Message}" } }
                };
            }
            catch (Exception ex)
            {
                return new RfcResult
                {
                    Success = false,
                    Exports = new Dictionary<string, dynamic> { { "error", $"Unexpected Error: {ex.Message}" } }
                };
            }
        }

        private RfcDestination CreateDestination(SapConnectionInfo info)
        {
            var parms = new RfcConfigParameters {
                { RfcConfigParameters.Name, "SAP" },
                { RfcConfigParameters.AppServerHost, info.Ashost },
                { RfcConfigParameters.SystemNumber, info.Sysnr },
                { RfcConfigParameters.Client, info.Client },
                { RfcConfigParameters.User, info.User },
                { RfcConfigParameters.Password, info.Password },
                { RfcConfigParameters.Language, info.Lang },
            };
            return RfcDestinationManager.GetDestination(parms);
        }
    }
}