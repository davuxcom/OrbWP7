 using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using RestSharp;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace OrbAPI
{
    public class Orb
    {
        private const string APIKey = "3orf1r01syqih";

        private string UserName, Password;

        public string SessionID { get; private set; }

        public string LoginName { get; private set; }

        public Orb(string UserName, string Password, Action SuccessfulLogin, Action<string> LoginError)
        {
            this.UserName = UserName;
            this.Password = Password;

            CallXmlMethod("session.login",
                new Dictionary<string, string>() 
                { 
                    // TODO: add speed  
                    { "l", UserName }, 
                    { "password", Password },
                    { "width", "800" },
                    { "height", "480" },
                },
                doc =>
                {
                    SessionID = (from el in doc.Descendants("orbSessionId")
                                        select el).First().Value;
                    Debug.WriteLine("SessionID: " + SessionID);

                    SuccessfulLogin();
                }, LoginError);
        }

        public void Search(string mediaType, string filter, Action<SearchResult> Callback, Action<string> ErrorCallback)
        {
            var dict = new Dictionary<string, string>() 
                { 
                    { "fields", "title,orbMediumId,size,thumbnailId" },
                    { "sortBy", "title" },
                    { "groupBy", "virtualPath" }, 
                };

            if (!string.IsNullOrEmpty(filter))
            {
                dict.Add("filter", filter);
            }
            else
            {
                dict.Add( "q", "mediaType=" + mediaType);
            }

            CallXmlMethod("media.search",
                dict,
                doc =>
                {
                    Debug.WriteLine("search: " + doc);
                    Callback(new SearchResult(doc,mediaType));
                }, 
                ErrorCallback);
        }

        private void CallXmlMethod(string method, Dictionary<string, string> keys, Action<XDocument> Callback, Action<string> ErrorCallback)
        {
            CallWebMethod("https://api.orb.com/orb/xml/", method, keys, (rgContent, strContent) =>
                {
                    try
                    {
                        PreParse(XDocument.Load(new StringReader(strContent)), Callback, ErrorCallback);
                    }
                    catch (Exception ex)
                    {
                        string error = string.Format("Orb Parse Exception: {0}", ex);
                        Debug.WriteLine(error);
                        ErrorCallback(error);
                    }
                },
                ErrorCallback);
        }

        private void CallWebMethod(string url, string method, Dictionary<string, string> keys, Action<byte[], string> Callback, Action<string> ErrorCallback)
        {
            Debug.WriteLine(string.Format("Calling Orb Method {0}", method));

            var request = new RestRequest(method, Method.GET);
            request.AddParameter("apiKey", APIKey, ParameterType.GetOrPost);
            if (SessionID != null)
            {
                request.AddParameter("sid", SessionID, ParameterType.GetOrPost);
            }
            foreach (var pair in keys)
            {
                request.AddParameter(pair.Key, pair.Value, ParameterType.GetOrPost);
            }

            new RestClient(url).ExecuteAsync(request, (response) =>
            {
                Debug.WriteLine(string.Format("Orb Call ({0}): {1}", method, response.Content));
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Callback(response.RawBytes, response.Content);
                }
                else
                {
                    string error = string.Format("Orb HTTP Error: {0} {1}", response.StatusCode, response.StatusDescription);
                    Debug.WriteLine(error);
                    ErrorCallback(error);
                }
            });
        }

        private void CallDataMethod(string method, Dictionary<string, string> keys, Action<byte[]> Callback, Action<string> ErrorCallback)
        {
            CallWebMethod("https://api.orb.com/orb/data/", method, keys, (rgContent, strContent) =>
            {
                try
                {
                    Callback(rgContent);
                }
                catch (Exception ex)
                {
                    string error = string.Format("Orb Parse Exception: {0}", ex);
                    Debug.WriteLine(error);
                    ErrorCallback(error);
                }
            }, ErrorCallback);
        }

        private void CallDataMethod(string method, Dictionary<string, string> keys, Action<string> Callback, Action<string> ErrorCallback)
        {
            CallWebMethod("https://api.orb.com/orb/data/", method, keys, (rgContent, strContent) =>
            {
                try
                {
                    Callback(strContent);
                }
                catch (Exception ex)
                {
                    string error = string.Format("Orb Parse Exception: {0}", ex);
                    Debug.WriteLine(error);
                    ErrorCallback(error);
                }
            }, ErrorCallback);
        }

        public void DownloadThumbnail(string item, Action<byte[]> Callback, Action<string> ErrorCallback)
        {
            CallDataMethod("image.png", new Dictionary<string, string>() 
                { 
                    { "mediumId", item },
                    { "maxWidth", "96" },
                    { "maxHeight", "96" }, 
                    { "allowEnlarge", "1" }, 
                    { "forceSize", "1" }, 
                }, 
                Callback, ErrorCallback);
        }

        public void DownloadImage(string item, Action<byte[]> Callback, Action<string> ErrorCallback)
        {
            CallDataMethod("image.jpg", new Dictionary<string, string>() 
                { 
                    { "mediumId", item },
                },
                Callback, ErrorCallback);
        }

        public void GetStream(string item, int speed, Action<string> asxContent, Action<string> ErrorCallback)
        {
            CallDataMethod("stream.asx", new Dictionary<string, string>() 
                { 
                    { "mediumId", item },
                    { "speed", speed.ToString() },
                },
                asxContent, ErrorCallback);
        }

        private void PreParse(XDocument doc, Action<XDocument> SuccessCallback, Action<string> ErrorCallback)
        {
            var orb = (from el in doc.Descendants("status") select el).First();
            
            var error = int.Parse(orb.Attribute("code").Value);
            
            Debug.WriteLine("Error: " + error);

            if (error == 0)
            {
                var login = doc.Root.Attribute("login").Value;
                Debug.WriteLine("Login: " + login);
                SuccessCallback(doc);
            }
            else
            {
                var errorDesc = string.Format("Orb Error: {0}", orb.Attribute("desc").Value);
                Debug.WriteLine(errorDesc);
                ErrorCallback(errorDesc);
            }
        }
    }
}
