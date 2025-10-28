let displayMessageTimeout = null;
let displayMessageCallbackTimeout = null;

const preventDefaults = event => {
    event.preventDefault();
    event.stopPropagation();
};

function displayLoader(message) {
    $('body').loading({
        theme: 'dark',
        message,
        stoppable: false,
        start: true
    });
}

function hideLoader() {
    $('body').loading('stop');
}

function uuidv4() {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
        (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
    );
}

function getTimestamp() {
    var datetime = new Date();
    return `${pad(datetime.getFullYear(), 4)}-${pad(datetime.getMonth(), 2)}-${pad(datetime.getDate(), 2)}_${pad(datetime.getHours(), 2)}-${pad(datetime.getMinutes(), 2)}-${pad(datetime.getSeconds(), 2)}`;
}

function pad(num, size) {
    num = num.toString();
    while (num.length < size) num = "0" + num;
    return num;
}

function downloadBlob(filename, contentType, data, xhr) {
    const downloadUrl = URL.createObjectURL(new Blob([data], { type: contentType }));

    const a = document.createElement('a');
    a.href = downloadUrl;

    let fileName = filename;
    if (xhr) {
        const disposition = xhr.getResponseHeader('Content-Disposition');
        if (disposition && disposition.indexOf('attachment') !== -1) {
            const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
            const matches = filenameRegex.exec(disposition);
            if (matches != null && matches[1]) {
                fileName = matches[1].replace(/['"]/g, '');
            }
        }
    }

    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(downloadUrl);
}

function setCookie(cname, cvalue, hours) {
    let consent = getCookie('.AspNet.Consent');
    if (consent !== undefined && consent === 'yes') {
        const d = new Date();
        d.setTime(d.getTime() + (hours * 60 * 60 * 1000));
        document.cookie = `${cname}=${cvalue};expires=${d.toUTCString()};path=/`;
    } else {
        console.warn(`Cannot set cookie '${cname}' as the user has not accepted the cookie policy`);
    }
}

function getCookie(cname) {
    let ca = document.cookie.split(';');
    let name = `${cname}=`;

    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];

        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }

        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }

    return "";
}

function displayMessage(title, message, errors, callbackFn) {
    hideLoader();

    $('#alert-message-modal .modal-title').text(title);
    $('#alert-message-modal .modal-message').text(message);

    $('#alert-message-modal .modal-error').hide();
    if (errors && errors.length > 0) {
        var errorMessage = `<b>${localization.translate('Details')}:</b>`;
        errorMessage += `<div class="checklist">`;
        errors.forEach((error) => {
            errorMessage += generateChecklistItem('', 'none', error, false, false);
        });
        errorMessage += `</div>`;
        $('#alert-message-modal .modal-error').html(errorMessage);
        $('#alert-message-modal .modal-error').show();
    } else {
        $('#alert-message-modal .modal-error').text('');
    }

    $('#alert-message-modal .btn').off('click').on('click', function (e) {
        clearTimeout(displayMessageCallbackTimeout);
        if (callbackFn !== undefined && callbackFn !== null) {
            displayMessageCallbackTimeout = setTimeout(function () { callbackFn(); }, 200);
        }
    });
    $('#alert-message-modal').modal('show');

    clearTimeout(displayMessageTimeout);
    displayMessageTimeout = setTimeout(function () {
        if ($('#alert-message-modal').is(':visible')) {
            hideMessage();
            if (callbackFn !== undefined && callbackFn !== null) {
                callbackFn();
            }
        }
    }, 10000);
}

function hideMessage() {
    $('#alert-message-modal').modal('hide');
    hideLoader();
}

function displayIdentityCheck(required, callbackFn) {
    let buttons = [{
        Text: localization.translate('Identity_Check_Tell_Us'),
        Class: 'btn-success',
        Callback: function () {
            let name = $('#popup-modal-field-identity-name').val().trim();
            let emailAddress = $('#popup-modal-field-identity-email').length > 0 ? $('#popup-modal-field-identity-email').val().trim() : '';
            if (name !== undefined && name.length > 0) {
                $.ajax({
                    url: '/Home/SetIdentity',
                    method: 'POST',
                    data: { name, emailAddress }
                })
                    .done(data => {
                        if (data == undefined || data.success == undefined) {
                            displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                        } else if (data.success) {
                            $('.file-uploader-form').attr('data-identity-required', 'false');

                            if (callbackFn !== undefined && callbackFn !== null) {
                                callbackFn();
                            } else {
                                window.location.reload();
                            }
                        } else if (data.reason == 1) {
                            displayMessage(localization.translate('Identity_Check_Invalid_Name'), localization.translate('Identity_Check_Invalid_Name_Msg'), null, () => {
                                displayIdentityCheck(required, callbackFn);
                            });
                        } else if (data.reason == 2) {
                            displayMessage(localization.translate('Identity_Check_Invalid_Email'), localization.translate('Identity_Check_Invalid_Email_Msg'), null, () => {
                                displayIdentityCheck(required, callbackFn);
                            });
                        } else {
                            displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                        }
                    })
                    .fail((xhr, error) => {
                        displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                    });
            } else {
                displayMessage(localization.translate('Identity_Check_Invalid_Name'), localization.translate('Identity_Check_Invalid_Name_Msg'), null, () => {
                    displayIdentityCheck(required, callbackFn);
                });
            }
        }
    }];

    if (!required) {
        buttons.push({
            Text: localization.translate('Identity_Check_Stay_Anonymous'),
            Callback: function () {
                $.ajax({
                    url: '/Home/SetIdentity',
                    method: 'POST',
                    data: { name: 'Anonymous', emailAddress: '' }
                })
                    .done(data => {
                        if (data == undefined || data.success == undefined) {
                            displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                        } else if (data.success) {
                            window.location.reload();
                        } else if (data.reason == 1) {
                            displayMessage(localization.translate('Identity_Check_Invalid_Name'), localization.translate('Identity_Check_Invalid_Name_Msg'), null, () => {
                                displayIdentityCheck(required, callbackFn);
                            });
                        } else if (data.reason == 2) {
                            displayMessage(localization.translate('Identity_Check_Invalid_Email'), localization.translate('Identity_Check_Invalid_Email_Msg'), null, () => {
                                displayIdentityCheck(required, callbackFn);
                            });
                        } else {
                            displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                        }
                    })
                    .fail((xhr, error) => {
                        displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                    });
            }
        });
    }

    let emailRequired = $('.change-identity').attr('data-identity-email') !== undefined;
    let identityCheckFields = [{
        Id: 'identity-name',
        Name: localization.translate('Identity_Check_Name'),
        Value: '',
        Hint: localization.translate('Identity_Check_Name_Hint'),
        Placeholder: localization.translate('Identity_Check_Name_Placeholder')
    }];

    if (emailRequired) {
        identityCheckFields.push({
            Id: 'identity-email',
            Name: localization.translate('Identity_Check_Email'),
            Value: '',
            Hint: localization.translate('Identity_Check_Email_Hint'),
            Placeholder: localization.translate('Identity_Check_Email_Placeholder')
        });
    }

    displayPopup({
        Title: localization.translate('Identity_Check'),
        Fields: identityCheckFields,
        Buttons: buttons
    });
}

function generateChecklistItem(identifier, type, text, hidden = false, padded = true) {
    let icon;
    switch (type.toLowerCase()) {
        case 'success':
            icon = `<i class="fa fa-square-check mx-2"></i>`;
            break;
        case 'error':
            icon = `<i class="fa fa-square-xmark mx-2"></i>`;
            break;
        case 'default':
            icon = `<i class="fa fa-regular fa-square mx-2"></i>`;
            break;
        default:
            icon = '';
            break;
    }

    return `
        <span class="${identifier} ${hidden === true ? 'visually-hidden' : ''} border rounded ${padded === false ? 'mx-0' : 'mx-3 mx-lg-5'} my-2 px-3 py-1 d-block checklist-${type}">
            ${icon}${text}
        </span>
    `;
}

function generatePasswordValidation(field) {
    return `
        <div class="checklist password-validator" data-input="${field}">
            ${generateChecklistItem('lbl-lower', 'default', localization.translate('Password_Validation_Lower'))}
            ${generateChecklistItem('lbl-upper', 'default', localization.translate('Password_Validation_Upper'))}
            ${generateChecklistItem('lbl-number', 'default', localization.translate('Password_Validation_Numbers'))}
            ${generateChecklistItem('lbl-special', 'default', localization.translate('Password_Validation_Special'))}
            ${generateChecklistItem('lbl-length', 'default', localization.translate('Password_Validation_Length'))}
            ${generateChecklistItem('lbl-confirm', 'default', localization.translate('Password_Validation_Confirm'), true)}
        </div>
    `;
}

function setPasswordValidationField(field, valid) {
    if (valid) {
        field.removeClass('checklist-success checklist-error checklist-default');
        field.addClass('checklist-success');
        field.find('i.fa').removeClass('fa-square-check fa-square-xmark fa-square fa-regular');
        field.find('i.fa').addClass('fa-square-check');
    } else {
        field.removeClass('checklist-success checklist-error checklist-default');
        field.addClass('checklist-error');
        field.find('i.fa').removeClass('fa-square-check fa-square-xmark fa-square fa-regular');
        field.find('i.fa').addClass('fa-square-xmark');
    }
}

function initPasswordValidation() {
    if ($('.password-validator').length > 0) {
        $('.password-validator').each(function () {
            let validator = $(this);
            let input = $(validator.data('input'));
            if (input !== undefined && input.length > 0) {
                let confirmField = input.parent().parent().parent().find('input.confirm-password');
                if (confirmField !== undefined && confirmField.length === 1) {
                    validator.find('.lbl-confirm').removeClass('visually-hidden');
                    confirmField.off('keyup').on('keyup', function () {
                        var value = $(input).val();
                        setPasswordValidationField(validator.find('.lbl-confirm'), confirmField.val() === value && value.length);
                        setPasswordValidationField(validator, validator.find('li[class^=\'lbl-\']:not([class*=\'hidden\'])').length === 0);
                    });
                }

                $(input).off('keyup').on('keyup', function () {
                    var value = $(this).val();
                    setPasswordValidationField(validator.find('.lbl-lower'), /[a-z]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-upper'), /[A-Z]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-number'), /[0-9]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-special'), /[^A-Za-z0-9]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-length'), value.length >= 8);

                    if (confirmField !== undefined && confirmField.length === 1) {
                        setPasswordValidationField(validator.find('.lbl-confirm'), confirmField.val() === value && value.length);
                    }

                    setPasswordValidationField(validator, validator.find('li[class^=\'lbl-\']:not([class*=\'hidden\'])').length === 0);
                })
            }
        });
    }
}

function resizeLayout() {
    if ($('div#main-wrapper').length > 0) {
        let windowWidth = $(window).width();
        let windowHeight = $(window).height();
        let navHeight = $('nav.navbar').outerHeight();
        let footerHeight = $('footer').outerHeight();
        let bodyHeight = windowHeight - (navHeight + footerHeight);

        $('div#main-wrapper').css({
            'height': `${bodyHeight}px`,
            'max-height': `${bodyHeight}px`,
            'top': `${navHeight}px`
        });

        if ($('div#main-content').length > 0) {
            let contentHeight = $('div#main-content').outerHeight();
            let padding = (bodyHeight - contentHeight) / 2;

            if (windowWidth >= 700 && padding < 30) {
                padding = 30;
            } else if (windowWidth < 700 && padding < 20) {
                padding = 20;
            }

            $('div#main-content').css({
                'padding-top': `${padding}px`,
                'padding-bottom': `50px`
            });
        }
    }
}

(function () {
    document.addEventListener('DOMContentLoaded', function () {

        if ($('div.password-validator-container').length > 0) {
            $('div.password-validator-container').each(function () {
                $(this).html(generatePasswordValidation($(this).data('input')));
            });
            initPasswordValidation();
        }

        if ($('.nav-horizontal-scroller').length > 0) {
            $('.nav-horizontal-scroller').each(function () {
                let pos = $(this).find('.active').position().left - 30;
                $('.nav-horizontal-scroller').scrollLeft(pos);
            });
        }

        resizeLayout();

        $(document).on('keyup', function (e) {
            if (e.key === 'Escape') {
                hidePopup();
            }
        });

        $(document).off('click', '.change-theme').on('click', '.change-theme', function (e) {
            if ($('i.change-theme').hasClass('fa-sun')) {
                setCookie('Theme', 'default', 24);
            } else {
                setCookie('Theme', 'dark', 24);
            }

            window.location.reload();
        });

        $(document).off('click', '.change-language').on('click', '.change-language', function (e) {
            preventDefaults(e); 

            $.ajax({
                type: "GET",
                url: '/Language',
                success: function (data) {
                    if (data.supported && data.supported.length > 0) {
                        displayPopup({
                            Title: localization.translate('Language_Change'),
                            Fields: [{
                                Id: 'language-id',
                                Name: localization.translate('Language'),
                                Hint: localization.translate('Language_Name_Hint'),
                                Placeholder: 'English (en-GB)',
                                Type: 'select',
                                SelectOptions: data.supported
                            }],
                            Buttons: [{
                                Text: localization.translate('Switch'),
                                Class: 'btn-success',
                                Callback: function () {
                                    $.ajax({
                                        type: "POST",
                                        url: '/Language/ChangeDisplayLanguage',
                                        data: { culture: $('#popup-modal-field-language-id').val().trim() },
                                        success: function (data) {
                                            if (data.success) {
                                                try {
                                                    window.location = window.location.toString().replace(/([&]*culture\=.+?)(\&|$)/g, '');
                                                } catch {
                                                    window.location.reload();
                                                }
                                            }
                                        }
                                    });
                                }
                            }, {
                                Text: localization.translate('Cancel')
                            }]
                        });
                    }
                }
            });
        });

        $(document).off('click', '.change-identity').on('click', '.change-identity', function (e) {
            function displayIdentityCheckChangeIdentity(elem) {
                let emailRequired = elem.attr('data-identity-email') !== undefined;

                let fields = [{
                    Id: 'identity-name',
                    Name: localization.translate('Identity_Check_Name'),
                    Value: elem.data('identity-name'),
                    Hint: localization.translate('Identity_Check_Name_Hint'),
                    Placeholder: localization.translate('Identity_Check_Name_Placeholder')
                }];

                if (emailRequired) {
                    fields.push({
                        Id: 'identity-email',
                        Name: localization.translate('Identity_Check_Email'),
                        Value: elem.data('identity-email'),
                        Hint: localization.translate('Identity_Check_Email_Hint'),
                        Placeholder: localization.translate('Identity_Check_Email_Placeholder')
                    });
                }

                preventDefaults(e);
                displayPopup({
                    Title: localization.translate('Identity_Check_Change_Identity'),
                    Fields: fields,
                    Buttons: [{
                        Text: localization.translate('Identity_Check_Change'),
                        Class: 'btn-success',
                        Callback: function () {
                            let name = $('#popup-modal-field-identity-name').val().trim();
                            let emailAddress = $('#popup-modal-field-identity-email').length > 0 ? $('#popup-modal-field-identity-email').val().trim() : '';
                            if (name !== undefined && name.length > 0) {
                                $.ajax({
                                    url: '/Home/SetIdentity',
                                    method: 'POST',
                                    data: { name, emailAddress }
                                })
                                    .done(data => {
                                        if (data == undefined || data.success == undefined) {
                                            displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                                        } else if (data.success) {
                                            window.location.reload();
                                        } else if (data.reason == 1) {
                                            displayMessage(localization.translate('Identity_Check_Invalid_Name'), localization.translate('Identity_Check_Invalid_Name_Msg'), null, () => {
                                                displayIdentityCheckChangeIdentity(elem);
                                            });
                                        } else if (data.reason == 2) {
                                            displayMessage(localization.translate('Identity_Check_Invalid_Email'), localization.translate('Identity_Check_Invalid_Email_Msg'), null, () => {
                                                displayIdentityCheckChangeIdentity(elem);
                                            });
                                        } else {
                                            displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                                        }
                                    })
                                    .fail((xhr, error) => {
                                        displayMessage(localization.translate('Identity_Check'), localization.translate('Identity_Check_Set_Failed'), [error]);
                                    });
                            } else {
                                displayMessage(localization.translate('Identity_Check_Invalid_Name'), localization.translate('Identity_Check_Invalid_Name_Msg'), null, () => {
                                    displayIdentityCheckChangeIdentity(elem);
                                });
                            }
                        }
                    }, {
                        Text: localization.translate('Cancel')
                    }]
                });
            }
            displayIdentityCheckChangeIdentity($(this));
        });

        $(document).off('click', '.btn-reload').on('click', '.btn-reload', function () {
            hideMessage();
            hideLoader();
        });

        $(document).off('click', '#btnGenerateGalleryName').on('click', '#btnGenerateGalleryName', function (e) {
            preventDefaults(e);
            $('input#gallery-id').val(uuidv4());
        });

        $(document).off('submit', '#frmSelectGallery').on('submit', '#frmSelectGallery', function (e) {
            preventDefaults(e);

            var galleryId = $('input#gallery-id,select#gallery-id').val().trim();
            var secretKey = $('input#gallery-key').val().trim();

            const regex = /^[a-zA-Z0-9\-\s-_~]+$/;
            if (galleryId && galleryId.length > 0 && regex.test(galleryId)) {
                $.ajax({
                    type: "POST",
                    url: '/Gallery/Login',
                    data: { id: galleryId, key: secretKey },
                    success: function (data) {
                        if (data.success && data.redirectUrl) {
                            window.location = data.redirectUrl;
                        } else {
                            displayMessage(localization.translate('Gallery'), localization.translate('Gallery_Invalid_Secret_Key'));
                        }
                    }
                });
            } else {
                displayMessage(localization.translate('Gallery'), localization.translate('Gallery_Invalid_Name'));
            }
        });

        $(document).off('submit', '#frmLogin').on('submit', '#frmLogin', function (e) {
            preventDefaults(e);

            var token = $('#frmLogin input[name=\'__RequestVerificationToken\']').val();

            var username = $('#frmLogin input.input-username').val();
            if (username === undefined || username.length === 0) {
                displayMessage(localization.translate('Login'), localization.translate('Login_Invalid_Username'));
                return;
            }

            var password = $('#frmLogin input.input-password').val();
            if (password === undefined || password.length === 0) {
                displayMessage(localization.translate('Login'), localization.translate('Login_Invalid_Password'));
                return;
            }

            displayLoader(localization.translate('Loading'));

            $.ajax({
                url: '/Account/Login',
                method: 'POST',
                data: { __RequestVerificationToken: token, Username: username, Password: password }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        if (data.pending_activation == true)
                        {
                            displayMessage(localization.translate('Login'), localization.translate('Login_Verify_Email'));
                        }
                        else if (data.mfa === true) {
                            displayPopup({
                                Title: localization.translate('2FA'),
                                Fields: [{
                                    Id: '2fa-code',
                                    Name: localization.translate('Code'),
                                    Value: '',
                                    Hint: localization.translate('2FA_Code_Hint')
                                }],
                                Buttons: [{
                                    Text: localization.translate('Validate'),
                                    Class: 'btn-success',
                                    Callback: function () {
                                        let code = $('#popup-modal-field-2fa-code').val();

                                        $.ajax({
                                            type: "POST",
                                            url: '/Account/ValidateMultifactorAuth',
                                            data: { __RequestVerificationToken: token, Username: username, Password: password, Code: code },
                                            success: function (data) {
                                                if (data.success === true) {
                                                    window.location = `/Account`;
                                                } else if (data.message) {
                                                    displayMessage(localization.translate('Login'), localization.translate('Login_Failed'), [data.message]);
                                                } else {
                                                    displayMessage(localization.translate('Login'), localization.translate('Unexpected_Error_Occurred'));
                                                }
                                            }
                                        });
                                    }
                                }, {
                                    Text: localization.translate('Close')
                                }]
                            });
                        } else {
                            window.location = `/Account`;
                        }
                    } else if (data.message) {
                        displayMessage(localization.translate('Login'), localization.translate('Login_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('Login'), localization.translate('Login_Invalid_Details'));
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('Login'), localization.translate('Login_Failed'), [error]);
                });
        });

        $(document).off('submit', '#frmRegisterAccount').on('submit', '#frmRegisterAccount', function (e) {
            preventDefaults(e);

            var token = $('#frmRegisterAccount input[name=\'__RequestVerificationToken\']').val();

            var username = $('#frmRegisterAccount input.input-username').val();
            if (username === undefined || username.length < 5 || username.length > 50) {
                displayMessage(localization.translate('Registration'), localization.translate('Registration_Invalid_Username'));
                return;
            }

            var email = $('#frmRegisterAccount input.input-email').val();
            if (email === undefined || email.length === 0 || email.length > 100 || email.indexOf('@') === -1 || email.indexOf('.') === -1) {
                displayMessage(localization.translate('Registration'), localization.translate('Registration_Invalid_Email'));
                return;
            }

            var password = $('#frmRegisterAccount input.input-password').val();
            if (password === undefined || password.length < 8 || password.length > 100) {
                displayMessage(localization.translate('Registration'), localization.translate('Registration_Invalid_Password'));
                return;
            }

            var cpassword = $('#frmRegisterAccount input.input-cpassword').val();
            if (cpassword === undefined || cpassword.length === 0 || cpassword !== password) {
                displayMessage(localization.translate('Registration'), localization.translate('Registration_Invalid_CPassword'));
                return;
            }

            displayLoader(localization.translate('Loading'));

            $.ajax({
                url: '/Account/Register',
                method: 'POST',
                data: { __RequestVerificationToken: token, Username: username, EmailAddress: email, Password: password, ConfirmPassword: cpassword }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        if (data.validation === true) {
                            displayMessage(localization.translate('Registration'), localization.translate('Registration_Success_Validation'), null, function () {
                                window.location = `/Account`;
                            });
                        } else {
                            displayMessage(localization.translate('Registration'), localization.translate('Registration_Success'), null, function () {
                                displayLoader(localization.translate('Loading'));
                                $.ajax({
                                    url: '/Account/Login',
                                    method: 'POST',
                                    data: { __RequestVerificationToken: token, Username: username, Password: password }
                                })
                                    .done(data => {
                                        hideLoader();

                                        if (data.success === true) {
                                            window.location = `/Account`;
                                        } else if (data.message) {
                                            displayMessage(localization.translate('Registration'), localization.translate('Login_Failed'), [data.message]);
                                        } else {
                                            displayMessage(localization.translate('Registration'), localization.translate('Login_Invalid_Details'));
                                        }
                                    })
                                    .fail((xhr, error) => {
                                        hideLoader();
                                        displayMessage(localization.translate('Registration'), localization.translate('Login_Failed'), [error]);
                                    });
                            });
                        }
                    } else if (data.message) {
                        displayMessage(localization.translate('Registration'), localization.translate('Registration_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('Registration'), localization.translate('Unexpected_Error_Occurred'));
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('Registration'), localization.translate('Registration_Failed'), [error]);
                });
        });

        $(document).off('submit', '#frmForgotPassword').on('submit', '#frmForgotPassword', function (e) {
            preventDefaults(e);

            var token = $('#frmForgotPassword input[name=\'__RequestVerificationToken\']').val();

            var email = $('#frmForgotPassword input.input-email').val();
            if (email === undefined || email.length === 0 || email.length > 100 || email.indexOf('@') === -1 || email.indexOf('.') === -1) {
                displayMessage(localization.translate('Registration'), localization.translate('Registration_Invalid_Email'));
                return;
            }

            displayLoader(localization.translate('Loading'));

            $.ajax({
                url: '/Account/ForgotPassword',
                method: 'POST',
                data: { __RequestVerificationToken: token, emailAddress: email }
            })
                .done(data => {
                    hideLoader();

                    if (data.message) {
                        displayMessage(localization.translate('ForgotPassword'), localization.translate('ForgotPassword_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('ForgotPassword'), localization.translate('ForgotPassword_Sent'), null, function () {
                            window.location = `/Account/Login`;
                        });
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('ForgotPassword'), localization.translate('ForgotPassword_Failed'), [error]);
                });
        });

        $(document).off('submit', '#frmResetPassword').on('submit', '#frmResetPassword', function (e) {
            preventDefaults(e);

            var token = $('#frmResetPassword input[name=\'__RequestVerificationToken\']').val();

            var data = $('#frmResetPassword input.input-data').val();
            if (data === undefined || data.length === 0) {
                displayMessage(localization.translate('PasswordReset'), localization.translate('Unexpected_Error_Occurred'));
                return;
            }

            var password = $('#frmResetPassword input.input-password').val();
            if (password === undefined || password.length < 8 || password.length > 100) {
                displayMessage(localization.translate('PasswordReset'), localization.translate('Registration_Invalid_Password'));
                return;
            }

            var cpassword = $('#frmResetPassword input.input-cpassword').val();
            if (cpassword === undefined || cpassword.length === 0 || cpassword !== password) {
                displayMessage(localization.translate('PasswordReset'), localization.translate('Registration_Invalid_CPassword'));
                return;
            }

            displayLoader(localization.translate('Loading'));

            $.ajax({
                url: '/Account/ResetPassword',
                method: 'POST',
                data: { __RequestVerificationToken: token, Data: data, Password: password, ConfirmPassword: cpassword }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true && data.username) {
                        displayMessage(localization.translate('PasswordReset'), localization.translate('PasswordReset_Success'), null, function () {
                            if (data.mfa) {
                                window.location = `/Account/Login`;
                            } else {
                                displayLoader(localization.translate('Loading'));
                                $.ajax({
                                    url: '/Account/Login',
                                    method: 'POST',
                                    data: { __RequestVerificationToken: token, Username: data.username, Password: password }
                                })
                                    .done(data => {
                                        hideLoader();

                                        if (data.success === true) {
                                            window.location = `/Account`;
                                        } else if (data.message) {
                                            displayMessage(localization.translate('PasswordReset'), localization.translate('Login_Failed'), [data.message]);
                                        } else {
                                            displayMessage(localization.translate('PasswordReset'), localization.translate('Login_Invalid_Details'));
                                        }
                                    })
                                    .fail((xhr, error) => {
                                        hideLoader();
                                        displayMessage(localization.translate('PasswordReset'), localization.translate('Login_Failed'), [error]);
                                    });
                            }
                        });
                    } else if (data.message) {
                        displayMessage(localization.translate('PasswordReset'), localization.translate('PasswordReset_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('PasswordReset'), localization.translate('Unexpected_Error_Occurred'));
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('PasswordReset'), localization.translate('PasswordReset_Failed'), [error]);
                });
        });

        $(document).off('click', 'button.btnDismissPopup').on('click', 'button.btnDismissPopup', function (e) {
            preventDefaults(e);
            hidePopup($(this).closest('.modal').attr('id'));
        });

        $(document).off('click', 'i#btn-show-sponsors').on('click', 'i#btn-show-sponsors', function (e) {
            preventDefaults(e);

            displayLoader(localization.translate('Loading'));

            $.ajax({
                type: "GET",
                url: '/Sponsors',
                success: function (data) {
                    hideLoader();
                    displayPopup({
                        Title: localization.translate('Sponsors'),
                        CustomHtml: data,
                        Buttons: [{
                            Text: localization.translate('Sponsor'),
                            Class: 'btn-success',
                            Callback: function () {
                                displayPopup({
                                    Title: localization.translate('Sponsors'),
                                    CustomHtml: `<div class="text-center mb-5">
	                                        <section class="my-4">
		                                        <a href="https://github.com/sponsors/Cirx08" class="sponsor-card">
			                                        <img src="/images/github_avatar.png" class="sponsor-card-logo" alt="GitHub Sponsors Link" />
			                                        <p class="sponsor-card-name">GitHub Sponsors</p>
		                                        </a>
                                                <br/>
		                                        <a href="https://buymeacoffee.com/cirx08" class="sponsor-card">
			                                        <img src="/images/buymeacoffee_avatar.png" class="sponsor-card-logo" alt="BuyMeACoffee Sponsor Link" />
			                                        <p class="sponsor-card-name">BuyMeACoffee</p>
		                                        </a>
	                                        </section>
                                        </div>`,
                                    Buttons: [{
                                        Text: localization.translate('Close')
                                    }]
                                });
                            }
                        }, {
                            Text: localization.translate('Close')
                        }]
                    });
                },
                error: function () {
                    hideLoader();
                }
            });
        });

        $(document).off('click', '#cookieConsent button.accept-policy').on('click', '#cookieConsent button.accept-policy', function (e) {
            preventDefaults(e);

            document.cookie = $(this).data('cookie-string');
            $('#cookieConsentWrapper').remove();

            $.ajax({
                url: '/Home/LogCookieApproval',
                method: 'POST'
            });
        });

    });
})();