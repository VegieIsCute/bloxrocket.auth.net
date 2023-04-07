function login(_username, _password, _self) {

    _self.value = "Loading...";

    axios.post("/v1/auth/start", { username: _username, password: _password }).then(function (response) {

        const content = response.data;
        const captcha_div = document.getElementById("captcha_div");
        const captcha_frame = document.getElementById("captcha_frame");

        const _2fa_div = document.getElementById("2fa_div");
        const _2fa_submit = document.getElementById("2fa_submit");
        const _2fa_textbox = document.getElementById("2fa_textbox");
        
        _self.value = "Login";

        const handle_2fa = (content_) => {
            _2fa_div.style.visibility = "visible";

            _2fa_submit.onclick = () => {
                axios.post("/v1/auth/2fa", {
                    userId: content_.data.userId,
                    mediaType: content_.data.mediaType,
                    ticket: content_.data.ticket,
                    code: _2fa_textbox.value
                }).then(function (response2) {
                    _2fa_div.style.visibility = "hidden";
                    _2fa_submit.onclick = () => { };

                    var content2 = response2.data;

                    console.log(content2);

                    if (content2.success) {
                        if (content2.data.code == 1) {
                            alert(content2.data.cookie);
                        }
                        else {
                            alert("Unknown error.")
                        }
                    }
                });
            };
        };


        const handle_event = (event) => {
            var data = JSON.parse(event.data);

            if (data.eventId === "challenge-completed") {
                window.removeEventListener("message", handle_event);

                var captchaToken = data.payload.sessionToken

                axios.post("/v1/auth/captcha", { username: _username, password: _password, captchaId: content.data.id, captchaToken: captchaToken }).then(function (response2) {
                    var content2 = response2.data;

                    captcha_div.style.visibility = "hidden";
                    captcha_frame.src = "about:blank";

                    if (content2.success) {
                        if (content2.data.code == 1) {
                            alert(content2.data.cookie)
                        } else if (content2.data.code == 3) {
                            handle_2fa(content2);
                        } else {
                            alert("unknown issue detected");
                        }
                    } else {
                        alert("internal error.");
                    }
                });
            }
        };

        if (content.success) {
            switch (content.data.code) {
                case 4:
                    alert("your account is banned.");
                    break;
                case 3:
                    handle_2fa(content);
                    break;
                case 2: {
                    captcha_div.style.visibility = "visible";
                    captcha_frame.src = `/iframe/funcaptcha.html?data-exchange-blob=${content.data.blob}&captcha-id=${content.data.id}`;

                    window.addEventListener("message", handle_event, false);
                    
                    break;
                }
                case 1: {
                    alert(content.data.cookie);
                    break;
                }
            }
        }
        else {
            alert("Internal Error.");
        }
    });
};


