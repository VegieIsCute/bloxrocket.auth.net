const express = require("express");
const funcaptcha = require("funcaptcha");

const app = express();

app.use(express.json());

app.post("/v1/auth/proxy/captcha", async (request, response) => {
    response.json(await funcaptcha.getToken({
        pkey: "476068BF-9607-4799-B53D-966BE98E2B81",
        surl: "https://roblox-api.arkoselabs.com",
        data: {
            blob: request.body.blob
        },
        headers: {
            "User-Agent": "Mozilla/5.0 (iPhone; CPU iPhone OS 16_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/111.2  Mobile/15E148 Safari/605.1.15"
        },
        site: "https://www.roblox.com"
    }));
});

app.listen(5112, () => {
    console.log("Arkose relay is online.");
});