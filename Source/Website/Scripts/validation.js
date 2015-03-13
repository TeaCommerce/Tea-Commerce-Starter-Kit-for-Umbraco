jQuery(function() {
  var formsToValidate = jQuery('.form-autoValidate');
  //Sets error class if input is not valid
  formsToValidate.each(function () {
    jQuery(this).validate({
      ignore: 'input[type=text]:hidden', //Ignore hidden fields
      errorClass: 'has-error', //Our error class matching the twitter bootstrap classes
      validClass: 'has-success', //Our success class matching the twitter bootstrap classes
      rules: {},
      errorPlacement: function () { }, //Make sure no error messages is written in the UI
      highlight: function (element, errorClass, validClass) {
        //Set classes on highlight on twitter bootstrap elements
        jQuery(element).closest('.control-group, .form-group').addClass(errorClass).removeClass(validClass);
      },
      unhighlight: function (element, errorClass, validClass) {
        //Set classes on unhighlight on twitter bootstrap elements
        jQuery(element).closest('.control-group, .form-group').removeClass(errorClass).addClass(validClass);
      },
    });
  });
});