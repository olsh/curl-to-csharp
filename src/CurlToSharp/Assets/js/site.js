(function($) {

    $('#convert-button').on('click',
        function() {
            console.log('test');
            $.ajax({
                    url: 'convert',
                    data : JSON.stringify({ curl: $('#curl').val() }),
                    contentType : 'application/json',
                    type : 'POST'
                });
        });

}(window.jQuery));
