(function($) {

    $('#convert-button').on('click',
        function() {
            console.log('test');
            $.ajax({
                    url: 'convert',
                    data : JSON.stringify({ curl: $('#curl').val() }),
                    contentType : 'application/json',
                    type : 'POST',
                    success: function (data) {
                        $('#csharp').text(data).removeClass('d-none');

                        $('pre').each(function (i, block) {
                            hljs.highlightBlock(block);
                        });
                    }
                });
        });

}(window.jQuery));
