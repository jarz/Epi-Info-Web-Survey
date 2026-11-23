using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Epi.Web.MVC.Test.Fakes
{
    public sealed class FakeBrowserCapabilities : HttpBrowserCapabilitiesBase
    {
        private readonly bool _isMobileDevice;

        public FakeBrowserCapabilities(bool isMobileDevice)
        {
            _isMobileDevice = isMobileDevice;
        }

        public override bool IsMobileDevice { get { return _isMobileDevice; } }
    }

    public sealed class FakeSession : HttpSessionStateBase
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public override object this[string name]
        {
            get { object value; return _values.TryGetValue(name, out value) ? value : null; }
            set { _values[name] = value; }
        }

        public override void Remove(string name) { _values.Remove(name); }
        public override int Count { get { return _values.Count; } }
    }

    public sealed class FakeRequest : HttpRequestBase
    {
        private readonly Uri _url;
        private readonly string _userAgent;
        private readonly HttpBrowserCapabilitiesBase _browser;
        private readonly HttpCookieCollection _cookies = new HttpCookieCollection();
        private readonly NameValueCollection _headers = new NameValueCollection();
        private readonly NameValueCollection _queryString = new NameValueCollection();
        private readonly NameValueCollection _form = new NameValueCollection();

        public FakeRequest(string url, string userAgent, bool isMobileDevice)
        {
            _url = new Uri(url);
            _userAgent = userAgent;
            _browser = new FakeBrowserCapabilities(isMobileDevice);
        }

        public override Uri Url { get { return _url; } }
        public override string UserAgent { get { return _userAgent; } }
        public override HttpBrowserCapabilitiesBase Browser { get { return _browser; } }
        public override string FilePath { get { return _url.AbsolutePath; } }
        public override string Path { get { return _url.AbsolutePath; } }
        public override string AppRelativeCurrentExecutionFilePath { get { return "~" + _url.AbsolutePath; } }
        public override string PathInfo { get { return string.Empty; } }
        public override string ApplicationPath { get { return "/"; } }
        public override string HttpMethod { get { return "GET"; } }
        public override Uri UrlReferrer { get { return null; } }
        public override HttpCookieCollection Cookies { get { return _cookies; } }
        public override NameValueCollection Headers { get { return _headers; } }
        public override NameValueCollection QueryString { get { return _queryString; } }
        public override NameValueCollection Form { get { return _form; } }
    }

    public sealed class FakeResponse : HttpResponseBase
    {
        private readonly HttpCookieCollection _cookies = new HttpCookieCollection();

        public override HttpCookieCollection Cookies { get { return _cookies; } }
        public override string ApplyAppPathModifier(string virtualPath) { return virtualPath; }
    }

    public sealed class FakeHttpContext : HttpContextBase
    {
        private readonly FakeRequest _request;
        private readonly FakeResponse _response = new FakeResponse();
        private readonly FakeSession _session = new FakeSession();
        private readonly IDictionary _items = new Hashtable();

        public FakeHttpContext(string url, string userAgent, bool isMobileDevice)
        {
            _request = new FakeRequest(url, userAgent, isMobileDevice);
        }

        public override HttpRequestBase Request { get { return _request; } }
        public override HttpResponseBase Response { get { return _response; } }
        public override HttpSessionStateBase Session { get { return _session; } }
        public override IDictionary Items { get { return _items; } }
        public override DateTime Timestamp { get { return new DateTime(2024, 1, 1); } }
    }

    public static class ControllerTestContext
    {
        public const string DesktopUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36";
        public const string AndroidUserAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Mobile Safari/537.36";

        public static FakeHttpContext Attach(Controller controller, string url = "http://localhost/", string userAgent = DesktopUserAgent, bool isMobileDevice = false)
        {
            var httpContext = new FakeHttpContext(url, userAgent, isMobileDevice);
            controller.ControllerContext = new ControllerContext(new RequestContext(httpContext, new RouteData()), controller);
            return httpContext;
        }
    }
}
