(function($) {

    $('#convert-button').on('click',
        function() {
            console.log('test');
            $.ajax({
                    url: 'convert',
                    data : JSON.stringify({ curl: $('#curl').val() }),
                    contentType : 'application/json',
                    type : 'POST',
                    success: function (response) {
                        console.log(response);
                        $('#csharp').text(response.data).collapse('show');
                        $('#errors').collapse('hide');

                        if (response.warnings.length > 0) {
                            $('#warnings').text(response.warnings.join('\n')).collapse('show');
                        } else {
                            $('#warnings').collapse('hide');
                        }

                        $('pre').each(function (i, block) {
                            hljs.highlightBlock(block);
                        });
                    },
                    error: function(response) {
                        console.log(response);
                        $('#csharp').collapse('hide');
                        $('#errors').text(response.responseJSON.errors.join('\n')).collapse('show');
                    }
                });
        });

}(window.jQuery));
