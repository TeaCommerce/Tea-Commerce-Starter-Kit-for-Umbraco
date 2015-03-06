jQuery(function() {
  var variants = jQuery('#variants');

  if (variants[0]) {
    variants.change(function() {
      var selectedVariant = jQuery(this).val();

      jQuery('[data-variantid]').addClass('hidden');
      jQuery('[data-variantid=' + selectedVariant + ']').removeClass('hidden');
    });
  }
});