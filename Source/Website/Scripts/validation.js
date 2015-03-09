jQuery(function() {
  var formsToValidate = jQuery('.form-autoValidate');
  //Sets error class if input is not valid
  formsToValidate.each(function () {
    jQuery(this).validate({
      ignore: 'input[type=text]:hidden',
      errorClass: 'has-error',
      validClass: 'has-success',
      rules: {},
      errorPlacement: function () { },
      highlight: function (element, errorClass, validClass) {
        console.log('highlight');
        jQuery(element).closest('.control-group, .form-group').addClass(errorClass).removeClass(validClass);
      },
      unhighlight: function (element, errorClass, validClass) {
        jQuery(element).closest('.control-group, .form-group').removeClass(errorClass).addClass(validClass);
      },
    });
  });
});