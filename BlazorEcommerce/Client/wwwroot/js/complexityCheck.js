const symbols = /['<>@!#$%^&*()_+\]\[\{}?:;|'"\\,.\/~`\-=']+/;
const numerical = /.*[0-9].*/;
const upperCase = /.*[A-Z].*/;
const lowerCase = /.*[a-z].*/;

(function (global) {
    global.customStrength = function (dotNetObject) {
        const allowedSymbols = '<>@!#$%^&*()_+[]{}?:;|\'"\\,./~`-=';
        const passwordField = document.querySelectorAll("smart-password-text-box")[0];
        const passwordConfirm = document.querySelectorAll("smart-password-text-box")[1];
        passwordConfirm.style.border = '';
        passwordConfirm.addEventListener('change', function (event) {
            if (event.detail.value) {
                this.style.border = "#26b050 solid 1.5px";
            } else {
                this.style.border = "red solid 3px";
            }
        });
        passwordField.messages = {
            'en': {
                'passwordStrength': 'Password strength',
                'short': 'Short',
                'init': 'init',
                'weak': 'complexity Week - missing at least 1 symbol, ' + allowedSymbols,
                'far': 'complexity Fair - missing at least 1 numerical value, 0 - 9',
                'better': 'complexity Better - missing at least 1 upper case characture, A- Z',
                'good': 'complexity Good - missing at least 1 lower case characture, a - z',
                'strong': 'Strong =)',
                'showPassword': 'Show password'
            }
        };
        passwordField.style.textIndent = '0.7rem'
        setTimeout(function () {
            passwordField.passwordStrength = function (password, allowableSymbols) {
                const passwordLength = password.length;
                let message = null;
                //debugger;                
                passwordField.children[0].classList.remove("smart-password-better");
                if (passwordLength < 7) {
                    message = 'short';
                } else if (!new RegExp(symbols).exec(password)) {
                    message = 'weak';
                } else if (!new RegExp(numerical).exec(password)) {
                    message = 'far';
                } else if (!new RegExp(upperCase).exec(password)) {
                    message = 'better';
                } else if (!new RegExp(lowerCase).exec(password)) {
                    message = 'good';
                }
                //console.log(message);
                if (message) {
                    dotNetObject.invokeMethodAsync('CallbackComplexityCheck', false)
                    return message;
                }
                dotNetObject.invokeMethodAsync('CallbackComplexityCheck', true)
                return 'strong';
            }
        }, 0);
        setTimeout(function () {
            passwordField.children[0].classList.remove("smart-password-short");
        }, 10);
        return true;
    }
})(window);