using BA.MicroService.SAPRfc.Models;
using SAP.Middleware.Connector;
using SapRfcMicroservice.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
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
                    if (func.Metadata.TryNameToIndex(param.Key) >= 0)
                    {
                        var v = ((JsonElement)param.Value);

                        object value = v.ValueKind switch
                        {
                            JsonValueKind.Object => ConvertJsonToRfcStructure(func, param.Key, v),
                            JsonValueKind.Array => ConvertListToRfcTable(func, param.Key, v),
                            JsonValueKind.Number => v.GetInt32(),
                            _ => v.GetString()
                        };

                        func.SetValue(param.Key, value);
                    }

                func.Invoke(dest);

                var result = new RfcResult { Success = true };

                for (int i = 0; i < func.Metadata.ParameterCount; i++)
                {
                    var param = func.Metadata[i];

                    if (param.Direction == RfcDirection.EXPORT || param.Direction == RfcDirection.CHANGING)
                    {
                        object value = param.DataType switch
                        {
                            RfcDataType.STRUCTURE => ConvertStructure(func.GetStructure(param.Name)),
                            RfcDataType.TABLE => ConvertTable(func.GetTable(param.Name)),
                            _ => func.GetValue(param.Name),
                        };

                        result.Exports[param.Name] = value;
                    }

                    if (param.Direction == RfcDirection.TABLES)
                    {
                        var value = ConvertTable(func.GetTable(param.Name));

                        result.Tables[param.Name] = value;
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
        /// <summary>
        /// 將 List&lt;Dictionary&lt;string, object&gt;&gt; 轉換為 SAP IRfcTable
        /// </summary>
        /// <param name="func">SAP 函數物件</param>
        /// <param name="tableName">TABLE 欄位名稱（如 "RETURN"）</param>
        /// <param name="rows">欄位資料</param>
        /// <returns>IRfcTable</returns>
        public static IRfcTable ConvertListToRfcTable(IRfcFunction func, string tableName, JsonElement rows)
        {
            if (rows.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("傳入的 JsonElement 不是 Array");

            var table = func.GetTable(tableName);
            table.Clear();

            foreach (var rowElement in rows.EnumerateArray())
            {
                if (rowElement.ValueKind != JsonValueKind.Object)
                    continue;

                var row = table.Metadata.LineType.CreateStructure();

                foreach (var prop in rowElement.EnumerateObject())
                {
                    var key = prop.Name;
                    var value = prop.Value;

                    try
                    {
                        if (value.ValueKind == JsonValueKind.Array)
                        {
                            // 巢狀子表格處理（如 MESSAGES）
                            var subTable = row.GetTable(key);
                            subTable.Clear();

                            foreach (var item in value.EnumerateArray())
                            {
                                var subRow = subTable.Metadata.LineType.CreateStructure();
                                foreach (var subProp in item.EnumerateObject())
                                {
                                    subRow.SetValue(subProp.Name, JsonElementToValue(subProp.Value));
                                }
                                subTable.Append(subRow);
                            }

                            continue; // ⚠️ 不可呼叫 row.SetValue(key, subTable)
                        }

                        if (value.ValueKind == JsonValueKind.Object)
                        {
                            // 子 STRUCTURE 處理
                            var subStruct = row.GetStructure(key);
                            foreach (var sub in value.EnumerateObject())
                            {
                                subStruct.SetValue(sub.Name, JsonElementToValue(sub.Value));
                            }
                            continue;
                        }

                        row.SetValue(key, JsonElementToValue(value));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ 欄位 {key} 寫入失敗：{ex.Message}");
                    }
                }

                table.Append(row);
            }

            return table;
        }
        /// <summary>
        /// 將 JsonElement（Object）轉為 IRfcStructure。
        /// </summary>
        public static IRfcStructure ConvertJsonToRfcStructure(IRfcFunction func, string structureName, JsonElement jsonObject)
        {
            if (jsonObject.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("傳入的 JSON 不是 Object");

            var structure = func.GetStructure(structureName);

            foreach (var prop in jsonObject.EnumerateObject())
            {
                var key = prop.Name;
                var value = prop.Value;

                try
                {
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.String:
                        case JsonValueKind.Number:
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            structure.SetValue(key, JsonElementToValue(value));
                            break;

                        case JsonValueKind.Object:
                            // 巢狀 STRUCTURE
                            var subStruct = structure.GetStructure(key);
                            foreach (var sub in value.EnumerateObject())
                            {
                                subStruct.SetValue(sub.Name, JsonElementToValue(sub.Value));
                            }
                            break;

                        case JsonValueKind.Array:
                            // 巢狀 TABLE
                            var subTable = structure.GetTable(key);
                            subTable.Clear();
                            foreach (var item in value.EnumerateArray())
                            {
                                var row = subTable.Metadata.LineType.CreateStructure();
                                foreach (var itemProp in item.EnumerateObject())
                                {
                                    row.SetValue(itemProp.Name, JsonElementToValue(itemProp.Value));
                                }
                                subTable.Append(row);
                            }
                            break;

                        default:
                            Console.WriteLine($"⚠️ 無法處理欄位 {key}，型別 {value.ValueKind}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 寫入欄位 {key} 發生錯誤：{ex.Message}");
                }
            }

            return structure;
        }

        private static object? JsonElementToValue(JsonElement e)
        {
            return e.ValueKind switch
            {
                JsonValueKind.String => e.GetString(),
                JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => e.ToString()
            };
        }
        public static List<Dictionary<string, object>> ConvertJsonArray(JsonElement jsonArray)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (var element in jsonArray.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    list.Add(ConvertJsonObject(element));
                }
                else
                {
                    throw new InvalidOperationException("JsonElement array must contain objects.");
                }
            }

            return list;
        }
        public static Dictionary<string, object> ConvertJsonObject(JsonElement jsonObject)
        {
            var dict = new Dictionary<string, object>();

            foreach (var property in jsonObject.EnumerateObject())
            {
                var key = property.Name;
                var value = property.Value;

                switch (value.ValueKind)
                {
                    case JsonValueKind.Object:
                        dict[key] = ConvertJsonObject(value);
                        break;

                    case JsonValueKind.Array:
                        // 遞迴處理巢狀陣列
                        var subList = new List<object>();
                        foreach (var item in value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                                subList.Add(ConvertJsonObject(item));
                            else
                                subList.Add(GetPrimitiveValue(item));
                        }
                        dict[key] = subList;
                        break;

                    default:
                        dict[key] = GetPrimitiveValue(value);
                        break;
                }
            }

            return dict;
        }

        private static object? GetPrimitiveValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
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

        // 將 STRUCTURE 轉換成 Dictionary
        public static Dictionary<string, dynamic> ConvertStructure(IRfcStructure structure)
        {
            var result = new Dictionary<string, dynamic>();

            for (int i = 0; i < structure.Metadata.FieldCount; i++)
            {
                var metadata = structure.Metadata[i];
                var fieldName = metadata.Name;
                var fieldValue = structure.GetString(fieldName); // 或 GetValue(fieldName)
                result[fieldName] = fieldValue;
            }

            return result;
        }

        // 將 TABLE 轉換成 List<Dictionary>
        public static List<Dictionary<string, dynamic>> ConvertTable(IRfcTable table)
        {
            var result = new List<Dictionary<string, dynamic>>();

            for (int i = 0; i < table.Count; i++)
            {
                table.CurrentIndex = i;
                var rowDict = new Dictionary<string, dynamic>();

                for (int j = 0; j < table.Metadata.LineType.FieldCount; j++)
                {
                    var metadata = table.Metadata.LineType[j];
                    var fieldName = metadata.Name;

                    object value = metadata.DataType switch
                    {
                        RfcDataType.STRUCTURE => ConvertStructure(table.GetStructure(metadata.Name)),
                        RfcDataType.TABLE => ConvertTable(table.GetTable(metadata.Name)),
                        _ => table.GetValue(metadata.Name),
                    };

                    rowDict[fieldName] = value;
                }

                result.Add(rowDict);
            }

            return result;
        }
    }
}