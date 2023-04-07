using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace bloxrocket.auth.Endpoints
{
    public class Auth
    {
        Dictionary<string, Arkose.Session> Captchas = new();
        Dictionary<string, Arkose.Session> _2fas = new();

        public class StartRequestBody
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        public async Task<object> DetermineStatusOk(RestResponse Response, Arkose.Session ArkoseSession)
        {
            var JContent = JObject.Parse(Response.Content);

            if (JContent["isBanned"].ToObject<bool>())
            {
                ArkoseSession.Dispose();

                return new
                {
                    Success = true,
                    Data = new
                    {
                        Code = 4
                    }
                };
            }
            else
            {
                if (JContent.ContainsKey("twoStepVerificationData"))
                {
                    var mediaType = JContent["twoStepVerificationData"]["mediaType"].ToObject<string>();
                    var ticket = JContent["twoStepVerificationData"]["ticket"].ToObject<string>();
                    var userId = JContent["user"]["id"].ToObject<string>();

                    await ArkoseSession.Report2FA(userId, ticket, mediaType);

                    _2fas.Add(ticket, ArkoseSession);

                    return new
                    {
                        Success = true,
                        Data = new
                        {
                            Code = 3,
                            userId,
                            mediaType,
                            ticket
                        }
                    };
                }
                else
                {
                    if (Response.Cookies != null && Response.Cookies.Count != 0)
                    {
                        var Cookie = Response.Cookies.Single((C) => C.Name == ".ROBLOSECURITY");

                        if (Cookie != null)
                        {
                            return new
                            {
                                Success = true,
                                Data = new
                                {
                                    Code = 1,
                                    Cookie = Cookie.Value
                                }
                            };
                        }
                    }
                }
            }


            return new
            {
                Success = false,
                Reason = "Authorization failure."
            };
        }

        public async Task<object> Start(HttpRequest Request)
        {
            var Body = await Request.ReadFromJsonAsync<StartRequestBody>();

            if (Body != null)
            {
                var ArkoseSession = await new Arkose.Session().Build();
                var Login = await ArkoseSession.Login(Body.Username, Body.Password);

                var Response = Login.Item1;

                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    return await DetermineStatusOk(Response, ArkoseSession);
                }
                else if(Response.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (Login.Item2 is JObject Content)
                    {
                        if (Content["errors"] is JToken Errors)
                        {
                            if (Errors[0] is JToken FirstError)
                            {
                                if (FirstError["code"] is JToken Code)
                                {
                                    if (Code.ToObject<int>() == 2)
                                    {
                                        var fieldData = JObject.Parse(FirstError["fieldData"].ToObject<string>());
                                        var Id = fieldData["unifiedCaptchaId"].ToObject<string>();
                                        var Blob = fieldData["dxBlob"].ToObject<string>();

                                        Captchas.Add(Id, ArkoseSession);

                                        return new
                                        {
                                            Success = true,
                                            Data = new
                                            {
                                                Code = 2,
                                                Blob,
                                                Id
                                            }
                                        };
                                    }
                                }
                            }
                        }
                    }
                }

                ArkoseSession.Dispose();

                return new
                {
                    Success = false,
                    Reason = "Authorization failure."
                };
            }

            return new { Succes = false, Reason = "Failed to read request data." };
        }

        public async Task<object> ForwardFC(HttpRequest Request)
        {
            var Body = await Request.ReadFormAsync();

            if (Body != null)
            {
                var Referer = Request.Headers.Referer.First().ToString();
                var Cid = Referer.Split("captcha-id=")[1];

                return await Captchas[Cid].Proxy(Body);
            }

            return new { };
        }

        public class CaptchaRequestBody
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string CaptchaId { get; set; } = null!;
            public string CaptchaToken { get; set; } = null!;
        }

        public async Task<object> Captcha(HttpRequest Request)
        {
            var Body = await Request.ReadFromJsonAsync<CaptchaRequestBody>();

            if (Body != null)
            {
                var ArkoseSession = Captchas[Body.CaptchaId];

                var Response = await ArkoseSession.LoginWithCaptcha(Body.Username, Body.Password, Body.CaptchaId, Body.CaptchaToken);

                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    return await DetermineStatusOk(Response, ArkoseSession);
                }

                ArkoseSession.Dispose();

                return new
                {
                    Success = false,
                    Reason = "Authorization failure."
                };
            }

            return new { Succes = false, Reason = "Failed to read request data." };
        }

        public class _2faRequestBody
        {
            public string UserId { get; set; } = null!;
            public string MediaType { get; set; } = null!;
            public string Ticket { get; set; } = null!;
            public string Code { get; set; } = null!;
        }
        public async Task<object> _2fa(HttpRequest Request)
        {
            var Body = await Request.ReadFromJsonAsync<_2faRequestBody>();

            if (Body != null)
            {
                var ArkoseSession = _2fas[Body.Ticket];

                var Data = await ArkoseSession.Send2FA(Body.UserId, Body.Ticket, Body.MediaType, Body.Code);

                ArkoseSession.Dispose();

                if (Data.Item1)
                {
                    return new
                    {
                        Success = true,
                        Data = new
                        {
                            Code = 1,
                            Cookie = Data.Item2
                        }
                    };
                }

                return new
                {
                    Success = false,
                    Reason = "Authorization failure."
                };
            }

            return new { Succes = false, Reason = "Failed to read request data." };
        }

        public Auth(WebApplication App)
        {
            App.MapPost("/v1/auth/start", Start).WithName("Start Auth").WithOpenApi();
            App.MapPost("/v1/auth/captcha", Captcha).WithName("Captcha Auth").WithOpenApi();
            App.MapPost("/v1/auth/2fa", _2fa).WithName("2FA Auth").WithOpenApi();
            App.MapPost("/api/arkose/fc/gt2/public_key/476068BF-9607-4799-B53D-966BE98E2B81", ForwardFC).WithName("Auth Proxy").WithOpenApi();
        }
    }
}