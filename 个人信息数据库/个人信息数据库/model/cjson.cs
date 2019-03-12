//using System;
//using System.Data;
//using System.Text;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Data.Common;
//using System.Collections;
//using System.IO;
//using System.Text.RegularExpressions;
//using System.Runtime.Serialization.Json;



//namespace ZZUdp.Tool
//{
//    public static class JsonHepler
//    {
//        /// <summary>
//        /// List转成json 
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="jsonName"></param>
//        /// <param name="list"></param>
//        /// <returns></returns>
//        public static string ListToJson<T>(IList<T> list , string jsonName)
//        {
//            StringBuilder Json = new StringBuilder();
//            if (string.IsNullOrEmpty(jsonName))
//                jsonName = list[0].GetType().Name;
//            Json.Append("{\"" + jsonName + "\":[");
//            if (list.Count > 0)
//            {
//                for (int i = 0; i < list.Count; i++)
//                {
//                    T obj = Activator.CreateInstance<T>();
//                    PropertyInfo[] pi = obj.GetType().GetProperties();
//                    Json.Append("{");
//                    for (int j = 0; j < pi.Length; j++)
//                    {
//                        Type type;
//                        object o = pi[j].GetValue(list[i] , null);
//                        string v = string.Empty;
//                        if (o != null)
//                        {
//                            type = o.GetType();
//                            v = o.ToString();
//                        }
//                        else
//                        {
//                            type = typeof(string);
//                        }

//                        Json.Append("\"" + pi[j].Name.ToString() + "\":" + StringFormat(v , type));

//                        if (j < pi.Length - 1)
//                        {
//                            Json.Append(",");
//                        }
//                    }
//                    Json.Append("}");
//                    if (i < list.Count - 1)
//                    {
//                        Json.Append(",");
//                    }
//                }
//            }
//            Json.Append("]}");
//            return Json.ToString();
//        }

//        /// <summary>
//        /// 序列化集合对象
//        /// </summary>
//        public static string JsonSerializerByArrayData<T>(T[] tArray)
//        {
//            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T[]));
//            MemoryStream ms = new MemoryStream();
//            ser.WriteObject(ms , tArray);
//            string jsonString = Encoding.UTF8.GetString(ms.ToArray());
//            ms.Close();
//            string p = @"\\/Date\((\d+)\+\d+\)\\/";
//            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
//            Regex reg = new Regex(p);
//            jsonString = reg.Replace(jsonString , matchEvaluator);
//            return jsonString;
//        }

//        /// <summary>
//        /// 序列化单个对象
//        /// </summary>
//        public static string JsonSerializerBySingleData<T>(T t)
//        {
//            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
//            MemoryStream ms = new MemoryStream();
//            ser.WriteObject(ms , t);
//            string jsonString = Encoding.UTF8.GetString(ms.ToArray());
//            ms.Close();
//            string p = @"\\/Date\((\d+)\+\d+\)\\/";
//            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertJsonDateToDateString);
//            Regex reg = new Regex(p);
//            jsonString = reg.Replace(jsonString , matchEvaluator);
//            return jsonString;
//        }

//        /// <summary> 
//        /// 反序列化单个对象
//        /// </summary> 
//        public static T JsonDeserializeBySingleData<T>(string jsonString)
//        {
//            //将"yyyy-MM-dd HH:mm:ss"格式的字符串转为"\/Date(1294499956278+0800)\/"格式  
//            string p = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}";
//            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertDateStringToJsonDate);
//            Regex reg = new Regex(p);
//            jsonString = reg.Replace(jsonString , matchEvaluator);
//            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
//            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
//            T obj = (T)ser.ReadObject(ms);
//            return obj;
//        }

//        /// <summary> 
//        /// 反序列化集合对象
//        /// </summary> 
//        public static T[] JsonDeserializeByArrayData<T>(string jsonString)
//        {
//            //将"yyyy-MM-dd HH:mm:ss"格式的字符串转为"\/Date(1294499956278+0800)\/"格式  
//            string p = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}";
//            MatchEvaluator matchEvaluator = new MatchEvaluator(ConvertDateStringToJsonDate);
//            Regex reg = new Regex(p);
//            jsonString = reg.Replace(jsonString , matchEvaluator);
//            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T[]));
//            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
//            T[] arrayObj = (T[])ser.ReadObject(ms);
//            return arrayObj;
//        }

//        /// <summary> 
//        /// 将Json序列化的时间由/Date(1294499956278+0800)转为字符串 
//        /// </summary> 
//        private static string ConvertJsonDateToDateString(Match m)
//        {
//            string result = string.Empty;
//            DateTime dt = new DateTime(1970 , 1 , 1);
//            dt = dt.AddMilliseconds(long.Parse(m.Groups[1].Value));
//            dt = dt.ToLocalTime();
//            result = dt.ToString("yyyy-MM-dd HH:mm:ss");
//            return result;
//        }

//        /// <summary>  
//        /// 将时间字符串转为Json时间 
//        /// </summary> 
//        private static string ConvertDateStringToJsonDate(Match m)
//        {
//            string result = string.Empty;
//            DateTime dt = DateTime.Parse(m.Groups[0].Value);
//            dt = dt.ToUniversalTime();
//            TimeSpan ts = dt - DateTime.Parse("1970-01-01");
//            result = string.Format("\\/Date({0}+0800)\\/" , ts.TotalMilliseconds);
//            return result;
//        }

//        /// <summary>
//        /// List转成json 
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="list"></param>
//        /// <returns></returns>
//        public static string ListToJson<T>(IList<T> list)
//        {
//            object obj = list[0];
//            return ListToJson<T>(list , obj.GetType().Name);
//        }

//        /// <summary> 
//        /// 对象转换为Json字符串 
//        /// </summary> 
//        /// <param name="jsonObject">对象</param> 
//        /// <returns>Json字符串</returns> 
//        public static string ToJson(object jsonObject)
//        {
//            try
//            {
//                StringBuilder jsonString = new StringBuilder();
//                jsonString.Append("{");
//                PropertyInfo[] propertyInfo = jsonObject.GetType().GetProperties();
//                for (int i = 0; i < propertyInfo.Length; i++)
//                {
//                    object objectValue = propertyInfo[i].GetGetMethod().Invoke(jsonObject , null);
//                    if (objectValue == null)
//                    {
//                        continue;
//                    }
//                    StringBuilder value = new StringBuilder();
//                    if (objectValue is DateTime || objectValue is Guid || objectValue is TimeSpan)
//                    {
//                        value.Append("\"" + objectValue.ToString() + "\"");
//                    }
//                    else if (objectValue is string)
//                    {
//                        value.Append("\"" + objectValue.ToString() + "\"");
//                    }
//                    else if (objectValue is IEnumerable)
//                    {
//                        value.Append(ToJson((IEnumerable)objectValue));
//                    }
//                    else
//                    {
//                        value.Append("\"" + objectValue.ToString() + "\"");
//                    }
//                    jsonString.Append("\"" + propertyInfo[i].Name + "\":" + value + ",");
//                    ;
//                }
//                return jsonString.ToString().TrimEnd(',') + "}";
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }

//        /// <summary> 
//        /// 对象集合转换Json 
//        /// </summary> 
//        /// <param name="array">集合对象</param> 
//        /// <returns>Json字符串</returns> 
//        public static string ToJson(IEnumerable array)
//        {
//            string jsonString = "[";
//            foreach (object item in array)
//            {
//                jsonString += ToJson(item) + ",";
//            }
//            if (jsonString.Length > 1)
//            {
//                jsonString.Remove(jsonString.Length - 1 , jsonString.Length);
//            }
//            else
//            {
//                jsonString = "[]";
//            }
//            return jsonString + "]";
//        }

//        /// <summary> 
//        /// 普通集合转换Json 
//        /// </summary> 
//        /// <param name="array">集合对象</param> 
//        /// <returns>Json字符串</returns> 
//        public static string ToArrayString(IEnumerable array)
//        {
//            string jsonString = "[";
//            foreach (object item in array)
//            {
//                jsonString = ToJson(item.ToString()) + ",";
//            }
//            jsonString.Remove(jsonString.Length - 1 , jsonString.Length);
//            return jsonString + "]";
//        }

//        /// <summary> 
//        /// Datatable转换为Json 
//        /// </summary> 
//        /// <param name="table">Datatable对象</param> 
//        /// <returns>Json字符串</returns> 
//        public static string ToJson(DataTable dt)
//        {
//            StringBuilder jsonString = new StringBuilder();
//            jsonString.Append("[");
//            DataRowCollection drc = dt.Rows;
//            for (int i = 0; i < drc.Count; i++)
//            {
//                jsonString.Append("{");
//                for (int j = 0; j < dt.Columns.Count; j++)
//                {
//                    string strKey = dt.Columns[j].ColumnName;
//                    string strValue = drc[i][j].ToString();
//                    Type type = dt.Columns[j].DataType;
//                    jsonString.Append("\"" + strKey + "\":");
//                    strValue = StringFormat(strValue , type);
//                    if (j < dt.Columns.Count - 1)
//                    {
//                        jsonString.Append(strValue + ",");
//                    }
//                    else
//                    {
//                        jsonString.Append(strValue);
//                    }
//                }
//                jsonString.Append("},");
//            }
//            jsonString.Remove(jsonString.Length - 1 , 1);
//            jsonString.Append("]");
//            if (jsonString.Length == 1)
//            {
//                return "[]";
//            }
//            return jsonString.ToString();
//        }

//        /// <summary>
//        /// DataTable转成Json 
//        /// </summary>
//        /// <param name="jsonName"></param>
//        /// <param name="dt"></param>
//        /// <returns></returns>
//        public static string ToJson(DataTable dt , string jsonName)
//        {
//            StringBuilder Json = new StringBuilder();
//            if (string.IsNullOrEmpty(jsonName))
//                jsonName = dt.TableName;
//            Json.Append("{\"" + jsonName + "\":[");
//            if (dt.Rows.Count > 0)
//            {
//                for (int i = 0; i < dt.Rows.Count; i++)
//                {
//                    Json.Append("{");
//                    for (int j = 0; j < dt.Columns.Count; j++)
//                    {
//                        Type type = dt.Rows[i][j].GetType();
//                        Json.Append("\"" + dt.Columns[j].ColumnName.ToString() + "\":" + StringFormat(dt.Rows[i][j] is DBNull ? string.Empty : dt.Rows[i][j].ToString() , type));
//                        if (j < dt.Columns.Count - 1)
//                        {
//                            Json.Append(",");
//                        }
//                    }
//                    Json.Append("}");
//                    if (i < dt.Rows.Count - 1)
//                    {
//                        Json.Append(",");
//                    }
//                }
//            }
//            Json.Append("]}");
//            return Json.ToString();
//        }

//        /// <summary> 
//        /// DataReader转换为Json 
//        /// </summary> 
//        /// <param name="dataReader">DataReader对象</param> 
//        /// <returns>Json字符串</returns> 
//        public static string ToJson(IDataReader dataReader)
//        {
//            try
//            {
//                StringBuilder jsonString = new StringBuilder();
//                jsonString.Append("[");

//                while (dataReader.Read())
//                {
//                    jsonString.Append("{");
//                    for (int i = 0; i < dataReader.FieldCount; i++)
//                    {
//                        Type type = dataReader.GetFieldType(i);
//                        string strKey = dataReader.GetName(i);
//                        string strValue = dataReader[i].ToString();
//                        jsonString.Append("\"" + strKey + "\":");
//                        strValue = StringFormat(strValue , type);
//                        if (i < dataReader.FieldCount - 1)
//                        {
//                            jsonString.Append(strValue + ",");
//                        }
//                        else
//                        {
//                            jsonString.Append(strValue);
//                        }
//                    }
//                    jsonString.Append("},");
//                }
//                if (!dataReader.IsClosed)
//                {
//                    dataReader.Close();
//                }
//                jsonString.Remove(jsonString.Length - 1 , 1);
//                jsonString.Append("]");
//                if (jsonString.Length == 1)
//                {
//                    return "[]";
//                }
//                return jsonString.ToString();
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }

//        /// <summary> 
//        /// DataSet转换为Json 
//        /// </summary> 
//        /// <param name="dataSet">DataSet对象</param> 
//        /// <returns>Json字符串</returns> 
//        public static string ToJson(DataSet dataSet)
//        {
//            string jsonString = "{";
//            foreach (DataTable table in dataSet.Tables)
//            {
//                jsonString += "\"" + table.TableName + "\":" + ToJson(table) + ",";
//            }
//            jsonString = jsonString.TrimEnd(',');
//            return jsonString + "}";
//        }

//        /// <summary>
//        /// 过滤特殊字符
//        /// </summary>
//        /// <param name="s"></param>
//        /// <returns></returns>
//        public static string String2Json(String s)
//        {
//            StringBuilder sb = new StringBuilder();
//            for (int i = 0; i < s.Length; i++)
//            {
//                char c = s.ToCharArray()[i];
//                switch (c)
//                {
//                    case '\"':
//                        sb.Append("\\\"");
//                        break;
//                    case '\\':
//                        sb.Append("\\\\");
//                        break;
//                    case '/':
//                        sb.Append("\\/");
//                        break;
//                    case '\b':
//                        sb.Append("\\b");
//                        break;
//                    case '\f':
//                        sb.Append("\\f");
//                        break;
//                    case '\n':
//                        sb.Append("\\n");
//                        break;
//                    case '\r':
//                        sb.Append("\\r");
//                        break;
//                    case '\t':
//                        sb.Append("\\t");
//                        break;
//                    case '\v':
//                        sb.Append("\\v");
//                        break;
//                    case '\0':
//                        sb.Append("\\0");
//                        break;
//                    default:
//                        sb.Append(c);
//                        break;
//                }
//            }
//            return sb.ToString();
//        }

//        /// <summary>
//        /// 格式化字符型、日期型、布尔型
//        /// </summary>
//        /// <param name="str"></param>
//        /// <param name="type"></param>
//        /// <returns></returns>
//        private static string StringFormat(string str , Type type)
//        {
//            if (type != typeof(string) && string.IsNullOrEmpty(str))
//            {
//                str = "\"" + str + "\"";
//            }
//            else if (type == typeof(string))
//            {
//                str = String2Json(str);
//                str = "\"" + str + "\"";
//            }
//            else if (type == typeof(DateTime))
//            {
//                str = "\"" + str + "\"";
//            }
//            else if (type == typeof(bool))
//            {
//                str = str.ToLower();
//            }
//            else if (type == typeof(byte[]))
//            {
//                str = "\"" + str + "\"";
//            }
//            else if (type == typeof(Guid))
//            {
//                str = "\"" + str + "\"";
//            }
//            return str;
//        }
//    }
//}