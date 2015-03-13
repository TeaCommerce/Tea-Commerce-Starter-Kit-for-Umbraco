/**********************************
  * CART
***********************************/
jQuery(function () {
  //Get the carts information step
  var cartContainerInformation = jQuery('.cartContainer.information');
  if (cartContainerInformation[0]) {

    //Country regions
    var countryRegionWrappers = cartContainerInformation.find('.countryRegionWrapper');

    //Each country region wrapper is removed from the DOM
    countryRegionWrappers.each(function () {
      var countryRegionWrapper = jQuery(this),
          id = countryRegionWrapper.find('select').attr('id');
      countryRegionWrapper.after('<div id="' + id + '"></div>').remove();
    });

    //When a country is selected a country region select field is shown if necessary
    cartContainerInformation.on('change', '.countrySelect', function() {
      var countrySelect = jQuery(this),
          countrySelectWrap = countrySelect.closest('.form-group').parent(),
          selector = countrySelect.children(':checked').attr('data-country-region-selector'),
          placeholder = jQuery(selector),
          countryRegionWrapper = null;

      //Find a country region select if one is available
      for (var i = 0; i < countryRegionWrappers.length; i++) {
        if (countryRegionWrappers.eq(i).find('select').attr('id') === placeholder.attr('id')) {
          countryRegionWrapper = countryRegionWrappers.eq(i);
          break;
        }
      }

      //remove all shown country region selects and show the correct one
      countrySelectWrap.children('.countryRegionWrapper').remove();
      if (countryRegionWrapper) {
        placeholder.after(countryRegionWrapper);
      }
    });

    //Trigger change on country selects to show first country regions
    cartContainerInformation.find('.countrySelect').change();
  }

  //Accept conditions check
  jQuery('body').on('click', '.cartContainer.accept button[type=submit]', function() {
    if (!jQuery('#acceptConditions').is(':checked')) {
      alert('Please accept our terms and conditions');
      return false;
    }
  });
});