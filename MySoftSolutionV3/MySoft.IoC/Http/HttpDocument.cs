﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace MySoft.IoC.Http
{
    /// <summary>
    /// API文档类
    /// </summary>
    internal class HttpDocument
    {
        private IDictionary<string, HttpCallerInfo> callers;
        private int port;
        public HttpDocument(IDictionary<string, HttpCallerInfo> callers, int port)
        {
            this.callers = callers;
            this.port = port;
        }

        /// <summary>
        /// 生成文档
        /// </summary>
        /// <returns></returns>
        public string MakeDocument(string name)
        {
            #region 读取资源

            Assembly assm = this.GetType().Assembly;
            Stream helpStream = assm.GetManifestResourceStream("MySoft.IoC.Http.Template.help.htm");
            Stream helpitemStream = assm.GetManifestResourceStream("MySoft.IoC.Http.Template.helpitem.htm");

            StreamReader helpReader = new StreamReader(helpStream);
            StreamReader helpitemReader = new StreamReader(helpitemStream);

            string html = helpReader.ReadToEnd(); helpReader.Close();
            string item = helpitemReader.ReadToEnd(); helpitemReader.Close();

            #endregion

            string uri = string.Format("http://{0}:{1}/", DnsHelper.GetIPAddress(), port);
            html = html.Replace("${uri}", uri);

            StringBuilder sbUrl = new StringBuilder();
            foreach (var kv in callers)
            {
                sbUrl.Append(GetItemDocument(kv, item, name));
            }

            html = html.Replace("${body}", sbUrl.ToString());
            return html;
        }

        /// <summary>
        /// 获取Item文档
        /// </summary>
        /// <param name="kv"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private string GetItemDocument(KeyValuePair<string, HttpCallerInfo> kv, string item, string name)
        {
            var template = item;
            var plist = new List<string>();
            foreach (var p in kv.Value.Method.GetParameters())
            {
                plist.Add(string.Format("{0}=[{0}]", p.Name.ToLower()).Replace('[', '{').Replace(']', '}'));
            }

            string uri = string.Empty;
            if (kv.Value.HttpMethod == HttpMethod.GET && plist.Count > 0)
                uri = string.Format("/{0}?{1}", kv.Key, string.Join("&", plist.ToArray()));
            else
                uri = string.Format("/{0}", kv.Key);

            var url = string.Format("<a rel=\"operation\" target=\"_blank\" title=\"{0}\" href=\"{0}\">{0}</a> 处的服务", uri);

            var serviceInfo = string.Format("【{0}】\r\n{1}", kv.Value.ServiceName, kv.Value.Method.ToString());
            template = template.Replace("${method}", string.Format("<p title=\"分布式服务接口:\r\n{2}\"><b><a href='/help/{0}'>{0}</a></b><br/>{1}</p>",
                kv.Key, kv.Value.Description, serviceInfo));

            var strParameter = GetMethodParameter(kv.Value.Method, name);
            if (string.IsNullOrEmpty(strParameter))
                template = template.Replace("${parameter}", "&nbsp;");
            else
                template = template.Replace("${parameter}", strParameter);

            template = template.Replace("${type}", kv.Value.HttpMethod == HttpMethod.GET ? "GET<br/>POST" : "<font color='red'>POST</font>");
            template = template.Replace("${uri}", url.ToString());

            return template;
        }

        private string GetMethodParameter(MethodInfo method, string name)
        {
            StringBuilder buider = new StringBuilder();
            List<string> plist = new List<string>();

            var parameters = method.GetParameters();
            var parametersCount = parameters.Count();
            if (parametersCount > 0) buider.Append("<b>INPUT</b> -><br/>");
            foreach (var p in parameters)
            {
                if (!string.IsNullOrEmpty(name))
                    buider.Append(GetTypeDetail(p.Name, p.ParameterType, 1));
                else
                    buider.AppendFormat(string.Format("&nbsp;&nbsp;&nbsp;&nbsp;&lt;{0} : {1}&gt;", p.Name, GetTypeName(p.ParameterType)) + "<br/>");
            }
            if (parametersCount > 0) buider.Append("<hr/>");
            var value = string.Format("<b>OUTPUT</b> -> {0}<br/>", GetTypeName(method.ReturnType));
            buider.Append("<font color=\"#336699\">").Append(value);
            if (!string.IsNullOrEmpty(name)) buider.Append(GetTypeDetail(null, method.ReturnType, 1));
            buider.Append("</font>");

            return buider.ToString();
        }

        #region 处理参数

        private bool GetTypeClass(Type type)
        {
            if (type.IsGenericType)
                return GetTypeClass(type.GetGenericArguments()[0]);
            else
                return (type.IsClass && type != typeof(string)) || type.IsEnum;
        }

        private string GetTypeName(Type type)
        {
            string typeName = type.Name;
            if (type.IsGenericType) type = type.GetGenericArguments()[0];
            if (typeName.Contains("`1"))
            {
                typeName = typeName.Replace("`1", "&lt;" + type.Name + "&gt;");
            }
            return typeName;
        }

        private string GetTypeDetail(string name, Type type, int index)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(name))
            {
                for (int i = 0; i < index; i++) sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                sb.AppendFormat(string.Format("&lt;{0} : {1}&gt;", name, GetTypeName(type)) + "<br/>");
            }

            type = CoreHelper.GetPrimitiveType(type);
            if (GetTypeClass(type))
            {
                if (type.IsEnum)
                {
                    var names = Enum.GetNames(type);
                    var values = Enum.GetValues(type);

                    for (int i = 0; i < index; i++) sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                    sb.Append("<b style='color:#999;'>[" + GetTypeName(type) + "]</b><br/>");
                    for (int n = 0; n < names.Length; n++)
                    {
                        for (int i = 0; i <= index; i++) sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                        sb.AppendFormat(string.Format("&lt;{0} : {1}&gt;", names[n], Convert.ToInt32(values.GetValue(n))) + "<br/>");
                    }
                }
                else
                {
                    for (int i = 0; i < index; i++) sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                    sb.Append("<b style='color:#999;'>[" + GetTypeName(type) + "]</b><br/>");

                    foreach (var p in CoreHelper.GetPropertiesFromType(type))
                    {
                        if (GetTypeClass(p.PropertyType) && type != p.PropertyType)
                        {
                            sb.Append(GetTypeDetail(p.Name, p.PropertyType, index + 1));
                        }
                        else
                        {
                            for (int i = 0; i <= index; i++) sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                            sb.AppendFormat(string.Format("&lt;{0} : {1}&gt;", p.Name, GetTypeName(p.PropertyType)) + "<br/>");
                        }
                    }

                    foreach (var p in type.GetFields())
                    {
                        if (GetTypeClass(p.FieldType))
                        {
                            sb.Append(GetTypeDetail(p.Name, p.FieldType, index + 1));
                        }
                        else
                        {
                            for (int i = 0; i <= index; i++) sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                            sb.AppendFormat(string.Format("&lt;{0} : {1}&gt;", p.Name, GetTypeName(p.FieldType)) + "<br/>");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}