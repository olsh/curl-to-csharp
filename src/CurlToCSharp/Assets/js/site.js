(function($) {

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

        $.ajax({
            url: 'convert',
            data : JSON.stringify({ curl: curlCommand }),
            contentType : 'application/json',
            type : 'POST',
            success: function (response) {
                $('#csharp').text(response.data).collapse('show');
                $('#errors').collapse('hide');

                showWarningsIfNeeded(response.warnings);

                $('pre').each(function (i, block) {
                    hljs.highlightBlock(block);
                });
            },
            error: function(response) {
                $('#csharp').collapse('hide');
                $('#warnings').collapse('hide');
                $('#errors').text(response.responseJSON.errors.join('\n')).collapse('show');
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

}(window.jQuery));
