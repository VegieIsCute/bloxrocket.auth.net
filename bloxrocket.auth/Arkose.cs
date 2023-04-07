using System;
using System.Net;
using RestSharp;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Arkose
{
    public class Request
    {
        RestRequest _Request;

        public Request(string URL)
        {
            _Request = new RestRequest(URL);
            _Request.Timeout = 10000;
        }

        public Request SetAccept(string _Type = "any")
        {
            switch (_Type)
            {
                case "html":
                    _Request.AddHeader("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    break;
                case "json":
                    _Request.AddHeader("accept", "application/json, text/plain, */*");
                    break;
                case "image":
                    _Request.AddHeader("accept", "image/webp,image/avif,video/*;q=0.8,image/png,image/svg+xml,image/*;q=0.8,*/*;q=0.5");
                    break;
                case "any":
                    _Request.AddHeader("accept", "*/*");
                    break;

            }

            return this;
        }

        public Request SetReferer(string _Host = "")
        {
            if (_Host.Equals(""))
            {
                _Request.AddHeader("referer", "https://www.roblox.com/");
            }
            else
            {
                _Request.AddHeader("referer", _Host);
            }

            return this;
        }

        public Request SetOrigin(string _Host = "")
        {
            if (_Host.Equals(""))
            {
                _Request.AddHeader("origin", "https://www.roblox.com");
            }
            else
            {
                _Request.AddHeader("origin", _Host);
            }

            return this;
        }

        public Request SetCookies(CookieContainer Cookies)
        {
            Cookies.GetAllCookies().ToList().ForEach((_Cookie) =>
            {
                _Request.AddCookie(_Cookie.Name, _Cookie.Value, _Cookie.Path, _Cookie.Domain);
            });

            return this;
        }

        public Request AddCSRF(string Token)
        {
            _Request.AddHeader("x-csrf-token", Token);
            return this;
        }

        public Request SetBody(object Body)
        {
            _Request.AddBody(Body);
            return this;
        }

        public Request SetBodyString(string Body, ContentType Type)
        {
            _Request.AddStringBody(Body, Type);
            return this;
        }

        public Request AddParams(Dictionary<string, string> Params)
        {
            foreach (var Param in Params)
            {
                _Request.AddQueryParameter(Param.Key, Param.Value);
            }

            return this;
        }

        public Request AddHeaders(Dictionary<string, string> Headers)
        {
            foreach (var Header in Headers)
            {
                _Request.AddHeader(Header.Key, Header.Value);
            }

            return this;
        }

        public RestRequest Get()
        {
            return _Request;
        }
    }

    public class Session : IDisposable
    {
        RestClient Client = new RestClient();
        CookieContainer CookieJar = new CookieContainer();
        string CSRF_TOKEN = "";
        int HeartBeat = 1;

        bool Finished;

        JObject Body;
        JObject FuncaptchaData;

        public Session()
        {
            Client.AddDefaultHeaders(new Dictionary<string, string>(){
                { "accept-encoding", "gzip, deflate, br" },
                { "accept-language", "en-US,en;q=0.9" },
                { "user-agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 16_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/111.2  Mobile/15E148 Safari/605.1.15" },
                { "connection", "keep-alive" }
            });
        }

        public void StoreCookies(CookieCollection Cookies)
        {
            if (Cookies != null)
            {
                CookieJar.Add(Cookies);
            }
        }

        public void SafeExecute(Action _Action, string Message)
        {
            try
            {
                _Action();
            }
            catch { throw new Exception(Message); };
        }

        public async Task<RestResponse> ecsv2(Dictionary<string, string> Data)
        {
            Data.Add("url", "https://www.roblox.com/login");
            Data.Add("lt", DateTime.UtcNow.ToString("o"));

            var gid = "";

            CookieJar.GetAllCookies().ToList().ForEach((_Cookie) =>
            {
                if (_Cookie.Name == "GuestData")
                {
                    gid = _Cookie.Value.Split("UserID=")[1];
                }
            });

            if (!gid.Equals(""))
            {
                Data.Add("gid", gid);
            }

            return await Client.GetAsync(new Request("https://ecsv2.roblox.com/www/e.png").SetAccept("image").SetCookies(this.CookieJar).AddParams(Data).SetReferer().Get());
        }

        public async Task<RestResponse> AgReportEvent(string Name, string Value = null, string Referer = "https://www.roblox.com/")
        {
            var Params = new Dictionary<string, string>() { { "name", Name } };

            if (Value != null)
            {
                Params.Add("value", Value);
            }

            return await Client.PostAsync(new Request("https://assetgame.roblox.com/game/report-event").SetAccept().SetCookies(this.CookieJar).AddParams(Params).SetReferer(Referer).SetOrigin().AddCSRF(this.CSRF_TOKEN).Get());
        }

        public async Task<Session> Build()
        {
            var roblox_com_login = await Client.GetAsync(new Request("https://www.roblox.com/login").SetAccept("html").Get());

            SafeExecute(() =>
            {
                if (roblox_com_login.Cookies != null && roblox_com_login.Content != null)
                {
                    CookieJar.Add(roblox_com_login.Cookies);
                    this.CSRF_TOKEN = roblox_com_login.Content.Split("<meta name=\"csrf-token\" data-token=\"")[1].Split("\"")[0];

                    CookieJar.Add(new Cookie() { Name = "RBXSource", Value = "rbx_acquisition_time=4/6/2023 10:26:55 AM&rbx_acquisition_referrer=https://www.roblox.com/login&rbx_medium=Direct&rbx_source=www.roblox.com&rbx_campaign=&rbx_adgroup=&rbx_keyword=&rbx_matchtype=&rbx_send_info=0", Domain = "roblox.com" });
                }
                else
                    throw new Exception();
            }, "Failed to get CSRF token.");

            await Client.GetAsync(new Request("https://roblox.com/js/hsts.js?v=3").SetAccept().SetReferer().SetCookies(this.CookieJar).Get());
            await Client.GetAsync(new Request("https://metrics.roblox.com/v1/thumbnails/metadata").SetAccept().SetReferer().SetOrigin().SetCookies(this.CookieJar).Get());
            await Client.GetAsync(new Request("https://www.roblox.com/appbumper/metadata").SetAccept("json").SetReferer().SetCookies(this.CookieJar).Get());
            await Client.GetAsync(new Request("https://apis.roblox.com/auth-token-service/v1/login/metadata").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).Get());
            await Client.GetAsync(new Request("https://apis.roblox.com/universal-app-configuration/v1/behaviors/cookie-policy/content").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).Get());
            await Client.PostAsync(new Request("https://www.roblox.com/game/report-stats?name=ResourcePerformance_Loaded_funcaptcha_Phone&value=3").SetAccept().SetReferer("https://www.roblox.com/login").SetOrigin().SetCookies(this.CookieJar).Get());
            await Client.PostAsync(new Request("https://apis.roblox.com/product-experimentation-platform/v1/projects/1/values").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).AddCSRF(this.CSRF_TOKEN).SetBodyString("{\"layers\":{\"Website.Login\":{}}}", ContentType.Json).Get());
            await Client.GetAsync(new Request("https://apis.roblox.com/otp-service/v1/metadata?Origin=login").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).Get());
            await Client.GetAsync(new Request("https://apis.roblox.com/product-experimentation-platform/v1/projects/1/layers/Website.Login.CrossDeviceLogin.DisplayCode/values?parameters=alt_title,alt_instruction,alt_device_specific_instruction").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).Get());

            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (this.Finished)
                        break;

                    await ecsv2(new Dictionary<string, string>()
                    {
                        { "evt", "pageHeartbeat" },
                        { "ctx", $"heartbeat{this.HeartBeat++}" }
                    });

                    await Task.Delay(8000);
                }
            });

            await ecsv2(new Dictionary<string, string>() { { "field", "username" }, { "aType", "focus" }, { "evt", "formInteraction" }, { "ctx", "LoginForm" } });
            await ecsv2(new Dictionary<string, string>() { { "field", "username" }, { "aType", "offFocus" }, { "evt", "formInteraction" }, { "ctx", "LoginForm" } });
            await ecsv2(new Dictionary<string, string>() { { "field", "password" }, { "aType", "focus" }, { "evt", "formInteraction" }, { "ctx", "LoginForm" } });

            return this;
        }
        public async Task<Tuple<RestResponse, JObject?>> Login(string Username, string Password)
        {
            await ecsv2(new Dictionary<string, string>() { { "field", "password" }, { "aType", "offFocus" }, { "evt", "formInteraction" }, { "ctx", "LoginForm" } });
            await ecsv2(new Dictionary<string, string>() { { "field", "loginSubmit" }, { "aType", "click" }, { "evt", "formInteraction" }, { "ctx", "loginPage" } });

            await Client.PostAsync(new Request("https://apis.roblox.com/product-experimentation-platform/v1/projects/1/values").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).AddCSRF(this.CSRF_TOKEN).SetBodyString("{\"layers\":{\"Account.HardwareBackendAuth\":{}}}", ContentType.Json).Get());

            await AgReportEvent("WebsiteLogin_FirstAttempt");
            await AgReportEvent("WebsiteLogin_Attempt");

            var Login = await Client.ExecutePostAsync(new Request("https://auth.roblox.com/v2/login").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBody(new
            {
                ctype = "Username",
                cvalue = Username,
                password = Password
            }).Get());

            if (Login.Content != null && Login.ContentType != null && Login.ContentType.Contains("application/json"))
            {
                Body = JObject.Parse(Login.Content);

                return Tuple.Create<RestResponse, JObject?>(Login, Body);
            }
            else
            {
                return Tuple.Create<RestResponse, JObject?>(Login, null);
            }
        }

        public async Task<RestResponse> LoginWithCaptcha(string Username, string Password, string CaptchaId, string CaptchaToken)
        {
            return await Client.ExecutePostAsync(new Request("https://auth.roblox.com/v2/login").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBody(new
            {
                ctype = "Username",
                cvalue = Username,
                password = Password,
                captchaId = CaptchaId,
                captchaToken = CaptchaToken
            }).Get());
        }

        public async Task<string> Proxy(IFormCollection Data)
        {
            FuncaptchaData = JObject.Parse(Body["errors"][0]["fieldData"].ToObject<string>());

            var btid = "";

            CookieJar.GetAllCookies().ToList().ForEach((_Cookie) =>
            {
                if (_Cookie.Name == "RBXEventTrackerV2")
                {
                    btid = _Cookie.Value.Split("browserid=")[1];
                }
            });

            await ecsv2(new Dictionary<string, string>() { { "btid", btid }, { "provider", "FunCaptcha" }, { "ucid", FuncaptchaData["unifiedCaptchaId"].ToObject<string>() }, { "captchaVersion", "V2" }, { "evt", "captchaV2Experimentation" }, { "ctx", "Login" }, });

            await AgReportEvent("WebsiteLogin_Captcha");
            await AgReportEvent("LoginFunCaptcha_Triggered");

            var Response = await Client.ExecutePostAsync(new Request("http://localhost:5112/v1/auth/proxy/captcha").SetBody(new
            {
                blob = FuncaptchaData["dxBlob"].ToObject<string>(),
            }).Get());

            await Client.PostAsync(new Request("https://apis.roblox.com/account-security-service/v1/metrics/record").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBodyString("{\"name\":\"event_captcha\",\"value\":1,\"labelValues\":{\"action_type\":\"Login\",\"event_type\":\"FunCaptcha_Initialized\",\"application_type\":\"unknown\",\"version\":\"V2\"}}", ContentType.Json).Get());
            await AgReportEvent("LoginFunCaptcha_Initialized");

            if (Response.Content != null)
                return Response.Content.ToString();
            else
                return "{}";
        }

        public async Task Report2FA(string UserId, string Ticket, string MediaType)
        {
            await AgReportEvent("WebsiteLogin_Success");
            await AgReportEvent("Login_TwoStepVerification_Initialized");

            await ecsv2(new Dictionary<string, string>() {
                { "challengeId", Ticket},
                { "targetUserId", UserId},
                { "evt", "accountSecurityChallengeTwoStepVerificationEvent"},
                { "ctx", "challengeInitialized"}
            });

            await Client.GetAsync(new Request($"https://twostepverification.roblox.com/v1/metadata?userId={UserId}&challengeId={Ticket}&actionType=Login").SetAccept("json").SetReferer().SetOrigin().SetCookies(this.CookieJar).Get());

            await ecsv2(new Dictionary<string, string>() {
                { "challengeId", Ticket},
                { "targetUserId", UserId},
                { "mediaType", MediaType},
                { "evt", "accountSecurityChallengeTwoStepVerificationEvent"},
                { "ctx", "mediaTypeChanged"}
            });

            await ecsv2(new Dictionary<string, string>() {
                { "challengeId", Ticket},
                { "targetUserId", UserId},
                { "mediaType", MediaType},
                { "evt", "accountSecurityChallengeTwoStepVerificationEvent"},
                { "ctx", "userConfigurationLoaded"}
            });
        }

        public async Task<Tuple<bool, string>> Send2FA(string UserId, string Ticket, string MediaType, string Code)
        {
            Console.WriteLine(UserId);
            Console.WriteLine(Ticket);
            Console.WriteLine(MediaType);
            Console.WriteLine(Code);

            await ecsv2(new Dictionary<string, string>() {
                { "challengeId", Ticket},
                { "targetUserId", UserId},
                { "mediaType", MediaType},
                { "evt", "accountSecurityChallengeTwoStepVerificationEvent"},
                { "ctx", "codeSubmitted"}
            });

            var TwoStep = await Client.ExecutePostAsync(new Request($"https://twostepverification.roblox.com/v1/users/{UserId}/challenges/email/verify").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBody(new
            {
                challengeId = Ticket,
                actionType = "Login",
                code = Code
            }).Get());

            if (TwoStep.StatusCode == HttpStatusCode.OK && TwoStep.Content != null)
            {
                if (JObject.Parse(TwoStep.Content) is JObject JTwoStep)
                {
                    await ecsv2(new Dictionary<string, string>() {
                        { "challengeId", Ticket},
                        { "targetUserId", UserId},
                        { "mediaType", MediaType},
                        { "evt", "accountSecurityChallengeTwoStepVerificationEvent"},
                        { "ctx", "codeVerified"}
                    });

                    await Client.PostAsync(new Request("https://apis.roblox.com/account-security-service/v1/metrics/record").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBodyString("{\"name\":\"event_2sv\",\"value\":1,\"labelValues\":{\"action_type\":\"Login\",\"event_type\":\"VerifiedEmail\",\"application_type\":\"unknown\"}}", ContentType.Json).Get());
                    await Client.PostAsync(new Request("https://apis.roblox.com/account-security-service/v1/metrics/record").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBodyString("{\"name\":\"solve_time_2sv\",\"value\":21214,\"labelValues\":{\"action_type\":\"Login\",\"event_type\":\"VerifiedEmail\",\"application_type\":\"unknown\"}}", ContentType.Json).Get());

                    await AgReportEvent("Login_TwoStepVerification_VerifiedEmail");

                    var Login = await Client.ExecutePostAsync(new Request($"https://auth.roblox.com/v3/users/{UserId}/two-step-verification/login").SetAccept("json").SetOrigin().SetReferer().AddCSRF(this.CSRF_TOKEN).SetCookies(this.CookieJar).SetBody(new
                    {
                        challengeId = Ticket,
                        verificationToken = JTwoStep["verificationToken"].ToObject<string>(),
                        rememberDevice = false
                    }).Get());

                    if (Login.StatusCode == HttpStatusCode.OK && Login.Cookies != null && Login.Cookies.Count != 0)
                    {
                        return Tuple.Create(true, Login.Cookies.Single((C) => C.Name == ".ROBLOSECURITY").Value);
                    }
                }
            }

            return Tuple.Create(false, "");
        }

        public void Dispose()
        {
            Finished = true;
        }
    }
}