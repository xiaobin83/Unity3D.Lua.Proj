using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using UnityEngine;

namespace utils 
{
	public class WebRequest2
	{
		public delegate void WebRequestRespondedCallback(WebExceptionStatus s, HttpStatusCode code, string payload, CookieCollection cookies, WebHeaderCollection headers, Context context);

		public class Context
		{
			public float timeOutTime = 60;
			public bool responseAsBinary = false;

			public System.Uri srv;
			public string function;
			public Method method;
			public Dictionary<string, object> parameters;
			public string parametersStr;
			public WebRequestRespondedCallback callback;
			public WebHeaderCollection headers;
			public Context parent;

			public Context Clone(System.Uri srv, string function, Method method, Dictionary<string, object> parameters,string parametersStr, WebRequestRespondedCallback callback)
			{
				var cloned = MemberwiseClone() as Context;
				cloned.srv = srv;
				cloned.function = function;
				cloned.method = method;
				cloned.parameters = parameters;
				cloned.parametersStr = parametersStr;
				cloned.callback = callback;
				cloned.parent = this;
				return cloned;
			}
		}
		static Context defaultContext = new Context();

		public enum Method
		{
			GET,
			POST,
		}


		static bool acceptAnyCert_ = false;
		public static bool SetupServerCertificateValidationCallback(bool acceptAnyCert = false)
		{
#if WEBREQUEST_USE_SELF_CERT
			Debug.LogWarning("Always accept any certification.")
			ServicePointManager.ServerCertificateValidationCallback	= (sender, certificate,	chain, sslPolicyErrors)	=> true;
			return true;
#else
			var old = acceptAnyCert_;
			acceptAnyCert_ = acceptAnyCert;
			if (acceptAnyCert_)
			{
				ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
			}
			else
			{
				ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallback;
			}
			return old;
#endif
		}

		// all client side error will be reported to federation.Error
		public static bool IsClientSideError(HttpStatusCode code)
		{
			return (int)code >= 400 && (int)code < 500;
		}

		public static void MakeRequestTo(
			System.Uri srv,	
			string function,
			Method method,
			Dictionary<string, object> parameter,
            WebRequestRespondedCallback callback,
			Context	context	= null,
            string parametersStr = "")
		{
			if (context == null) context = defaultContext;
			context = context.Clone(srv, function, method, parameter, parametersStr, callback);

			if (srv != null)
			{
				TaskManager.StartUnbreakableCoroutine(MakeRequestTo_(context));
			}
			else
			{
				if (callback != null)
					callback(WebExceptionStatus.ConnectFailure, HttpStatusCode.Unused, null, null, null, context);
			}
		}

		class GetResponse : UnityEngine.CustomYieldInstruction
		{
			System.DateTime expireTime;
			HttpWebRequest req;
			HttpWebResponse resp;
			System.IO.MemoryStream outputStream = new System.IO.MemoryStream();
			Context context;
			string payload_;
			public string payload
			{
				get
				{
					if (payload_ == null)
					{
						if (outputStream != null)
						{
							outputStream.Seek(0, System.IO.SeekOrigin.Begin);
							if (context.responseAsBinary)
							{
								try
								{
									var bytes = new System.IO.BinaryReader(outputStream).ReadBytes((int)outputStream.Length);
									var str = Convert.ToBase64String(bytes);
									Debug.LogFormat("RAW RESPONSE: binary data received {0} bytes", bytes.Length);
									payload_ = str;
								}
								catch (Exception e)
								{
									Debug.Log("Response as binary failed: " + e.Message);
								}
							}
							else
							{
								var str = new System.IO.StreamReader(outputStream).ReadToEnd();
								Debug.LogFormat("RAW RESPONSE: {0}", str);
								if (!string.IsNullOrEmpty(str))
								{
									payload_ = str;
								}
							}
						}
						outputStream = null;
					}
					return payload_ != null ? payload_ : string.Empty;
				}
			}

			byte[] temp = new byte[512]; // managed somewhere
			byte[] postData = null;
			List<Dictionary<string, byte[]>> attachement = null;

			public GetResponse(HttpWebRequest req, byte[] postData, Context context, List<Dictionary<string, byte[]>> attachement = null)
			{
				this.requestingUri = req.RequestUri.ToString();
				this.expireTime = System.DateTime.Now.AddSeconds(context.timeOutTime);
				this.req = req;
				this.postData = postData;
				this.attachement = attachement;
				this.context = context;
				if (this.postData != null)
				{
					try
					{
						req.BeginGetRequestStream(RequestWriteCallback, this);
					}
					catch (Exception e)
					{
						SetRespondedWithClientError(e);
					}
				}
				else
				{
					DoGetResponse();
				}
			}

			public void Abort()
			{
				try
				{
					if (resp != null)
						resp.Close();
					resp = null;
					if (req != null)
						req.Abort();
					req = null;
				}
				catch { }
			}

			void DoGetResponse()
			{
				try
				{
					req.BeginGetResponse(ResponseCallback, this);
				}
				catch (Exception e)
				{
					SetRespondedWithClientError(e);
				}
			}

			static void RequestWriteCallback(IAsyncResult ar)
			{
				var This = (GetResponse)ar.AsyncState;
				Debug.Assert(This.postData != null);
				try
				{
					Debug.Log("Writing post data on req " + This.req.RequestUri.ToString());
					using (var stream = This.req.EndGetRequestStream(ar))
					{
						if (This.attachement != null)
						{
							// send	attachements with postData

							string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
							This.req.ContentType = "multipart/form-data; boundary=" + boundary;

							byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
							var endBoundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--");

							// write postData
							stream.Write(boundarybytes, 0, boundarybytes.Length);
							var postDataHeader = "Content-Type: application/x-www-form-urlencoded\r\n\r\n";
							var postDataHeaderBytes = System.Text.Encoding.ASCII.GetBytes(postDataHeader);
							stream.Write(This.postData, 0, This.postData.Length);

							// write files
							const string attachmentHeaderTemplate =
								"Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
								"Content-Type: application/octet-stream\r\n\r\n";
							foreach (var fileList in This.attachement)
							{
								foreach (var file in fileList)
								{
									stream.Write(boundarybytes, 0, boundarybytes.Length);

									string header = string.Format(attachmentHeaderTemplate, "attachment", file.Key);
									var headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
									stream.Write(headerBytes, 0, headerBytes.Length);

									stream.Write(file.Value, 0, file.Value.Length);
								}
							}

							stream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
							stream.Close();

						}
						else
						{
							This.req.ContentType = "application/x-www-form-urlencoded";
							stream.Write(This.postData, 0, This.postData.Length);
							stream.Close();
						}
						This.attachement = null;
						This.postData = null;
						This.DoGetResponse();
					}
				}
				catch (Exception e)
				{
					This.SetRespondedWithClientError(e);
				}
			}

			static void ResponseCallback(IAsyncResult ar)
			{
				var This = (GetResponse)ar.AsyncState;
				try
				{
					This.resp = This.req.EndGetResponse(ar) as HttpWebResponse;
					var stream = This.resp.GetResponseStream();
					stream.BeginRead(This.temp, 0, This.temp.Length, StreamReadCallback, This);
				}
				catch (WebException e)
				{
					if (e.Response != null) // has error response
					{
						This.webExceptionStatus = e.Status;
						// do not set This.error here, because e.Response will give	the	detail.
						// and This.error also used for check if a client error happens (to save various test)

						// replace resp with error resp
						This.resp = e.Response as HttpWebResponse;
						// read	exception returned content
						var stream = This.resp.GetResponseStream();
						stream.BeginRead(This.temp, 0, This.temp.Length, StreamReadCallback, This);
					}
					else
					{
						This.SetRespondedWithClientError(e);
					}
				}
				catch (System.Exception e)
				{
					This.SetRespondedWithClientError(e);
				}
			}
			
			static void StreamReadCallback(IAsyncResult ar)
			{
				var This = (GetResponse)ar.AsyncState;
				System.IO.Stream stream = null;
				try
				{
					stream = This.resp.GetResponseStream();
					var szRead = stream.EndRead(ar);
					if (szRead > 0)
					{
						This.outputStream.Write(This.temp, 0, szRead);
						stream.BeginRead(This.temp, 0, This.temp.Length, StreamReadCallback, This);
					}
					else
					{
						This.SetResponded(This.resp.StatusCode, This.resp.StatusDescription, This.resp.Cookies, This.resp.Headers);
						stream.Close();
						stream.Dispose();
						stream = null;
						This.resp.Close();
						This.resp = null;
					}
				}
				catch (System.Exception e)
				{
					if (stream != null)
					{
						stream.Close();
						stream.Dispose();
						stream = null;
					}
					This.SetRespondedWithClientError(e);
				}
			}

			public WebExceptionStatus webExceptionStatus = WebExceptionStatus.Success;
			public HttpStatusCode statusCode;
			public string statusDescription;
			public CookieCollection cookies;
			public WebHeaderCollection headers;

			public bool responded = false;
			public string error
			{
				get; private set;
			}

			string requestingUri;

			void SetResponded(HttpStatusCode code, string description, CookieCollection cookies, WebHeaderCollection headers)
			{
				this.statusCode = code;
				this.statusDescription = description;
				this.cookies = cookies;
				this.headers = headers;
				this.responded = true;
				error = null;
			}

			void SetRespondedWithClientError(System.Exception e)
			{
				Abort();
				if (e is WebException)
				{
					var ee = (WebException)e;
					webExceptionStatus = ee.Status;
				}
				responded = true;
				error = "req " + (string.IsNullOrEmpty(requestingUri)?"null":requestingUri) + " error, " + e.GetType().ToString() + ": " + e.Message;
			}

			public override bool keepWaiting
			{
				get
				{
					if (!responded)
					{
						if (System.DateTime.Now > expireTime)
						{
							SetRespondedWithClientError(new System.Net.WebException("Client detected timeout", System.Net.WebExceptionStatus.Timeout));
						}
					}
					return !responded;
				}
			}
		}

		static void LogErrorPayload(Uri url, Method method, Dictionary<string, object> parameter, GetResponse resp)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append("WebRequest failed: ");
			sb.Append(resp.statusDescription);
			sb.AppendLine();
			sb.Append(method);
			sb.Append(" ");
			sb.Append(url);
			sb.AppendLine();
			sb.Append("Request payload:");
			sb.AppendLine();
			if (parameter != null)
			{
				foreach (var kv in parameter)
				{
					sb.Append("  ");
					sb.Append(kv.Key);
					sb.Append("=");
					sb.Append(kv.Value);
					sb.AppendLine();
				}
			}
			sb.Append("Responding payload:");
			sb.AppendLine();
			if (resp.payload != null)
				sb.Append(resp.payload.ToString());
			Debug.LogError(sb.ToString());
		}

		static bool ValueIsFiles(object value)
		{
			return value is Dictionary<string, byte[]>;
		}

		public static System.Uri BuildGetQuery(System.Uri url, Dictionary<string, object> parameters)
		{
			if (parameters != null && parameters.Count > 0)
			{
				var sb = new StringBuilder();
				foreach (var kv in parameters)
				{
					if (ValueIsFiles(kv.Value))
					{
						Debug.LogWarningFormat("ignore attachement {1} in GET request", kv.Key);
						continue;
					}
					sb.Append(kv.Key);
					sb.Append("=");
					sb.Append(kv.Value.ToString());
					sb.Append("&");
				}
				sb.Length = sb.Length - 1;
				var ub = new System.UriBuilder(url);
				ub.Query = sb.ToString();
				url = ub.Uri;
			}
			return url;
		}

		static HashSet<GetResponse> processingRequests = new HashSet<GetResponse>();
		public static void AbortAllProcessingRequests()
		{
			foreach (var r in processingRequests)
			{
				r.Abort();
			}
			processingRequests.Clear();
        }

		static IEnumerator MakeRequestTo_(Context context)
		{
			Debug.Assert(context.srv != null);
			var url = context.srv;
			if (!string.IsNullOrEmpty(context.function))
			{
				url = new System.Uri(context.srv, context.function);
			}

			HttpWebRequest req = null;

			var payloadRecord = new System.Text.StringBuilder();

			byte[] postData = null;
			List<Dictionary<string, byte[]>> attachments = null;

			if (context.method == Method.GET)
			{
				url = BuildGetQuery(url, context.parameters);
                req = System.Net.WebRequest.Create(url.AbsoluteUri) as HttpWebRequest;
				req.CookieContainer = new CookieContainer();
				req.Method = "GET";
				req.KeepAlive = false;
			}
			else if (context.method == Method.POST)
			{
				req = System.Net.WebRequest.Create(url.ToString()) as HttpWebRequest;
				req.Method = "POST";
				req.KeepAlive = false;

				req.CookieContainer = new CookieContainer();
				if (context.parameters != null && context.parameters.Count > 0)
				{
					var sb = new System.Text.StringBuilder();
					var first = true;
					foreach (var kv in context.parameters)
					{
						if (ValueIsFiles(kv.Value))
						{
							// construct mime here
							if (attachments == null)
							{
								attachments = new List<Dictionary<string, byte[]>>();
							}
							attachments.Add((Dictionary<string, byte[]>)kv.Value);
						}
						else
						{
							if (!first) sb.Append("&");
							sb.Append(kv.Key); sb.Append("="); sb.Append(kv.Value);
							first = false;
						}
					}
					postData = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
				}
				else if(!string.IsNullOrEmpty(context.parametersStr))
                {
                    postData = System.Text.Encoding.UTF8.GetBytes(context.parametersStr);
				}
			}


			Debug.Log("Start Request: " + req.RequestUri.ToString());

			if (context.headers != null)
			{
				req.Headers = context.headers;
			}

			var	resp = new GetResponse(req, postData, context, attachments);
			processingRequests.Add(resp);

			yield return resp;

			processingRequests.Remove(resp);

			Debug.Log("Request Responded: " + req.RequestUri.ToString());

			if (!string.IsNullOrEmpty(resp.error))
			{
				Debug.LogError(resp.error);
				if (context.callback != null)
					context.callback(resp.webExceptionStatus, HttpStatusCode.Unused, null, null, null, context);
			}
			else if (IsClientSideError(resp.statusCode))
			{
				LogErrorPayload(url, context.method, context.parameters, resp);
				if (context.callback != null)
					context.callback(resp.webExceptionStatus, resp.statusCode, resp.payload, resp.cookies, resp.headers, context);
			}
			else if (resp.statusCode != HttpStatusCode.OK)
			{
				LogErrorPayload(url, context.method, context.parameters, resp);
				if (context.callback != null)
					context.callback(resp.webExceptionStatus, resp.statusCode, resp.payload, resp.cookies, resp.headers, context);
			}
			else // used by Eve, Auth, 
			{
				if (context.callback != null)
					context.callback(resp.webExceptionStatus, resp.statusCode, resp.payload, resp.cookies, resp.headers, context);
			}
		}

		public static void Get(System.Uri srv, string function, Dictionary<string, object> parameter, WebRequestRespondedCallback callback, Context context = null)
		{
			MakeRequestTo(srv, function, Method.GET, parameter, callback, context: context);
		}

		public static void Download(string url, System.Action<byte[]> complete)
		{
			try
			{
				Get(new System.Uri(url), null, null,
					(s,	code, payload, _1, _2, _3) =>
					{
						if (s == WebExceptionStatus.Success
						&& code == HttpStatusCode.OK)
						{
							var base64data = payload;
							var data = System.Convert.FromBase64String(base64data);
							complete(data);
						}
						else
						{
							complete(null);
						}
					}, 
					new	Context() {	responseAsBinary = true	});
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Download exception caught {0} {1}", e.Message, e.StackTrace);
				complete(null);
			}
		}

		public static void Post(System.Uri srv, string function, Dictionary<string, object> parameter, WebRequestRespondedCallback callback, Context context = null,string parametersStr = "")
		{
			MakeRequestTo(srv, function, Method.POST, parameter, callback, context: context,parametersStr:parametersStr);
		}

		public static void GetWithAuth(System.Uri srv, string function, Dictionary<string, object> parameter, WebRequestRespondedCallback callback, Context context = null)
		{
			MakeRequestTo(srv, function, Method.GET, parameter, callback, context);
		}

		public static void PostWithAuth(System.Uri srv, string function, Dictionary<string, object> parameter, WebRequestRespondedCallback callback, Context context = null)
		{
			MakeRequestTo(srv, function, Method.POST, parameter, callback, context);
		}

		internal static bool DefaultRetryCondition(WebExceptionStatus status, HttpStatusCode code)
		{
			return status != WebExceptionStatus.Success || code == HttpStatusCode.InternalServerError;
		}

		public static WebRequestRespondedCallback RetryIfFailed(
			WebRequestRespondedCallback callback, 
			int	tries = 3,
			System.Func<WebExceptionStatus, HttpStatusCode, bool> cond = null, 
			float nextRetryAfter = 0, float retryDurationIncr = 5)
		{
			if (cond == null) cond = DefaultRetryCondition;
			return (status, code, _1, _2, _3, context) =>
			{
				if (status == WebExceptionStatus.Success && code == HttpStatusCode.OK)
				{
					callback(status, code, _1, _2, _3, context);
				}
				else
				{
					tries = tries - 1;
					if (tries > 0 && cond(status, code))
					{
						float delay = nextRetryAfter;
						nextRetryAfter += retryDurationIncr;
						Debug.LogWarningFormat("Retry will start in {0} seconds ...", delay);
						TaskManager.DelayExecute(
							delay,
							() => MakeRequestTo(context.srv, context.function, context.method, context.parameters, context.callback, context));
					}
					else
					{
						callback(status, code, _1, _2, _3, context);
					}
				}
			};
		}


		static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			bool isOk = true;
			// If there are errors in the certificate chain, look at each error to determine the cause.
			if (sslPolicyErrors != SslPolicyErrors.None)
			{
				for (int i = 0; i < chain.ChainStatus.Length; i++)
				{
					if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
					{
						chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
						chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
						chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
						chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
						bool chainIsValid = chain.Build((X509Certificate2)certificate);
						if (!chainIsValid)
						{
							isOk = false;
						}
					}
				}
			}
			return isOk;
		}


        private static string DetermineValidPathCharacters()
        {
            const string basePathCharacters = "/:'()!*[]";

            var sb = new StringBuilder();
            foreach (var c in basePathCharacters)
            {
                var escaped = Uri.EscapeUriString(c.ToString());
                if (escaped.Length == 1 && escaped[0] == c)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string UrlEncode(string data)
        {
            StringBuilder encoded = new StringBuilder(data.Length * 2);
            string validUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

            string unreservedChars = String.Concat(validUrlCharacters, DetermineValidPathCharacters());

            foreach (char symbol in System.Text.Encoding.UTF8.GetBytes(data))
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                {
                    encoded.Append(symbol);
                }
                else
                {
                    encoded.Append("%").Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:X2}", (int)symbol));
                }
            }

            return encoded.ToString();
        }
	}
}