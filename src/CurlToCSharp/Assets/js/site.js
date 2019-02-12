(function($, prism) {

    $('#convert-button').on('click',
        function() {
            convert($('#curl').val());
        });

    $('#examples').on('click',
        'button[data-curl]',
        function(e) {
            var curlCommand = $(e.target).data('curl');
            $('#curl').val(curlCommand);
            convert(curlCommand);
        });

    function convert(curlCommand) {
        if (!curlCommand) {
            return;
        }

        blockUi();

        $.ajax({
            url: 'convert',
            data : JSON.stringify({ curl: curlCommand }),
            contentType : 'application/json',
            type : 'POST',
            success: function (response) {
                $('#csharp').text(response.data).parent().collapse('show');
                $('#errors').collapse('hide');

                showWarningsIfNeeded(response.warnings);

                prism.highlightAll();
            },
            error: function(response) {
                var errors = response.responseJSON && response.responseJSON.errors || [];

                $('#csharp').parent().collapse('hide');
                $('#warnings').collapse('hide');

                if (errors.length > 0) {
                    $('#errors').text(errors.join('\n')).collapse('show');
                }
            },
            complete: function() {
                unblockUi();
            }
        });
    }

    function showWarningsIfNeeded(warnings) {
        var $warnings = $('#warnings');
        if (warnings.length > 0) {
            $warnings.text(warnings.join('\n')).collapse('show');
        } else {
            $warnings.collapse('hide');
        }
    }

    function blockUi() {
        $('button').attr('disabled', 'disabled');
    }

    function unblockUi() {
        $('button').removeAttr('disabled');
    }

}(window.jQuery, window.Prism));
