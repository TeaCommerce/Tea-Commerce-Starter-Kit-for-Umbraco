jQuery(function () {
  var cartContainerInformation = jQuery('.cartContainer.information');
  if (cartContainerInformation[0]) {
    var countryRegionWrappers = cartContainerInformation.find('.countryRegionWrapper');

    countryRegionWrappers.each(function () {
      var countryRegionWrapper = jQuery(this),
          id = countryRegionWrapper.find('select').attr('id');
      countryRegionWrapper.after('<div id="' + id + '"></div>').remove();
    });


    cartContainerInformation.on('change', '.countrySelect', function() {
      var countrySelect = jQuery(this),
          countrySelectWrap = countrySelect.closest('.form-group').parent(),
          selector = countrySelect.children(':checked').attr('data-country-region-selector'),
          placeholder = jQuery(selector),
          countryRegionWrapper = null;

      for (var i = 0; i < countryRegionWrappers.length; i++) {
        if (countryRegionWrappers.eq(i).find('select').attr('id') === placeholder.attr('id')) {
          countryRegionWrapper = countryRegionWrappers.eq(i);
          break;
        }
      }

      countrySelectWrap.children('.countryRegionWrapper').remove();
      if (countryRegionWrapper) {
        placeholder.after(countryRegionWrapper);
      }
    });

    cartContainerInformation.find('.countrySelect').change();
  }
});