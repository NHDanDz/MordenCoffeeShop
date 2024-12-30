
(function ($) {
    "use strict";

    
    /*==================================================================
    [ Validate ]*/
    var input = $('.validate-input .input100');

    $('.validate-form').on('submit',function(){
        var check = true;

        for(var i=0; i<input.length; i++) {
            if(validate(input[i]) == false){
                showValidate(input[i]);
                check=false;
            }
        }

        return check;
    });


    $('.validate-form .input100').each(function(){
        $(this).focus(function(){
           hideValidate(this);
        });
    });

    function validate (input) {
        if($(input).attr('type') == 'email' || $(input).attr('name') == 'email') {
            if($(input).val().trim().match(/^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,5}|[0-9]{1,3})(\]?)$/) == null) {
                return false;
            }
        }
        else {
            if($(input).val().trim() == ''){
                return false;
            }
        }
    }

    function showValidate(input) {
        var thisAlert = $(input).parent();

        $(thisAlert).addClass('alert-validate');
    }

    function hideValidate(input) {
        var thisAlert = $(input).parent();

        $(thisAlert).removeClass('alert-validate');
    }
    $(document).ready(function () {
        // Form animations
        $('.input100').focus(function () {
            $(this).parent().addClass('focused');
        }).blur(function () {
            if (!$(this).val()) {
                $(this).parent().removeClass('focused');
            }
        });

        // Password strength checker
        $('#Password').on('input', function () {
            var password = $(this).val();
            var strength = 0;

            if (password.length >= 6) strength++;
            if (password.match(/[a-z]+/)) strength++;
            if (password.match(/[A-Z]+/)) strength++;
            if (password.match(/[0-9]+/)) strength++;
            if (password.match(/[$@#&!]+/)) strength++;

            var width = (strength / 5) * 100;
            $('.password-strength-bar').css('width', width + '%');

            if (strength < 2) {
                $('.password-strength-bar').css('background', '#dc3545');
            } else if (strength < 4) {
                $('.password-strength-bar').css('background', '#ffc107');
            } else {
                $('.password-strength-bar').css('background', '#28a745');
            }
        });

        // Form validation
        $('#registerForm').on('submit', function (e) {
            var password = $('#Password').val();
            var confirm = $('#ConfirmPassword').val();

            if (password !== confirm) {
                e.preventDefault();
                alert('Passwords do not match!');
                return false;
            }

            $(this).find('button[type="submit"]').addClass('loading');
        });

        // Tilt effect
        $('.js-tilt').tilt({
            scale: 1.1,
            glare: true,
            maxGlare: .3,
            perspective: 500
        });
    });
    

})(jQuery);